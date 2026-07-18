extern alias KrytenApplication;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using KrytenAssist.Core.Cruises;
using Details = KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseDetails;
using DismissUseCase = KrytenApplication::KrytenAssist.Application.Cruises.DismissCruise;
using ListStatus = KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseDetailsListStatus;
using ListUseCase = KrytenApplication::KrytenAssist.Application.Cruises.ListSavedCruiseDetails;
using MutationStatus = KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseMutationStatus;
using RemoveUseCase = KrytenApplication::KrytenAssist.Application.Cruises.RemoveSavedCruise;
using RestoreUseCase = KrytenApplication::KrytenAssist.Application.Cruises.RestoreCruiseAndEvaluateCriteria;
using AlertStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertOperationStatus;
using KrytenAssist.Avalonia.Tools;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class SavedCruisesViewModel : INotifyPropertyChanged
{
    private readonly ListUseCase _list;
    private readonly DismissUseCase _dismiss;
    private readonly RestoreUseCase _restore;
    private readonly RemoveUseCase _remove;
    private readonly CruiseSaveAndEvaluationViewModel _evaluation;
    private readonly CruisePreferencesViewModel? _preferences;
    private readonly IClock _clock;
    private readonly AsyncCommand _refreshCommand;
    private readonly AsyncCommand _changeLifecycleCommand;
    private readonly DelegateCommand _requestRemoveCommand;
    private readonly AsyncCommand _confirmRemoveCommand;
    private readonly DelegateCommand _cancelRemoveCommand;
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _mutationCancellation;
    private int _loadGeneration;
    private int _mutationGeneration;
    private bool _isActive;
    private bool _isLoading;
    private bool _isMutating;
    private bool _hasLoaded;
    private string? _message;
    private string? _errorMessage;
    private SavedCruiseFilter _filter = SavedCruiseFilter.Shortlist;
    private IReadOnlyList<Details> _allDetails = [];
    private IReadOnlyList<SavedCruiseItemViewModel> _items = [];
    private SavedCruiseItemViewModel? _selectedItem;
    private bool _isRemoveConfirmationOpen;
    private bool _isPreferencesMode;

    public SavedCruisesViewModel(
        ListUseCase list,
        DismissUseCase dismiss,
        RestoreUseCase restore,
        RemoveUseCase remove,
        CruiseSaveAndEvaluationViewModel evaluation,
        IClock clock,
        CruisePreferencesViewModel? preferences = null)
    {
        _list = list;
        _dismiss = dismiss;
        _restore = restore;
        _remove = remove;
        _evaluation = evaluation;
        _clock = clock;
        _preferences = preferences;
        _refreshCommand = new AsyncCommand(RefreshAsync, () => !IsLoading && !IsMutating);
        _changeLifecycleCommand = new AsyncCommand(ChangeLifecycleAsync, () => SelectedItem is not null && !IsLoading && !IsMutating);
        _requestRemoveCommand = new DelegateCommand(RequestRemove, () => SelectedItem is not null && !IsMutating);
        _confirmRemoveCommand = new AsyncCommand(RemoveAsync, () => IsRemoveConfirmationOpen && SelectedItem is not null && !IsMutating);
        _cancelRemoveCommand = new DelegateCommand(() => IsRemoveConfirmationOpen = false, () => IsRemoveConfirmationOpen && !IsMutating);
        _evaluation.SavedCruiseChanged += OnSavedCruiseChanged;
        _evaluation.FavouriteShipChanged += OnFavouriteShipChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CruiseSaveAndEvaluationViewModel Evaluation => _evaluation;
    public CruisePreferencesViewModel? Preferences => _preferences;
    public bool IsOrganisationMode
    {
        get => !IsPreferencesMode;
        set { if (value) IsPreferencesMode = false; }
    }
    public bool IsPreferencesMode
    {
        get => _isPreferencesMode;
        set
        {
            if (_isPreferencesMode == value || (value && Preferences is null)) return;
            _isPreferencesMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOrganisationMode));
            OnPropertyChanged(nameof(ShowOrganisationItems));
            if (value)
            {
                _evaluation.ClearTarget();
                _loadGeneration++;
                CancelAndDispose(ref _loadCancellation);
                IsLoading = false;
                _ = ActivatePreferencesAsync();
            }
            else
            {
                Preferences?.Deactivate();
                if (_isActive && !HasLoaded) _ = RefreshAsync();
            }
        }
    }
    public ICommand RefreshCommand => _refreshCommand;
    public ICommand ChangeLifecycleCommand => _changeLifecycleCommand;
    public ICommand RequestRemoveCommand => _requestRemoveCommand;
    public ICommand ConfirmRemoveCommand => _confirmRemoveCommand;
    public ICommand CancelRemoveCommand => _cancelRemoveCommand;
    public IReadOnlyList<SavedCruiseItemViewModel> Items => _items;
    public bool HasItems => Items.Count > 0;
    public bool ShowOrganisationItems => IsOrganisationMode && HasItems;
    public bool HasAnySavedCruises => _allDetails.Count > 0;
    public bool IsEmpty => HasLoaded && !IsLoading && !HasAnySavedCruises && !HasError;
    public bool IsFilterEmpty => HasLoaded && !IsLoading && HasAnySavedCruises && !HasItems && !HasError;
    public string EmptyMessage => IsEmpty
        ? "No saved cruises yet. Save one from a Discovery capture or Recorded History."
        : Filter switch
        {
            SavedCruiseFilter.StrongCandidates => "No shortlisted cruises are marked Strong candidate.",
            SavedCruiseFilter.Favourites => "No shortlisted cruises or ships are marked as favourites.",
            SavedCruiseFilter.NotForUs => "No cruises have been moved to Not for us.",
            _ => "No cruises are currently on your shortlist."
        };

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (Set(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsRefreshing));
                OnPropertyChanged(nameof(ShowEmptyMessage));
                RaiseCommands();
            }
        }
    }
    public bool IsMutating { get => _isMutating; private set { if (Set(ref _isMutating, value)) RaiseCommands(); } }
    public bool HasLoaded
    {
        get => _hasLoaded;
        private set
        {
            if (Set(ref _hasLoaded, value)) OnPropertyChanged(nameof(ShowEmptyMessage));
        }
    }
    public bool IsRefreshing => IsLoading && HasLoaded;
    public string? Message { get => _message; private set => Set(ref _message, value); }
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (Set(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(ShowEmptyMessage));
            }
        }
    }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool ShowEmptyMessage => HasLoaded && !IsLoading && !HasItems && !HasError;

    public SavedCruiseFilter Filter
    {
        get => _filter;
        set
        {
            if (Set(ref _filter, value))
            {
                OnPropertyChanged(nameof(IsShortlistFilter));
                OnPropertyChanged(nameof(IsStrongCandidatesFilter));
                OnPropertyChanged(nameof(IsFavouritesFilter));
                OnPropertyChanged(nameof(IsNotForUsFilter));
                RebuildItems(SelectedItem?.SailingKey);
            }
        }
    }

    public bool IsShortlistFilter { get => Filter == SavedCruiseFilter.Shortlist; set { if (value) Filter = SavedCruiseFilter.Shortlist; } }
    public bool IsStrongCandidatesFilter { get => Filter == SavedCruiseFilter.StrongCandidates; set { if (value) Filter = SavedCruiseFilter.StrongCandidates; } }
    public bool IsFavouritesFilter { get => Filter == SavedCruiseFilter.Favourites; set { if (value) Filter = SavedCruiseFilter.Favourites; } }
    public bool IsNotForUsFilter { get => Filter == SavedCruiseFilter.NotForUs; set { if (value) Filter = SavedCruiseFilter.NotForUs; } }
    public int ShortlistCount => _allDetails.Count(item => item.SavedCruise.Status == SavedCruiseStatus.Shortlisted);
    public int StrongCandidatesCount => _allDetails.Count(item => IsStrongCandidate(item));
    public int FavouritesCount => _allDetails.Count(item => IsFavourite(item));
    public int NotForUsCount => _allDetails.Count(item => item.SavedCruise.Status == SavedCruiseStatus.Dismissed);
    public string ShortlistFilterText => $"Shortlist ({ShortlistCount})";
    public string StrongCandidatesFilterText => $"Strong candidates ({StrongCandidatesCount})";
    public string FavouritesFilterText => $"Favourites ({FavouritesCount})";
    public string NotForUsFilterText => $"Not for us ({NotForUsCount})";

    public SavedCruiseItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (Set(ref _selectedItem, value))
            {
                IsRemoveConfirmationOpen = false;
                OnPropertyChanged(nameof(HasSelectedItem));
                OnPropertyChanged(nameof(RemoveConfirmationMessage));
                RaiseCommands();
                if (value is not null)
                {
                    _evaluation.OpenSavedCruise(value.SavedCruise, value.IsFavouriteShip);
                }
                else
                {
                    _evaluation.ClearTarget();
                }
            }
        }
    }

    public bool HasSelectedItem => SelectedItem is not null;
    public bool IsRemoveConfirmationOpen { get => _isRemoveConfirmationOpen; private set { if (Set(ref _isRemoveConfirmationOpen, value)) RaiseCommands(); } }
    public string RemoveConfirmationMessage => SelectedItem is null
        ? "Remove this saved cruise?"
        : $"Remove {SelectedItem.Title} from Saved Cruises? Recorded History will not be changed.";

    public Task ActivateAsync()
    {
        _isActive = true;
        return IsPreferencesMode && Preferences is not null
            ? Preferences.ActivateAsync()
            : RefreshAsync();
    }

    public void Deactivate()
    {
        _isActive = false;
        _loadGeneration++;
        _mutationGeneration++;
        CancelAndDispose(ref _loadCancellation);
        CancelAndDispose(ref _mutationCancellation);
        IsLoading = false;
        IsMutating = false;
        IsRemoveConfirmationOpen = false;
        Preferences?.Deactivate();
        _evaluation.ClearTarget();
    }

    private async Task ActivatePreferencesAsync()
    {
        try
        {
            if (_isActive && Preferences is not null) await Preferences.ActivateAsync();
        }
        catch
        {
            // Preference operations expose controlled errors at their boundary.
        }
    }

    public async Task RefreshAsync()
    {
        var preferredKey = SelectedItem?.SailingKey;
        var generation = ++_loadGeneration;
        CancelAndDispose(ref _loadCancellation);
        _loadCancellation = new CancellationTokenSource();
        IsLoading = true;
        ErrorMessage = null;
        Message = HasLoaded ? "Refreshing saved cruises…" : "Loading saved cruises…";
        try
        {
            var result = await _list.ExecuteAsync(_loadCancellation.Token);
            if (generation != _loadGeneration || !_isActive) return;
            if (result.Status == ListStatus.Success)
            {
                _allDetails = result.Details;
                HasLoaded = true;
                Message = null;
                RebuildItems(preferredKey);
            }
            else if (result.Status == ListStatus.Cancelled)
            {
                Message = "Loading saved cruises was cancelled. You can try again.";
            }
            else
            {
                ErrorMessage = result.Message ?? "Saved cruises could not be loaded locally.";
                Message = null;
            }
        }
        finally
        {
            if (generation == _loadGeneration) IsLoading = false;
        }
    }

    public async Task ChangeLifecycleAsync()
    {
        var selected = SelectedItem;
        if (selected is null) return;
        var key = selected.SailingKey;
        var generation = BeginMutation();
        try
        {
            KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseMutationAndAlertResult? restored = null;
            KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseMutationResult mutation;
            if (selected.IsDismissed)
            {
                restored = await _restore.ExecuteAsync(
                    key,
                    _clock.Now,
                    _mutationCancellation!.Token);
                mutation = restored.Mutation;
            }
            else
            {
                mutation = await _dismiss.ExecuteAsync(
                    key,
                    _mutationCancellation!.Token);
            }

            if (!IsCurrentMutation(generation, key)) return;
            if (mutation.Status is MutationStatus.Dismissed or MutationStatus.Restored or MutationStatus.Unchanged)
            {
                var updated = mutation.SavedCruise ?? selected.SavedCruise.WithStatus(
                    selected.IsDismissed ? SavedCruiseStatus.Shortlisted : SavedCruiseStatus.Dismissed);
                ReplaceSavedCruise(updated);
                Message = mutation.Status switch
                {
                    MutationStatus.Dismissed => "Cruise moved to Not for us.",
                    MutationStatus.Restored => "Cruise restored to your shortlist.",
                    _ => "Saved cruise status was already up to date."
                };
                if (restored?.SavedCriteriaAlerts is { } alerts)
                {
                    Message = alerts.Status switch
                    {
                        AlertStatus.Cancelled => $"{Message} Saved criteria evaluation was cancelled.",
                        AlertStatus.Failed => $"{Message} Saved criteria could not be evaluated locally.",
                        _ when alerts.CreatedAlerts.Count == 1 => $"{Message} 1 alert was created.",
                        _ when alerts.CreatedAlerts.Count > 1 => $"{Message} {alerts.CreatedAlerts.Count} alerts were created.",
                        _ => Message
                    };
                }
                RebuildItems(key);
            }
            else
            {
                HandleMutationFailure(mutation.Status, key);
            }
        }
        finally
        {
            EndMutation(generation);
        }
    }

    public void RequestRemove() => IsRemoveConfirmationOpen = SelectedItem is not null;

    public async Task RemoveAsync()
    {
        var selected = SelectedItem;
        if (selected is null || !IsRemoveConfirmationOpen) return;
        var key = selected.SailingKey;
        var generation = BeginMutation();
        try
        {
            var result = await _remove.ExecuteAsync(key, _mutationCancellation!.Token);
            if (!IsCurrentMutation(generation, key)) return;
            if (result.Status == MutationStatus.Removed)
            {
                _allDetails = _allDetails.Where(item => item.SavedCruise.SailingKey != key).ToArray();
                Message = "Saved cruise removed. Recorded History was not changed.";
                IsRemoveConfirmationOpen = false;
                RebuildItems(null);
            }
            else
            {
                HandleMutationFailure(result.Status, key);
            }
        }
        finally
        {
            EndMutation(generation);
        }
    }

    private int BeginMutation()
    {
        var generation = ++_mutationGeneration;
        CancelAndDispose(ref _mutationCancellation);
        _mutationCancellation = new CancellationTokenSource();
        IsMutating = true;
        ErrorMessage = null;
        Message = null;
        return generation;
    }

    private bool IsCurrentMutation(int generation, CruiseSailingKey key) =>
        generation == _mutationGeneration && _isActive && SelectedItem?.SailingKey == key;

    private void EndMutation(int generation)
    {
        if (generation == _mutationGeneration) IsMutating = false;
    }

    private void HandleMutationFailure(MutationStatus status, CruiseSailingKey key)
    {
        if (status == MutationStatus.NotFound)
        {
            _allDetails = _allDetails.Where(item => item.SavedCruise.SailingKey != key).ToArray();
            Message = "This item is no longer saved and was removed from the list.";
            IsRemoveConfirmationOpen = false;
            RebuildItems(null);
        }
        else if (status == MutationStatus.Cancelled)
        {
            Message = "The saved-cruise change was cancelled. You can try again.";
        }
        else
        {
            ErrorMessage = "The saved-cruise change could not be completed locally. Please try again.";
        }
    }

    private void ReplaceSavedCruise(SavedCruise savedCruise)
    {
        _allDetails = _allDetails.Select(item => item.SavedCruise.SailingKey == savedCruise.SailingKey
            ? new Details(savedCruise, item.IsFavouriteShip, item.Histories)
            : item).ToArray();
    }

    private void OnSavedCruiseChanged(object? sender, SavedCruiseChangedEventArgs args)
    {
        if (!_isActive) return;
        if (!_allDetails.Any(item => item.SavedCruise.SailingKey == args.SavedCruise.SailingKey)) return;
        var selectedKey = SelectedItem?.SailingKey;
        ReplaceSavedCruise(args.SavedCruise);
        RebuildItems(selectedKey);
    }

    private void OnFavouriteShipChanged(object? sender, FavouriteCruiseShipChangedEventArgs args)
    {
        if (!_isActive) return;
        var selectedKey = SelectedItem?.SailingKey;
        _allDetails = _allDetails.Select(item => CruiseShipKey.From(item.SavedCruise.SailingKey) == args.ShipKey
            ? new Details(item.SavedCruise, args.IsFavourite, item.Histories)
            : item).ToArray();
        RebuildItems(selectedKey);
    }

    private void RebuildItems(CruiseSailingKey? preferredKey)
    {
        var filtered = _allDetails.Where(MatchesFilter)
            .OrderBy(item => item.SavedCruise.SailingKey.DepartureDate)
            .ThenBy(item => item.SavedCruise.SailingKey.OperatorId, StringComparer.Ordinal)
            .ThenBy(item => item.SavedCruise.SailingKey.ShipName, StringComparer.Ordinal)
            .ThenBy(item => item.SavedCruise.SailingKey.DurationNights)
            .ThenBy(item => item.SavedCruise.Snapshot.Title, StringComparer.Ordinal)
            .Select(item => new SavedCruiseItemViewModel(item))
            .ToArray();
        _items = filtered;
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(ShowOrganisationItems));
        OnPropertyChanged(nameof(HasAnySavedCruises));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsFilterEmpty));
        OnPropertyChanged(nameof(EmptyMessage));
        OnPropertyChanged(nameof(ShowEmptyMessage));
        NotifyCounts();
        SelectedItem = filtered.FirstOrDefault(item => item.SailingKey == preferredKey) ?? filtered.FirstOrDefault();
    }

    private bool MatchesFilter(Details item) => Filter switch
    {
        SavedCruiseFilter.Shortlist => item.SavedCruise.Status == SavedCruiseStatus.Shortlisted,
        SavedCruiseFilter.StrongCandidates => IsStrongCandidate(item),
        SavedCruiseFilter.Favourites => IsFavourite(item),
        SavedCruiseFilter.NotForUs => item.SavedCruise.Status == SavedCruiseStatus.Dismissed,
        _ => false
    };

    private static bool IsStrongCandidate(Details item) =>
        item.SavedCruise.Status == SavedCruiseStatus.Shortlisted &&
        item.SavedCruise.Evaluation.InterestLevel == CruiseInterestLevel.StrongCandidate;

    private static bool IsFavourite(Details item) =>
        item.SavedCruise.Status == SavedCruiseStatus.Shortlisted &&
        (item.SavedCruise.IsFavourite || item.IsFavouriteShip);

    private void NotifyCounts()
    {
        OnPropertyChanged(nameof(ShortlistCount));
        OnPropertyChanged(nameof(StrongCandidatesCount));
        OnPropertyChanged(nameof(FavouritesCount));
        OnPropertyChanged(nameof(NotForUsCount));
        OnPropertyChanged(nameof(ShortlistFilterText));
        OnPropertyChanged(nameof(StrongCandidatesFilterText));
        OnPropertyChanged(nameof(FavouritesFilterText));
        OnPropertyChanged(nameof(NotForUsFilterText));
    }

    private void RaiseCommands()
    {
        _refreshCommand.RaiseCanExecuteChanged();
        _changeLifecycleCommand.RaiseCanExecuteChanged();
        _requestRemoveCommand.RaiseCanExecuteChanged();
        _confirmRemoveCommand.RaiseCanExecuteChanged();
        _cancelRemoveCommand.RaiseCanExecuteChanged();
    }

    private static void CancelAndDispose(ref CancellationTokenSource? source)
    {
        source?.Cancel();
        source?.Dispose();
        source = null;
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed class DelegateCommand(Action action, Func<bool> canExecute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => canExecute();
        public void Execute(object? parameter) { if (CanExecute(parameter)) action(); }
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class AsyncCommand(Func<Task> action, Func<bool> canExecute) : ICommand
    {
        private bool _running;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => !_running && canExecute();
        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            _running = true;
            RaiseCanExecuteChanged();
            try { await action(); }
            catch { }
            finally { _running = false; RaiseCanExecuteChanged(); }
        }
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
