extern alias KrytenApplication;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using KrytenAssist.Avalonia.Cruises.Discovery;
using KrytenAssist.Avalonia.Tools;
using KrytenAssist.Core.Cruises;
using CruiseCaptureStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureStatus;
using CruisePageCaptureRequest = KrytenApplication::KrytenAssist.Application.Cruises.CruisePageCaptureRequest;
using ICruisePageCaptureService = KrytenApplication::KrytenAssist.Application.Cruises.ICruisePageCaptureService;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseBrowserFeasibilityViewModel : INotifyPropertyChanged
{
    private const int MaximumNavigationHistoryEntries = 12;

    private readonly CruiseTrustedHostPolicy _trustedHostPolicy;
    private readonly DelegateCommand _loadCommand;
    private readonly DelegateCommand _backCommand;
    private readonly DelegateCommand _forwardCommand;
    private readonly DelegateCommand _stopCommand;
    private readonly DelegateCommand _refreshCommand;
    private readonly DelegateCommand _closeCommand;
    private readonly DelegateCommand _verifyReadAccessCommand;
    private readonly DelegateCommand _captureCommand;
    private readonly DelegateCommand _cancelCaptureCommand;
    private readonly DelegateCommand _openExternalCommand;
    private readonly ICruisePageCaptureService? _captureService;
    private readonly IClock? _clock;
    private bool _hasStarted;
    private bool _isNavigating;
    private bool _isPageReady;
    private bool _isVerifying;
    private string _statusMessage = "Choose a trusted cruise source to begin.";
    private string? _errorMessage;
    private string? _currentAddress;
    private string? _pageTitle;
    private bool _hasVisibleTextSample;
    private readonly List<string> _navigationHistory = [];
    private readonly List<string> _cruiseLinks = [];
    private CruiseDiscoverySource? _selectedSource;
    private bool _canGoBack;
    private bool _canGoForward;
    private bool _hasUnsupportedHost;
    private bool _wasNavigationStopped;
    private bool _isCapturing;
    private CruiseCaptureStatus? _captureStatus;
    private string? _captureMessage;
    private CruiseObservation? _capturedObservation;
    private IReadOnlyList<string> _captureMissingFields = Array.Empty<string>();
    private CancellationTokenSource? _captureCancellation;
    private int _captureGeneration;

    public CruiseBrowserFeasibilityViewModel()
        : this(new CruiseDiscoverySourceCatalog(), new CruiseTrustedHostPolicy())
    {
    }

    public CruiseBrowserFeasibilityViewModel(
        CruiseDiscoverySourceCatalog sourceCatalog,
        CruiseTrustedHostPolicy trustedHostPolicy,
        ICruisePageCaptureService? captureService = null,
        IClock? clock = null,
        CruiseHistoryViewModel? history = null)
    {
        ArgumentNullException.ThrowIfNull(sourceCatalog);
        ArgumentNullException.ThrowIfNull(trustedHostPolicy);

        _trustedHostPolicy = trustedHostPolicy;
        _captureService = captureService;
        _clock = clock;
        History = history;
        AvailableSources = sourceCatalog.Sources;
        var sourceOptions = new List<CruiseDiscoverySourceOptionViewModel>(AvailableSources.Count);
        foreach (var source in AvailableSources)
        {
            sourceOptions.Add(new CruiseDiscoverySourceOptionViewModel(source, OpenSource));
        }

        SourceOptions = sourceOptions;
        _loadCommand = new DelegateCommand(
            () => OpenSource(AvailableSources[0]),
            () => CanLoad);
        _backCommand = new DelegateCommand(RequestBack, () => CanGoBack);
        _forwardCommand = new DelegateCommand(RequestForward, () => CanGoForward);
        _stopCommand = new DelegateCommand(RequestStop, () => IsNavigating);
        _refreshCommand = new DelegateCommand(RequestRefresh, () => CanRefresh);
        _closeCommand = new DelegateCommand(RequestClose, () => HasStarted);
        _verifyReadAccessCommand = new DelegateCommand(
            RequestReadAccessVerification,
            () => CanVerifyReadAccess);
        _captureCommand = new DelegateCommand(RequestCapture, () => CanCapture);
        _cancelCaptureCommand = new DelegateCommand(CancelCapture, () => IsCapturing);
        _openExternalCommand = new DelegateCommand(RequestExternalOpen, () => CanOpenExternal);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<BrowserNavigationRequestedEventArgs>? LoadRequested;

    public event EventHandler? BackRequested;

    public event EventHandler? ForwardRequested;

    public event EventHandler? StopRequested;

    public event EventHandler? RefreshRequested;

    public event EventHandler? CloseRequested;

    public event EventHandler? ReadAccessVerificationRequested;

    public event EventHandler? UntrustedAddressObserved;

    public event EventHandler? CapturePayloadRequested;

    public event EventHandler<BrowserNavigationRequestedEventArgs>? ExternalOpenRequested;

    public IReadOnlyList<CruiseDiscoverySource> AvailableSources { get; }

    public CruiseHistoryViewModel? History { get; }

    public IReadOnlyList<CruiseDiscoverySourceOptionViewModel> SourceOptions { get; }

    public CruiseDiscoverySource? SelectedSource
    {
        get => _selectedSource;
        private set
        {
            if (SetField(ref _selectedSource, value))
            {
                OnPropertyChanged(nameof(HasSelectedSource));
                OnPropertyChanged(nameof(SelectedSourceName));
                OnPropertyChanged(nameof(TrustedHost));
            }
        }
    }

    public bool HasSelectedSource => SelectedSource is not null;

    public string? SelectedSourceName => SelectedSource?.DisplayName;

    public string? TrustedHost => SelectedSource?.TrustedHost;

    public ICommand LoadCommand => _loadCommand;

    public ICommand BackCommand => _backCommand;

    public ICommand ForwardCommand => _forwardCommand;

    public ICommand StopCommand => _stopCommand;

    public ICommand RefreshCommand => _refreshCommand;

    public ICommand CloseCommand => _closeCommand;

    public ICommand VerifyReadAccessCommand => _verifyReadAccessCommand;

    public ICommand CaptureCommand => _captureCommand;

    public ICommand CancelCaptureCommand => _cancelCaptureCommand;

    public ICommand OpenExternalCommand => _openExternalCommand;

    public bool IsCapturing
    {
        get => _isCapturing;
        private set
        {
            if (SetField(ref _isCapturing, value))
            {
                OnCommandStateChanged();
            }
        }
    }

    public bool CanCapture => _captureService is not null &&
                              _clock is not null &&
                              IsPageReady &&
                              HasSelectedSource &&
                              !HasUnsupportedHost &&
                              !IsCapturing;

    public bool CanOpenExternal => HasSelectedSource &&
                                   !HasUnsupportedHost &&
                                   Uri.TryCreate(CurrentAddress, UriKind.Absolute, out var address) &&
                                   _trustedHostPolicy.Classify(address, SelectedSource!) == CruiseAddressTrust.Trusted;

    public CruiseCaptureStatus? CaptureStatus => _captureStatus;

    public string? CaptureMessage => _captureMessage;

    public bool HasCaptureMessage => !string.IsNullOrWhiteSpace(CaptureMessage);

    public IReadOnlyList<string> CaptureMissingFields => _captureMissingFields;

    public string CaptureMissingFieldsText => string.Join(", ", CaptureMissingFields);

    public bool HasCaptureMissingFields => CaptureMissingFields.Count > 0;

    public CruiseObservation? CapturedObservation => _capturedObservation;

    public bool HasCapturedObservation => CapturedObservation is not null;

    public string? CapturedTitle => CapturedObservation?.Snapshot.Offer.Title;
    public string? CapturedOperator => CapturedObservation?.Snapshot.Offer.Provider.Name;
    public string? CapturedSource => CapturedObservation?.Source?.Name;
    public string? CapturedShip => CapturedObservation?.Snapshot.Offer.ShipName;
    public string? CapturedDeparture => CapturedObservation?.Snapshot.Offer.DepartureDate.ToString("d MMMM yyyy");
    public string? CapturedDuration => CapturedObservation is null ? null : $"{CapturedObservation.Snapshot.Offer.DurationNights} nights";
    public string? CapturedDeparturePort => CapturedObservation?.Snapshot.Offer.DeparturePort;
    public string? CapturedItinerary => CapturedObservation?.Snapshot.Offer.ItinerarySummary;
    public string? CapturedPrices => CapturedObservation is null ? null : string.Join(Environment.NewLine, CapturedObservation.Snapshot.Prices.Select(price => $"{price.Currency} {price.Amount:0.##}{(price.Basis is null ? string.Empty : $" {price.Basis}")}"));
    public string? CapturedPromotion => CapturedObservation?.Snapshot.PromotionSummary;
    public string? CapturedSourceReference => CapturedObservation?.SourceReference;
    public string? CapturedObservedAt => CapturedObservation?.ObservedAt.ToString("d MMMM yyyy 'at' HH:mm zzz");
    public bool HasCapturedDeparturePort => !string.IsNullOrWhiteSpace(CapturedDeparturePort);
    public bool HasCapturedItinerary => !string.IsNullOrWhiteSpace(CapturedItinerary);
    public bool HasCapturedPrices => !string.IsNullOrWhiteSpace(CapturedPrices);
    public bool HasCapturedPromotion => !string.IsNullOrWhiteSpace(CapturedPromotion);

    public bool HasStarted
    {
        get => _hasStarted;
        private set
        {
            if (SetField(ref _hasStarted, value))
            {
                OnPropertyChanged(nameof(IsBrowserVisible));
                OnCommandStateChanged();
            }
        }
    }

    public bool CanGoBack => HasStarted && _canGoBack && !IsVerifying;

    public bool CanGoForward => HasStarted && _canGoForward && !IsVerifying;

    public string? CurrentHost => Uri.TryCreate(CurrentAddress, UriKind.Absolute, out var address) &&
                                  address.Scheme == Uri.UriSchemeHttps
        ? address.Host
        : null;

    public bool HasCurrentHost => !string.IsNullOrWhiteSpace(CurrentHost);

    public bool HasUnsupportedHost
    {
        get => _hasUnsupportedHost;
        private set
        {
            if (SetField(ref _hasUnsupportedHost, value))
            {
                OnPropertyChanged(nameof(IsBrowserVisible));
            }
        }
    }

    public bool IsBrowserVisible => HasStarted && !HasUnsupportedHost;

    public bool IsNavigating
    {
        get => _isNavigating;
        private set
        {
            if (!SetField(ref _isNavigating, value))
            {
                return;
            }

            OnCommandStateChanged();
        }
    }

    public bool IsPageReady
    {
        get => _isPageReady;
        private set
        {
            if (!SetField(ref _isPageReady, value))
            {
                return;
            }

            OnCommandStateChanged();
        }
    }

    public bool IsVerifying
    {
        get => _isVerifying;
        private set
        {
            if (!SetField(ref _isVerifying, value))
            {
                return;
            }

            OnCommandStateChanged();
        }
    }

    public bool CanLoad => !IsNavigating && !IsVerifying;

    public bool CanRefresh => HasStarted && !IsNavigating && !IsVerifying;

    public bool CanVerifyReadAccess => HasStarted && !IsVerifying;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetField(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string? CurrentAddress
    {
        get => _currentAddress;
        private set
        {
            if (SetField(ref _currentAddress, value))
            {
                OnPropertyChanged(nameof(HasCurrentAddress));
                OnPropertyChanged(nameof(CurrentHost));
                OnPropertyChanged(nameof(HasCurrentHost));
                OnCommandStateChanged();
            }
        }
    }

    public bool HasCurrentAddress => !string.IsNullOrWhiteSpace(CurrentAddress);

    public string NavigationHistory => string.Join(Environment.NewLine, _navigationHistory);

    public bool HasNavigationHistory => _navigationHistory.Count > 0;

    public string CruiseLinks => string.Join(Environment.NewLine, _cruiseLinks);

    public bool HasCruiseLinks => _cruiseLinks.Count > 0;

    public string? PageTitle
    {
        get => _pageTitle;
        private set
        {
            if (SetField(ref _pageTitle, value))
            {
                OnPropertyChanged(nameof(HasPageTitle));
            }
        }
    }

    public bool HasPageTitle => !string.IsNullOrWhiteSpace(PageTitle);

    public bool HasVisibleTextSample
    {
        get => _hasVisibleTextSample;
        private set => SetField(ref _hasVisibleTextSample, value);
    }

    public string VisibleTextSampleStatus => HasVisibleTextSample ? "Yes" : "No";

    public void ReportNavigationStarted(Uri? address)
    {
        if (SelectedSource is null)
        {
            return;
        }

        if (_wasNavigationStopped &&
            (address is null ||
             string.Equals(CurrentAddress, address.AbsoluteUri, StringComparison.Ordinal)))
        {
            return;
        }

        _wasNavigationStopped = false;

        if (IsPageReady &&
            address is not null &&
            string.Equals(CurrentAddress, address.AbsoluteUri, StringComparison.Ordinal))
        {
            return;
        }

        HasStarted = true;
        ClearCaptureState(cancelActive: true);
        _wasNavigationStopped = false;
        IsVerifying = false;
        IsPageReady = false;
        IsNavigating = true;
        ErrorMessage = null;
        PageTitle = null;
        HasVisibleTextSample = false;
        OnPropertyChanged(nameof(VisibleTextSampleStatus));
        if (!ObserveAddress(address))
        {
            return;
        }

        StatusMessage = "Loading the selected cruise source...";
    }

    public void ReportNavigationCompleted(Uri? address)
    {
        if (SelectedSource is null)
        {
            return;
        }

        HasStarted = true;
        _wasNavigationStopped = false;
        IsNavigating = false;
        IsPageReady = true;
        ErrorMessage = null;
        if (!ObserveAddress(address))
        {
            return;
        }

        StatusMessage = "The selected cruise source is displayed.";
    }

    public void ReportNavigationFailed(Uri? address)
    {
        if (SelectedSource is null)
        {
            return;
        }

        HasStarted = true;
        IsNavigating = false;
        IsPageReady = false;
        if (!ObserveAddress(address) && HasUnsupportedHost)
        {
            return;
        }

        StatusMessage = "The selected cruise source could not be loaded.";
        ErrorMessage = "Check your connection and try loading the TUI page again.";
    }

    public void ReportNavigationStopped()
    {
        IsNavigating = false;
        IsPageReady = false;
        _wasNavigationStopped = true;
        StatusMessage = "Navigation was stopped.";
        ErrorMessage = null;
    }

    public void ReportNavigationCapabilities(bool canGoBack, bool canGoForward)
    {
        _canGoBack = canGoBack;
        _canGoForward = canGoForward;
        OnCommandStateChanged();
    }

    public void ReportReadAccessSucceeded(
        string? pageTitle,
        string? currentAddress,
        bool hasVisibleTextSample,
        IReadOnlyList<string>? cruiseLinks = null)
    {
        IsVerifying = false;
        IsNavigating = false;
        IsPageReady = true;
        _wasNavigationStopped = false;
        ErrorMessage = null;
        PageTitle = string.IsNullOrWhiteSpace(pageTitle) ? null : pageTitle.Trim();
        CurrentAddress = string.IsNullOrWhiteSpace(currentAddress)
            ? CurrentAddress
            : ObserveDiagnosticAddress(currentAddress);
        HasVisibleTextSample = hasVisibleTextSample;
        ReplaceCruiseLinks(cruiseLinks);
        OnPropertyChanged(nameof(VisibleTextSampleStatus));
        StatusMessage = HasCruiseLinks
            ? "Read-only page access was verified and cruise links were found."
            : "Read-only page access was verified; no detailed cruise links were found on this page.";
    }

    public void ReportReadAccessFailed()
    {
        IsVerifying = false;
        IsNavigating = false;
        IsPageReady = true;
        _wasNavigationStopped = false;
        StatusMessage = "Read-only page access could not be verified.";
        ErrorMessage = "The embedded page did not return the requested diagnostics.";
    }

    public void ReportBrowserOperationFailed()
    {
        IsNavigating = false;
        IsVerifying = false;
        IsPageReady = false;
        HasUnsupportedHost = false;
        StatusMessage = "The embedded browser operation failed.";
        ErrorMessage = "The embedded browser is unavailable. Please try again.";
    }

    public void ReportBrowserClosed()
    {
        ClearCaptureState(cancelActive: true);
        HasStarted = false;
        IsNavigating = false;
        IsVerifying = false;
        IsPageReady = false;
        ErrorMessage = null;
        CurrentAddress = null;
        SelectedSource = null;
        HasUnsupportedHost = false;
        _canGoBack = false;
        _canGoForward = false;
        _wasNavigationStopped = false;
        _navigationHistory.Clear();
        OnPropertyChanged(nameof(NavigationHistory));
        OnPropertyChanged(nameof(HasNavigationHistory));
        _cruiseLinks.Clear();
        OnPropertyChanged(nameof(CruiseLinks));
        OnPropertyChanged(nameof(HasCruiseLinks));
        PageTitle = null;
        HasVisibleTextSample = false;
        OnPropertyChanged(nameof(VisibleTextSampleStatus));
        StatusMessage = "Choose a trusted cruise source to begin.";
        OnCommandStateChanged();
    }

    private void OpenSource(CruiseDiscoverySource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (_trustedHostPolicy.Classify(source.StartingAddress, source) != CruiseAddressTrust.Trusted)
        {
            StatusMessage = "The selected cruise source is not configured safely.";
            ErrorMessage = "Choose another trusted cruise source.";
            return;
        }

        if (HasStarted && ReferenceEquals(SelectedSource, source))
        {
            return;
        }

        ClearCaptureState(cancelActive: true);
        SelectedSource = source;
        _wasNavigationStopped = false;
        HasStarted = true;
        IsPageReady = false;
        IsNavigating = true;
        HasUnsupportedHost = false;
        ErrorMessage = null;
        CurrentAddress = source.StartingAddress.AbsoluteUri;
        RecordNavigationAddress(source.StartingAddress);
        StatusMessage = $"Opening {source.DisplayName}...";
        LoadRequested?.Invoke(this, new BrowserNavigationRequestedEventArgs(source.StartingAddress));
    }

    private void RequestBack()
    {
        ClearCaptureState(cancelActive: true);
        _wasNavigationStopped = false;
        BackRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RequestForward()
    {
        ClearCaptureState(cancelActive: true);
        _wasNavigationStopped = false;
        ForwardRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RequestStop()
    {
        StopRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RequestRefresh()
    {
        ClearCaptureState(cancelActive: true);
        _wasNavigationStopped = false;
        IsPageReady = false;
        IsNavigating = true;
        ErrorMessage = null;
        StatusMessage = "Refreshing the embedded page...";
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RequestClose()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RequestReadAccessVerification()
    {
        IsVerifying = true;
        ErrorMessage = null;
        StatusMessage = "Verifying read-only page access...";
        ReadAccessVerificationRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RequestCapture()
    {
        _captureCancellation?.Cancel();
        _captureCancellation?.Dispose();
        _captureCancellation = new CancellationTokenSource();
        _captureGeneration++;
        SetCaptureResult(null, null, Array.Empty<string>());
        IsCapturing = true;
        CapturePayloadRequested?.Invoke(this, EventArgs.Empty);
        if (CapturePayloadRequested is null)
        {
            ReportCaptureBridgeFailed();
        }
    }

    public async Task ProcessCapturePayloadAsync(string payload, Uri sourceReference)
    {
        if (!IsCapturing ||
            _captureService is null ||
            _clock is null ||
            SelectedSource is null)
        {
            return;
        }

        var generation = _captureGeneration;
        var cancellation = _captureCancellation!;
        if (_trustedHostPolicy.Classify(sourceReference, SelectedSource) != CruiseAddressTrust.Trusted ||
            payload.Length > CruisePageCaptureRequest.MaximumPagePayloadLength)
        {
            ReportCaptureBridgeFailed();
            return;
        }

        try
        {
            var request = new CruisePageCaptureRequest(
                SelectedSource.Identifier,
                new CruiseSource("tui", "TUI"),
                sourceReference.AbsoluteUri,
                _clock.Now,
                payload);
            var result = await _captureService.CaptureAsync(request, cancellation.Token);
            if (generation != _captureGeneration || cancellation.IsCancellationRequested)
            {
                return;
            }

            SetCaptureResult(result.Status, result.Message, result.MissingFields, result.Observation);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Cancellation is a neutral user action.
        }
        catch (Exception)
        {
            if (generation == _captureGeneration)
            {
                SetCaptureResult(
                    CruiseCaptureStatus.Failed,
                    "The displayed cruise could not be captured. Refresh the page and try again.",
                    Array.Empty<string>());
            }
        }
        finally
        {
            if (generation == _captureGeneration)
            {
                IsCapturing = false;
            }
        }
    }

    public void ReportCaptureBridgeFailed()
    {
        if (!IsCapturing)
        {
            return;
        }

        SetCaptureResult(
            CruiseCaptureStatus.Failed,
            "The displayed page could not be read for capture. Refresh the page and try again.",
            Array.Empty<string>());
        IsCapturing = false;
    }

    public void ReportExternalOpenFailed()
    {
        ErrorMessage = "The trusted TUI page could not be opened externally.";
    }

    private void CancelCapture()
    {
        _captureGeneration++;
        _captureCancellation?.Cancel();
        _captureCancellation?.Dispose();
        _captureCancellation = null;
        IsCapturing = false;
        SetCaptureResult(null, null, Array.Empty<string>());
    }

    private void RequestExternalOpen()
    {
        if (Uri.TryCreate(CurrentAddress, UriKind.Absolute, out var address) &&
            SelectedSource is not null &&
            _trustedHostPolicy.Classify(address, SelectedSource) == CruiseAddressTrust.Trusted)
        {
            ExternalOpenRequested?.Invoke(this, new BrowserNavigationRequestedEventArgs(address));
        }
    }

    private void ClearCaptureState(bool cancelActive)
    {
        if (cancelActive)
        {
            _captureGeneration++;
            _captureCancellation?.Cancel();
            _captureCancellation?.Dispose();
            _captureCancellation = null;
        }

        IsCapturing = false;
        SetCaptureResult(null, null, Array.Empty<string>());
    }

    private void SetCaptureResult(
        CruiseCaptureStatus? status,
        string? message,
        IReadOnlyList<string> missingFields,
        CruiseObservation? observation = null)
    {
        _captureStatus = status;
        _captureMessage = message;
        _captureMissingFields = missingFields;
        _capturedObservation = observation;
        History?.SetCapturedObservation(observation);
        OnPropertyChanged(nameof(CaptureStatus));
        OnPropertyChanged(nameof(CaptureMessage));
        OnPropertyChanged(nameof(HasCaptureMessage));
        OnPropertyChanged(nameof(CaptureMissingFields));
        OnPropertyChanged(nameof(CaptureMissingFieldsText));
        OnPropertyChanged(nameof(HasCaptureMissingFields));
        OnPropertyChanged(nameof(CapturedObservation));
        OnPropertyChanged(nameof(HasCapturedObservation));
        OnPropertyChanged(nameof(CapturedTitle));
        OnPropertyChanged(nameof(CapturedOperator));
        OnPropertyChanged(nameof(CapturedSource));
        OnPropertyChanged(nameof(CapturedShip));
        OnPropertyChanged(nameof(CapturedDeparture));
        OnPropertyChanged(nameof(CapturedDuration));
        OnPropertyChanged(nameof(CapturedDeparturePort));
        OnPropertyChanged(nameof(CapturedItinerary));
        OnPropertyChanged(nameof(CapturedPrices));
        OnPropertyChanged(nameof(CapturedPromotion));
        OnPropertyChanged(nameof(CapturedSourceReference));
        OnPropertyChanged(nameof(CapturedObservedAt));
        OnPropertyChanged(nameof(HasCapturedDeparturePort));
        OnPropertyChanged(nameof(HasCapturedItinerary));
        OnPropertyChanged(nameof(HasCapturedPrices));
        OnPropertyChanged(nameof(HasCapturedPromotion));
    }

    private void OnCommandStateChanged()
    {
        OnPropertyChanged(nameof(CanLoad));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanVerifyReadAccess));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
        OnPropertyChanged(nameof(CanCapture));
        OnPropertyChanged(nameof(CanOpenExternal));
        _loadCommand.RaiseCanExecuteChanged();
        _backCommand.RaiseCanExecuteChanged();
        _forwardCommand.RaiseCanExecuteChanged();
        _stopCommand.RaiseCanExecuteChanged();
        _refreshCommand.RaiseCanExecuteChanged();
        _closeCommand.RaiseCanExecuteChanged();
        _verifyReadAccessCommand.RaiseCanExecuteChanged();
        _captureCommand.RaiseCanExecuteChanged();
        _cancelCaptureCommand.RaiseCanExecuteChanged();
        _openExternalCommand.RaiseCanExecuteChanged();
    }

    private void RecordNavigationAddress(Uri? address)
    {
        if (address is null || !address.IsAbsoluteUri)
        {
            return;
        }

        var value = address.AbsoluteUri;
        if (_navigationHistory.Count > 0 &&
            string.Equals(_navigationHistory[^1], value, StringComparison.Ordinal))
        {
            return;
        }

        _navigationHistory.Add(value);
        if (_navigationHistory.Count > MaximumNavigationHistoryEntries)
        {
            _navigationHistory.RemoveAt(0);
        }

        OnPropertyChanged(nameof(NavigationHistory));
        OnPropertyChanged(nameof(HasNavigationHistory));
    }

    private bool ObserveAddress(Uri? address)
    {
        if (SelectedSource is null || address is null)
        {
            return true;
        }

        var trust = _trustedHostPolicy.Classify(address, SelectedSource);
        if (trust == CruiseAddressTrust.BrowserInternal)
        {
            StatusMessage = "The embedded browser is preparing the trusted page.";
            return false;
        }

        if (trust == CruiseAddressTrust.Untrusted)
        {
            IsNavigating = false;
            IsPageReady = false;
            HasUnsupportedHost = true;
            StatusMessage = "Navigation outside the trusted cruise source was stopped.";
            ErrorMessage = $"Kryten only allows browsing on {SelectedSource.TrustedHost}.";
            UntrustedAddressObserved?.Invoke(this, EventArgs.Empty);
            return false;
        }

        HasUnsupportedHost = false;
        ErrorMessage = null;
        CurrentAddress = address.AbsoluteUri;
        RecordNavigationAddress(address);
        return true;
    }

    private string? ObserveDiagnosticAddress(string currentAddress)
    {
        if (!Uri.TryCreate(currentAddress.Trim(), UriKind.Absolute, out var address) ||
            !ObserveAddress(address))
        {
            return CurrentAddress;
        }

        return address.AbsoluteUri;
    }

    private void ReplaceCruiseLinks(IReadOnlyList<string>? cruiseLinks)
    {
        _cruiseLinks.Clear();

        if (cruiseLinks is not null)
        {
            foreach (var value in cruiseLinks)
            {
                if (_cruiseLinks.Count == 10)
                {
                    break;
                }

                if (SelectedSource is null ||
                    !Uri.TryCreate(value, UriKind.Absolute, out var address) ||
                    address.Scheme != Uri.UriSchemeHttps ||
                    !string.Equals(
                        address.Host,
                        SelectedSource.TrustedHost,
                        StringComparison.OrdinalIgnoreCase) ||
                    _cruiseLinks.Exists(existing =>
                        string.Equals(existing, address.AbsoluteUri, StringComparison.Ordinal)))
                {
                    continue;
                }

                _cruiseLinks.Add(address.AbsoluteUri);
            }
        }

        OnPropertyChanged(nameof(CruiseLinks));
        OnPropertyChanged(nameof(HasCruiseLinks));
    }

    private bool SetField<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class DelegateCommand(
        Action execute,
        Func<bool> canExecute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => canExecute();

        public void Execute(object? parameter)
        {
            if (canExecute())
            {
                execute();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

public sealed class CruiseDiscoverySourceOptionViewModel
{
    public CruiseDiscoverySourceOptionViewModel(
        CruiseDiscoverySource source,
        Action<CruiseDiscoverySource> open)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        ArgumentNullException.ThrowIfNull(open);
        OpenCommand = new SourceCommand(() => open(Source));
    }

    public CruiseDiscoverySource Source { get; }

    public string DisplayName => Source.DisplayName;

    public string Description => Source.Description;

    public ICommand OpenCommand { get; }

    private sealed class SourceCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute();
    }
}

public sealed class BrowserNavigationRequestedEventArgs(Uri address) : EventArgs
{
    public Uri Address { get; } = address ?? throw new ArgumentNullException(nameof(address));
}
