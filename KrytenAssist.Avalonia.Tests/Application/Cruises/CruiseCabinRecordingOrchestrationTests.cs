extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using RecordCabin = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseCabinObservation;
using CompositeCabin = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseCabinObservationAndEvaluateAlerts;
using EvaluateCabin = KrytenApplication::KrytenAssist.Application.Cruises.EvaluateRecordedCruiseCabinAlerts;
using GetSaved = KrytenApplication::KrytenAssist.Application.Cruises.GetSavedCruise;
using GetPreferences = KrytenApplication::KrytenAssist.Application.Cruises.GetCruisePreferences;
using Materialize = KrytenApplication::KrytenAssist.Application.Cruises.MaterializeCruiseAlertCandidates;
using CabinHistory = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRecordedHistory;
using CabinRepositoryResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRepositoryRecordResult;
using CabinRepositoryState = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRepositoryRecordState;
using CabinStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinOperationStatus;
using AlertQuery = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertQuery;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class CruiseCabinRecordingOrchestrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ChangedPreferredTransition_MaterializesCabinAndSavedCriteriaAfterCommit()
    {
        var context = CreateContext();
        var previous = Observation(CruiseCabinAvailabilityState.Unavailable, Now);
        var current = Observation(CruiseCabinAvailabilityState.Available, Now.AddHours(1));
        context.Cabins.RecordResult = Result(CabinRepositoryState.ChangedObservationRecorded, previous, current);

        var result = await context.Composite.ExecuteAsync(current, Now.AddHours(2));

        result.Recording.Status.Should().Be(CabinStatus.ChangedObservationRecorded);
        result.CabinAvailabilityAlerts!.CreatedAlerts.Should().ContainSingle(x => x.Type == CruiseAlertType.CabinAvailability);
        result.SavedCriteriaAlerts!.CreatedAlerts.Should().ContainSingle(x => x.Type == CruiseAlertType.SavedCriteria);
        result.CreatedAlertCount.Should().Be(2);
    }

    [Fact]
    public async Task FirstObservation_ReevaluatesCriteriaWithoutCabinTransitionAlert()
    {
        var context = CreateContext();
        var current = Observation(CruiseCabinAvailabilityState.Available, Now);
        context.Cabins.RecordResult = Result(CabinRepositoryState.FirstObservationRecorded, current);

        var result = await context.Composite.ExecuteAsync(current, Now.AddMinutes(1));

        result.Recording.Status.Should().Be(CabinStatus.FirstObservationRecorded);
        result.CabinAvailabilityAlerts.Should().BeNull();
        result.SavedCriteriaAlerts!.CreatedAlerts.Should().ContainSingle(x => x.Type == CruiseAlertType.SavedCriteria);
    }

    [Fact]
    public async Task AlreadyCurrent_RetriesLatestTransitionWithoutDuplicatingAlerts()
    {
        var context = CreateContext();
        var previous = Observation(CruiseCabinAvailabilityState.Unavailable, Now);
        var current = Observation(CruiseCabinAvailabilityState.Available, Now.AddHours(1));
        context.Cabins.RecordResult = Result(CabinRepositoryState.ChangedObservationRecorded, previous, current);
        await context.Composite.ExecuteAsync(current, Now.AddHours(2));
        context.Cabins.RecordResult = Result(CabinRepositoryState.AlreadyCurrent, previous, current);

        var retry = await context.Composite.ExecuteAsync(current, Now.AddHours(3));
        var alerts = await context.Alerts.ListAsync(new AlertQuery());

        retry.CreatedAlertCount.Should().Be(0);
        alerts.Should().HaveCount(2);
        retry.CabinAvailabilityAlerts!.ExistingCount.Should().Be(1);
    }

    [Fact]
    public async Task EvaluationFailure_DoesNotUndoCommittedRecording()
    {
        var context = CreateContext(new TestAlertSettingsRepository(new InvalidOperationException()));
        var previous = Observation(CruiseCabinAvailabilityState.Unavailable, Now);
        var current = Observation(CruiseCabinAvailabilityState.Available, Now.AddHours(1));
        context.Cabins.RecordResult = Result(CabinRepositoryState.ChangedObservationRecorded, previous, current);

        var result = await context.Composite.ExecuteAsync(current, Now.AddHours(2));

        result.RecordingSucceeded.Should().BeTrue();
        result.CabinAvailabilityAlerts!.Status.Should().Be(KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertOperationStatus.Failed);
        result.AlertEvaluationRetryable.Should().BeTrue();
        result.SavedCriteriaAlerts.Should().NotBeNull();
    }

    [Fact]
    public async Task IncomingObservationThatIsNotCommittedCurrent_CreatesNoFalseTransition()
    {
        var context = CreateContext();
        var incoming = Observation(CruiseCabinAvailabilityState.Unavailable, Now);
        var committed = Observation(CruiseCabinAvailabilityState.Available, Now.AddHours(1));
        context.Cabins.RecordResult = Result(CabinRepositoryState.ChangedObservationRecorded, incoming, committed);

        var result = await context.Composite.ExecuteAsync(incoming, Now.AddHours(2));

        result.CabinAvailabilityAlerts.Should().BeNull();
        (await context.Alerts.ListAsync(new AlertQuery())).Where(x => x.Type == CruiseAlertType.CabinAvailability).Should().BeEmpty();
    }

    private static TestContext CreateContext(TestAlertSettingsRepository? settings = null)
    {
        var cabins = new FakeCruiseCabinObservationRepository();
        var prices = new FakeCruiseObservationRepository { ListResult = [] };
        var saved = new FakeSavedCruiseRepository();
        saved.Items[Key()] = new SavedCruise(new SavedCruiseSnapshot(Key(), "Cruise", "Marella Cruises",
            new CruisePrice(900m, "GBP", "per person"), Now.AddDays(-1), retailSource: Source()));
        var preferences = new FakeCruisePreferencesRepository
        {
            Value = new CruisePreferences(preferredCabins: [CruiseCabinType.Inside])
        };
        var alerts = new TestAlertRepository();
        var criteria = CruiseCriteriaTestFactory.CreateForSailing(saved, preferences, prices, cabins, alerts);
        var composite = new CompositeCabin(cabins, new RecordCabin(cabins, new()),
            new GetSaved(saved), new GetPreferences(preferences),
            new EvaluateCabin(new CruiseCabinAvailabilityAlertDetector(new()), settings ?? new TestAlertSettingsRepository(), new Materialize(alerts)),
            criteria);
        return new(composite, cabins, alerts);
    }

    private static CabinRepositoryResult Result(CabinRepositoryState state, params CruiseCabinObservation[] observations) =>
        new(state, new CabinHistory(observations[0].SeriesKey, observations.Max(x => x.ObservedAt), observations));

    private static CruiseCabinObservation Observation(CruiseCabinAvailabilityState inside, DateTimeOffset time)
    {
        var states = Enum.GetValues<CruiseCabinType>().Select(type => new CruiseCabinState(type,
            type == CruiseCabinType.Inside ? inside : CruiseCabinAvailabilityState.Unknown));
        return new(Key(), Source(), new CruiseCabinSearchContext(2, 0, [], true,
            CruiseCabinPackageMode.FlyCruise, "STN", 1), CruiseCabinEvidenceCoverage.Partial,
            states, time, $"evidence-{inside}-{time:HHmm}", "https://www.tui.co.uk/cruise/bookitineraries/test?itineraryCodeOne=1");
    }

    private static CruiseSailingKey Key() => new("marella", "Marella Voyager", new DateOnly(2026, 10, 2), 7);
    private static CruiseSource Source() => new("tui", "TUI");
    private sealed record TestContext(CompositeCabin Composite, FakeCruiseCabinObservationRepository Cabins, TestAlertRepository Alerts);
}
