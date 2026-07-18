extern alias KrytenApplication;

using System.ComponentModel;
using KrytenAssist.Avalonia.Cruises.Discovery;
using KrytenAssist.Avalonia.Tests.Application.Cruises;
using KrytenAssist.Avalonia.Tools;
using KrytenAssist.Avalonia.ViewModels;
using KrytenAssist.Core.Cruises;
using BatchResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureBatchResult;
using BatchService = KrytenApplication::KrytenAssist.Application.Cruises.ICruisePageBatchCaptureService;
using CandidateResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureCandidateResult;
using CaptureRequest = KrytenApplication::KrytenAssist.Application.Cruises.CruisePageCaptureRequest;
using GetHistory = KrytenApplication::KrytenAssist.Application.Cruises.GetCruiseHistory;
using ListHistories = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseHistories;
using RepositoryResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordResult;
using RepositoryState = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordState;

namespace KrytenAssist.Avalonia.Tests.ViewModels;

public sealed class CruiseBatchObservationRecordingViewModelTests
{
    private const string Payload = "{\"version\":1,\"candidates\":[]}";
    private static readonly DateTimeOffset ObservedAt =
        new(2026, 7, 18, 14, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public async Task RecordSelected_ProcessesOnlyStableSelectedReadySnapshotInOrder()
    {
        var first = Observation(1);
        var second = Observation(2);
        var third = Observation(3);
        var (viewModel, repository) = await CreateReviewedViewModel([
            Ready(first),
            CandidateResult.Incomplete("Incomplete", Reference(9), "Missing.", ["prices"]),
            Ready(second),
            Ready(third)
        ]);
        repository.RecordHandler = (key, _, observation, _) => Task.FromResult(
            Result(RepositoryState.FirstObservationRecorded, observation));
        repository.ListResult = [History(first), History(third)];
        viewModel.CapturedCandidates[0].IsSelected = true;
        viewModel.CapturedCandidates[3].IsSelected = true;

        viewModel.RecordSelectedCommand.Execute(null);
        await WaitUntilAsync(viewModel, () => !viewModel.IsBatchRecording);

        Assert.Equal(2, repository.RecordCalls);
        Assert.Equal(CruiseBatchRecordingStatus.FirstObservationRecorded,
            viewModel.CapturedCandidates[0].RecordingStatus);
        Assert.Equal(CruiseBatchRecordingStatus.NotAttempted,
            viewModel.CapturedCandidates[2].RecordingStatus);
        Assert.Equal(CruiseBatchRecordingStatus.FirstObservationRecorded,
            viewModel.CapturedCandidates[3].RecordingStatus);
        Assert.Equal(1, repository.ListCalls);
        Assert.Contains("2 observations checked", viewModel.BatchRecordingSummary);
    }

    [Fact]
    public async Task RecordAll_PreservesIndependentOutcomesAndContinuesAfterFailure()
    {
        var first = Observation(1);
        var failed = Observation(2);
        var changed = Observation(3);
        var (viewModel, repository) = await CreateReviewedViewModel([
            Ready(first), Ready(failed), Ready(changed)
        ]);
        repository.RecordHandler = (_, _, observation, _) => repository.RecordCalls switch
        {
            1 => Task.FromResult(Result(RepositoryState.FirstObservationRecorded, observation)),
            2 => Task.FromException<RepositoryResult>(new InvalidOperationException("private failure")),
            _ => Task.FromResult(Result(RepositoryState.ChangedObservationRecorded, observation))
        };
        repository.ListResult = [History(first), History(changed)];

        viewModel.RecordAllObservationsCommand.Execute(null);
        await WaitUntilAsync(viewModel, () => !viewModel.IsBatchRecording);

        Assert.Equal(3, repository.RecordCalls);
        Assert.Equal(CruiseBatchRecordingStatus.FirstObservationRecorded,
            viewModel.CapturedCandidates[0].RecordingStatus);
        Assert.Equal(CruiseBatchRecordingStatus.Failed,
            viewModel.CapturedCandidates[1].RecordingStatus);
        Assert.True(viewModel.CapturedCandidates[1].IsRetryable);
        Assert.DoesNotContain("private failure", viewModel.CapturedCandidates[1].RecordingMessage);
        Assert.Equal(CruiseBatchRecordingStatus.ChangedObservationRecorded,
            viewModel.CapturedCandidates[2].RecordingStatus);
        Assert.Equal(1, repository.ListCalls);
        Assert.Contains("1 failed", viewModel.BatchRecordingSummary);
    }

    [Fact]
    public async Task Retry_ProcessesOnlyPreviouslyFailedCandidate()
    {
        var first = Observation(1);
        var second = Observation(2);
        var (viewModel, repository) = await CreateReviewedViewModel([Ready(first), Ready(second)]);
        repository.RecordHandler = (_, _, observation, _) => repository.RecordCalls switch
        {
            1 => Task.FromResult(Result(RepositoryState.FirstObservationRecorded, observation)),
            2 => Task.FromException<RepositoryResult>(new InvalidOperationException()),
            _ => Task.FromResult(Result(RepositoryState.FirstObservationRecorded, observation))
        };
        repository.ListResult = [History(first), History(second)];

        viewModel.RecordAllObservationsCommand.Execute(null);
        await WaitUntilAsync(viewModel, () => !viewModel.IsBatchRecording);
        viewModel.RecordAllObservationsCommand.Execute(null);
        await WaitUntilAsync(viewModel, () => !viewModel.IsBatchRecording);

        Assert.Equal(3, repository.RecordCalls);
        Assert.All(viewModel.CapturedCandidates, candidate =>
            Assert.True(candidate.IsRecordingComplete));
        Assert.False(viewModel.RecordAllObservationsCommand.CanExecute(null));
        Assert.Equal(2, repository.ListCalls);
    }

    [Fact]
    public async Task Cancel_PreservesCompletedOutcomeAndLeavesLaterCandidateUnattempted()
    {
        var first = Observation(1);
        var second = Observation(2);
        var third = Observation(3);
        var current = new TaskCompletionSource<RepositoryResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var (viewModel, repository) = await CreateReviewedViewModel([
            Ready(first), Ready(second), Ready(third)
        ]);
        repository.RecordHandler = (_, _, observation, cancellationToken) =>
        {
            if (repository.RecordCalls == 1)
            {
                return Task.FromResult(Result(
                    RepositoryState.FirstObservationRecorded,
                    observation));
            }

            cancellationToken.Register(() => current.TrySetCanceled(cancellationToken));
            return current.Task;
        };
        repository.ListResult = [History(first)];

        viewModel.RecordAllObservationsCommand.Execute(null);
        await WaitUntilAsync(viewModel, () => repository.RecordCalls == 2);
        viewModel.CancelBatchRecordingCommand.Execute(null);
        await WaitUntilAsync(viewModel, () => !viewModel.IsBatchRecording);

        Assert.Equal(2, repository.RecordCalls);
        Assert.Equal(CruiseBatchRecordingStatus.FirstObservationRecorded,
            viewModel.CapturedCandidates[0].RecordingStatus);
        Assert.Equal(CruiseBatchRecordingStatus.Cancelled,
            viewModel.CapturedCandidates[1].RecordingStatus);
        Assert.True(viewModel.CapturedCandidates[1].IsRetryable);
        Assert.Equal(CruiseBatchRecordingStatus.NotAttempted,
            viewModel.CapturedCandidates[2].RecordingStatus);
        Assert.Equal(1, repository.ListCalls);
        Assert.Contains("Recording cancelled", viewModel.BatchRecordingSummary);
    }

    [Fact]
    public async Task AllFailed_DoesNotRefreshHistory()
    {
        var (viewModel, repository) = await CreateReviewedViewModel([
            Ready(Observation(1)), Ready(Observation(2))
        ]);
        repository.RecordException = new InvalidOperationException("private failure");

        viewModel.RecordAllObservationsCommand.Execute(null);
        await WaitUntilAsync(viewModel, () => !viewModel.IsBatchRecording);

        Assert.Equal(2, repository.RecordCalls);
        Assert.Equal(0, repository.ListCalls);
        Assert.All(viewModel.CapturedCandidates, candidate =>
            Assert.Equal(CruiseBatchRecordingStatus.Failed, candidate.RecordingStatus));
    }

    [Fact]
    public async Task AlreadyCurrent_IsUsefulCompletedOutcomeAndRefreshesOnce()
    {
        var first = Observation(1);
        var second = Observation(2);
        var (viewModel, repository) = await CreateReviewedViewModel([Ready(first), Ready(second)]);
        repository.RecordHandler = (_, _, observation, _) => Task.FromResult(
            Result(RepositoryState.AlreadyCurrent, observation));
        repository.ListResult = [History(first), History(second)];

        viewModel.RecordAllObservationsCommand.Execute(null);
        await WaitUntilAsync(viewModel, () => !viewModel.IsBatchRecording);

        Assert.Equal(1, repository.ListCalls);
        Assert.All(viewModel.CapturedCandidates, candidate =>
            Assert.Equal(CruiseBatchRecordingStatus.AlreadyCurrent, candidate.RecordingStatus));
        Assert.Contains("already current", viewModel.CapturedCandidates[0].RecordingMessage);
        Assert.DoesNotContain("newly", viewModel.CapturedCandidates[0].RecordingMessage);
    }

    [Fact]
    public async Task ChangedObservation_ReportsCreatedAlertPerRowAndInSummary()
    {
        var current = Observation(1);
        var previous = WithPrice(current, 900m, current.ObservedAt.AddDays(-1));
        var (viewModel, repository) = await CreateReviewedViewModel([
            Ready(current),
            CandidateResult.Incomplete("Incomplete", Reference(9), "Missing.", ["prices"])
        ]);
        repository.RecordResult = new RepositoryResult(
            RepositoryState.ChangedObservationRecorded,
            CruiseHistoryApplicationTestData.History(previous, current));
        repository.ListResult = [History(current)];

        viewModel.RecordAllObservationsCommand.Execute(null);
        await WaitUntilAsync(viewModel, () => !viewModel.IsBatchRecording);

        var candidate = Assert.Single(viewModel.CapturedCandidates, item => item.IsReady);
        Assert.Equal(CruiseBatchRecordingStatus.ChangedObservationRecorded, candidate.RecordingStatus);
        Assert.Equal(1, candidate.CreatedAlertCount);
        Assert.Contains("1 alert was created", candidate.RecordingMessage);
        Assert.Contains("1 alert created", viewModel.BatchRecordingSummary);
        Assert.Contains("0 alert evaluations failed", viewModel.BatchRecordingSummary);
    }

    [Fact]
    public async Task AlertFailureAfterChangedCommit_RemainsARecordingSuccessAndIsCountedSeparately()
    {
        var current = Observation(1);
        var previous = WithPrice(current, 900m, current.ObservedAt.AddDays(-1));
        var repository = new FakeCruiseObservationRepository();
        var analyzer = new CruisePriceHistoryAnalyzer();
        var record = CruiseAlertApplicationTestFactory.CreateRecorder(
            repository,
            analyzer,
            settings: new TestAlertSettingsRepository(new InvalidOperationException("private failure")));
        var history = new CruiseHistoryViewModel(
            record,
            new GetHistory(repository, analyzer),
            new ListHistories(repository, analyzer),
            new FixedClock());
        var viewModel = await CreateReviewedViewModel(
            [
                Ready(current),
                CandidateResult.Incomplete("Incomplete", Reference(9), "Missing.", ["prices"])
            ],
            repository,
            record,
            history);
        repository.RecordResult = new RepositoryResult(
            RepositoryState.ChangedObservationRecorded,
            CruiseHistoryApplicationTestData.History(previous, current));
        repository.ListResult = [History(current)];

        viewModel.RecordAllObservationsCommand.Execute(null);
        await WaitUntilAsync(viewModel, () => !viewModel.IsBatchRecording);

        var candidate = Assert.Single(viewModel.CapturedCandidates, item => item.IsReady);
        Assert.Equal(CruiseBatchRecordingStatus.ChangedObservationRecorded, candidate.RecordingStatus);
        Assert.True(candidate.AlertEvaluationFailed);
        Assert.DoesNotContain("private failure", candidate.RecordingMessage);
        Assert.Contains("1 alert evaluations failed", viewModel.BatchRecordingSummary);
        Assert.Contains("0 failed, 0 cancelled", viewModel.BatchRecordingSummary);
        Assert.Equal(1, repository.ListCalls);
    }

    private static async Task<(CruiseBrowserFeasibilityViewModel ViewModel,
        FakeCruiseObservationRepository Repository)> CreateReviewedViewModel(
        IReadOnlyList<CandidateResult> candidates)
    {
        var repository = new FakeCruiseObservationRepository();
        var analyzer = new CruisePriceHistoryAnalyzer();
        var record = CruiseAlertApplicationTestFactory.CreateRecorder(repository, analyzer);
        var history = new CruiseHistoryViewModel(
            record,
            new GetHistory(repository, analyzer),
            new ListHistories(repository, analyzer),
            new FixedClock());
        var viewModel = new CruiseBrowserFeasibilityViewModel(
            new CruiseDiscoverySourceCatalog(),
            new CruiseTrustedHostPolicy(),
            clock: new FixedClock(),
            history: history,
            batchCaptureService: new FixedBatchService(BatchResult.Completed(candidates)),
            recordObservation: record);
        viewModel.LoadCommand.Execute(null);
        var address = new Uri(viewModel.CurrentAddress!);
        viewModel.ReportNavigationCompleted(address);
        viewModel.CapturePayloadRequested += (_, _) => { };
        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, address);
        return (viewModel, repository);
    }

    private static async Task<CruiseBrowserFeasibilityViewModel> CreateReviewedViewModel(
        IReadOnlyList<CandidateResult> candidates,
        FakeCruiseObservationRepository repository,
        KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseObservationAndEvaluateAlerts record,
        CruiseHistoryViewModel history)
    {
        var viewModel = new CruiseBrowserFeasibilityViewModel(
            new CruiseDiscoverySourceCatalog(),
            new CruiseTrustedHostPolicy(),
            clock: new FixedClock(),
            history: history,
            batchCaptureService: new FixedBatchService(BatchResult.Completed(candidates)),
            recordObservation: record);
        viewModel.LoadCommand.Execute(null);
        var address = new Uri(viewModel.CurrentAddress!);
        viewModel.ReportNavigationCompleted(address);
        viewModel.CapturePayloadRequested += (_, _) => { };
        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, address);
        return viewModel;
    }

    private static CandidateResult Ready(CruiseObservation observation) =>
        CandidateResult.Ready(
            observation.Snapshot.Offer.Title,
            observation.SourceReference!,
            observation);

    private static CruiseObservation Observation(int index) =>
        new(
            new CruiseSnapshot(
                new CruiseOffer(
                    new CruiseProvider("marella", "Marella Cruises"),
                    $"offer-{index}",
                    $"Cruise {index}",
                    $"Ship {index}",
                    new DateOnly(2027, 3, index),
                    7),
                [new CruisePrice(800m + index, "GBP", "per person")]),
            ObservedAt,
            Reference(index),
            new CruiseSource("tui", "TUI"));

    private static CruiseObservation WithPrice(
        CruiseObservation observation,
        decimal price,
        DateTimeOffset observedAt) =>
        new(
            new CruiseSnapshot(
                observation.Snapshot.Offer,
                [new CruisePrice(price, "GBP", "per person")],
                observation.Snapshot.PromotionSummary),
            observedAt,
            observation.SourceReference,
            observation.Source);

    private static string Reference(int index) =>
        $"https://www.tui.co.uk/cruise/bookitineraries/Cruise-{index}?itineraryCodeOne={index}";

    private static KrytenApplication::KrytenAssist.Application.Cruises.CruiseRecordedHistory History(
        CruiseObservation observation) =>
        CruiseHistoryApplicationTestData.History(observation);

    private static RepositoryResult Result(
        RepositoryState state,
        CruiseObservation observation) =>
        new(state, History(observation));

    private static async Task WaitUntilAsync(
        CruiseBrowserFeasibilityViewModel viewModel,
        Func<bool> condition)
    {
        if (condition())
        {
            return;
        }

        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        PropertyChangedEventHandler? handler = null;
        handler = (_, _) =>
        {
            if (!condition())
            {
                return;
            }

            viewModel.PropertyChanged -= handler;
            completion.TrySetResult();
        };
        viewModel.PropertyChanged += handler;
        await completion.Task;
    }

    private sealed class FixedBatchService(BatchResult result) : BatchService
    {
        public Task<BatchResult> CaptureAsync(
            CaptureRequest request,
            CancellationToken cancellationToken = default) => Task.FromResult(result);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset Now => ObservedAt;
    }
}
