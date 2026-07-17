extern alias KrytenApplication;

using KrytenAssist.Avalonia.Cruises.Discovery;
using KrytenAssist.Avalonia.Tools;
using KrytenAssist.Avalonia.ViewModels;
using KrytenAssist.Core.Cruises;
using CaptureRequest = KrytenApplication::KrytenAssist.Application.Cruises.CruisePageCaptureRequest;
using CaptureResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureResult;
using CaptureService = KrytenApplication::KrytenAssist.Application.Cruises.ICruisePageCaptureService;
using CaptureStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureStatus;

namespace KrytenAssist.Avalonia.Tests.ViewModels;

public sealed class CruiseCaptureLifecycleViewModelTests
{
    private const string Payload = "{\"version\":1,\"candidates\":[]}";
    private static readonly DateTimeOffset ObservedAt =
        new(2026, 7, 16, 10, 30, 0, TimeSpan.FromHours(1));

    [Fact]
    public void Capture_IsAvailableOnlyWhenTrustedPageIsReadyAndDependenciesExist()
    {
        var service = new RecordingCaptureService(CaptureResult.Failed("Not used."));
        var viewModel = CreateViewModel(service);

        Assert.False(viewModel.CaptureCommand.CanExecute(null));
        viewModel.LoadCommand.Execute(null);
        Assert.False(viewModel.CaptureCommand.CanExecute(null));

        viewModel.ReportNavigationCompleted(CurrentAddress(viewModel));

        Assert.True(viewModel.CaptureCommand.CanExecute(null));
        Assert.False(new CruiseBrowserFeasibilityViewModel().CaptureCommand.CanExecute(null));
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public void Capture_RaisesOneBridgeRequestAndPreventsDuplicateCapture()
    {
        var viewModel = CreateReadyViewModel(new PendingCaptureService());
        var requests = 0;
        viewModel.CapturePayloadRequested += (_, _) => requests++;

        viewModel.CaptureCommand.Execute(null);
        viewModel.CaptureCommand.Execute(null);

        Assert.Equal(1, requests);
        Assert.True(viewModel.IsCapturing);
        Assert.False(viewModel.CaptureCommand.CanExecute(null));
        Assert.True(viewModel.CancelCaptureCommand.CanExecute(null));
    }

    [Fact]
    public void Go_IsUnavailableWhileCaptureIsActive()
    {
        var viewModel = CreateReadyViewModel(new PendingCaptureService());
        viewModel.CapturePayloadRequested += (_, _) => { };
        viewModel.CaptureCommand.Execute(null);
        viewModel.AddressDraft = "https://www.tui.co.uk/cruise/deals/voyager-cruises";

        Assert.False(viewModel.CanGo);
        Assert.False(viewModel.GoCommand.CanExecute(null));
    }

    [Fact]
    public async Task Capture_PassesExactTrustedRequestAndFixedClockValue()
    {
        var service = new RecordingCaptureService(CaptureResult.Failed("Controlled failure."));
        var viewModel = CreateReadyViewModel(service);
        var address = CurrentAddress(viewModel);
        viewModel.CapturePayloadRequested += (_, _) => { };

        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, address);

        Assert.Equal(1, service.CallCount);
        Assert.NotNull(service.Request);
        Assert.Equal("marella-cruise-of-the-week", service.Request!.SourceIdentifier);
        Assert.Equal("tui", service.Request.Source.Id);
        Assert.Equal("TUI", service.Request.Source.Name);
        Assert.Equal(address.AbsoluteUri, service.Request.SourceReference);
        Assert.Equal(ObservedAt, service.Request.ObservedAt);
        Assert.Equal(Payload, service.Request.PagePayload);
    }

    [Fact]
    public async Task Success_PresentsEveryCapturedReviewValueWithoutSaving()
    {
        var observation = CompleteObservation();
        var viewModel = CreateReadyViewModel(
            new RecordingCaptureService(CaptureResult.Succeeded(observation)));
        viewModel.CapturePayloadRequested += (_, _) => { };

        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));

        Assert.Equal(CaptureStatus.Success, viewModel.CaptureStatus);
        Assert.Same(observation, viewModel.CapturedObservation);
        Assert.True(viewModel.HasCapturedObservation);
        Assert.Equal("Atlantic Discovery", viewModel.CapturedTitle);
        Assert.Equal("Marella Cruises", viewModel.CapturedOperator);
        Assert.Equal("TUI", viewModel.CapturedSource);
        Assert.Equal("Marella Example", viewModel.CapturedShip);
        Assert.Equal("18 December 2026", viewModel.CapturedDeparture);
        Assert.Equal("7 nights", viewModel.CapturedDuration);
        Assert.Equal("Santa Cruz, Tenerife", viewModel.CapturedDeparturePort);
        Assert.Equal("Tenerife, Gran Canaria and Lanzarote", viewModel.CapturedItinerary);
        Assert.Equal(
            $"GBP 988 per person{Environment.NewLine}GBP 1975 total based on 2 sharing",
            viewModel.CapturedPrices);
        Assert.Equal("GBP 380 per person discount", viewModel.CapturedPromotion);
        Assert.Equal(observation.SourceReference, viewModel.CapturedSourceReference);
        Assert.Contains("+01:00", viewModel.CapturedObservedAt);
        Assert.True(viewModel.HasCapturedDeparturePort);
        Assert.True(viewModel.HasCapturedItinerary);
        Assert.True(viewModel.HasCapturedPrices);
        Assert.True(viewModel.HasCapturedPromotion);
    }

    [Fact]
    public async Task Go_ClearsCompletedCaptureReviewBeforeRequestingTrustedNavigation()
    {
        var viewModel = CreateReadyViewModel(
            new RecordingCaptureService(CaptureResult.Succeeded(CompleteObservation())));
        viewModel.CapturePayloadRequested += (_, _) => { };
        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));
        Uri? requestedAddress = null;
        viewModel.LoadRequested += (_, args) => requestedAddress = args.Address;
        viewModel.AddressDraft = "https://www.tui.co.uk/cruise/deals/voyager-cruises";

        viewModel.GoCommand.Execute(null);

        Assert.Equal("https://www.tui.co.uk/cruise/deals/voyager-cruises",
            requestedAddress?.AbsoluteUri);
        Assert.False(viewModel.HasCapturedObservation);
        Assert.Null(viewModel.CaptureStatus);
        Assert.False(viewModel.HasCaptureMessage);
    }

    [Fact]
    public async Task Success_WithAbsentOptionalValuesDoesNotInventReviewContent()
    {
        var observation = ObservationWithoutOptionalValues();
        var viewModel = CreateReadyViewModel(
            new RecordingCaptureService(CaptureResult.Succeeded(observation)));
        viewModel.CapturePayloadRequested += (_, _) => { };

        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));

        Assert.Null(viewModel.CapturedDeparturePort);
        Assert.Null(viewModel.CapturedItinerary);
        Assert.Null(viewModel.CapturedPromotion);
        Assert.False(viewModel.HasCapturedDeparturePort);
        Assert.False(viewModel.HasCapturedItinerary);
        Assert.False(viewModel.HasCapturedPromotion);
    }

    [Theory]
    [InlineData(CaptureStatus.Ambiguous, "More than one cruise was found.")]
    [InlineData(CaptureStatus.Unsupported, "This TUI page is unsupported.")]
    [InlineData(CaptureStatus.Failed, "The displayed page could not be captured.")]
    [InlineData(CaptureStatus.Cancelled, "Cruise capture was cancelled.")]
    public async Task ControlledNonSuccess_PreservesStatusAndMessage(
        CaptureStatus status,
        string message)
    {
        var result = status switch
        {
            CaptureStatus.Ambiguous => CaptureResult.Ambiguous(message),
            CaptureStatus.Unsupported => CaptureResult.Unsupported(message),
            CaptureStatus.Failed => CaptureResult.Failed(message),
            CaptureStatus.Cancelled => CaptureResult.Cancelled(message),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
        var viewModel = CreateReadyViewModel(new RecordingCaptureService(result));
        viewModel.CapturePayloadRequested += (_, _) => { };

        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));

        Assert.Equal(status, viewModel.CaptureStatus);
        Assert.Equal(message, viewModel.CaptureMessage);
        Assert.False(viewModel.HasCapturedObservation);
        Assert.False(viewModel.IsCapturing);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankPayload_FailsBeforeCallingCaptureService(string payload)
    {
        var service = new RecordingCaptureService(CaptureResult.Failed("Not reached."));
        var viewModel = CreateReadyViewModel(service);
        viewModel.CapturePayloadRequested += (_, _) => { };

        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(payload, CurrentAddress(viewModel));

        Assert.Equal(0, service.CallCount);
        Assert.Equal(CaptureStatus.Failed, viewModel.CaptureStatus);
        Assert.True(viewModel.HasCaptureMessage);
    }

    [Fact]
    public async Task OversizedPayload_FailsBeforeCallingCaptureService()
    {
        var service = new RecordingCaptureService(CaptureResult.Failed("Not reached."));
        var viewModel = CreateReadyViewModel(service);
        viewModel.CapturePayloadRequested += (_, _) => { };

        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(
            new string('x', CaptureRequest.MaximumPagePayloadLength + 1),
            CurrentAddress(viewModel));

        Assert.Equal(0, service.CallCount);
        Assert.Equal(CaptureStatus.Failed, viewModel.CaptureStatus);
    }

    [Fact]
    public async Task UntrustedSourceReference_FailsBeforeCallingCaptureService()
    {
        var service = new RecordingCaptureService(CaptureResult.Failed("Not reached."));
        var viewModel = CreateReadyViewModel(service);
        viewModel.CapturePayloadRequested += (_, _) => { };

        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, new Uri("https://example.test/cruise"));

        Assert.Equal(0, service.CallCount);
        Assert.Equal(CaptureStatus.Failed, viewModel.CaptureStatus);
    }

    [Fact]
    public async Task UnexpectedServiceException_BecomesSafeFailedResult()
    {
        var viewModel = CreateReadyViewModel(new ThrowingCaptureService());
        viewModel.CapturePayloadRequested += (_, _) => { };

        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));

        Assert.Equal(CaptureStatus.Failed, viewModel.CaptureStatus);
        Assert.DoesNotContain("deterministic exception", viewModel.CaptureMessage);
        Assert.False(viewModel.IsCapturing);
    }

    [Fact]
    public async Task Cancel_CancelsTokenAndIgnoresLateCompletion()
    {
        var service = new PendingCaptureService();
        var viewModel = CreateReadyViewModel(service);
        viewModel.CapturePayloadRequested += (_, _) => { };
        viewModel.CaptureCommand.Execute(null);
        var capture = viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));

        viewModel.CancelCaptureCommand.Execute(null);
        service.Complete(CaptureResult.Succeeded(CompleteObservation()));
        await capture;

        Assert.True(service.CancellationToken.IsCancellationRequested);
        Assert.False(viewModel.IsCapturing);
        Assert.Null(viewModel.CaptureStatus);
        Assert.False(viewModel.HasCapturedObservation);
        Assert.False(viewModel.HasCaptureMessage);
    }

    [Theory]
    [InlineData("navigate")]
    [InlineData("refresh")]
    [InlineData("close")]
    public async Task BrowserStateChange_CancelsCaptureAndIgnoresLateCompletion(string action)
    {
        var service = new PendingCaptureService();
        var viewModel = CreateReadyViewModel(service);
        viewModel.CapturePayloadRequested += (_, _) => { };
        viewModel.CaptureCommand.Execute(null);
        var capture = viewModel.ProcessCapturePayloadAsync(Payload, CurrentAddress(viewModel));

        switch (action)
        {
            case "navigate":
                viewModel.ReportNavigationStarted(new Uri("https://www.tui.co.uk/cruise/deals/another"));
                break;
            case "refresh":
                viewModel.RefreshCommand.Execute(null);
                break;
            case "close":
                viewModel.ReportBrowserClosed();
                break;
        }

        service.Complete(CaptureResult.Succeeded(CompleteObservation()));
        await capture;

        Assert.True(service.CancellationToken.IsCancellationRequested);
        Assert.False(viewModel.HasCapturedObservation);
        Assert.Null(viewModel.CaptureStatus);
    }

    [Fact]
    public void OpenExternal_RaisesExactTrustedAddressWithoutChangingReviewState()
    {
        var viewModel = CreateReadyViewModel(
            new RecordingCaptureService(CaptureResult.Failed("Not used.")));
        Uri? requested = null;
        viewModel.ExternalOpenRequested += (_, args) => requested = args.Address;
        var before = viewModel.CurrentAddress;

        viewModel.OpenExternalCommand.Execute(null);

        Assert.Equal(before, requested?.AbsoluteUri);
        Assert.Equal(before, viewModel.CurrentAddress);
        Assert.False(viewModel.IsNavigating);
    }

    [Fact]
    public void ExternalLauncherFailure_IsControlled()
    {
        var viewModel = CreateReadyViewModel(
            new RecordingCaptureService(CaptureResult.Failed("Not used.")));

        viewModel.ReportExternalOpenFailed();

        Assert.Equal("The trusted TUI page could not be opened externally.", viewModel.ErrorMessage);
    }

    private static CruiseBrowserFeasibilityViewModel CreateViewModel(CaptureService service) =>
        new(
            new CruiseDiscoverySourceCatalog(),
            new CruiseTrustedHostPolicy(),
            service,
            new FixedClock());

    private static CruiseBrowserFeasibilityViewModel CreateReadyViewModel(CaptureService service)
    {
        var viewModel = CreateViewModel(service);
        viewModel.LoadCommand.Execute(null);
        viewModel.ReportNavigationCompleted(CurrentAddress(viewModel));
        return viewModel;
    }

    private static Uri CurrentAddress(CruiseBrowserFeasibilityViewModel viewModel) =>
        new(viewModel.CurrentAddress!);

    private static CruiseObservation CompleteObservation() =>
        new(
            new CruiseSnapshot(
                new CruiseOffer(
                    new CruiseProvider("marella", "Marella Cruises"),
                    "fictional-offer-101",
                    "Atlantic Discovery",
                    "Marella Example",
                    new DateOnly(2026, 12, 18),
                    7,
                    "Santa Cruz, Tenerife",
                    "Tenerife, Gran Canaria and Lanzarote"),
                [
                    new CruisePrice(988m, "GBP", "per person"),
                    new CruisePrice(1975m, "GBP", "total based on 2 sharing")
                ],
                "GBP 380 per person discount"),
            ObservedAt,
            "https://www.tui.co.uk/cruise/bookitineraries/fictional-101",
            new CruiseSource("tui", "TUI"));

    private static CruiseObservation ObservationWithoutOptionalValues() =>
        new(
            new CruiseSnapshot(
                new CruiseOffer(
                    new CruiseProvider("marella", "Marella Cruises"),
                    "fictional-offer-102",
                    "Island Discovery",
                    "Marella Example",
                    new DateOnly(2027, 1, 8),
                    7),
                [new CruisePrice(900m, "GBP")]),
            ObservedAt);

    private sealed class RecordingCaptureService(CaptureResult result) : CaptureService
    {
        public int CallCount { get; private set; }
        public CaptureRequest? Request { get; private set; }

        public Task<CaptureResult> CaptureAsync(
            CaptureRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Request = request;
            return Task.FromResult(result);
        }
    }

    private sealed class PendingCaptureService : CaptureService
    {
        private readonly TaskCompletionSource<CaptureResult> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CancellationToken CancellationToken { get; private set; }

        public Task<CaptureResult> CaptureAsync(
            CaptureRequest request,
            CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            return _completion.Task;
        }

        public void Complete(CaptureResult result) => _completion.TrySetResult(result);
    }

    private sealed class ThrowingCaptureService : CaptureService
    {
        public Task<CaptureResult> CaptureAsync(
            CaptureRequest request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("deterministic exception");
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset Now => ObservedAt;
    }
}
