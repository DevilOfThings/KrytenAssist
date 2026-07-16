using KrytenAssist.Avalonia.ViewModels;

namespace KrytenAssist.Avalonia.Tests.ViewModels;

public sealed class CruiseBrowserFeasibilityViewModelTests
{
    [Fact]
    public void Constructor_PerformsNoNavigationAndStartsIdle()
    {
        var viewModel = new CruiseBrowserFeasibilityViewModel();
        var loadRequests = 0;
        viewModel.LoadRequested += (_, _) => loadRequests++;

        Assert.Equal(0, loadRequests);
        Assert.False(viewModel.HasStarted);
        Assert.False(viewModel.IsNavigating);
        Assert.False(viewModel.IsPageReady);
        Assert.False(viewModel.IsVerifying);
        Assert.False(viewModel.HasError);
        Assert.False(viewModel.HasCurrentAddress);
        Assert.False(viewModel.HasNavigationHistory);
        Assert.False(viewModel.HasPageTitle);
        Assert.Equal("The TUI page has not been loaded.", viewModel.StatusMessage);
        Assert.True(viewModel.LoadCommand.CanExecute(null));
        Assert.False(viewModel.StopCommand.CanExecute(null));
        Assert.False(viewModel.RefreshCommand.CanExecute(null));
        Assert.False(viewModel.CloseCommand.CanExecute(null));
        Assert.False(viewModel.VerifyReadAccessCommand.CanExecute(null));
    }

    [Fact]
    public void LoadCommand_RequestsOnlyConfiguredHttpsTuiAddress()
    {
        var viewModel = new CruiseBrowserFeasibilityViewModel();
        Uri? requestedAddress = null;
        viewModel.LoadRequested += (_, args) => requestedAddress = args.Address;

        viewModel.LoadCommand.Execute(null);

        Assert.Equal(CruiseBrowserFeasibilityViewModel.StartingAddress, requestedAddress);
        Assert.Equal(Uri.UriSchemeHttps, requestedAddress!.Scheme);
        Assert.Equal("www.tui.co.uk", requestedAddress.Host);
        Assert.Equal(
            "/cruise/deals/marella-cruise-of-the-week",
            requestedAddress.AbsolutePath);
        Assert.True(viewModel.HasStarted);
        Assert.True(viewModel.IsNavigating);
        Assert.False(viewModel.LoadCommand.CanExecute(null));
        Assert.True(viewModel.StopCommand.CanExecute(null));
        Assert.True(viewModel.CloseCommand.CanExecute(null));
        Assert.True(viewModel.VerifyReadAccessCommand.CanExecute(null));
        Assert.True(viewModel.HasNavigationHistory);
        Assert.Equal(requestedAddress.AbsoluteUri, viewModel.NavigationHistory);
    }

    [Fact]
    public void NavigationLifecycle_MapsReadyStateWithoutFabricatingDiagnostics()
    {
        var viewModel = new CruiseBrowserFeasibilityViewModel();
        var redirectedAddress = new Uri("https://www.tui.co.uk/cruise/deals/example");

        viewModel.LoadCommand.Execute(null);
        viewModel.ReportNavigationStarted(CruiseBrowserFeasibilityViewModel.StartingAddress);

        Assert.True(viewModel.IsNavigating);
        Assert.False(viewModel.IsPageReady);
        Assert.Equal(
            CruiseBrowserFeasibilityViewModel.StartingAddress.AbsoluteUri,
            viewModel.CurrentAddress);

        viewModel.ReportNavigationCompleted(redirectedAddress);

        Assert.False(viewModel.IsNavigating);
        Assert.True(viewModel.IsPageReady);
        Assert.True(viewModel.CanRefresh);
        Assert.True(viewModel.CanVerifyReadAccess);
        Assert.Equal(redirectedAddress.AbsoluteUri, viewModel.CurrentAddress);
        Assert.False(viewModel.HasPageTitle);
        Assert.False(viewModel.HasVisibleTextSample);
        Assert.Equal("The embedded page is ready.", viewModel.StatusMessage);
    }

    [Fact]
    public void NavigationFailure_ProducesControlledRetryableError()
    {
        var viewModel = new CruiseBrowserFeasibilityViewModel();

        viewModel.LoadCommand.Execute(null);
        viewModel.ReportNavigationFailed(CruiseBrowserFeasibilityViewModel.StartingAddress);

        Assert.False(viewModel.IsNavigating);
        Assert.False(viewModel.IsPageReady);
        Assert.True(viewModel.HasError);
        Assert.DoesNotContain("Exception", viewModel.ErrorMessage);
        Assert.True(viewModel.LoadCommand.CanExecute(null));
        Assert.True(viewModel.RefreshCommand.CanExecute(null));
        Assert.True(viewModel.VerifyReadAccessCommand.CanExecute(null));
    }

    [Fact]
    public void StopCommand_RequestsStopAndStoppedNavigationIsNotReady()
    {
        var viewModel = new CruiseBrowserFeasibilityViewModel();
        var stopRequests = 0;
        viewModel.StopRequested += (_, _) => stopRequests++;
        viewModel.LoadCommand.Execute(null);

        viewModel.StopCommand.Execute(null);
        viewModel.ReportNavigationStopped();

        Assert.Equal(1, stopRequests);
        Assert.False(viewModel.IsNavigating);
        Assert.False(viewModel.IsPageReady);
        Assert.False(viewModel.HasError);
        Assert.Equal("Navigation was stopped.", viewModel.StatusMessage);
    }

    [Fact]
    public void RefreshCommand_IsExplicitAndReturnsToNavigatingState()
    {
        var viewModel = CreateReadyViewModel();
        var refreshRequests = 0;
        viewModel.RefreshRequested += (_, _) => refreshRequests++;

        viewModel.RefreshCommand.Execute(null);

        Assert.Equal(1, refreshRequests);
        Assert.True(viewModel.IsNavigating);
        Assert.False(viewModel.IsPageReady);
        Assert.False(viewModel.RefreshCommand.CanExecute(null));
        Assert.True(viewModel.StopCommand.CanExecute(null));
        Assert.Equal("Refreshing the embedded page...", viewModel.StatusMessage);
    }

    [Fact]
    public void VerifyReadAccessCommand_IsUnavailableBeforeSuccessfulNavigation()
    {
        var viewModel = new CruiseBrowserFeasibilityViewModel();
        var verificationRequests = 0;
        viewModel.ReadAccessVerificationRequested += (_, _) => verificationRequests++;

        viewModel.VerifyReadAccessCommand.Execute(null);

        Assert.Equal(0, verificationRequests);
        Assert.False(viewModel.IsVerifying);
    }

    [Fact]
    public void ReadAccessSuccess_MapsOnlyBoundedDiagnosticState()
    {
        var viewModel = CreateReadyViewModel();
        var verificationRequests = 0;
        viewModel.ReadAccessVerificationRequested += (_, _) => verificationRequests++;

        viewModel.VerifyReadAccessCommand.Execute(null);

        Assert.Equal(1, verificationRequests);
        Assert.True(viewModel.IsVerifying);
        Assert.False(viewModel.VerifyReadAccessCommand.CanExecute(null));

        viewModel.ReportReadAccessSucceeded(
            "  Marella Cruise of the Week | TUI  ",
            "https://www.tui.co.uk/cruise/deals/marella-cruise-of-the-week",
            true,
            [
                "https://www.tui.co.uk/cruise/bookitineraries/Canarian-Flavours-101842?shipCode=150013",
                "https://example.com/not-trusted"
            ]);

        Assert.False(viewModel.IsVerifying);
        Assert.False(viewModel.HasError);
        Assert.Equal("Marella Cruise of the Week | TUI", viewModel.PageTitle);
        Assert.True(viewModel.HasVisibleTextSample);
        Assert.Equal("Yes", viewModel.VisibleTextSampleStatus);
        Assert.True(viewModel.HasCruiseLinks);
        Assert.Contains("shipCode=150013", viewModel.CruiseLinks);
        Assert.DoesNotContain("example.com", viewModel.CruiseLinks);
        Assert.Equal(
            "Read-only page access was verified and cruise links were found.",
            viewModel.StatusMessage);
    }

    [Fact]
    public void ReadAccessFailure_ProducesControlledErrorAndAllowsRetry()
    {
        var viewModel = CreateReadyViewModel();
        viewModel.VerifyReadAccessCommand.Execute(null);

        viewModel.ReportReadAccessFailed();

        Assert.False(viewModel.IsVerifying);
        Assert.True(viewModel.HasError);
        Assert.DoesNotContain("JavaScript", viewModel.ErrorMessage);
        Assert.True(viewModel.VerifyReadAccessCommand.CanExecute(null));
    }

    [Fact]
    public void NewNavigation_CancelsVerificationState()
    {
        var viewModel = CreateReadyViewModel();
        viewModel.VerifyReadAccessCommand.Execute(null);

        viewModel.ReportNavigationStarted(
            new Uri("https://www.tui.co.uk/cruise/deals/another-page"));

        Assert.False(viewModel.IsVerifying);
        Assert.True(viewModel.IsNavigating);
        Assert.False(viewModel.IsPageReady);
    }

    [Fact]
    public void BrowserOperationFailure_IsControlledAndDoesNotExposeDetails()
    {
        var viewModel = new CruiseBrowserFeasibilityViewModel();
        viewModel.LoadCommand.Execute(null);

        viewModel.ReportBrowserOperationFailed();

        Assert.False(viewModel.IsNavigating);
        Assert.False(viewModel.IsPageReady);
        Assert.False(viewModel.IsVerifying);
        Assert.True(viewModel.HasError);
        Assert.Equal(
            "The embedded browser is unavailable. Please try again.",
            viewModel.ErrorMessage);
    }

    [Fact]
    public void CloseCommand_RequestsReleaseAndRestoresIdleState()
    {
        var viewModel = CreateReadyViewModel();
        var closeRequests = 0;
        viewModel.CloseRequested += (_, _) => closeRequests++;
        viewModel.ReportReadAccessSucceeded("TUI", null, true);

        viewModel.CloseCommand.Execute(null);
        viewModel.ReportBrowserClosed();

        Assert.Equal(1, closeRequests);
        Assert.False(viewModel.HasStarted);
        Assert.False(viewModel.IsNavigating);
        Assert.False(viewModel.IsPageReady);
        Assert.False(viewModel.HasCurrentAddress);
        Assert.False(viewModel.HasPageTitle);
        Assert.False(viewModel.HasVisibleTextSample);
        Assert.False(viewModel.HasCruiseLinks);
        Assert.Equal("The TUI page has not been loaded.", viewModel.StatusMessage);
        Assert.True(viewModel.LoadCommand.CanExecute(null));
        Assert.False(viewModel.CloseCommand.CanExecute(null));
    }

    [Fact]
    public void NewNavigationClearsEarlierReadDiagnostics()
    {
        var viewModel = CreateReadyViewModel();
        viewModel.ReportReadAccessSucceeded("Previous title", null, true);

        viewModel.LoadCommand.Execute(null);
        viewModel.ReportNavigationStarted(CruiseBrowserFeasibilityViewModel.StartingAddress);

        Assert.False(viewModel.HasPageTitle);
        Assert.Null(viewModel.PageTitle);
        Assert.False(viewModel.HasVisibleTextSample);
        Assert.Equal("No", viewModel.VisibleTextSampleStatus);
    }

    [Fact]
    public void NavigationHistory_RetainsDistinctAddressesUntilBrowserCloses()
    {
        var viewModel = new CruiseBrowserFeasibilityViewModel();
        var itineraryAddress = new Uri(
            "https://www.tui.co.uk/cruise/bookitineraries/Canarian-Flavours-101842");
        var bookingAddress = new Uri(
            "https://www.tui.co.uk/cruise/book/flow/cruiseoptions");

        viewModel.LoadCommand.Execute(null);
        viewModel.ReportNavigationStarted(itineraryAddress);
        viewModel.ReportNavigationStarted(itineraryAddress);
        viewModel.ReportNavigationStarted(bookingAddress);

        Assert.Equal(
            string.Join(
                Environment.NewLine,
                CruiseBrowserFeasibilityViewModel.StartingAddress.AbsoluteUri,
                itineraryAddress.AbsoluteUri,
                bookingAddress.AbsoluteUri),
            viewModel.NavigationHistory);

        viewModel.ReportBrowserClosed();

        Assert.False(viewModel.HasNavigationHistory);
        Assert.Empty(viewModel.NavigationHistory);
    }

    private static CruiseBrowserFeasibilityViewModel CreateReadyViewModel()
    {
        var viewModel = new CruiseBrowserFeasibilityViewModel();
        viewModel.LoadCommand.Execute(null);
        viewModel.ReportNavigationCompleted(CruiseBrowserFeasibilityViewModel.StartingAddress);
        return viewModel;
    }
}
