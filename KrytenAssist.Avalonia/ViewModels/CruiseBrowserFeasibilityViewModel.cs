using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseBrowserFeasibilityViewModel : INotifyPropertyChanged
{
    private const int MaximumNavigationHistoryEntries = 12;

    public static readonly Uri StartingAddress = new(
        "https://www.tui.co.uk/cruise/deals/marella-cruise-of-the-week",
        UriKind.Absolute);

    private readonly DelegateCommand _loadCommand;
    private readonly DelegateCommand _stopCommand;
    private readonly DelegateCommand _refreshCommand;
    private readonly DelegateCommand _closeCommand;
    private readonly DelegateCommand _verifyReadAccessCommand;
    private bool _hasStarted;
    private bool _isNavigating;
    private bool _isPageReady;
    private bool _isVerifying;
    private string _statusMessage = "The TUI page has not been loaded.";
    private string? _errorMessage;
    private string? _currentAddress;
    private string? _pageTitle;
    private bool _hasVisibleTextSample;
    private readonly List<string> _navigationHistory = [];
    private readonly List<string> _cruiseLinks = [];

    public CruiseBrowserFeasibilityViewModel()
    {
        _loadCommand = new DelegateCommand(RequestLoad, () => CanLoad);
        _stopCommand = new DelegateCommand(RequestStop, () => IsNavigating);
        _refreshCommand = new DelegateCommand(RequestRefresh, () => CanRefresh);
        _closeCommand = new DelegateCommand(RequestClose, () => HasStarted);
        _verifyReadAccessCommand = new DelegateCommand(
            RequestReadAccessVerification,
            () => CanVerifyReadAccess);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<BrowserNavigationRequestedEventArgs>? LoadRequested;

    public event EventHandler? StopRequested;

    public event EventHandler? RefreshRequested;

    public event EventHandler? CloseRequested;

    public event EventHandler? ReadAccessVerificationRequested;

    public ICommand LoadCommand => _loadCommand;

    public ICommand StopCommand => _stopCommand;

    public ICommand RefreshCommand => _refreshCommand;

    public ICommand CloseCommand => _closeCommand;

    public ICommand VerifyReadAccessCommand => _verifyReadAccessCommand;

    public bool HasStarted
    {
        get => _hasStarted;
        private set => SetField(ref _hasStarted, value);
    }

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
        HasStarted = true;
        IsVerifying = false;
        IsPageReady = false;
        IsNavigating = true;
        ErrorMessage = null;
        PageTitle = null;
        HasVisibleTextSample = false;
        OnPropertyChanged(nameof(VisibleTextSampleStatus));
        CurrentAddress = address?.AbsoluteUri ?? CurrentAddress;
        RecordNavigationAddress(address);
        StatusMessage = "Loading the embedded TUI page...";
    }

    public void ReportNavigationCompleted(Uri? address)
    {
        HasStarted = true;
        IsNavigating = false;
        IsPageReady = true;
        ErrorMessage = null;
        CurrentAddress = address?.AbsoluteUri ?? CurrentAddress;
        RecordNavigationAddress(address);
        StatusMessage = "The embedded page is ready.";
    }

    public void ReportNavigationFailed(Uri? address)
    {
        HasStarted = true;
        IsNavigating = false;
        IsPageReady = false;
        CurrentAddress = address?.AbsoluteUri ?? CurrentAddress;
        RecordNavigationAddress(address);
        StatusMessage = "The embedded page could not be loaded.";
        ErrorMessage = "Check your connection and try loading the TUI page again.";
    }

    public void ReportNavigationStopped()
    {
        IsNavigating = false;
        IsPageReady = false;
        StatusMessage = "Navigation was stopped.";
        ErrorMessage = null;
    }

    public void ReportReadAccessSucceeded(
        string? pageTitle,
        string? currentAddress,
        bool hasVisibleTextSample,
        IReadOnlyList<string>? cruiseLinks = null)
    {
        IsVerifying = false;
        ErrorMessage = null;
        PageTitle = string.IsNullOrWhiteSpace(pageTitle) ? null : pageTitle.Trim();
        CurrentAddress = string.IsNullOrWhiteSpace(currentAddress)
            ? CurrentAddress
            : currentAddress.Trim();
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
        StatusMessage = "Read-only page access could not be verified.";
        ErrorMessage = "The embedded page did not return the requested diagnostics.";
    }

    public void ReportBrowserOperationFailed()
    {
        IsNavigating = false;
        IsVerifying = false;
        IsPageReady = false;
        StatusMessage = "The embedded browser operation failed.";
        ErrorMessage = "The embedded browser is unavailable. Please try again.";
    }

    public void ReportBrowserClosed()
    {
        HasStarted = false;
        IsNavigating = false;
        IsVerifying = false;
        IsPageReady = false;
        ErrorMessage = null;
        CurrentAddress = null;
        _navigationHistory.Clear();
        OnPropertyChanged(nameof(NavigationHistory));
        OnPropertyChanged(nameof(HasNavigationHistory));
        _cruiseLinks.Clear();
        OnPropertyChanged(nameof(CruiseLinks));
        OnPropertyChanged(nameof(HasCruiseLinks));
        PageTitle = null;
        HasVisibleTextSample = false;
        OnPropertyChanged(nameof(VisibleTextSampleStatus));
        StatusMessage = "The TUI page has not been loaded.";
        OnCommandStateChanged();
    }

    private void RequestLoad()
    {
        HasStarted = true;
        IsPageReady = false;
        IsNavigating = true;
        ErrorMessage = null;
        CurrentAddress = StartingAddress.AbsoluteUri;
        RecordNavigationAddress(StartingAddress);
        StatusMessage = "Preparing the embedded TUI page...";
        LoadRequested?.Invoke(this, new BrowserNavigationRequestedEventArgs(StartingAddress));
    }

    private void RequestStop()
    {
        StopRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RequestRefresh()
    {
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

    private void OnCommandStateChanged()
    {
        OnPropertyChanged(nameof(CanLoad));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanVerifyReadAccess));
        _loadCommand.RaiseCanExecuteChanged();
        _stopCommand.RaiseCanExecuteChanged();
        _refreshCommand.RaiseCanExecuteChanged();
        _closeCommand.RaiseCanExecuteChanged();
        _verifyReadAccessCommand.RaiseCanExecuteChanged();
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

                if (!Uri.TryCreate(value, UriKind.Absolute, out var address) ||
                    address.Scheme != Uri.UriSchemeHttps ||
                    !string.Equals(address.Host, "www.tui.co.uk", StringComparison.OrdinalIgnoreCase) ||
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

public sealed class BrowserNavigationRequestedEventArgs(Uri address) : EventArgs
{
    public Uri Address { get; } = address ?? throw new ArgumentNullException(nameof(address));
}
