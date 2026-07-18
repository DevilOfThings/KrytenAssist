extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using AlertOperationStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertOperationStatus;
using AlertQuery = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertQuery;
using EvaluateCriteria = KrytenApplication::KrytenAssist.Application.Cruises.EvaluateSavedCruiseCriteriaAlerts;
using Materialize = KrytenApplication::KrytenAssist.Application.Cruises.MaterializeCruiseAlertCandidates;
using MutationStatus = KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseMutationStatus;
using PreferenceMutationStatus = KrytenApplication::KrytenAssist.Application.Cruises.PersonalCruisePreferenceMutationStatus;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class SavedCruiseCriteriaOrchestrationTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 18, 15, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public async Task SaveWithoutHistory_UsesSavedSnapshotAndPreservesPrimaryResult()
    {
        var saved = new FakeSavedCruiseRepository();
        var preferences = new FakeCruisePreferencesRepository
        {
            Value = new CruisePreferences([8])
        };
        var observations = new FakeCruiseObservationRepository();
        var alerts = new TestAlertRepository();
        var useCase = CruiseCriteriaTestFactory.CreateSave(
            saved,
            preferences,
            observations,
            alerts);
        var snapshot = Snapshot("Voyager", new DateOnly(2027, 8, 2));

        var result = await useCase.ExecuteAsync(snapshot, Now);

        result.Mutation.Status.Should().Be(MutationStatus.Created);
        result.SavedCriteriaAlerts!.CreatedAlerts.Should().ContainSingle();
        var details = result.SavedCriteriaAlerts.CreatedAlerts[0].Details
            .Should().BeOfType<CruiseSavedCriteriaAlertDetails>().Subject;
        details.EvidenceOrigin.Should().Be(CruiseAlertEvidenceOrigin.SavedSnapshot);
        observations.ListCalls.Should().Be(1);
    }

    [Fact]
    public async Task SaveFailure_DoesNotAttemptCriteriaEvaluation()
    {
        var saved = new FakeSavedCruiseRepository { Exception = new InvalidOperationException() };
        var preferences = new FakeCruisePreferencesRepository();
        var observations = new FakeCruiseObservationRepository();
        var result = await CruiseCriteriaTestFactory.CreateSave(saved, preferences, observations)
            .ExecuteAsync(Snapshot("Voyager", new DateOnly(2027, 8, 2)), Now);

        result.Mutation.Status.Should().Be(MutationStatus.Failed);
        result.SavedCriteriaAlerts.Should().BeNull();
        preferences.GetCalls.Should().Be(0);
        observations.ListCalls.Should().Be(0);
    }

    [Fact]
    public async Task Restore_EvaluatesShortlistedAggregateWithoutChangingRestoreSuccess()
    {
        var saved = new FakeSavedCruiseRepository();
        var snapshot = Snapshot("Voyager", new DateOnly(2027, 8, 2));
        saved.Items[snapshot.SailingKey] = new SavedCruise(
            snapshot,
            SavedCruiseStatus.Dismissed);
        var preferences = new FakeCruisePreferencesRepository
        {
            Value = new CruisePreferences([8])
        };
        var observations = new FakeCruiseObservationRepository();

        var result = await CruiseCriteriaTestFactory.CreateRestore(
                saved,
                preferences,
                observations)
            .ExecuteAsync(snapshot.SailingKey, Now);

        result.Mutation.Status.Should().Be(MutationStatus.Restored);
        result.Mutation.SavedCruise!.Status.Should().Be(SavedCruiseStatus.Shortlisted);
        result.SavedCriteriaAlerts!.CreatedAlerts.Should().ContainSingle();
    }

    [Fact]
    public async Task SavePreferences_EvaluatesOnlyShortlistedInDeterministicOrder()
    {
        var saved = new FakeSavedCruiseRepository();
        var later = new SavedCruise(Snapshot("Later", new DateOnly(2027, 8, 3)));
        var earlier = new SavedCruise(Snapshot("Earlier", new DateOnly(2027, 8, 1)));
        var dismissed = new SavedCruise(
            Snapshot("Dismissed", new DateOnly(2027, 8, 2)),
            SavedCruiseStatus.Dismissed);
        saved.Items[later.SailingKey] = later;
        saved.Items[dismissed.SailingKey] = dismissed;
        saved.Items[earlier.SailingKey] = earlier;
        var preferences = new FakeCruisePreferencesRepository();
        var observations = new FakeCruiseObservationRepository();

        var result = await CruiseCriteriaTestFactory.CreateSavePreferences(
                saved,
                preferences,
                observations)
            .ExecuteAsync(new CruisePreferences([8]), Now);

        result.Mutation.Status.Should().Be(PreferenceMutationStatus.Updated);
        result.SavedCriteriaAlerts!.EligibleCount.Should().Be(2);
        result.SavedCriteriaAlerts.Completed.Select(item => item.SailingKey)
            .Should().ContainInOrder(earlier.SailingKey, later.SailingKey);
        result.SavedCriteriaAlerts.CreatedAlertCount.Should().Be(2);
        observations.ListCalls.Should().Be(1);
    }

    [Fact]
    public async Task MaterializationFailure_DoesNotPersistMetAndRetryCreatesExactlyOneAlert()
    {
        var alerts = new TestAlertRepository { ThrowOnNextAdd = true };
        var states = new TestCriteriaStateRepository();
        var evaluator = new EvaluateCriteria(
            new SavedCruiseCriteriaAlertDetector(),
            new TestAlertSettingsRepository(),
            states,
            new Materialize(alerts));
        var saved = new SavedCruise(Snapshot("Voyager", new DateOnly(2027, 8, 2)));
        var preferences = new CruisePreferences([8]);
        var evidence = new CruiseCriteriaEvidence(
            CruiseAlertEvidenceOrigin.SavedSnapshot,
            "snapshot-evidence",
            Now,
            [saved.Snapshot.DisplayedPrice]);

        var failed = await evaluator.ExecuteAsync(saved, preferences, evidence, Now);
        var retried = await evaluator.ExecuteAsync(saved, preferences, evidence, Now.AddMinutes(1));
        var duplicate = await evaluator.ExecuteAsync(saved, preferences, evidence, Now.AddMinutes(2));

        failed.Status.Should().Be(AlertOperationStatus.Failed);
        states.UpsertCalls.Should().Be(2);
        retried.CreatedAlerts.Should().ContainSingle();
        duplicate.CreatedAlerts.Should().BeEmpty();
        (await alerts.ListAsync(new AlertQuery())).Should().ContainSingle();
    }

    private static SavedCruiseSnapshot Snapshot(string ship, DateOnly departure) =>
        new(
            new CruiseSailingKey("marella", ship, departure, 7),
            $"{ship} sailing",
            "Marella Cruises",
            new CruisePrice(999m, "GBP", "per person"),
            Now);
}
