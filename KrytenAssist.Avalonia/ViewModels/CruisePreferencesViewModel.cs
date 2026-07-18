extern alias KrytenApplication;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using KrytenAssist.Core.Cruises;
using GetPreferences = KrytenApplication::KrytenAssist.Application.Cruises.GetCruisePreferences;
using MutationStatus = KrytenApplication::KrytenAssist.Application.Cruises.PersonalCruisePreferenceMutationStatus;
using QueryStatus = KrytenApplication::KrytenAssist.Application.Cruises.PersonalCruisePreferenceQueryStatus;
using SavePreferences = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruisePreferences;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruisePreferencesViewModel : INotifyPropertyChanged
{
    private readonly GetPreferences _getPreferences;
    private readonly SavePreferences _savePreferences;
    private readonly AsyncCommand _saveCommand;
    private readonly AsyncCommand _retryCommand;
    private readonly DelegateCommand _cancelChangesCommand;
    private readonly DelegateCommand _clearDraftCommand;
    private readonly DelegateCommand _cancelOperationCommand;
    private CancellationTokenSource? _operationCancellation;
    private int _generation;
    private bool _isActive;
    private bool _isApplying;
    private bool _hasLoaded;
    private bool _isLoading;
    private bool _isSaving;
    private bool _isMaximumBudgetEnabled;
    private string? _maximumBudgetAmount;
    private string _maximumBudgetCurrency = "GBP";
    private string _budgetBasis = "Per person";
    private string? _message;
    private string? _errorMessage;
    private string? _budgetAmountError;
    private string? _currencyError;
    private string? _basisError;
    private CruisePreferences? _confirmed;

    public CruisePreferencesViewModel(GetPreferences getPreferences, SavePreferences savePreferences)
    {
        _getPreferences = getPreferences;
        _savePreferences = savePreferences;
        MonthOptions = Enumerable.Range(1, 12)
            .Select(month => new CruisePreferenceMonthOptionViewModel(
                month,
                CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.GetMonthName(month),
                DraftChanged))
            .ToArray();
        CabinOptions = Enum.GetValues<CruiseCabinType>()
            .Select(cabin => new CruisePreferenceCabinOptionViewModel(cabin, CabinLabel(cabin), DraftChanged))
            .ToArray();
        _saveCommand = new AsyncCommand(SaveAsync, () => HasLoaded && HasUnsavedChanges && !IsBusy);
        _retryCommand = new AsyncCommand(LoadAsync, () => !IsBusy && !HasLoaded);
        _cancelChangesCommand = new DelegateCommand(CancelChanges, () => HasLoaded && HasUnsavedChanges && !IsBusy);
        _clearDraftCommand = new DelegateCommand(ClearDraft, () => HasLoaded && !IsBusy);
        _cancelOperationCommand = new DelegateCommand(CancelOperation, () => IsBusy);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<CruisePreferenceMonthOptionViewModel> MonthOptions { get; }
    public IReadOnlyList<CruisePreferenceCabinOptionViewModel> CabinOptions { get; }
    public IReadOnlyList<string> BudgetBasisOptions { get; } = ["Per person", "Total booking"];
    public ICommand SaveCommand => _saveCommand;
    public ICommand RetryCommand => _retryCommand;
    public ICommand CancelChangesCommand => _cancelChangesCommand;
    public ICommand ClearDraftCommand => _clearDraftCommand;
    public ICommand CancelOperationCommand => _cancelOperationCommand;
    public bool IsBusy => IsLoading || IsSaving;
    public bool CanEdit => HasLoaded && !IsBusy;

    public bool HasLoaded
    {
        get => _hasLoaded;
        private set
        {
            if (Set(ref _hasLoaded, value))
            {
                OnPropertyChanged(nameof(CanEdit));
                RaiseCommands();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (Set(ref _isLoading, value)) BusyChanged();
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (Set(ref _isSaving, value)) BusyChanged();
        }
    }

    public bool IsMaximumBudgetEnabled
    {
        get => _isMaximumBudgetEnabled;
        set
        {
            if (Set(ref _isMaximumBudgetEnabled, value)) DraftChanged();
        }
    }

    public string? MaximumBudgetAmount
    {
        get => _maximumBudgetAmount;
        set
        {
            if (Set(ref _maximumBudgetAmount, value)) DraftChanged();
        }
    }

    public string MaximumBudgetCurrency
    {
        get => _maximumBudgetCurrency;
        set
        {
            if (Set(ref _maximumBudgetCurrency, value ?? string.Empty)) DraftChanged();
        }
    }

    public string BudgetBasis
    {
        get => _budgetBasis;
        set
        {
            if (Set(ref _budgetBasis, value ?? string.Empty)) DraftChanged();
        }
    }

    public string? Message { get => _message; private set => Set(ref _message, value); }
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (Set(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError));
        }
    }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string? BudgetAmountError { get => _budgetAmountError; private set => SetValidation(ref _budgetAmountError, value); }
    public string? CurrencyError { get => _currencyError; private set => SetValidation(ref _currencyError, value); }
    public string? BasisError { get => _basisError; private set => SetValidation(ref _basisError, value); }
    public bool HasValidationError => BudgetAmountError is not null || CurrencyError is not null || BasisError is not null;
    public string ValidationSummary => "Check the maximum budget fields and try again.";
    public bool HasUnsavedChanges => HasLoaded && !DraftMatchesConfirmed();
    public string UnsavedChangesText => HasUnsavedChanges ? "Unsaved changes" : "All changes saved";
    public int SelectedMonthCount => MonthOptions.Count(option => option.IsSelected);
    public int SelectedCabinCount => CabinOptions.Count(option => option.IsSelected);
    public string MonthSummary => SelectedMonthCount == 0
        ? "No preferred months set — all months remain open."
        : $"{SelectedMonthCount} preferred month{Plural(SelectedMonthCount)}";
    public string CabinSummary => SelectedCabinCount == 0
        ? "No preferred cabin types set."
        : $"{SelectedCabinCount} preferred cabin type{Plural(SelectedCabinCount)}";

    public Task ActivateAsync()
    {
        _isActive = true;
        return HasLoaded ? Task.CompletedTask : LoadAsync();
    }

    public void Deactivate()
    {
        _isActive = false;
        InvalidateOperation();
    }

    public async Task LoadAsync()
    {
        if (IsBusy) return;
        var (generation, token) = BeginOperation();
        IsLoading = true;
        ErrorMessage = null;
        Message = "Loading cruise preferences…";
        try
        {
            var result = await _getPreferences.ExecuteAsync(token);
            if (!IsCurrent(generation)) return;
            if (result.Status == QueryStatus.Success && result.Preferences is not null)
            {
                _confirmed = result.Preferences;
                ApplyConfirmed();
                HasLoaded = true;
                Message = null;
            }
            else if (result.Status == QueryStatus.Cancelled)
            {
                Message = "Loading cruise preferences was cancelled. You can try again.";
            }
            else
            {
                ErrorMessage = "Cruise preferences could not be loaded locally. Please try again.";
                Message = null;
            }
        }
        finally
        {
            if (generation == _generation) IsLoading = false;
        }
    }

    public async Task SaveAsync()
    {
        if (!HasLoaded || IsBusy) return;
        if (!TryBuildDraft(out var draft))
        {
            Message = null;
            ErrorMessage = null;
            return;
        }

        var (generation, token) = BeginOperation();
        IsSaving = true;
        ErrorMessage = null;
        Message = "Saving cruise preferences…";
        try
        {
            var result = await _savePreferences.ExecuteAsync(draft, token);
            if (!IsCurrent(generation)) return;
            if (result.Status is MutationStatus.Updated or MutationStatus.Unchanged)
            {
                _confirmed = draft;
                ApplyConfirmed();
                Message = result.Status == MutationStatus.Updated
                    ? "Cruise preferences saved."
                    : "These cruise preferences are already saved.";
            }
            else if (result.Status == MutationStatus.Cancelled)
            {
                Message = "Saving cruise preferences was cancelled. You can try again.";
            }
            else
            {
                ErrorMessage = "Cruise preferences could not be saved locally. Please try again.";
                Message = null;
            }
        }
        finally
        {
            if (generation == _generation) IsSaving = false;
        }
    }

    public void CancelChanges()
    {
        if (_confirmed is null || IsBusy) return;
        ApplyConfirmed();
        Message = "Preference changes cancelled.";
        ErrorMessage = null;
    }

    public void ClearDraft()
    {
        if (!HasLoaded || IsBusy) return;
        _isApplying = true;
        foreach (var month in MonthOptions) month.IsSelected = false;
        foreach (var cabin in CabinOptions) cabin.IsSelected = false;
        IsMaximumBudgetEnabled = false;
        MaximumBudgetAmount = null;
        MaximumBudgetCurrency = "GBP";
        BudgetBasis = "Per person";
        _isApplying = false;
        ClearValidation();
        Message = "Preference draft cleared. Choose Save Preferences to persist it.";
        DraftChanged();
    }

    public void CancelOperation()
    {
        if (!IsBusy) return;
        InvalidateOperation();
        Message = "Preference operation cancelled. You can try again.";
        IsLoading = false;
        IsSaving = false;
    }

    private bool TryBuildDraft(out CruisePreferences draft)
    {
        ClearValidation();
        CruiseBudget? budget = null;
        if (IsMaximumBudgetEnabled)
        {
            if (string.IsNullOrWhiteSpace(MaximumBudgetAmount))
            {
                BudgetAmountError = "Enter a maximum budget amount.";
            }
            else if (!TryParseAmount(MaximumBudgetAmount, out var amount) || amount < 0)
            {
                BudgetAmountError = "Maximum budget must be a non-negative number.";
            }

            var currency = MaximumBudgetCurrency.Trim().ToUpperInvariant();
            if (currency.Length != 3 || !currency.All(char.IsAsciiLetter))
            {
                CurrencyError = "Currency must contain exactly three letters.";
            }

            var basis = ParseBasis(BudgetBasis);
            if (basis is null)
            {
                BasisError = "Choose whether the budget is per person or total booking.";
            }

            if (!HasValidationError)
            {
                TryParseAmount(MaximumBudgetAmount!, out var amount);
                budget = new CruiseBudget(amount, currency, basis!.Value);
            }
        }

        if (HasValidationError)
        {
            OnPropertyChanged(nameof(ValidationSummary));
            draft = new CruisePreferences();
            return false;
        }

        draft = new CruisePreferences(
            MonthOptions.Where(option => option.IsSelected).Select(option => option.Month),
            CabinOptions.Where(option => option.IsSelected).Select(option => option.Cabin),
            budget);
        return true;
    }

    private bool DraftMatchesConfirmed()
    {
        if (_confirmed is null) return true;
        if (!TryBuildDraftForComparison(out var draft)) return false;
        return _confirmed.Equals(draft);
    }

    private bool TryBuildDraftForComparison(out CruisePreferences draft)
    {
        CruiseBudget? budget = null;
        if (IsMaximumBudgetEnabled)
        {
            var currency = MaximumBudgetCurrency.Trim().ToUpperInvariant();
            var basis = ParseBasis(BudgetBasis);
            if (!TryParseAmount(MaximumBudgetAmount, out var amount) || amount < 0 ||
                currency.Length != 3 || !currency.All(char.IsAsciiLetter) || basis is null)
            {
                draft = new CruisePreferences();
                return false;
            }
            budget = new CruiseBudget(amount, currency, basis.Value);
        }

        draft = new CruisePreferences(
            MonthOptions.Where(option => option.IsSelected).Select(option => option.Month),
            CabinOptions.Where(option => option.IsSelected).Select(option => option.Cabin),
            budget);
        return true;
    }

    private void ApplyConfirmed()
    {
        var preferences = _confirmed ?? new CruisePreferences();
        _isApplying = true;
        foreach (var option in MonthOptions) option.IsSelected = preferences.DepartureMonths.Contains(option.Month);
        foreach (var option in CabinOptions) option.IsSelected = preferences.PreferredCabins.Contains(option.Cabin);
        IsMaximumBudgetEnabled = preferences.MaximumBudget is not null;
        MaximumBudgetAmount = preferences.MaximumBudget?.Amount.ToString("0.############################", CultureInfo.CurrentCulture);
        MaximumBudgetCurrency = preferences.MaximumBudget?.Currency ?? "GBP";
        BudgetBasis = preferences.MaximumBudget?.Basis == CruiseBudgetBasis.TotalBooking ? "Total booking" : "Per person";
        _isApplying = false;
        ClearValidation();
        NotifyDraft();
    }

    private void DraftChanged()
    {
        if (_isApplying) return;
        if (IsBusy)
        {
            InvalidateOperation();
            IsLoading = false;
            IsSaving = false;
        }
        ClearValidation();
        Message = null;
        ErrorMessage = null;
        NotifyDraft();
    }

    private void NotifyDraft()
    {
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(UnsavedChangesText));
        OnPropertyChanged(nameof(SelectedMonthCount));
        OnPropertyChanged(nameof(SelectedCabinCount));
        OnPropertyChanged(nameof(MonthSummary));
        OnPropertyChanged(nameof(CabinSummary));
        RaiseCommands();
    }

    private void ClearValidation()
    {
        BudgetAmountError = null;
        CurrencyError = null;
        BasisError = null;
    }

    private (int Generation, CancellationToken Token) BeginOperation()
    {
        _generation++;
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        _operationCancellation = new CancellationTokenSource();
        return (_generation, _operationCancellation.Token);
    }

    private void InvalidateOperation()
    {
        _generation++;
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        _operationCancellation = null;
    }

    private bool IsCurrent(int generation) => generation == _generation && _isActive;

    private void BusyChanged()
    {
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(CanEdit));
        RaiseCommands();
    }

    private void RaiseCommands()
    {
        _saveCommand.RaiseCanExecuteChanged();
        _retryCommand.RaiseCanExecuteChanged();
        _cancelChangesCommand.RaiseCanExecuteChanged();
        _clearDraftCommand.RaiseCanExecuteChanged();
        _cancelOperationCommand.RaiseCanExecuteChanged();
    }

    private bool SetValidation(ref string? field, string? value, [CallerMemberName] string? name = null)
    {
        if (!Set(ref field, value, name)) return false;
        OnPropertyChanged(nameof(HasValidationError));
        return true;
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

    private static bool TryParseAmount(string? value, out decimal amount) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out amount) ||
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);

    private static CruiseBudgetBasis? ParseBasis(string value) => value switch
    {
        "Per person" => CruiseBudgetBasis.PerPerson,
        "Total booking" => CruiseBudgetBasis.TotalBooking,
        _ => null
    };

    private static string CabinLabel(CruiseCabinType cabin) => cabin switch
    {
        CruiseCabinType.Inside => "Inside",
        CruiseCabinType.Outside => "Outside",
        CruiseCabinType.Balcony => "Balcony",
        CruiseCabinType.Suite => "Suite",
        CruiseCabinType.Solo => "Solo",
        _ => cabin.ToString()
    };

    private static string Plural(int value) => value == 1 ? string.Empty : "s";

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
