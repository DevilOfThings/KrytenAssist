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
using ListDetails = KrytenApplication::KrytenAssist.Application.Cruises.ListFirstObservedCruiseItineraryDetails;
using OperationStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseDiscoveryOperationStatus;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseNewItinerariesViewModel : INotifyPropertyChanged
{
    private readonly ListDetails _list;
    private readonly CruiseDiscoverySourceCatalog _sources;
    private readonly CruiseTrustedHostPolicy _hostPolicy;
    private readonly AsyncCommand _refreshCommand;
    private readonly DelegateCommand _cancelCommand;
    private readonly DelegateCommand _openCommand;
    private CancellationTokenSource? _cancellation;
    private IReadOnlyList<CruiseNewItineraryItemViewModel> _items = [];
    private CruiseNewItineraryItemViewModel? _selectedItem;
    private bool _isLoading;
    private bool _hasLoaded;
    private bool _isStale = true;
    private int _generation;
    private string? _message;
    private string? _errorMessage;

    public CruiseNewItinerariesViewModel(ListDetails list, CruiseDiscoverySourceCatalog sources, CruiseTrustedHostPolicy hostPolicy)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list)); _sources = sources ?? throw new ArgumentNullException(nameof(sources)); _hostPolicy = hostPolicy ?? throw new ArgumentNullException(nameof(hostPolicy));
        _refreshCommand = new AsyncCommand(RefreshAsync, () => !IsLoading);
        _cancelCommand = new DelegateCommand(Cancel, () => IsLoading);
        _openCommand = new DelegateCommand(OpenSelected, () => CanOpenSelectedInDiscovery);
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<Uri>? OpenInDiscoveryRequested;
    public ICommand RefreshCommand => _refreshCommand;
    public ICommand CancelLoadingCommand => _cancelCommand;
    public ICommand OpenInDiscoveryCommand => _openCommand;
    public IReadOnlyList<CruiseNewItineraryItemViewModel> Items => _items;
    public bool HasItems => Items.Count > 0;
    public bool IsEmpty => HasLoaded && !IsLoading && !HasItems && !HasError;
    public bool HasLoaded => _hasLoaded;
    public bool IsLoading { get => _isLoading; private set { if (Set(ref _isLoading, value)) { _refreshCommand.Raise(); _cancelCommand.Raise(); } } }
    public string? Message { get => _message; private set => Set(ref _message, value); }
    public string? ErrorMessage { get => _errorMessage; private set { if (Set(ref _errorMessage, value)) Changed(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string EmptyMessage => "No first-observed itineraries yet. Record one supported capture to seed a baseline, then record a later comparable capture. Kryten does not monitor TUI in the background.";
    public CruiseNewItineraryItemViewModel? SelectedItem { get => _selectedItem; set { if (Set(ref _selectedItem, value)) { Changed(nameof(HasSelection)); Changed(nameof(CanOpenSelectedInDiscovery)); _openCommand.Raise(); } } }
    public bool HasSelection => SelectedItem is not null;
    public bool CanOpenSelectedInDiscovery => TryGetTrustedAddress(out _);

    public async Task ActivateAsync() { if (!_hasLoaded || _isStale) await RefreshAsync(); }
    public void Deactivate() => Cancel();
    public void Invalidate() => _isStale = true;
    public async Task RefreshAfterRecordingAsync() { Invalidate(); await RefreshAsync(); }
    private async Task RefreshAsync()
    {
        Cancel(); var generation = ++_generation; _cancellation = new(); var token = _cancellation.Token; var selectedKey = SelectedItem?.CatalogueKey;
        IsLoading = true; ErrorMessage = null; Message = "Loading local New Itineraries…";
        try
        {
            var result = await _list.ExecuteAsync(token);
            if (generation != _generation) return;
            if (result.Status == OperationStatus.Success)
            {
                _items = result.Items.Select(item => new CruiseNewItineraryItemViewModel(item)).ToArray();
                Changed(nameof(Items)); Changed(nameof(HasItems)); Changed(nameof(IsEmpty));
                SelectedItem = _items.FirstOrDefault(item => item.CatalogueKey == selectedKey) ?? _items.FirstOrDefault();
                _hasLoaded = true; _isStale = false; Message = _items.Count == 0 ? null : $"{_items.Count} first-observed itinerary/itineraries.";
            }
            else if (result.Status == OperationStatus.Cancelled) Message = "Loading New Itineraries was cancelled.";
            else { ErrorMessage = result.Message ?? "New Itineraries could not be loaded locally."; Message = null; }
        }
        catch (OperationCanceledException) { if (generation == _generation) Message = "Loading New Itineraries was cancelled."; }
        finally { if (generation == _generation && _cancellation?.Token == token) { _cancellation.Dispose(); _cancellation = null; IsLoading = false; Changed(nameof(IsEmpty)); } }
    }
    private void Cancel() { ++_generation; _cancellation?.Cancel(); _cancellation?.Dispose(); _cancellation = null; IsLoading = false; }
    private void OpenSelected() { if (TryGetTrustedAddress(out var address)) OpenInDiscoveryRequested?.Invoke(this, address); }
    private bool TryGetTrustedAddress(out Uri address)
    {
        address = null!; var reference = SelectedItem?.SourceReference;
        if (!Uri.TryCreate(reference, UriKind.Absolute, out var parsed)) return false;
        var source = _sources.Sources.FirstOrDefault(value => string.Equals(value.TrustedHost, parsed.Host, StringComparison.OrdinalIgnoreCase));
        if (source is null || _hostPolicy.Classify(parsed, source) != CruiseAddressTrust.Trusted) return false;
        address = parsed; return true;
    }
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; Changed(name); return true; }
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    private sealed class AsyncCommand(Func<Task> execute, Func<bool> canExecute) : ICommand { public event EventHandler? CanExecuteChanged; public bool CanExecute(object? p) => canExecute(); public async void Execute(object? p) => await execute(); public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    private sealed class DelegateCommand(Action execute, Func<bool> canExecute) : ICommand { public event EventHandler? CanExecuteChanged; public bool CanExecute(object? p) => canExecute(); public void Execute(object? p) => execute(); public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
}
