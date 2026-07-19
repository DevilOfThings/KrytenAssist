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
using ListHistories = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseCabinHistories;
using GetPreferences = KrytenApplication::KrytenAssist.Application.Cruises.GetCruisePreferences;
using CabinStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinOperationStatus;
using PreferenceStatus = KrytenApplication::KrytenAssist.Application.Cruises.PersonalCruisePreferenceQueryStatus;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseCabinAvailabilityViewModel : INotifyPropertyChanged
{
    private readonly ListHistories _listHistories;
    private readonly GetPreferences _getPreferences;
    private readonly AsyncCommand _refreshCommand;
    private readonly DelegateCommand _cancelCommand;
    private CancellationTokenSource? _cancellation;
    private int _generation;
    private bool _active;
    private bool _loaded;
    private bool _loading;
    private CruiseCabinAvailabilityItemViewModel? _selectedItem;
    private string? _message;
    private string? _errorMessage;
    private string? _preferenceMessage;

    public CruiseCabinAvailabilityViewModel(ListHistories listHistories, GetPreferences getPreferences)
    {
        _listHistories = listHistories ?? throw new ArgumentNullException(nameof(listHistories));
        _getPreferences = getPreferences ?? throw new ArgumentNullException(nameof(getPreferences));
        _refreshCommand = new AsyncCommand(RefreshAsync, () => !IsLoading);
        _cancelCommand = new DelegateCommand(CancelLoading, () => IsLoading);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<CruiseCabinAvailabilityItemViewModel> Items { get; } = [];
    public ICommand RefreshCommand => _refreshCommand;
    public ICommand CancelLoadingCommand => _cancelCommand;
    public bool HasItems => Items.Count > 0;
    public bool HasLoaded => _loaded;
    public bool HasSelection => SelectedItem is not null;
    public bool IsLoading { get => _loading; private set { if (Set(ref _loading, value)) RaiseCommands(); } }
    public CruiseCabinAvailabilityItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set { if (Set(ref _selectedItem, value)) Changed(nameof(HasSelection)); }
    }
    public string? Message { get => _message; private set => Set(ref _message, value); }
    public string? ErrorMessage { get => _errorMessage; private set { if (Set(ref _errorMessage, value)) Changed(nameof(HasError)); } }
    public bool HasError => ErrorMessage is not null;
    public string? PreferenceMessage { get => _preferenceMessage; private set { if (Set(ref _preferenceMessage, value)) Changed(nameof(HasPreferenceMessage)); } }
    public bool HasPreferenceMessage => PreferenceMessage is not null;
    public string EmptyMessage => !_loaded
        ? "Cabin observations are loaded from local storage."
        : "No cabin observations have been recorded yet. Capture a supported TUI result in Discovery, review its cabin evidence, then choose Record Cabin Observation. Kryten does not monitor cabin availability in the background.";

    public async Task ActivateAsync()
    {
        _active = true;
        await RefreshAsync();
    }

    public void Deactivate()
    {
        _active = false;
        CancelLoading(false);
    }

    public async Task RefreshAsync()
    {
        if (IsLoading) return;
        var generation = ++_generation;
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;
        IsLoading = true;
        Message = null;
        ErrorMessage = null;
        try
        {
            var historiesTask = _listHistories.ExecuteAsync(token);
            var preferencesTask = _getPreferences.ExecuteAsync(token);
            await Task.WhenAll(historiesTask, preferencesTask);
            if (!_active || generation != _generation) return;

            var histories = await historiesTask;
            if (histories.Status == CabinStatus.Cancelled)
            {
                Message = "Loading cabin availability was cancelled. You can try again.";
                return;
            }
            if (histories.Status != CabinStatus.Success)
            {
                ErrorMessage = "Cabin availability could not be loaded locally. Please try again.";
                return;
            }

            var preferenceResult = await preferencesTask;
            var preferenceUnavailable = preferenceResult.Status != PreferenceStatus.Success || preferenceResult.Preferences is null;
            var preferences = preferenceUnavailable ? null : preferenceResult.Preferences;
            PreferenceMessage = preferenceUnavailable
                ? "Cabin history is available, but preference matching is temporarily unavailable."
                : null;
            var selectedKey = SelectedItem?.SeriesKey;
            var projections = histories.Histories.Select(details =>
                    new CruiseCabinAvailabilityItemViewModel(details, preferences, preferenceUnavailable))
                .OrderBy(item => item.DepartureDate)
                .ThenBy(item => item.OperatorId, StringComparer.Ordinal)
                .ThenBy(item => item.ShipName, StringComparer.Ordinal)
                .ThenBy(item => item.RetailSourceId, StringComparer.Ordinal)
                .ThenBy(item => item.ContextFingerprint, StringComparer.Ordinal)
                .ThenBy(item => item.SeriesKey, StringComparer.Ordinal)
                .ToArray();
            Items.Clear();
            foreach (var item in projections) Items.Add(item);
            SelectedItem = selectedKey is null ? projections.FirstOrDefault() :
                projections.FirstOrDefault(item => item.SeriesKey == selectedKey) ?? projections.FirstOrDefault();
            _loaded = true;
            Changed(nameof(HasItems));
            Changed(nameof(HasLoaded));
            Changed(nameof(EmptyMessage));
        }
        catch (OperationCanceledException)
        {
            if (_active && generation == _generation)
                Message = "Loading cabin availability was cancelled. You can try again.";
        }
        finally
        {
            if (generation == _generation) IsLoading = false;
        }
    }

    public void CancelLoading() => CancelLoading(true);

    private void CancelLoading(bool showMessage)
    {
        ++_generation;
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
        IsLoading = false;
        if (showMessage) Message = "Loading cabin availability was cancelled. You can try again.";
    }

    private void RaiseCommands() { _refreshCommand.Raise(); _cancelCommand.Raise(); }
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; Changed(name); return true; }
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed class DelegateCommand(Action action, Func<bool> canExecute) : ICommand
    { public event EventHandler? CanExecuteChanged; public bool CanExecute(object? p) => canExecute(); public void Execute(object? p) { if (CanExecute(p)) action(); } public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    private sealed class AsyncCommand(Func<Task> action, Func<bool> canExecute) : ICommand
    { public event EventHandler? CanExecuteChanged; public bool CanExecute(object? p) => canExecute(); public async void Execute(object? p) { if (CanExecute(p)) await action(); } public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
}
