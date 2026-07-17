extern alias KrytenApplication;

using KrytenAssist.Avalonia.Cruises.Discovery;
using KrytenAssist.Avalonia.Tests.Application.Cruises;
using KrytenAssist.Avalonia.Tools;
using KrytenAssist.Avalonia.ViewModels;
using KrytenAssist.Core.Cruises;
using BatchResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureBatchResult;
using BatchService = KrytenApplication::KrytenAssist.Application.Cruises.ICruisePageBatchCaptureService;
using CandidateResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureCandidateResult;
using CandidateStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureCandidateStatus;
using CaptureRequest = KrytenApplication::KrytenAssist.Application.Cruises.CruisePageCaptureRequest;
using GetHistory = KrytenApplication::KrytenAssist.Application.Cruises.GetCruiseHistory;
using ListHistories = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseHistories;
using RecordObservation = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseObservation;

namespace KrytenAssist.Avalonia.Tests.ViewModels;

public sealed class CruiseBatchCaptureReviewViewModelTests
{
    private const string Payload = "{\"version\":1,\"candidates\":[]}";
    private static readonly DateTimeOffset ObservedAt =
        new(2026, 7, 18, 10, 45, 0, TimeSpan.FromHours(1));

    [Fact]
    public async Task Capture_PassesExactRequestAndPresentsCandidatesInOrder()
    {
        var ready = ReadyCandidate(1, "Island Dreams", 899m, "First promotion");
        var incomplete = CandidateResult.Incomplete(
            "Aegean Shores",
            Reference(2),
            "Prices were unavailable.",
            ["prices"]);
        var failed = CandidateResult.Failed(
            "Adriatic Explorer",
            Reference(3),
            "The card could not be mapped.");
        var service = new RecordingBatchService(
            BatchResult.Completed([ready, incomplete, failed]));
        var viewModel = CreateReadyViewModel(service);
        viewModel.CapturePayloadRequested += (_, _) => { };

        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));

        Assert.Equal("Capture Loaded Cruises", viewModel.CaptureButtonText);
        Assert.Same(Payload, service.Request!.PagePayload);
        Assert.Equal(ObservedAt, service.Request.ObservedAt);
        Assert.Equal(CurrentAddress(viewModel).AbsoluteUri, service.Request.SourceReference);
        Assert.Equal("tui", service.Request.Source.Id);
        Assert.True(service.CancellationToken.CanBeCanceled);
        Assert.Equal(
            ["Island Dreams", "Aegean Shores", "Adriatic Explorer"],
            viewModel.CapturedCandidates.Select(candidate => candidate.DisplayLabel));
        Assert.Equal(1, viewModel.ReadyCandidateCount);
        Assert.Equal(1, viewModel.IncompleteCandidateCount);
        Assert.Equal(1, viewModel.FailedCandidateCount);
        Assert.Equal("First promotion", viewModel.CapturedCandidates[0].PromotionSummary);
        Assert.Equal("prices", viewModel.CapturedCandidates[1].MissingFieldsText);
        Assert.Null(viewModel.CapturedCandidates[1].PricesText);
        Assert.Equal(CandidateStatus.Failed, viewModel.CapturedCandidates[2].Status);
        Assert.False(viewModel.HasCapturedObservation);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<CruiseCaptureCandidateReviewItemViewModel>)viewModel.CapturedCandidates)
            .Clear());
    }

    [Fact]
    public async Task Selection_StartsClearAndBulkCommandsAffectOnlyReadyCandidates()
    {
        var result = BatchResult.Completed([
            ReadyCandidate(1, "Island Dreams"),
            CandidateResult.Incomplete("Aegean Shores", Reference(2), "Missing.", ["prices"]),
            ReadyCandidate(3, "Adriatic Explorer")
        ]);
        var viewModel = CreateReadyViewModel(new RecordingBatchService(result));
        viewModel.CapturePayloadRequested += (_, _) => { };
        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));

        Assert.Equal(0, viewModel.SelectedCandidateCount);
        Assert.True(viewModel.SelectAllReadyCommand.CanExecute(null));
        viewModel.CapturedCandidates[0].IsSelected = true;
        viewModel.CapturedCandidates[1].IsSelected = true;

        Assert.Equal(1, viewModel.SelectedCandidateCount);
        Assert.False(viewModel.CapturedCandidates[1].IsSelected);

        viewModel.SelectAllReadyCommand.Execute(null);

        Assert.Equal(2, viewModel.SelectedCandidateCount);
        Assert.False(viewModel.CapturedCandidates[1].IsSelected);
        Assert.True(viewModel.ClearSelectionCommand.CanExecute(null));

        viewModel.ClearSelectionCommand.Execute(null);

        Assert.Equal(0, viewModel.SelectedCandidateCount);
        Assert.False(viewModel.ClearSelectionCommand.CanExecute(null));
    }

    [Fact]
    public async Task CandidateOpen_RaisesExactTrustedReference()
    {
        var incomplete = CandidateResult.Incomplete(
            "Aegean Shores", Reference(2), "Missing.", ["prices"]);
        var viewModel = CreateReadyViewModel(new RecordingBatchService(
            BatchResult.Completed([ReadyCandidate(1, "Island Dreams"), incomplete])));
        viewModel.CapturePayloadRequested += (_, _) => { };
        Uri? requested = null;
        viewModel.ExternalOpenRequested += (_, args) => requested = args.Address;
        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));

        var item = viewModel.CapturedCandidates[1];
        Assert.True(item.OpenAtTuiCommand.CanExecute(null));
        item.OpenAtTuiCommand.Execute(null);

        Assert.Equal(Reference(2), requested?.AbsoluteUri);
        Assert.Equal(CurrentAddress(viewModel).AbsoluteUri, viewModel.CurrentAddress);
    }

    [Fact]
    public async Task CandidateOpen_DisablesUntrustedReferenceDefensively()
    {
        const string untrusted =
            "https://example.test/cruise/bookitineraries/Fictional-1?itineraryCodeOne=1";
        var observation = CreateObservation(1, "Island Dreams", untrusted);
        var candidate = CandidateResult.Ready("Island Dreams", untrusted, observation);
        var viewModel = CreateReadyViewModel(new RecordingBatchService(
            BatchResult.Completed([candidate], wasTruncated: true)));
        viewModel.CapturePayloadRequested += (_, _) => { };
        var raised = false;
        viewModel.ExternalOpenRequested += (_, _) => raised = true;
        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));

        var item = Assert.Single(viewModel.CapturedCandidates);
        Assert.False(item.CanOpenAtTui);
        Assert.False(item.OpenAtTuiCommand.CanExecute(null));
        item.OpenAtTuiCommand.Execute(null);
        Assert.False(raised);
    }

    [Fact]
    public async Task SingleCleanReadyCandidate_UsesExistingReviewAndHistoryHandoff()
    {
        var history = CreateHistoryViewModel();
        var candidate = ReadyCandidate(1, "Island Dreams");
        var viewModel = CreateReadyViewModel(
            new RecordingBatchService(BatchResult.Completed([candidate])),
            history);
        viewModel.CapturePayloadRequested += (_, _) => { };

        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));

        Assert.Same(candidate.Observation, viewModel.CapturedObservation);
        Assert.Same(candidate.Observation, history.CapturedObservation);
        Assert.True(history.RecordObservationCommand.CanExecute(null));
        Assert.False(viewModel.HasCapturedCandidates);
    }

    [Fact]
    public async Task TruncatedSingleCandidate_UsesBatchReviewAndDoesNotPopulateHistory()
    {
        var history = CreateHistoryViewModel();
        var viewModel = CreateReadyViewModel(
            new RecordingBatchService(BatchResult.Completed(
                [ReadyCandidate(1, "Island Dreams")],
                wasTruncated: true)),
            history);
        viewModel.CapturePayloadRequested += (_, _) => { };

        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));

        Assert.True(viewModel.HasCapturedCandidates);
        Assert.False(viewModel.HasCapturedObservation);
        Assert.Null(history.CapturedObservation);
        Assert.True(viewModel.WasCaptureTruncated);
        Assert.Contains("first 10 loaded cruise deals", viewModel.BatchCaptureSummary);
    }

    [Theory]
    [InlineData("incomplete")]
    [InlineData("unsupported")]
    [InlineData("failed")]
    [InlineData("cancelled")]
    public async Task BatchWideResult_ShowsSafeMessageAndNoRows(string status)
    {
        const string message = "The batch could not be completed.";
        var result = status switch
        {
            "incomplete" => BatchResult.Incomplete(message),
            "unsupported" => BatchResult.Unsupported(message),
            "failed" => BatchResult.Failed(message),
            "cancelled" => BatchResult.Cancelled(message),
            _ => throw new InvalidOperationException()
        };
        var viewModel = CreateReadyViewModel(new RecordingBatchService(result));
        viewModel.CapturePayloadRequested += (_, _) => { };

        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));

        Assert.Equal(message, viewModel.CaptureMessage);
        Assert.False(viewModel.HasCapturedCandidates);
        Assert.False(viewModel.HasCapturedObservation);
    }

    [Fact]
    public async Task Cancel_CancelsTokenAndIgnoresLateBatchCompletion()
    {
        var service = new PendingBatchService();
        var viewModel = CreateReadyViewModel(service);
        viewModel.CapturePayloadRequested += (_, _) => { };
        viewModel.CaptureCommand.Execute(null);
        var capture = viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));

        viewModel.CancelCaptureCommand.Execute(null);
        service.Complete(BatchResult.Completed([
            ReadyCandidate(1, "Late cruise"),
            ReadyCandidate(2, "Later cruise")
        ]));
        await capture;

        Assert.True(service.CancellationToken.IsCancellationRequested);
        Assert.False(viewModel.HasCapturedCandidates);
        Assert.False(viewModel.HasCapturedObservation);
        Assert.Null(viewModel.CaptureMessage);
    }

    [Theory]
    [InlineData("navigate")]
    [InlineData("back")]
    [InlineData("forward")]
    [InlineData("refresh")]
    [InlineData("close")]
    public async Task BrowserStateChange_ClearsBatchSelection(string action)
    {
        var initial = new RecordingBatchService(BatchResult.Completed([
            ReadyCandidate(1, "Island Dreams"),
            ReadyCandidate(2, "Aegean Shores")
        ]));
        var viewModel = CreateReadyViewModel(initial);
        viewModel.CapturePayloadRequested += (_, _) => { };
        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));
        viewModel.SelectAllReadyCommand.Execute(null);

        viewModel.ReportNavigationCapabilities(canGoBack: true, canGoForward: true);
        switch (action)
        {
            case "navigate":
                viewModel.ReportNavigationStarted(
                    new Uri("https://www.tui.co.uk/cruise/deals/another-page"));
                break;
            case "back":
                viewModel.BackCommand.Execute(null);
                break;
            case "forward":
                viewModel.ForwardCommand.Execute(null);
                break;
            case "refresh":
                viewModel.RefreshCommand.Execute(null);
                break;
            case "close":
                viewModel.ReportBrowserClosed();
                break;
        }

        Assert.False(viewModel.HasCapturedCandidates);
        Assert.Equal(0, viewModel.SelectedCandidateCount);
        Assert.False(viewModel.HasCapturedObservation);
    }

    [Fact]
    public async Task UnexpectedBatchServiceException_BecomesSafeFailure()
    {
        var viewModel = CreateReadyViewModel(new ThrowingBatchService());
        viewModel.CapturePayloadRequested += (_, _) => { };
        viewModel.CaptureCommand.Execute(null);

        await viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));

        Assert.True(viewModel.HasCaptureMessage);
        Assert.DoesNotContain("deterministic", viewModel.CaptureMessage);
        Assert.False(viewModel.HasCapturedCandidates);
        Assert.False(viewModel.IsCapturing);
    }

    private static CruiseBrowserFeasibilityViewModel CreateReadyViewModel(
        BatchService service,
        CruiseHistoryViewModel? history = null)
    {
        var viewModel = new CruiseBrowserFeasibilityViewModel(
            new CruiseDiscoverySourceCatalog(),
            new CruiseTrustedHostPolicy(),
            clock: new FixedClock(),
            history: history,
            batchCaptureService: service);
        viewModel.LoadCommand.Execute(null);
        viewModel.ReportNavigationCompleted(CurrentAddress(viewModel));
        return viewModel;
    }

    private static Uri CurrentAddress(CruiseBrowserFeasibilityViewModel viewModel) =>
        new(viewModel.CurrentAddress!);

    private static CandidateResult ReadyCandidate(
        int index,
        string title,
        decimal price = 899m,
        string? promotion = null)
    {
        var reference = Reference(index);
        var observation = CreateObservation(index, title, reference, price, promotion);
        return CandidateResult.Ready(title, reference, observation);
    }

    private static CruiseObservation CreateObservation(
        int index,
        string title,
        string reference,
        decimal price = 899m,
        string? promotion = null) =>
        new(
            new CruiseSnapshot(
                new CruiseOffer(
                    new CruiseProvider("marella", "Marella Cruises"),
                    $"fictional-{index}",
                    title,
                    $"Marella Example {index}",
                    new DateOnly(2027, 2, index),
                    7,
                    index == 1 ? "Palma" : null),
                [new CruisePrice(price, "GBP", "per person")],
                promotion),
            ObservedAt,
            reference,
            new CruiseSource("tui", "TUI"));

    private static string Reference(int index) =>
        $"https://www.tui.co.uk/cruise/bookitineraries/Fictional-{index}?itineraryCodeOne={index}";

    private static CruiseHistoryViewModel CreateHistoryViewModel()
    {
        var repository = new FakeCruiseObservationRepository();
        var analyzer = new CruisePriceHistoryAnalyzer();
        return new CruiseHistoryViewModel(
            new RecordObservation(repository, analyzer),
            new GetHistory(repository, analyzer),
            new ListHistories(repository, analyzer),
            new FixedClock());
    }

    private sealed class RecordingBatchService(BatchResult result) : BatchService
    {
        public CaptureRequest? Request { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<BatchResult> CaptureAsync(
            CaptureRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            CancellationToken = cancellationToken;
            return Task.FromResult(result);
        }
    }

    private sealed class PendingBatchService : BatchService
    {
        private readonly TaskCompletionSource<BatchResult> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CancellationToken CancellationToken { get; private set; }

        public Task<BatchResult> CaptureAsync(
            CaptureRequest request,
            CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            return _completion.Task;
        }

        public void Complete(BatchResult result) => _completion.TrySetResult(result);
    }

    private sealed class ThrowingBatchService : BatchService
    {
        public Task<BatchResult> CaptureAsync(
            CaptureRequest request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("deterministic batch exception");
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset Now => ObservedAt;
    }
}
