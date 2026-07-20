extern alias KrytenApplication;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using KrytenAssist.Core.Cruises;
using ListAlerts = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseAlerts;
using ChangeStatus = KrytenApplication::KrytenAssist.Application.Cruises.ChangeCruiseAlertStatus;
using AlertQuery = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertQuery;
using OperationStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertOperationStatus;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseAlertCentreViewModel : INotifyPropertyChanged
{
    private readonly ListAlerts _list;
    private readonly ChangeStatus _changeStatus;
    private readonly CruiseAlertCoordinator _coordinator;
    private readonly AsyncCommand _refreshCommand;
    private readonly DelegateCommand _cancelCommand;
    private readonly AsyncCommand _markReadCommand;
    private readonly AsyncCommand _markUnreadCommand;
    private readonly AsyncCommand _dismissCommand;
    private readonly AsyncCommand _restoreCommand;
    private readonly List<CruiseAlertItemViewModel> _snapshot = [];
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _mutationCancellation;
    private int _loadGeneration;
    private int _mutationGeneration;
    private bool _active;
    private bool _loaded;
    private bool _loading;
    private bool _mutating;
    private CruiseAlertCentreMode _mode;
    private CruiseAlertTypeFilter _typeFilter;
    private CruiseAlertLifecycleFilter _lifecycleFilter;
    private CruiseAlertItemViewModel? _selected;
    private string? _message;
    private string? _errorMessage;

    public CruiseAlertCentreViewModel(ListAlerts list, ChangeStatus changeStatus, CruiseAlertCoordinator coordinator, CruiseAlertSettingsViewModel settings)
    {
        _list = list;
        _changeStatus = changeStatus;
        _coordinator = coordinator;
        Settings = settings;
        _refreshCommand = new AsyncCommand(RefreshAsync, () => !IsLoading);
        _cancelCommand = new DelegateCommand(CancelLoading, () => IsLoading);
        _markReadCommand = MutationCommand(CruiseAlertStatus.Read, () => SelectedItem?.IsUnread == true);
        _markUnreadCommand = MutationCommand(CruiseAlertStatus.Unread, () => SelectedItem?.IsRead == true);
        _dismissCommand = MutationCommand(CruiseAlertStatus.Dismissed, () => SelectedItem is { IsDismissed: false });
        _restoreCommand = MutationCommand(CruiseAlertStatus.Unread, () => SelectedItem?.IsDismissed == true);
        _coordinator.AlertsChanged += CoordinatorOnAlertsChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<CruiseAlertItemViewModel> Items { get; } = [];
    public CruiseAlertSettingsViewModel Settings { get; }
    public ICommand RefreshCommand => _refreshCommand;
    public ICommand CancelLoadingCommand => _cancelCommand;
    public ICommand MarkReadCommand => _markReadCommand;
    public ICommand MarkUnreadCommand => _markUnreadCommand;
    public ICommand DismissCommand => _dismissCommand;
    public ICommand RestoreCommand => _restoreCommand;
    public bool IsInboxMode { get => Mode == CruiseAlertCentreMode.Inbox; set { if (value) Mode = CruiseAlertCentreMode.Inbox; } }
    public bool IsSettingsMode { get => Mode == CruiseAlertCentreMode.Settings; set { if (value) Mode = CruiseAlertCentreMode.Settings; } }
    public CruiseAlertCentreMode Mode
    {
        get => _mode;
        private set
        {
            if (!Set(ref _mode, value)) return;
            Changed(nameof(IsInboxMode)); Changed(nameof(IsSettingsMode));
            if (value == CruiseAlertCentreMode.Settings && _active) _ = Settings.ActivateAsync();
        }
    }
    public CruiseAlertTypeFilter TypeFilter { get => _typeFilter; set { if (Set(ref _typeFilter, value)) { NotifyFilterBooleans(); ApplyFilters(); } } }
    public CruiseAlertLifecycleFilter LifecycleFilter { get => _lifecycleFilter; set { if (Set(ref _lifecycleFilter, value)) { NotifyFilterBooleans(); ApplyFilters(); } } }
    public bool IsAllTypes { get => TypeFilter == CruiseAlertTypeFilter.All; set { if (value) TypeFilter = CruiseAlertTypeFilter.All; } }
    public bool IsPriceDrops { get => TypeFilter == CruiseAlertTypeFilter.PriceDrops; set { if (value) TypeFilter = CruiseAlertTypeFilter.PriceDrops; } }
    public bool IsPromotions { get => TypeFilter == CruiseAlertTypeFilter.Promotions; set { if (value) TypeFilter = CruiseAlertTypeFilter.Promotions; } }
    public bool IsSavedCriteria { get => TypeFilter == CruiseAlertTypeFilter.SavedCriteria; set { if (value) TypeFilter = CruiseAlertTypeFilter.SavedCriteria; } }
    public bool IsCabinAvailability { get => TypeFilter == CruiseAlertTypeFilter.CabinAvailability; set { if (value) TypeFilter = CruiseAlertTypeFilter.CabinAvailability; } }
    public bool IsNewItineraries { get => TypeFilter == CruiseAlertTypeFilter.NewItineraries; set { if (value) TypeFilter = CruiseAlertTypeFilter.NewItineraries; } }
    public bool IsActiveLifecycle { get => LifecycleFilter == CruiseAlertLifecycleFilter.Active; set { if (value) LifecycleFilter = CruiseAlertLifecycleFilter.Active; } }
    public bool IsUnreadLifecycle { get => LifecycleFilter == CruiseAlertLifecycleFilter.Unread; set { if (value) LifecycleFilter = CruiseAlertLifecycleFilter.Unread; } }
    public bool IsReadLifecycle { get => LifecycleFilter == CruiseAlertLifecycleFilter.Read; set { if (value) LifecycleFilter = CruiseAlertLifecycleFilter.Read; } }
    public bool IsDismissedLifecycle { get => LifecycleFilter == CruiseAlertLifecycleFilter.Dismissed; set { if (value) LifecycleFilter = CruiseAlertLifecycleFilter.Dismissed; } }
    public CruiseAlertItemViewModel? SelectedItem { get => _selected; set { if (Set(ref _selected, value)) SelectionChanged(); } }
    public bool HasSelection => SelectedItem is not null;
    public bool HasItems => Items.Count > 0;
    public bool HasLoaded => _loaded;
    public bool IsLoading { get => _loading; private set { if (Set(ref _loading, value)) RaiseCommands(); } }
    public bool IsMutating { get => _mutating; private set { if (Set(ref _mutating, value)) RaiseCommands(); } }
    public string? Message { get => _message; private set => Set(ref _message, value); }
    public string? ErrorMessage { get => _errorMessage; private set { if (Set(ref _errorMessage, value)) Changed(nameof(HasError)); } }
    public bool HasError => ErrorMessage is not null;
    public string EmptyMessage => !_loaded
        ? "Alerts are loaded from local storage."
        : _snapshot.Count == 0
            ? "No alerts yet. Alerts are created only from Cruise evidence you explicitly record or save. Kryten is not monitoring retailer sites in the background."
            : LifecycleFilter switch
            {
                CruiseAlertLifecycleFilter.Active => "No active alerts match these filters.",
                CruiseAlertLifecycleFilter.Unread => "No unread alerts match these filters.",
                CruiseAlertLifecycleFilter.Read => "No read alerts match these filters.",
                CruiseAlertLifecycleFilter.Dismissed => "No dismissed alerts match these filters.",
                _ => "No alerts match these filters."
            };

    public async Task ActivateAsync()
    {
        _active = true;
        await RefreshAsync();
        if (Mode == CruiseAlertCentreMode.Settings) await Settings.ActivateAsync();
    }

    public void Deactivate()
    {
        _active = false;
        CancelLoading(false);
        ++_mutationGeneration;
        _mutationCancellation?.Cancel(); _mutationCancellation?.Dispose(); _mutationCancellation = null;
        IsMutating = false;
        Settings.Deactivate();
    }

    public async Task RefreshAsync()
    {
        var generation = ++_loadGeneration;
        _loadCancellation?.Cancel(); _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        var token = _loadCancellation.Token;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var result = await _list.ExecuteAsync(new AlertQuery(), token);
            if (!_active || generation != _loadGeneration) return;
            if (result.Status == OperationStatus.Success)
            {
                var selectedId = SelectedItem?.Id;
                _snapshot.Clear();
                _snapshot.AddRange(result.Alerts.Select(alert => new CruiseAlertItemViewModel(alert)));
                _loaded = true;
                ApplyFilters(selectedId);
                await _coordinator.RefreshCountAsync();
            }
            else if (result.Status == OperationStatus.Failed)
            {
                ErrorMessage = "Cruise alerts could not be loaded locally. Please try again.";
            }
        }
        finally { if (generation == _loadGeneration) IsLoading = false; }
    }

    public void CancelLoading() => CancelLoading(true);

    private void CancelLoading(bool showMessage)
    {
        ++_loadGeneration;
        _loadCancellation?.Cancel(); _loadCancellation?.Dispose(); _loadCancellation = null;
        IsLoading = false;
        if (showMessage) Message = "Loading alerts was cancelled. You can try again.";
    }

    private AsyncCommand MutationCommand(CruiseAlertStatus status, Func<bool> selectedPredicate) =>
        new(() => ChangeStatusAsync(status), () => !IsMutating && selectedPredicate());

    private async Task ChangeStatusAsync(CruiseAlertStatus status)
    {
        var target = SelectedItem;
        if (target is null || IsMutating) return;
        var generation = ++_mutationGeneration;
        _mutationCancellation?.Cancel(); _mutationCancellation?.Dispose();
        _mutationCancellation = new CancellationTokenSource();
        IsMutating = true;
        ErrorMessage = null;
        try
        {
            var result = await _changeStatus.ExecuteAsync(target.Id, status, _mutationCancellation.Token);
            if (!_active || generation != _mutationGeneration || SelectedItem?.Id != target.Id) return;
            if (result.Status is OperationStatus.Updated or OperationStatus.Unchanged && result.Alert is not null)
            {
                var index = _snapshot.FindIndex(item => item.Id == target.Id);
                if (index >= 0) _snapshot[index] = new CruiseAlertItemViewModel(result.Alert);
                ApplyFilters(target.Id);
                Message = status switch
                {
                    CruiseAlertStatus.Read => "Alert marked read.",
                    CruiseAlertStatus.Unread when target.IsDismissed => "Alert restored as unread.",
                    CruiseAlertStatus.Unread => "Alert marked unread.",
                    _ => "Alert dismissed."
                };
                await _coordinator.RefreshCountAsync();
            }
            else if (result.Status == OperationStatus.NotFound)
            {
                _snapshot.RemoveAll(item => item.Id == target.Id);
                ApplyFilters();
                Message = "That alert no longer exists locally.";
                await _coordinator.RefreshCountAsync();
            }
            else if (result.Status == OperationStatus.Failed)
            {
                ErrorMessage = "The alert could not be changed locally. Please try again.";
            }
        }
        finally { if (generation == _mutationGeneration) IsMutating = false; }
    }

    private void ApplyFilters(Guid? preferredId = null)
    {
        preferredId ??= SelectedItem?.Id;
        var filtered = _snapshot.Where(Matches).ToArray();
        Items.Clear();
        foreach (var item in filtered) Items.Add(item);
        SelectedItem = preferredId is { } id ? filtered.FirstOrDefault(item => item.Id == id) ?? filtered.FirstOrDefault() : filtered.FirstOrDefault();
        Changed(nameof(HasItems)); Changed(nameof(EmptyMessage)); Changed(nameof(HasLoaded));
    }

    private bool Matches(CruiseAlertItemViewModel item)
    {
        var type = TypeFilter switch
        {
            CruiseAlertTypeFilter.PriceDrops => item.Type == CruiseAlertType.PriceDrop,
            CruiseAlertTypeFilter.Promotions => item.Type == CruiseAlertType.Promotion,
            CruiseAlertTypeFilter.SavedCriteria => item.Type == CruiseAlertType.SavedCriteria,
            CruiseAlertTypeFilter.CabinAvailability => item.Type == CruiseAlertType.CabinAvailability,
            CruiseAlertTypeFilter.NewItineraries => item.Type == CruiseAlertType.NewItinerary,
            _ => true
        };
        var lifecycle = LifecycleFilter switch
        {
            CruiseAlertLifecycleFilter.Active => item.Status != CruiseAlertStatus.Dismissed,
            CruiseAlertLifecycleFilter.Unread => item.Status == CruiseAlertStatus.Unread,
            CruiseAlertLifecycleFilter.Read => item.Status == CruiseAlertStatus.Read,
            CruiseAlertLifecycleFilter.Dismissed => item.Status == CruiseAlertStatus.Dismissed,
            _ => false
        };
        return type && lifecycle;
    }

    private void CoordinatorOnAlertsChanged(object? sender, EventArgs e) { if (_active) _ = RefreshAsync(); }
    private void NotifyFilterBooleans()
    {
        Changed(nameof(IsAllTypes)); Changed(nameof(IsPriceDrops)); Changed(nameof(IsPromotions)); Changed(nameof(IsSavedCriteria)); Changed(nameof(IsCabinAvailability)); Changed(nameof(IsNewItineraries));
        Changed(nameof(IsActiveLifecycle)); Changed(nameof(IsUnreadLifecycle)); Changed(nameof(IsReadLifecycle)); Changed(nameof(IsDismissedLifecycle));
    }
    private void SelectionChanged() { Changed(nameof(HasSelection)); RaiseCommands(); }
    private void RaiseCommands() { _refreshCommand.Raise(); _cancelCommand.Raise(); _markReadCommand.Raise(); _markUnreadCommand.Raise(); _dismissCommand.Raise(); _restoreCommand.Raise(); }
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; Changed(name); return true; }
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed class DelegateCommand(Action action, Func<bool> canExecute) : ICommand
    { public event EventHandler? CanExecuteChanged; public bool CanExecute(object? p) => canExecute(); public void Execute(object? p) { if (CanExecute(p)) action(); } public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    private sealed class AsyncCommand(Func<Task> action, Func<bool> canExecute) : ICommand
    { public event EventHandler? CanExecuteChanged; public bool CanExecute(object? p) => canExecute(); public async void Execute(object? p) { if (CanExecute(p)) await action(); } public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
}
