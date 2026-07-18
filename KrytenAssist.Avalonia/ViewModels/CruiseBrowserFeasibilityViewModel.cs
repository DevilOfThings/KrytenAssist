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
using CruiseCaptureBatchResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureBatchResult;
using CruiseCaptureBatchStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureBatchStatus;
using CruisePageCaptureRequest = KrytenApplication::KrytenAssist.Application.Cruises.CruisePageCaptureRequest;
using ICruisePageBatchCaptureService = KrytenApplication::KrytenAssist.Application.Cruises.ICruisePageBatchCaptureService;
using ICruisePageCaptureService = KrytenApplication::KrytenAssist.Application.Cruises.ICruisePageCaptureService;
using RecordObservation = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseObservation;
using RecordStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRecordStatus;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseBrowserFeasibilityViewModel : INotifyPropertyChanged
{
    private const int MaximumNavigationHistoryEntries = 12;

    private readonly CruiseTrustedHostPolicy _trustedHostPolicy;
    private readonly DelegateCommand _loadCommand;
    private readonly DelegateCommand _goCommand;
    private readonly DelegateCommand _backCommand;
    private readonly DelegateCommand _forwardCommand;
    private readonly DelegateCommand _stopCommand;
    private readonly DelegateCommand _refreshCommand;
    private readonly DelegateCommand _closeCommand;
    private readonly DelegateCommand _verifyReadAccessCommand;
    private readonly DelegateCommand _captureCommand;
    private readonly DelegateCommand _cancelCaptureCommand;
    private readonly DelegateCommand _openExternalCommand;
    private readonly DelegateCommand _openSelectedHistoryAtTuiCommand;
    private readonly DelegateCommand _saveCapturedCruiseCommand;
    private readonly DelegateCommand _saveSelectedHistoryCommand;
    private readonly DelegateCommand _selectAllReadyCommand;
    private readonly DelegateCommand _clearSelectionCommand;
    private readonly DelegateCommand _recordSelectedCommand;
    private readonly DelegateCommand _recordAllObservationsCommand;
    private readonly DelegateCommand _cancelBatchRecordingCommand;
    private readonly DelegateCommand _selectDesktopPresentationCommand;
    private readonly DelegateCommand _selectMobilePresentationCommand;
    private readonly ICruisePageCaptureService? _captureService;
    private readonly ICruisePageBatchCaptureService? _batchCaptureService;
    private readonly IClock? _clock;
    private readonly RecordObservation? _recordObservation;
    private bool _hasStarted;
    private bool _isNavigating;
    private bool _isPageReady;
    private bool _isVerifying;
    private string _statusMessage = "Choose a trusted cruise source to begin.";
    private string? _errorMessage;
    private string? _currentAddress;
    private string? _addressDraft;
    private string? _pageTitle;
    private bool _hasVisibleTextSample;
    private readonly List<string> _navigationHistory = [];
    private readonly List<CruiseNavigationHistoryEntryViewModel> _navigationHistoryEntries = [];
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
    private CruiseCaptureBatchStatus? _batchCaptureStatus;
    private IReadOnlyList<CruiseCaptureCandidateReviewItemViewModel> _capturedCandidates =
        Array.Empty<CruiseCaptureCandidateReviewItemViewModel>();
    private bool _wasCaptureTruncated;
    private bool _isBatchRecording;
    private string? _batchRecordingProgressText;
    private string? _batchRecordingSummary;
    private CancellationTokenSource? _batchRecordingCancellation;
    private int _batchRecordingGeneration;
    private CruiseBrowserPresentation _browserPresentation = CruiseBrowserPresentation.Mobile;

    public CruiseBrowserFeasibilityViewModel()
        : this(new CruiseDiscoverySourceCatalog(), new CruiseTrustedHostPolicy())
    {
    }

    public CruiseBrowserFeasibilityViewModel(
        CruiseDiscoverySourceCatalog sourceCatalog,
        CruiseTrustedHostPolicy trustedHostPolicy,
        ICruisePageCaptureService? captureService = null,
        IClock? clock = null,
        CruiseHistoryViewModel? history = null,
        ICruisePageBatchCaptureService? batchCaptureService = null,
        RecordObservation? recordObservation = null,
        CruiseSaveAndEvaluationViewModel? evaluation = null)
    {
        ArgumentNullException.ThrowIfNull(sourceCatalog);
        ArgumentNullException.ThrowIfNull(trustedHostPolicy);

        _trustedHostPolicy = trustedHostPolicy;
        _captureService = captureService;
        _batchCaptureService = batchCaptureService;
        _clock = clock;
        _recordObservation = recordObservation;
        Evaluation = evaluation;
        if (Evaluation is not null) Evaluation.PropertyChanged += OnEvaluationPropertyChanged;
        History = history;
        if (History is not null)
        {
            History.PropertyChanged += OnHistoryPropertyChanged;
        }
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
        _goCommand = new DelegateCommand(RequestGo, () => CanGo);
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
        _openSelectedHistoryAtTuiCommand = new DelegateCommand(
            RequestOpenSelectedHistoryAtTui,
            () => CanOpenSelectedHistoryAtTui);
        _saveCapturedCruiseCommand = new DelegateCommand(() => _ = SaveCapturedCruiseAsync(), () => CanSaveCapturedCruise);
        _saveSelectedHistoryCommand = new DelegateCommand(() => _ = SaveOrEditSelectedHistoryAsync(), () => CanSaveSelectedHistory);
        _selectAllReadyCommand = new DelegateCommand(
            SelectAllReady,
            () => CanSelectAllReady);
        _clearSelectionCommand = new DelegateCommand(
            ClearSelection,
            () => CanClearSelection);
        _recordSelectedCommand = new DelegateCommand(
            () => StartBatchRecording(selectedOnly: true),
            () => CanRecordSelected);
        _recordAllObservationsCommand = new DelegateCommand(
            () => StartBatchRecording(selectedOnly: false),
            () => CanRecordAllObservations);
        _cancelBatchRecordingCommand = new DelegateCommand(
            CancelBatchRecording,
            () => IsBatchRecording);
        _selectDesktopPresentationCommand = new DelegateCommand(
            () => RequestPresentationChange(CruiseBrowserPresentation.Desktop),
            () => CanChangePresentation);
        _selectMobilePresentationCommand = new DelegateCommand(
            () => RequestPresentationChange(CruiseBrowserPresentation.Mobile),
            () => CanChangePresentation);
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

    public event EventHandler<BrowserPresentationReloadRequestedEventArgs>? BrowserPresentationReloadRequested;

    public event EventHandler<BrowserNavigationRequestedEventArgs>? ExternalOpenRequested;

    public IReadOnlyList<CruiseDiscoverySource> AvailableSources { get; }

    public CruiseHistoryViewModel? History { get; }
    public CruiseSaveAndEvaluationViewModel? Evaluation { get; }

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

    public ICommand GoCommand => _goCommand;

    public ICommand BackCommand => _backCommand;

    public ICommand ForwardCommand => _forwardCommand;

    public ICommand StopCommand => _stopCommand;

    public ICommand RefreshCommand => _refreshCommand;

    public ICommand CloseCommand => _closeCommand;

    public ICommand VerifyReadAccessCommand => _verifyReadAccessCommand;

    public ICommand CaptureCommand => _captureCommand;

    public ICommand CancelCaptureCommand => _cancelCaptureCommand;

    public ICommand OpenExternalCommand => _openExternalCommand;

    public ICommand OpenSelectedHistoryAtTuiCommand => _openSelectedHistoryAtTuiCommand;
    public ICommand SaveCapturedCruiseCommand => _saveCapturedCruiseCommand;
    public ICommand SaveSelectedHistoryCommand => _saveSelectedHistoryCommand;

    public ICommand SelectAllReadyCommand => _selectAllReadyCommand;

    public ICommand ClearSelectionCommand => _clearSelectionCommand;

    public ICommand RecordSelectedCommand => _recordSelectedCommand;

    public ICommand RecordAllObservationsCommand => _recordAllObservationsCommand;

    public ICommand CancelBatchRecordingCommand => _cancelBatchRecordingCommand;

    public ICommand SelectDesktopPresentationCommand => _selectDesktopPresentationCommand;

    public ICommand SelectMobilePresentationCommand => _selectMobilePresentationCommand;

    public CruiseBrowserPresentation BrowserPresentation
    {
        get => _browserPresentation;
        private set
        {
            if (!SetField(ref _browserPresentation, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsDesktopPresentation));
            OnPropertyChanged(nameof(IsMobilePresentation));
            OnPropertyChanged(nameof(BrowserPanelMinWidth));
            OnPropertyChanged(nameof(BrowserPanelMaxWidth));
        }
    }

    public bool IsDesktopPresentation => BrowserPresentation == CruiseBrowserPresentation.Desktop;

    public bool IsMobilePresentation => BrowserPresentation == CruiseBrowserPresentation.Mobile;

    public double BrowserPanelMinWidth => IsMobilePresentation ? 360 : 520;

    public double BrowserPanelMaxWidth => IsMobilePresentation ? 430 : double.PositiveInfinity;

    public bool CanChangePresentation =>
        HasStarted &&
        IsPageReady &&
        SelectedSource is not null &&
        !HasUnsupportedHost &&
        !IsNavigating &&
        !IsVerifying &&
        !IsCapturing &&
        !IsBatchRecording &&
        TryGetCurrentTrustedAddress(out _);

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

    public bool CanCapture => (_batchCaptureService is not null || _captureService is not null) &&
                              _clock is not null &&
                              IsPageReady &&
                              HasSelectedSource &&
                              !HasUnsupportedHost &&
                              !IsCapturing &&
                              !IsBatchRecording;

    public string CaptureButtonText => _batchCaptureService is null
        ? "Capture Displayed Cruise"
        : "Capture Loaded Cruises";

    public CruiseCaptureBatchStatus? BatchCaptureStatus => _batchCaptureStatus;

    public IReadOnlyList<CruiseCaptureCandidateReviewItemViewModel> CapturedCandidates =>
        _capturedCandidates;

    public bool HasCapturedCandidates => CapturedCandidates.Count > 0;

    public int ReadyCandidateCount => CapturedCandidates.Count(candidate => candidate.IsReady);

    public int IncompleteCandidateCount =>
        CapturedCandidates.Count(candidate => candidate.IsIncomplete);

    public int FailedCandidateCount => CapturedCandidates.Count(candidate => candidate.IsFailed);

    public int SelectedCandidateCount =>
        CapturedCandidates.Count(candidate => candidate.IsSelected);

    public bool HasSelectedCandidates => SelectedCandidateCount > 0;

    public bool CanSelectAllReady =>
        !IsBatchRecording &&
        CapturedCandidates.Any(candidate => candidate.IsReady && !candidate.IsSelected);

    public bool CanClearSelection => !IsBatchRecording && HasSelectedCandidates;

    public bool CanRecordSelected =>
        _recordObservation is not null &&
        !IsCapturing &&
        !IsBatchRecording &&
        CapturedCandidates.Any(candidate => candidate.IsSelected && candidate.CanRecord);

    public bool CanRecordAllObservations =>
        _recordObservation is not null &&
        !IsCapturing &&
        !IsBatchRecording &&
        CapturedCandidates.Any(candidate => candidate.CanRecord);

    public bool IsBatchRecording
    {
        get => _isBatchRecording;
        private set
        {
            if (!SetField(ref _isBatchRecording, value))
            {
                return;
            }

            foreach (var candidate in CapturedCandidates)
            {
                candidate.SetSelectionLocked(value);
            }

            OnBatchRecordingCommandStateChanged();
        }
    }

    public string? BatchRecordingProgressText
    {
        get => _batchRecordingProgressText;
        private set => SetField(ref _batchRecordingProgressText, value);
    }

    public bool HasBatchRecordingProgress =>
        !string.IsNullOrWhiteSpace(BatchRecordingProgressText);

    public string? BatchRecordingSummary
    {
        get => _batchRecordingSummary;
        private set
        {
            if (SetField(ref _batchRecordingSummary, value))
            {
                OnPropertyChanged(nameof(HasBatchRecordingSummary));
            }
        }
    }

    public bool HasBatchRecordingSummary =>
        !string.IsNullOrWhiteSpace(BatchRecordingSummary);

    public bool WasCaptureTruncated => _wasCaptureTruncated;

    public string? BatchCaptureSummary => !HasCapturedCandidates
        ? null
        : BuildBatchCaptureSummary();

    public bool HasBatchCaptureSummary => !string.IsNullOrWhiteSpace(BatchCaptureSummary);

    public bool CanOpenExternal => HasSelectedSource &&
                                   !HasUnsupportedHost &&
                                   Uri.TryCreate(CurrentAddress, UriKind.Absolute, out var address) &&
                                   _trustedHostPolicy.Classify(address, SelectedSource!) == CruiseAddressTrust.Trusted;

    public bool CanOpenSelectedHistoryAtTui => TryGetSelectedHistoryAddress(out _);

    public CruiseCaptureStatus? CaptureStatus => _captureStatus;

    public string? CaptureMessage => _captureMessage;

    public bool HasCaptureMessage => !string.IsNullOrWhiteSpace(CaptureMessage);

    public IReadOnlyList<string> CaptureMissingFields => _captureMissingFields;

    public string CaptureMissingFieldsText => string.Join(", ", CaptureMissingFields);

    public bool HasCaptureMissingFields => CaptureMissingFields.Count > 0;

    public CruiseObservation? CapturedObservation => _capturedObservation;

    public bool HasCapturedObservation => CapturedObservation is not null;
    public bool CanSaveCapturedCruise => CapturedObservation is not null && Evaluation is not null && !Evaluation.IsBusy;
    public bool CanSaveSelectedHistory => History?.SelectedHistory is not null && Evaluation is not null && !Evaluation.IsBusy;
    public string SelectedHistorySaveButtonText => Evaluation?.IsSaved == true ? "Edit Evaluation" : "Save Cruise";

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

    public bool CanGo => HasStarted &&
                         HasSelectedSource &&
                         !string.IsNullOrWhiteSpace(AddressDraft) &&
                         !IsNavigating &&
                         !IsVerifying &&
                         !IsCapturing &&
                         !IsBatchRecording;

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
                AddressDraft = value;
                OnPropertyChanged(nameof(HasCurrentAddress));
                OnPropertyChanged(nameof(CurrentHost));
                OnPropertyChanged(nameof(HasCurrentHost));
                OnCommandStateChanged();
            }
        }
    }

    public bool HasCurrentAddress => !string.IsNullOrWhiteSpace(CurrentAddress);

    public string? AddressDraft
    {
        get => _addressDraft;
        set
        {
            if (SetField(ref _addressDraft, value))
            {
                OnCommandStateChanged();
            }
        }
    }

    public string NavigationHistory => string.Join(Environment.NewLine, _navigationHistory);

    public IReadOnlyList<CruiseNavigationHistoryEntryViewModel> NavigationHistoryEntries =>
        _navigationHistoryEntries;

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
        _navigationHistoryEntries.Clear();
        OnPropertyChanged(nameof(NavigationHistoryEntries));
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

    private void RequestPresentationChange(CruiseBrowserPresentation presentation)
    {
        if (presentation == BrowserPresentation || !CanChangePresentation ||
            !TryGetCurrentTrustedAddress(out var address))
        {
            return;
        }

        BrowserPresentation = presentation;
        ClearCaptureState(cancelActive: true);
        _wasNavigationStopped = false;
        IsPageReady = false;
        IsNavigating = true;
        HasUnsupportedHost = false;
        ErrorMessage = null;
        PageTitle = null;
        HasVisibleTextSample = false;
        OnPropertyChanged(nameof(VisibleTextSampleStatus));
        StatusMessage = $"Loading the selected cruise source in {presentation.ToString().ToLowerInvariant()} presentation...";

        BrowserPresentationReloadRequested?.Invoke(
            this,
            new BrowserPresentationReloadRequestedEventArgs(address, presentation));
        LoadRequested?.Invoke(this, new BrowserNavigationRequestedEventArgs(address));
    }

    private void RequestGo()
    {
        if (SelectedSource is null)
        {
            return;
        }

        var draft = AddressDraft?.Trim();
        if (string.IsNullOrWhiteSpace(draft) ||
            !Uri.TryCreate(draft, UriKind.Absolute, out var address))
        {
            RejectAddressDraft("Enter a valid HTTPS address for the trusted cruise source.");
            return;
        }

        if (_trustedHostPolicy.Classify(address, SelectedSource) != CruiseAddressTrust.Trusted)
        {
            RejectAddressDraft($"Kryten only allows browsing on {SelectedSource.TrustedHost}.");
            return;
        }

        ClearCaptureState(cancelActive: true);
        _wasNavigationStopped = false;
        IsVerifying = false;
        IsPageReady = false;
        IsNavigating = true;
        HasUnsupportedHost = false;
        ErrorMessage = null;
        PageTitle = null;
        HasVisibleTextSample = false;
        OnPropertyChanged(nameof(VisibleTextSampleStatus));
        AddressDraft = address.AbsoluteUri;
        StatusMessage = "Opening the trusted cruise page...";
        LoadRequested?.Invoke(this, new BrowserNavigationRequestedEventArgs(address));
    }

    private void RejectAddressDraft(string message)
    {
        ErrorMessage = message;
        StatusMessage = "The address was not opened.";
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
            (_batchCaptureService is null && _captureService is null) ||
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
            if (_batchCaptureService is not null)
            {
                var batchResult = await _batchCaptureService.CaptureAsync(
                    request,
                    cancellation.Token);
                if (generation != _captureGeneration || cancellation.IsCancellationRequested)
                {
                    return;
                }

                SetBatchCaptureResult(batchResult);
                return;
            }

            var result = await _captureService!.CaptureAsync(request, cancellation.Token);
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

    private void RequestOpenSelectedHistoryAtTui()
    {
        if (TryGetSelectedHistoryAddress(out var address))
        {
            ExternalOpenRequested?.Invoke(this, new BrowserNavigationRequestedEventArgs(address));
        }
    }

    private void RequestCandidateExternalOpen(Uri address)
    {
        if (SelectedSource is not null &&
            _trustedHostPolicy.Classify(address, SelectedSource) == CruiseAddressTrust.Trusted)
        {
            ExternalOpenRequested?.Invoke(this, new BrowserNavigationRequestedEventArgs(address));
        }
    }

    private async Task SaveCapturedCruiseAsync()
    {
        if (CapturedObservation is not null && Evaluation is not null)
            await Evaluation.SaveAndEditAsync(CapturedObservation);
        NotifySaveCommands();
    }

    private async Task SaveOrEditSelectedHistoryAsync()
    {
        var observation = History?.SelectedHistory?.Details.History.Observations[^1];
        if (observation is null || Evaluation is null) return;
        if (Evaluation.IsSaved) Evaluation.OpenEditor();
        else await Evaluation.SaveAndEditAsync(observation);
        NotifySaveCommands();
    }

    private void NotifySaveCommands()
    {
        OnPropertyChanged(nameof(CanSaveCapturedCruise)); OnPropertyChanged(nameof(CanSaveSelectedHistory));
        OnPropertyChanged(nameof(SelectedHistorySaveButtonText));
        _saveCapturedCruiseCommand.RaiseCanExecuteChanged(); _saveSelectedHistoryCommand.RaiseCanExecuteChanged();
    }

    private void SelectAllReady()
    {
        foreach (var candidate in CapturedCandidates)
        {
            if (candidate.IsReady)
            {
                candidate.IsSelected = true;
            }
        }

        OnSelectionChanged();
    }

    private void ClearSelection()
    {
        foreach (var candidate in CapturedCandidates)
        {
            candidate.IsSelected = false;
        }

        OnSelectionChanged();
    }

    private void OnCandidateSelectionChanged(
        CruiseCaptureCandidateReviewItemViewModel candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        OnSelectionChanged();
    }

    private void OnSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCandidateCount));
        OnPropertyChanged(nameof(HasSelectedCandidates));
        OnPropertyChanged(nameof(CanSelectAllReady));
        OnPropertyChanged(nameof(CanClearSelection));
        _selectAllReadyCommand.RaiseCanExecuteChanged();
        _clearSelectionCommand.RaiseCanExecuteChanged();
        _recordSelectedCommand.RaiseCanExecuteChanged();
    }

    private void StartBatchRecording(bool selectedOnly)
    {
        if (_recordObservation is null || IsBatchRecording)
        {
            return;
        }

        var items = CapturedCandidates
            .Where(candidate => candidate.CanRecord && (!selectedOnly || candidate.IsSelected))
            .ToArray();
        if (items.Length == 0)
        {
            return;
        }

        _batchRecordingCancellation?.Cancel();
        _batchRecordingCancellation?.Dispose();
        _batchRecordingCancellation = new CancellationTokenSource();
        var generation = ++_batchRecordingGeneration;
        BatchRecordingSummary = null;
        IsBatchRecording = true;
        _ = RecordBatchAsync(items, generation, _batchRecordingCancellation);
    }

    private async Task RecordBatchAsync(
        IReadOnlyList<CruiseCaptureCandidateReviewItemViewModel> items,
        int generation,
        CancellationTokenSource cancellation)
    {
        CruiseObservation? preferredObservation = null;
        var usefulOutcome = false;
        try
        {
            for (var index = 0; index < items.Count; index++)
            {
                if (cancellation.IsCancellationRequested || generation != _batchRecordingGeneration)
                {
                    break;
                }

                var item = items[index];
                if (!item.CanRecord || item.Observation is null)
                {
                    continue;
                }

                BatchRecordingProgressText =
                    $"Recording observation {index + 1} of {items.Count}…";
                OnPropertyChanged(nameof(HasBatchRecordingProgress));
                item.MarkRecording();
                var result = await _recordObservation!.ExecuteAsync(
                    item.Observation,
                    cancellation.Token);
                if (generation != _batchRecordingGeneration)
                {
                    return;
                }

                item.ApplyRecordingResult(result);
                if (result.Status is RecordStatus.FirstObservationRecorded
                    or RecordStatus.ChangedObservationRecorded
                    or RecordStatus.AlreadyCurrent)
                {
                    usefulOutcome = true;
                    preferredObservation ??= item.Observation;
                }

                NotifyBatchRecordingCountsChanged();
                if (result.Status == RecordStatus.Cancelled || cancellation.IsCancellationRequested)
                {
                    break;
                }
            }

            if (generation == _batchRecordingGeneration &&
                usefulOutcome &&
                preferredObservation is not null &&
                History is not null)
            {
                await History.RefreshAfterBatchRecordingAsync(preferredObservation);
            }
        }
        catch (Exception)
        {
            if (generation == _batchRecordingGeneration)
            {
                BatchRecordingSummary =
                    "Batch recording stopped unexpectedly. Retry any observation not completed.";
            }
        }
        finally
        {
            if (generation == _batchRecordingGeneration)
            {
                BatchRecordingProgressText = null;
                OnPropertyChanged(nameof(HasBatchRecordingProgress));
                BatchRecordingSummary ??= BuildBatchRecordingSummary(
                    cancellation.IsCancellationRequested);
                if (ReferenceEquals(_batchRecordingCancellation, cancellation))
                {
                    cancellation.Dispose();
                    _batchRecordingCancellation = null;
                }

                IsBatchRecording = false;
                NotifyBatchRecordingCountsChanged();
            }
        }
    }

    private void CancelBatchRecording()
    {
        if (!IsBatchRecording)
        {
            return;
        }

        _batchRecordingCancellation?.Cancel();
        BatchRecordingProgressText = "Cancelling batch recording…";
        OnPropertyChanged(nameof(HasBatchRecordingProgress));
    }

    private void InvalidateBatchRecording()
    {
        _batchRecordingGeneration++;
        _batchRecordingCancellation?.Cancel();
        _batchRecordingCancellation?.Dispose();
        _batchRecordingCancellation = null;
        _isBatchRecording = false;
        _batchRecordingProgressText = null;
        _batchRecordingSummary = null;
    }

    private string BuildBatchRecordingSummary(bool wasCancelled)
    {
        var first = CapturedCandidates.Count(candidate =>
            candidate.RecordingStatus == CruiseBatchRecordingStatus.FirstObservationRecorded);
        var changed = CapturedCandidates.Count(candidate =>
            candidate.RecordingStatus == CruiseBatchRecordingStatus.ChangedObservationRecorded);
        var current = CapturedCandidates.Count(candidate =>
            candidate.RecordingStatus == CruiseBatchRecordingStatus.AlreadyCurrent);
        var failed = CapturedCandidates.Count(candidate =>
            candidate.RecordingStatus == CruiseBatchRecordingStatus.Failed);
        var cancelled = CapturedCandidates.Count(candidate =>
            candidate.RecordingStatus == CruiseBatchRecordingStatus.Cancelled);
        var notAttempted = CapturedCandidates.Count(candidate =>
            candidate.IsReady &&
            candidate.RecordingStatus == CruiseBatchRecordingStatus.NotAttempted);
        var checkedCount = first + changed + current + failed + cancelled;
        var prefix = wasCancelled ? "Recording cancelled. " : string.Empty;
        return prefix +
               $"{checkedCount} observations checked against local history. " +
               $"{first} first, {changed} changed, {current} already current, " +
               $"{failed} failed, {cancelled} cancelled, {notAttempted} not attempted.";
    }

    private void NotifyBatchRecordingCountsChanged()
    {
        OnPropertyChanged(nameof(CanRecordSelected));
        OnPropertyChanged(nameof(CanRecordAllObservations));
        _recordSelectedCommand.RaiseCanExecuteChanged();
        _recordAllObservationsCommand.RaiseCanExecuteChanged();
    }

    private void OnBatchRecordingCommandStateChanged()
    {
        OnPropertyChanged(nameof(CanGo));
        OnPropertyChanged(nameof(CanRecordSelected));
        OnPropertyChanged(nameof(CanRecordAllObservations));
        OnPropertyChanged(nameof(CanCapture));
        OnPropertyChanged(nameof(CanSelectAllReady));
        OnPropertyChanged(nameof(CanClearSelection));
        _recordSelectedCommand.RaiseCanExecuteChanged();
        _recordAllObservationsCommand.RaiseCanExecuteChanged();
        _cancelBatchRecordingCommand.RaiseCanExecuteChanged();
        _captureCommand.RaiseCanExecuteChanged();
        _selectAllReadyCommand.RaiseCanExecuteChanged();
        _clearSelectionCommand.RaiseCanExecuteChanged();
        _goCommand.RaiseCanExecuteChanged();
    }

    private void ClearCaptureState(bool cancelActive)
    {
        InvalidateBatchRecording();
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
        if (!ReferenceEquals(_capturedObservation, observation))
        {
            Evaluation?.ClearTarget();
        }

        ClearBatchReviewState();
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
        OnPropertyChanged(nameof(CanSaveCapturedCruise));
        _saveCapturedCruiseCommand.RaiseCanExecuteChanged();
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

    private void SetBatchCaptureResult(CruiseCaptureBatchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsCompleted &&
            !result.WasTruncated &&
            result.Candidates.Count == 1 &&
            result.ReadyCount == 1)
        {
            SetCaptureResult(
                CruiseCaptureStatus.Success,
                null,
                Array.Empty<string>(),
                result.Candidates[0].Observation);
            return;
        }

        SetCaptureResult(null, result.IsCompleted ? null : result.Message, Array.Empty<string>());
        _batchCaptureStatus = result.Status;
        _wasCaptureTruncated = result.WasTruncated;
        if (result.IsCompleted)
        {
            _capturedCandidates = result.Candidates
                .Select(candidate => new CruiseCaptureCandidateReviewItemViewModel(
                    candidate,
                    CanOpenCandidateAtTui(candidate.SourceReference),
                    RequestCandidateExternalOpen,
                    OnCandidateSelectionChanged,
                    Evaluation is null ? null : Evaluation.SaveAndEditAsync))
                .ToList()
                .AsReadOnly();
        }

        NotifyBatchReviewChanged();
    }

    private bool CanOpenCandidateAtTui(string sourceReference) =>
        SelectedSource is not null &&
        Uri.TryCreate(sourceReference, UriKind.Absolute, out var address) &&
        _trustedHostPolicy.Classify(address, SelectedSource) == CruiseAddressTrust.Trusted;

    private void ClearBatchReviewState()
    {
        InvalidateBatchRecording();
        _batchCaptureStatus = null;
        _wasCaptureTruncated = false;
        _capturedCandidates = Array.Empty<CruiseCaptureCandidateReviewItemViewModel>();
        NotifyBatchReviewChanged();
    }

    private void NotifyBatchReviewChanged()
    {
        OnPropertyChanged(nameof(BatchCaptureStatus));
        OnPropertyChanged(nameof(CapturedCandidates));
        OnPropertyChanged(nameof(HasCapturedCandidates));
        OnPropertyChanged(nameof(ReadyCandidateCount));
        OnPropertyChanged(nameof(IncompleteCandidateCount));
        OnPropertyChanged(nameof(FailedCandidateCount));
        OnPropertyChanged(nameof(WasCaptureTruncated));
        OnPropertyChanged(nameof(BatchCaptureSummary));
        OnPropertyChanged(nameof(HasBatchCaptureSummary));
        OnSelectionChanged();
        NotifyBatchRecordingCountsChanged();
    }

    private string BuildBatchCaptureSummary()
    {
        var total = CapturedCandidates.Count;
        var summary = $"Captured {total} loaded cruise {(total == 1 ? "deal" : "deals")}. " +
                      $"{ReadyCandidateCount} ready, " +
                      $"{IncompleteCandidateCount} incomplete, " +
                      $"{FailedCandidateCount} failed.";
        return WasCaptureTruncated
            ? summary + Environment.NewLine +
              "Kryten captured the first 10 loaded cruise deals. " +
              "Refine the TUI results or capture another page to review more."
            : summary;
    }

    private void OnCommandStateChanged()
    {
        OnPropertyChanged(nameof(CanLoad));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanVerifyReadAccess));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
        OnPropertyChanged(nameof(CanGo));
        OnPropertyChanged(nameof(CanCapture));
        OnPropertyChanged(nameof(CanOpenExternal));
        OnPropertyChanged(nameof(CanOpenSelectedHistoryAtTui));
        OnPropertyChanged(nameof(CanChangePresentation));
        OnPropertyChanged(nameof(CaptureButtonText));
        _loadCommand.RaiseCanExecuteChanged();
        _goCommand.RaiseCanExecuteChanged();
        _backCommand.RaiseCanExecuteChanged();
        _forwardCommand.RaiseCanExecuteChanged();
        _stopCommand.RaiseCanExecuteChanged();
        _refreshCommand.RaiseCanExecuteChanged();
        _closeCommand.RaiseCanExecuteChanged();
        _verifyReadAccessCommand.RaiseCanExecuteChanged();
        _captureCommand.RaiseCanExecuteChanged();
        _cancelCaptureCommand.RaiseCanExecuteChanged();
        _openExternalCommand.RaiseCanExecuteChanged();
        _openSelectedHistoryAtTuiCommand.RaiseCanExecuteChanged();
        _saveCapturedCruiseCommand.RaiseCanExecuteChanged();
        _saveSelectedHistoryCommand.RaiseCanExecuteChanged();
        _selectAllReadyCommand.RaiseCanExecuteChanged();
        _clearSelectionCommand.RaiseCanExecuteChanged();
        _recordSelectedCommand.RaiseCanExecuteChanged();
        _recordAllObservationsCommand.RaiseCanExecuteChanged();
        _cancelBatchRecordingCommand.RaiseCanExecuteChanged();
        _selectDesktopPresentationCommand.RaiseCanExecuteChanged();
        _selectMobilePresentationCommand.RaiseCanExecuteChanged();
    }

    private void OnHistoryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CruiseHistoryViewModel.SelectedHistory) or
            nameof(CruiseHistoryViewModel.HasSelectedHistory))
        {
            OnPropertyChanged(nameof(CanOpenSelectedHistoryAtTui));
            _openSelectedHistoryAtTuiCommand.RaiseCanExecuteChanged();
            var observation = History?.SelectedHistory?.Details.History.Observations[^1];
            if (observation is null) Evaluation?.ClearTarget();
            else if (Evaluation is not null) _ = Evaluation.InspectAsync(observation);
            NotifySaveCommands();
        }
    }

    private void OnEvaluationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CruiseSaveAndEvaluationViewModel.IsSaved) or nameof(CruiseSaveAndEvaluationViewModel.IsBusy))
            NotifySaveCommands();
    }

    private bool TryGetSelectedHistoryAddress(out Uri address)
    {
        address = null!;
        var sourceReference = History?.SelectedHistory?.LatestSourceReference;
        if (!Uri.TryCreate(sourceReference, UriKind.Absolute, out var parsedAddress) ||
            AvailableSources.Count == 0 ||
            _trustedHostPolicy.Classify(parsedAddress, AvailableSources[0]) != CruiseAddressTrust.Trusted)
        {
            return false;
        }

        address = parsedAddress;
        return true;
    }

    private bool TryGetCurrentTrustedAddress(out Uri address)
    {
        address = null!;
        if (SelectedSource is null ||
            !Uri.TryCreate(CurrentAddress, UriKind.Absolute, out var parsedAddress) ||
            _trustedHostPolicy.Classify(parsedAddress, SelectedSource) != CruiseAddressTrust.Trusted)
        {
            return false;
        }

        address = parsedAddress;
        return true;
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
        _navigationHistoryEntries.Add(new CruiseNavigationHistoryEntryViewModel(address));
        if (_navigationHistory.Count > MaximumNavigationHistoryEntries)
        {
            _navigationHistory.RemoveAt(0);
            _navigationHistoryEntries.RemoveAt(0);
        }

        OnPropertyChanged(nameof(NavigationHistory));
        OnPropertyChanged(nameof(NavigationHistoryEntries));
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

public sealed class BrowserPresentationReloadRequestedEventArgs(
    Uri address,
    CruiseBrowserPresentation presentation) : EventArgs
{
    public Uri Address { get; } = address ?? throw new ArgumentNullException(nameof(address));

    public CruiseBrowserPresentation Presentation { get; } = presentation;
}
