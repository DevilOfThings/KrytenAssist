using System;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using KrytenAssist.Avalonia.ViewModels;

namespace KrytenAssist.Avalonia.Views;

public partial class CruiseBrowserFeasibilityView : UserControl
{
    private const string ReadAccessScript =
        "JSON.stringify({" +
        "title: document.title || ''," +
        "address: document.location.href || ''," +
        "hasVisibleText: Boolean((document.body?.innerText || '').trim().slice(0, 512))," +
        "cruiseLinks: Array.from(new Set(" +
        "Array.from(document.querySelectorAll('a[href]'), a => a.href)" +
        ".filter(href => href.startsWith('https://www.tui.co.uk/') && " +
        "/bookitineraries|itineraryCode|shipCode|sailingDate|packageId/i.test(href))" +
        ")).slice(0, 10).map(href => href.slice(0, 2048))" +
        "})";

    private CruiseBrowserFeasibilityViewModel? _viewModel;
    private bool _isAttached;

    public CruiseBrowserFeasibilityView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        UpdateViewModelSubscription();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _isAttached = true;
        UpdateViewModelSubscription();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        try
        {
            EmbeddedBrowser.Stop();
        }
        catch (Exception)
        {
            _viewModel?.ReportBrowserOperationFailed();
        }

        _isAttached = false;
        UpdateViewModelSubscription();

        base.OnDetachedFromVisualTree(e);
    }

    private void UpdateViewModelSubscription()
    {
        DetachViewModel();

        _viewModel = _isAttached ? DataContext as CruiseBrowserFeasibilityViewModel : null;
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.LoadRequested += OnLoadRequested;
        _viewModel.BackRequested += OnBackRequested;
        _viewModel.ForwardRequested += OnForwardRequested;
        _viewModel.StopRequested += OnStopRequested;
        _viewModel.RefreshRequested += OnRefreshRequested;
        _viewModel.CloseRequested += OnCloseRequested;
        _viewModel.ReadAccessVerificationRequested += OnReadAccessVerificationRequested;
        _viewModel.UntrustedAddressObserved += OnUntrustedAddressObserved;
    }

    private void OnBackRequested(object? sender, EventArgs e)
    {
        try
        {
            EmbeddedBrowser.GoBack();
            ReportNavigationCapabilities();
        }
        catch (Exception)
        {
            _viewModel?.ReportBrowserOperationFailed();
        }
    }

    private void OnForwardRequested(object? sender, EventArgs e)
    {
        try
        {
            EmbeddedBrowser.GoForward();
            ReportNavigationCapabilities();
        }
        catch (Exception)
        {
            _viewModel?.ReportBrowserOperationFailed();
        }
    }

    private void OnLoadRequested(object? sender, BrowserNavigationRequestedEventArgs e)
    {
        try
        {
            EmbeddedBrowser.Navigate(e.Address);
        }
        catch (Exception)
        {
            _viewModel?.ReportBrowserOperationFailed();
        }
    }

    private void OnStopRequested(object? sender, EventArgs e)
    {
        try
        {
            EmbeddedBrowser.Stop();
            _viewModel?.ReportNavigationStopped();
            ReportNavigationCapabilities();
        }
        catch (Exception)
        {
            _viewModel?.ReportBrowserOperationFailed();
        }
    }

    private void OnRefreshRequested(object? sender, EventArgs e)
    {
        try
        {
            EmbeddedBrowser.Refresh();
            ReportNavigationCapabilities();
        }
        catch (Exception)
        {
            _viewModel?.ReportBrowserOperationFailed();
        }
    }

    private void OnUntrustedAddressObserved(object? sender, EventArgs e)
    {
        try
        {
            EmbeddedBrowser.Stop();
        }
        catch (Exception)
        {
            _viewModel?.ReportBrowserOperationFailed();
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        try
        {
            EmbeddedBrowser.Stop();
            _viewModel?.ReportBrowserClosed();
        }
        catch (Exception)
        {
            _viewModel?.ReportBrowserOperationFailed();
        }
    }

    private async void OnReadAccessVerificationRequested(object? sender, EventArgs e)
    {
        await VerifyReadAccessAsync();
    }

    private async Task VerifyReadAccessAsync()
    {
        try
        {
            var addressAtStart = EmbeddedBrowser.Source;
            var rawResult = await EmbeddedBrowser.InvokeScript(ReadAccessScript);
            if (_viewModel?.IsVerifying != true ||
                EmbeddedBrowser.Source != addressAtStart)
            {
                return;
            }

            var diagnostics = ParseDiagnostics(rawResult);
            _viewModel?.ReportReadAccessSucceeded(
                diagnostics.Title,
                diagnostics.Address,
                diagnostics.HasVisibleText,
                diagnostics.CruiseLinks);
        }
        catch (Exception)
        {
            _viewModel?.ReportReadAccessFailed();
        }
    }

    private void EmbeddedBrowser_OnNavigationStarted(
        object? sender,
        WebViewNavigationStartingEventArgs e)
    {
        _viewModel?.ReportNavigationStarted(EmbeddedBrowser.Source);
        ReportNavigationCapabilities();
    }

    private void EmbeddedBrowser_OnNavigationCompleted(
        object? sender,
        WebViewNavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            _viewModel?.ReportNavigationCompleted(EmbeddedBrowser.Source);
            ReportNavigationCapabilities();
            return;
        }

        _viewModel?.ReportNavigationFailed(EmbeddedBrowser.Source);
        ReportNavigationCapabilities();
    }

    private void DetachViewModel()
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.LoadRequested -= OnLoadRequested;
        _viewModel.BackRequested -= OnBackRequested;
        _viewModel.ForwardRequested -= OnForwardRequested;
        _viewModel.StopRequested -= OnStopRequested;
        _viewModel.RefreshRequested -= OnRefreshRequested;
        _viewModel.CloseRequested -= OnCloseRequested;
        _viewModel.ReadAccessVerificationRequested -= OnReadAccessVerificationRequested;
        _viewModel.UntrustedAddressObserved -= OnUntrustedAddressObserved;
        _viewModel = null;
    }

    private void ReportNavigationCapabilities()
    {
        _viewModel?.ReportNavigationCapabilities(
            EmbeddedBrowser.CanGoBack,
            EmbeddedBrowser.CanGoForward);
    }

    private static PageReadDiagnostics ParseDiagnostics(string? rawResult)
    {
        if (string.IsNullOrWhiteSpace(rawResult))
        {
            throw new JsonException("The script returned no diagnostics.");
        }

        var json = rawResult;
        if (rawResult.TrimStart().StartsWith('"'))
        {
            json = JsonSerializer.Deserialize<string>(rawResult)
                   ?? throw new JsonException("The script result was empty.");
        }

        return JsonSerializer.Deserialize<PageReadDiagnostics>(
                   json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new JsonException("The script result was invalid.");
    }

    private sealed record PageReadDiagnostics(
        string? Title,
        string? Address,
        bool HasVisibleText,
        string[]? CruiseLinks);
}
