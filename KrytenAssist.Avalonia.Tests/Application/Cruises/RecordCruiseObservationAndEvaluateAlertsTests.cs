extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using AlertOperationStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertOperationStatus;
using AlertQuery = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertQuery;
using RecordStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRecordStatus;
using RepositoryResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordResult;
using RepositoryState = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordState;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class RecordCruiseObservationAndEvaluateAlertsTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 7, 18, 16, 30, 0, TimeSpan.FromHours(1));

    [Theory]
    [InlineData(RepositoryState.FirstObservationRecorded)]
    [InlineData(RepositoryState.AlreadyCurrent)]
    public async Task FirstAndAlreadyCurrent_DoNotLoadHistoryOrEvaluateChanges(
        RepositoryState state)
    {
        var observation = Observation(988m, CreatedAt.AddDays(-1));
        var repository = new FakeCruiseObservationRepository
        {
            RecordResult = Result(state, observation)
        };
        var alerts = new TestAlertRepository();
        var useCase = CruiseAlertApplicationTestFactory.CreateRecorder(
            repository,
            new CruisePriceHistoryAnalyzer(),
            alerts);

        var result = await useCase.ExecuteAsync(observation, CreatedAt);

        result.Recording.Status.Should().Be(state == RepositoryState.FirstObservationRecorded
            ? RecordStatus.FirstObservationRecorded
            : RecordStatus.AlreadyCurrent);
        result.Alerts.Should().BeNull();
        repository.GetCalls.Should().Be(0);
        (await alerts.ListAsync(new AlertQuery())).Should().BeEmpty();
    }

    [Fact]
    public async Task ChangedCurrentObservation_EvaluatesCommittedPreviousAndCreatesAlertAtCallerTime()
    {
        var previous = Observation(988m, CreatedAt.AddDays(-2));
        var current = Observation(949m, CreatedAt.AddDays(-1));
        var history = CruiseHistoryApplicationTestData.History(previous, current);
        var repository = new FakeCruiseObservationRepository
        {
            RecordResult = new RepositoryResult(
                RepositoryState.ChangedObservationRecorded,
                history)
        };
        var alerts = new TestAlertRepository();
        var useCase = CruiseAlertApplicationTestFactory.CreateRecorder(
            repository,
            new CruisePriceHistoryAnalyzer(),
            alerts);

        var result = await useCase.ExecuteAsync(current, CreatedAt);

        result.Recording.Status.Should().Be(RecordStatus.ChangedObservationRecorded);
        result.Alerts!.Status.Should().Be(AlertOperationStatus.Success);
        result.Alerts.CreatedAlerts.Should().ContainSingle();
        result.Alerts.CreatedAlerts[0].Type.Should().Be(CruiseAlertType.PriceDrop);
        result.Alerts.CreatedAlerts[0].CreatedAt.Should().Be(CreatedAt);
        repository.RequestedKey.Should().Be(CruiseSailingKey.From(current));
        repository.RequestedSource.Should().Be(current.Source);
    }

    [Fact]
    public async Task ChangedOlderHistoricalInsertion_DoesNotEvaluateAsCurrentEvidence()
    {
        var insertedOlder = Observation(900m, CreatedAt.AddDays(-3), "Older promotion");
        var current = Observation(988m, CreatedAt.AddDays(-1), "Current promotion");
        var repository = new FakeCruiseObservationRepository
        {
            RecordResult = new RepositoryResult(
                RepositoryState.ChangedObservationRecorded,
                CruiseHistoryApplicationTestData.History(insertedOlder, current))
        };
        var alerts = new TestAlertRepository();
        var useCase = CruiseAlertApplicationTestFactory.CreateRecorder(
            repository,
            new CruisePriceHistoryAnalyzer(),
            alerts);

        var result = await useCase.ExecuteAsync(insertedOlder, CreatedAt);

        result.Recording.Status.Should().Be(RecordStatus.ChangedObservationRecorded);
        result.Alerts!.Status.Should().Be(AlertOperationStatus.Success);
        result.Alerts.CandidateCount.Should().Be(0);
        (await alerts.ListAsync(new AlertQuery())).Should().BeEmpty();
    }

    [Fact]
    public async Task HistoryFailureAfterCommit_PreservesRecordingAndReportsAlertFailure()
    {
        var previous = Observation(988m, CreatedAt.AddDays(-2));
        var current = Observation(949m, CreatedAt.AddDays(-1));
        var repository = new FakeCruiseObservationRepository
        {
            RecordResult = new RepositoryResult(
                RepositoryState.ChangedObservationRecorded,
                CruiseHistoryApplicationTestData.History(previous, current)),
            GetException = new InvalidOperationException("private database detail")
        };
        var useCase = CruiseAlertApplicationTestFactory.CreateRecorder(
            repository,
            new CruisePriceHistoryAnalyzer());

        var result = await useCase.ExecuteAsync(current, CreatedAt);

        result.RecordingSucceeded.Should().BeTrue();
        result.Recording.Status.Should().Be(RecordStatus.ChangedObservationRecorded);
        result.Alerts!.Status.Should().Be(AlertOperationStatus.Failed);
    }

    [Fact]
    public async Task CancellationAfterCommit_PreservesRecordingAndReportsAlertCancellation()
    {
        var previous = Observation(988m, CreatedAt.AddDays(-2));
        var current = Observation(949m, CreatedAt.AddDays(-1));
        var history = CruiseHistoryApplicationTestData.History(previous, current);
        using var cancellation = new CancellationTokenSource();
        var repository = new FakeCruiseObservationRepository
        {
            RecordHandler = (_, _, _, _) =>
            {
                cancellation.Cancel();
                return Task.FromResult(new RepositoryResult(
                    RepositoryState.ChangedObservationRecorded,
                    history));
            }
        };
        var useCase = CruiseAlertApplicationTestFactory.CreateRecorder(
            repository,
            new CruisePriceHistoryAnalyzer());

        var result = await useCase.ExecuteAsync(current, CreatedAt, cancellation.Token);

        result.RecordingSucceeded.Should().BeTrue();
        result.Alerts!.Status.Should().Be(AlertOperationStatus.Cancelled);
    }

    [Fact]
    public async Task RecordingFailure_DoesNotAttemptHistoryOrAlerts()
    {
        var observation = Observation(988m, CreatedAt.AddDays(-1));
        var repository = new FakeCruiseObservationRepository
        {
            RecordException = new InvalidOperationException("private failure")
        };
        var useCase = CruiseAlertApplicationTestFactory.CreateRecorder(
            repository,
            new CruisePriceHistoryAnalyzer());

        var result = await useCase.ExecuteAsync(observation, CreatedAt);

        result.Recording.Status.Should().Be(RecordStatus.Failed);
        result.Alerts.Should().BeNull();
        repository.GetCalls.Should().Be(0);
    }

    [Fact]
    public async Task FirstObservation_ForSavedMatchingSailingCreatesOnlyCriteriaAlert()
    {
        var observation = Observation(949m, CreatedAt.AddDays(-1));
        var repository = new FakeCruiseObservationRepository
        {
            RecordResult = Result(RepositoryState.FirstObservationRecorded, observation)
        };
        var saved = new FakeSavedCruiseRepository();
        var snapshot = new SavedCruiseSnapshot(
            CruiseSailingKey.From(observation),
            observation.Snapshot.Offer.Title,
            observation.Snapshot.Offer.Provider.Name,
            observation.Snapshot.Prices[0],
            CreatedAt.AddDays(-2));
        saved.Items[snapshot.SailingKey] = new SavedCruise(snapshot);
        var preferences = new FakeCruisePreferencesRepository
        {
            Value = new CruisePreferences([snapshot.SailingKey.DepartureDate.Month])
        };
        var alerts = new TestAlertRepository();
        var useCase = CruiseAlertApplicationTestFactory.CreateRecorder(
            repository,
            new CruisePriceHistoryAnalyzer(),
            alerts,
            saved: saved,
            preferences: preferences);

        var result = await useCase.ExecuteAsync(observation, CreatedAt);

        result.ObservationAlerts.Should().BeNull();
        result.SavedCriteriaAlerts!.CreatedAlerts.Should().ContainSingle();
        result.SavedCriteriaAlerts.CreatedAlerts[0].Type.Should().Be(CruiseAlertType.SavedCriteria);
        result.CreatedAlertCount.Should().Be(1);
    }

    [Fact]
    public async Task ChangedObservation_CanCreateObservationAndCriteriaAlertsIndependently()
    {
        var previous = Observation(988m, CreatedAt.AddDays(-2));
        var current = Observation(949m, CreatedAt.AddDays(-1));
        var repository = new FakeCruiseObservationRepository
        {
            RecordResult = new RepositoryResult(
                RepositoryState.ChangedObservationRecorded,
                CruiseHistoryApplicationTestData.History(previous, current))
        };
        var saved = new FakeSavedCruiseRepository();
        var snapshot = new SavedCruiseSnapshot(
            CruiseSailingKey.From(current),
            current.Snapshot.Offer.Title,
            current.Snapshot.Offer.Provider.Name,
            current.Snapshot.Prices[0],
            CreatedAt.AddDays(-3));
        saved.Items[snapshot.SailingKey] = new SavedCruise(snapshot);
        var preferences = new FakeCruisePreferencesRepository
        {
            Value = new CruisePreferences([snapshot.SailingKey.DepartureDate.Month])
        };
        var alerts = new TestAlertRepository();
        var useCase = CruiseAlertApplicationTestFactory.CreateRecorder(
            repository,
            new CruisePriceHistoryAnalyzer(),
            alerts,
            saved: saved,
            preferences: preferences);

        var result = await useCase.ExecuteAsync(current, CreatedAt);

        result.ObservationAlerts!.CreatedAlerts.Should().ContainSingle();
        result.SavedCriteriaAlerts!.CreatedAlerts.Should().ContainSingle();
        result.CreatedAlertCount.Should().Be(2);
        result.AnyAlertEvaluationFailed.Should().BeFalse();
    }

    private static CruiseObservation Observation(
        decimal price,
        DateTimeOffset observedAt,
        string promotion = "Promotion") =>
        CruiseHistoryApplicationTestData.Observation(
            price,
            observedAt,
            promotion: promotion,
            source: new CruiseSource("tui", "TUI"));

    private static RepositoryResult Result(
        RepositoryState state,
        CruiseObservation observation) =>
        new(state, CruiseHistoryApplicationTestData.History(observation));
}
