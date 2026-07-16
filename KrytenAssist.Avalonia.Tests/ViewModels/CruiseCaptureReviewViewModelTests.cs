extern alias KrytenApplication;

using KrytenAssist.Avalonia.Cruises.Discovery;
using KrytenAssist.Avalonia.Tools;
using KrytenAssist.Avalonia.ViewModels;
using CaptureRequest = KrytenApplication::KrytenAssist.Application.Cruises.CruisePageCaptureRequest;
using CaptureResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureResult;
using CaptureService = KrytenApplication::KrytenAssist.Application.Cruises.ICruisePageCaptureService;

namespace KrytenAssist.Avalonia.Tests.ViewModels;

public sealed class CruiseCaptureReviewViewModelTests
{
    [Fact]
    public async Task Capture_UsesExplicitBridgeAndPresentsIncompleteResult()
    {
        var service = new StubCaptureService(
            CaptureResult.Incomplete("Open one specific itinerary and try again.", ["shipName"]));
        var viewModel = new CruiseBrowserFeasibilityViewModel(
            new CruiseDiscoverySourceCatalog(),
            new CruiseTrustedHostPolicy(),
            service,
            new FixedClock());
        var address = viewModel.AvailableSources[0].StartingAddress;
        var bridgeRequests = 0;
        viewModel.CapturePayloadRequested += (_, _) => bridgeRequests++;

        viewModel.LoadCommand.Execute(null);
        viewModel.ReportNavigationCompleted(address);
        viewModel.CaptureCommand.Execute(null);
        await viewModel.ProcessCapturePayloadAsync("{\"version\":1,\"candidates\":[]}", address);

        Assert.Equal(1, bridgeRequests);
        Assert.False(viewModel.IsCapturing);
        Assert.True(viewModel.HasCaptureMessage);
        Assert.Equal("shipName", viewModel.CaptureMissingFieldsText);
        Assert.False(viewModel.HasCapturedObservation);
        Assert.Equal(address.AbsoluteUri, service.Request?.SourceReference);
    }

    [Fact]
    public void Close_ClearsCaptureAndDisablesExternalOpen()
    {
        var viewModel = new CruiseBrowserFeasibilityViewModel(
            new CruiseDiscoverySourceCatalog(),
            new CruiseTrustedHostPolicy(),
            new StubCaptureService(CaptureResult.Failed("Failed.")),
            new FixedClock());
        viewModel.LoadCommand.Execute(null);
        viewModel.ReportNavigationCompleted(viewModel.AvailableSources[0].StartingAddress);

        Assert.True(viewModel.OpenExternalCommand.CanExecute(null));
        viewModel.ReportBrowserClosed();

        Assert.False(viewModel.OpenExternalCommand.CanExecute(null));
        Assert.False(viewModel.HasCaptureMessage);
    }

    [Fact]
    public void FixedScript_IsBoundedAndDoesNotReadPrivateBrowserStorage()
    {
        Assert.Contains("slice(0, 10)", TuiCruiseCaptureScript.Script);
        Assert.Contains("slice(0, 512)", TuiCruiseCaptureScript.Script);
        Assert.Contains("[role=\"tab\"]", TuiCruiseCaptureScript.Script);
        Assert.Contains("h1,h2,h3,h4,h5,h6,dt,dd,label,strong,p,span", TuiCruiseCaptureScript.Script);
        Assert.Contains("value.length <= 160", TuiCruiseCaptureScript.Script);
        Assert.Contains("displayed price", TuiCruiseCaptureScript.Script);
        Assert.Contains("discount|saving", TuiCruiseCaptureScript.Script);
        Assert.Contains("a[href],[data-href],[data-url]", TuiCruiseCaptureScript.Script);
        Assert.Contains("url.searchParams.has('itineraryCodeOne')", TuiCruiseCaptureScript.Script);
        Assert.Contains("'150013': 'Marella Discovery 2'", TuiCruiseCaptureScript.Script);
        Assert.Contains("total based on 2 sharing", TuiCruiseCaptureScript.Script);
        Assert.Contains("document.querySelectorAll('tui-product-cards')", TuiCruiseCaptureScript.Script);
        Assert.Contains("element.shadowRoot", TuiCruiseCaptureScript.Script);
        Assert.Contains("roots.flatMap", TuiCruiseCaptureScript.Script);
        Assert.DoesNotContain("document.cookie", TuiCruiseCaptureScript.Script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("localStorage", TuiCruiseCaptureScript.Script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessionStorage", TuiCruiseCaptureScript.Script, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StubCaptureService(CaptureResult result) : CaptureService
    {
        public CaptureRequest? Request { get; private set; }

        public Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(result);
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset Now => new(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);
    }
}
