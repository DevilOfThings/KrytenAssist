extern alias KrytenApplication;

using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using KrytenAssist.Core.Cruises;
using GetSettings = KrytenApplication::KrytenAssist.Application.Cruises.GetCruiseAlertSettings;
using SaveSettings = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruiseAlertSettings;
using OperationStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertOperationStatus;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseAlertSettingsViewModel : INotifyPropertyChanged
{
    private readonly GetSettings _get;
    private readonly SaveSettings _save;
    private readonly AsyncCommand _saveCommand;
    private readonly DelegateCommand _cancelCommand;
    private readonly DelegateCommand _cancelOperationCommand;
    private CancellationTokenSource? _cancellation;
    private CruiseAlertSettings? _confirmed;
    private int _generation;
    private bool _active;
    private bool _loaded;
    private bool _busy;
    private bool _priceDropEnabled;
    private bool _promotionEnabled;
    private bool _savedCriteriaEnabled;
    private string _minimumPercentage = "0";
    private string? _validationError;
    private string? _message;
    private string? _errorMessage;

    public CruiseAlertSettingsViewModel(GetSettings get, SaveSettings save)
    {
        _get = get;
        _save = save;
        _saveCommand = new AsyncCommand(SaveAsync, () => HasLoaded && HasUnsavedChanges && !IsBusy);
        _cancelCommand = new DelegateCommand(CancelChanges, () => HasLoaded && HasUnsavedChanges && !IsBusy);
        _cancelOperationCommand = new DelegateCommand(CancelOperation, () => IsBusy);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ICommand SaveCommand => _saveCommand;
    public ICommand CancelChangesCommand => _cancelCommand;
    public ICommand CancelOperationCommand => _cancelOperationCommand;
    public bool HasLoaded { get => _loaded; private set => Set(ref _loaded, value); }
    public bool IsBusy { get => _busy; private set { if (Set(ref _busy, value)) RaiseCommands(); } }
    public bool PriceDropEnabled { get => _priceDropEnabled; set { if (Set(ref _priceDropEnabled, value)) DraftChanged(); } }
    public bool PromotionEnabled { get => _promotionEnabled; set { if (Set(ref _promotionEnabled, value)) DraftChanged(); } }
    public bool SavedCriteriaEnabled { get => _savedCriteriaEnabled; set { if (Set(ref _savedCriteriaEnabled, value)) DraftChanged(); } }
    public string MinimumPriceDropPercentage { get => _minimumPercentage; set { if (Set(ref _minimumPercentage, value ?? string.Empty)) DraftChanged(); } }
    public string? ValidationError { get => _validationError; private set { if (Set(ref _validationError, value)) Changed(nameof(HasValidationError)); } }
    public bool HasValidationError => ValidationError is not null;
    public string? Message { get => _message; private set => Set(ref _message, value); }
    public string? ErrorMessage { get => _errorMessage; private set { if (Set(ref _errorMessage, value)) Changed(nameof(HasError)); } }
    public bool HasError => ErrorMessage is not null;
    public bool HasUnsavedChanges => _confirmed is not null && (!TryBuild(false, out var draft) || draft != _confirmed);
    public string UnsavedChangesText => HasUnsavedChanges ? "Unsaved changes" : "All changes saved";

    public Task ActivateAsync()
    {
        _active = true;
        return HasLoaded ? Task.CompletedTask : LoadAsync();
    }

    public void Deactivate()
    {
        _active = false;
        CancelOperation(false);
    }

    public async Task LoadAsync()
    {
        var (generation, token) = Begin();
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await _get.ExecuteAsync(token);
            if (!Current(generation)) return;
            if (result.Status == OperationStatus.Success && result.Settings is not null)
            {
                _confirmed = result.Settings;
                Apply(result.Settings);
                HasLoaded = true;
            }
            else if (result.Status == OperationStatus.Failed)
            {
                ErrorMessage = "Alert settings could not be loaded locally. Please try again.";
            }
        }
        finally { if (generation == _generation) IsBusy = false; }
    }

    public async Task SaveAsync()
    {
        if (!TryBuild(true, out var draft) || !HasLoaded || IsBusy) return;
        var (generation, token) = Begin();
        IsBusy = true;
        ErrorMessage = null;
        Message = "Saving alert settings…";
        try
        {
            var result = await _save.ExecuteAsync(draft, token);
            if (!Current(generation)) return;
            if (result.Status is OperationStatus.Updated or OperationStatus.Unchanged && result.Settings is not null)
            {
                _confirmed = result.Settings;
                Apply(result.Settings);
                Message = result.Status == OperationStatus.Updated ? "Alert settings saved." : "These alert settings are already saved.";
            }
            else if (result.Status == OperationStatus.Failed)
            {
                ErrorMessage = "Alert settings could not be saved locally. Please try again.";
                Message = null;
            }
        }
        finally { if (generation == _generation) IsBusy = false; }
    }

    public void CancelChanges()
    {
        if (_confirmed is null || IsBusy) return;
        Apply(_confirmed);
        Message = "Alert setting changes cancelled.";
        ErrorMessage = null;
    }

    public void CancelOperation() => CancelOperation(true);

    private void CancelOperation(bool showMessage)
    {
        ++_generation;
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
        IsBusy = false;
        if (showMessage) Message = "Alert settings operation cancelled. You can try again.";
    }

    private bool TryBuild(bool validate, out CruiseAlertSettings settings)
    {
        if (string.IsNullOrWhiteSpace(MinimumPriceDropPercentage) ||
            !(decimal.TryParse(MinimumPriceDropPercentage, NumberStyles.Number, CultureInfo.CurrentCulture, out var percentage) ||
              decimal.TryParse(MinimumPriceDropPercentage, NumberStyles.Number, CultureInfo.InvariantCulture, out percentage)) ||
            percentage is < 0 or > 100)
        {
            if (validate) ValidationError = "Enter a minimum percentage from 0 through 100.";
            settings = new CruiseAlertSettings();
            return false;
        }
        if (validate) ValidationError = null;
        settings = new CruiseAlertSettings(PriceDropEnabled, PromotionEnabled, SavedCriteriaEnabled, percentage);
        return true;
    }

    private void Apply(CruiseAlertSettings settings)
    {
        _priceDropEnabled = settings.PriceDropEnabled;
        _promotionEnabled = settings.PromotionEnabled;
        _savedCriteriaEnabled = settings.SavedCriteriaEnabled;
        _minimumPercentage = settings.MinimumPriceDropPercentage.ToString("0.############################", CultureInfo.CurrentCulture);
        ValidationError = null;
        Changed(nameof(PriceDropEnabled)); Changed(nameof(PromotionEnabled)); Changed(nameof(SavedCriteriaEnabled)); Changed(nameof(MinimumPriceDropPercentage));
        DraftChanged(false);
    }

    private void DraftChanged(bool clearMessages = true)
    {
        ValidationError = null;
        if (clearMessages) { Message = null; ErrorMessage = null; }
        Changed(nameof(HasUnsavedChanges)); Changed(nameof(UnsavedChangesText)); RaiseCommands();
    }

    private (int, CancellationToken) Begin()
    {
        ++_generation;
        _cancellation?.Cancel(); _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();
        return (_generation, _cancellation.Token);
    }
    private bool Current(int generation) => _active && generation == _generation;
    private void RaiseCommands() { _saveCommand.Raise(); _cancelCommand.Raise(); _cancelOperationCommand.Raise(); }
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (Equals(field, value)) return false; field = value; Changed(name); return true; }
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed class DelegateCommand(Action action, Func<bool> canExecute) : ICommand
    { public event EventHandler? CanExecuteChanged; public bool CanExecute(object? p) => canExecute(); public void Execute(object? p) { if (CanExecute(p)) action(); } public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    private sealed class AsyncCommand(Func<Task> action, Func<bool> canExecute) : ICommand
    { public event EventHandler? CanExecuteChanged; public bool CanExecute(object? p) => canExecute(); public async void Execute(object? p) { if (CanExecute(p)) await action(); } public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
}
