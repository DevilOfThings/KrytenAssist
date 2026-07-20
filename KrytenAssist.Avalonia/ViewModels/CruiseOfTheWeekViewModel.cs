extern alias KrytenApplication;

using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using KrytenAssist.Avalonia.Cruises.Discovery;
using KrytenAssist.Avalonia.Skills.Models;
using KrytenAssist.Avalonia.Skills.Services;
using KrytenAssist.Avalonia.Tools;
using KrytenAssist.Core.Cruises;
using ICruisePageCaptureService = KrytenApplication::KrytenAssist.Application.Cruises.ICruisePageCaptureService;
using ICruisePageBatchCaptureService = KrytenApplication::KrytenAssist.Application.Cruises.ICruisePageBatchCaptureService;
using RecordObservation = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseObservationAndEvaluateAlerts;
using ICabinPageCaptureService = KrytenApplication::KrytenAssist.Application.Cruises.ICruiseCabinPageCaptureService;
using RecordCabinObservation = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseCabinObservationAndEvaluateAlerts;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseOfTheWeekViewModel : INotifyPropertyChanged
{
    private const string SkillId = "cruise.of-the-week";
    private const string GetCurrentOperation = "get-current";
    private const string DefaultErrorMessage =
        "Cruise of the Week could not be retrieved. Please try again.";
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-GB");

    private readonly ISkill? _skill;
    private readonly IClock _clock;
    private readonly AsyncCommand _retrieveCommand;
    private readonly DelegateCommand _cancelCommand;
    private CancellationTokenSource? _activeRequest;
    private CruiseObservation? _observation;
    private bool _isBusy;
    private string? _errorMessage;
    private CruiseWorkspaceMode _workspaceMode;

    public CruiseOfTheWeekViewModel(
        ISkillRegistry skillRegistry,
        IClock clock,
        CruiseDiscoverySourceCatalog? sourceCatalog = null,
        CruiseTrustedHostPolicy? trustedHostPolicy = null,
        ICruisePageCaptureService? captureService = null,
        CruiseHistoryViewModel? history = null,
        ICruisePageBatchCaptureService? batchCaptureService = null,
        RecordObservation? recordObservation = null,
        CruiseSaveAndEvaluationViewModel? evaluation = null,
        SavedCruisesViewModel? savedCruises = null,
        CruiseAlertCentreViewModel? alertCentre = null,
        CruiseAlertCoordinator? alertCoordinator = null,
        ICabinPageCaptureService? cabinCaptureService = null,
        RecordCabinObservation? recordCabinObservation = null,
        CruiseCabinAvailabilityViewModel? cabinAvailability = null,
        CruiseNewItinerariesViewModel? newItineraries = null,
        CruiseItineraryCaptureReviewViewModel? itineraryReview = null)
    {
        ArgumentNullException.ThrowIfNull(skillRegistry);
        ArgumentNullException.ThrowIfNull(clock);

        _skill = skillRegistry.Find(SkillId);
        _clock = clock;
        var sharedEvaluation = savedCruises?.Evaluation ?? evaluation;
        BrowserFeasibility = new CruiseBrowserFeasibilityViewModel(
            sourceCatalog ?? new CruiseDiscoverySourceCatalog(),
            trustedHostPolicy ?? new CruiseTrustedHostPolicy(),
            captureService,
            clock,
            history,
            batchCaptureService,
            recordObservation,
            sharedEvaluation,
            alertCoordinator,
            cabinCaptureService,
            recordCabinObservation,
            itineraryReview);
        SavedCruises = savedCruises;
        AlertCentre = alertCentre;
        CabinAvailability = cabinAvailability;
        NewItineraries = newItineraries;
        AlertCoordinator = alertCoordinator;
        _retrieveCommand = new AsyncCommand(RetrieveAsync, () => CanRetrieve);
        _cancelCommand = new DelegateCommand(Cancel, () => IsBusy);
        if (AlertCoordinator is not null)
        {
            AlertCoordinator.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CruiseAlertCoordinator.BadgeText))
                    OnPropertyChanged(nameof(AlertsModeText));
            };
        }
        if (NewItineraries is not null)
            NewItineraries.OpenInDiscoveryRequested += (_, address) =>
            {
                WorkspaceMode = CruiseWorkspaceMode.Discovery;
                BrowserFeasibility.OpenTrustedDiscoveryAddress(address);
            };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand RetrieveCommand => _retrieveCommand;

    public ICommand CancelCommand => _cancelCommand;

    public CruiseBrowserFeasibilityViewModel BrowserFeasibility { get; }

    public CruiseHistoryViewModel? History => BrowserFeasibility.History;

    public SavedCruisesViewModel? SavedCruises { get; }
    public CruiseAlertCentreViewModel? AlertCentre { get; }
    public CruiseAlertCoordinator? AlertCoordinator { get; }
    public CruiseCabinAvailabilityViewModel? CabinAvailability { get; }
    public CruiseNewItinerariesViewModel? NewItineraries { get; }
    public string AlertsModeText => AlertCoordinator?.BadgeText ?? "Alerts";

    public CruiseWorkspaceMode WorkspaceMode
    {
        get => _workspaceMode;
        private set
        {
            if (_workspaceMode == value) return;
            _workspaceMode = value;
            BrowserFeasibility.Evaluation?.ClearTarget();
            SavedCruises?.Deactivate();
            AlertCentre?.Deactivate();
            CabinAvailability?.Deactivate();
            NewItineraries?.Deactivate();
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDiscoveryMode));
            OnPropertyChanged(nameof(IsSavedCruisesMode));
            OnPropertyChanged(nameof(IsAlertsMode));
            OnPropertyChanged(nameof(IsCabinAvailabilityMode));
            OnPropertyChanged(nameof(IsNewItinerariesMode));
            if (value == CruiseWorkspaceMode.Discovery) History?.Activate();
            else History?.Deactivate();
            if (value == CruiseWorkspaceMode.SavedCruises) _ = ActivateSavedCruisesAsync();
            if (value == CruiseWorkspaceMode.Alerts) _ = ActivateAlertsAsync();
            if (value == CruiseWorkspaceMode.CabinAvailability) _ = ActivateCabinAvailabilityAsync();
            if (value == CruiseWorkspaceMode.NewItineraries) _ = ActivateNewItinerariesAsync();
        }
    }

    public bool IsDiscoveryMode
    {
        get => WorkspaceMode == CruiseWorkspaceMode.Discovery;
        set
        {
            if (value)
            {
                WorkspaceMode = CruiseWorkspaceMode.Discovery;
            }
        }
    }

    public bool IsSavedCruisesMode
    {
        get => WorkspaceMode == CruiseWorkspaceMode.SavedCruises;
        set
        {
            if (value && SavedCruises is not null) WorkspaceMode = CruiseWorkspaceMode.SavedCruises;
        }
    }

    public bool IsAlertsMode
    {
        get => WorkspaceMode == CruiseWorkspaceMode.Alerts;
        set { if (value && AlertCentre is not null) WorkspaceMode = CruiseWorkspaceMode.Alerts; }
    }

    public bool IsCabinAvailabilityMode
    {
        get => WorkspaceMode == CruiseWorkspaceMode.CabinAvailability;
        set { if (value && CabinAvailability is not null) WorkspaceMode = CruiseWorkspaceMode.CabinAvailability; }
    }

    public bool IsNewItinerariesMode
    {
        get => WorkspaceMode == CruiseWorkspaceMode.NewItineraries;
        set { if (value && NewItineraries is not null) WorkspaceMode = CruiseWorkspaceMode.NewItineraries; }
    }

    public void Activate()
    {
        _ = AlertCoordinator?.RefreshCountAsync();
        if (IsSavedCruisesMode)
        {
            _ = ActivateSavedCruisesAsync();
        }
        else if (IsAlertsMode)
        {
            _ = ActivateAlertsAsync();
        }
        else if (IsCabinAvailabilityMode)
        {
            _ = ActivateCabinAvailabilityAsync();
        }
        else if (IsNewItinerariesMode)
        {
            _ = ActivateNewItinerariesAsync();
        }
        else
        {
            History?.Activate();
        }
    }

    public void Deactivate()
    {
        History?.Deactivate();
        SavedCruises?.Deactivate();
        BrowserFeasibility.Evaluation?.Deactivate();
        AlertCentre?.Deactivate();
        CabinAvailability?.Deactivate();
        NewItineraries?.Deactivate();
        AlertCoordinator?.Cancel();
    }

    private async Task ActivateSavedCruisesAsync()
    {
        try
        {
            if (SavedCruises is not null)
            {
                await SavedCruises.ActivateAsync();
            }
        }
        catch
        {
            // Child ViewModels expose controlled errors; keep an unexpected
            // presentation exception at this mode-switch boundary.
        }
    }

    private async Task ActivateAlertsAsync()
    {
        try
        {
            if (AlertCentre is not null) await AlertCentre.ActivateAsync();
        }
        catch
        {
            // The child exposes controlled local failures.
        }
    }

    private async Task ActivateCabinAvailabilityAsync()
    {
        try
        {
            if (CabinAvailability is not null) await CabinAvailability.ActivateAsync();
        }
        catch
        {
            // The child exposes controlled local failures.
        }
    }

    private async Task ActivateNewItinerariesAsync()
    {
        try { if (NewItineraries is not null) await NewItineraries.ActivateAsync(); }
        catch { /* Child exposes controlled local failures. */ }
    }

    public CruiseObservation? Observation
    {
        get => _observation;
        private set
        {
            if (ReferenceEquals(_observation, value))
            {
                return;
            }

            _observation = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasObservation));
            OnPropertyChanged(nameof(RetrieveButtonText));
            OnPropertyChanged(nameof(Summary));
            OnPropertyChanged(nameof(CruiseTitle));
            OnPropertyChanged(nameof(ShipName));
            OnPropertyChanged(nameof(DepartureDateText));
            OnPropertyChanged(nameof(DeparturePort));
            OnPropertyChanged(nameof(HasDeparturePort));
            OnPropertyChanged(nameof(DurationText));
            OnPropertyChanged(nameof(PriceText));
            OnPropertyChanged(nameof(PriceBasis));
            OnPropertyChanged(nameof(HasPriceBasis));
            OnPropertyChanged(nameof(PromotionSummary));
            OnPropertyChanged(nameof(HasPromotionSummary));
            OnPropertyChanged(nameof(ProviderName));
            OnPropertyChanged(nameof(SourceReference));
            OnPropertyChanged(nameof(HasSourceReference));
            OnPropertyChanged(nameof(ObservedAtText));
        }
    }

    public bool HasObservation => Observation is not null;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRetrieve));
            _retrieveCommand.RaiseCanExecuteChanged();
            _cancelCommand.RaiseCanExecuteChanged();
        }
    }

    public bool CanRetrieve => !IsBusy;

    public string RetrieveButtonText => HasObservation
        ? "Refresh"
        : "Get Cruise of the Week";

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (string.Equals(_errorMessage, value, StringComparison.Ordinal))
            {
                return;
            }

            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string? Summary => Observation is null
        ? null
        : CreateSummary(Observation);

    public string? CruiseTitle => Observation?.Snapshot.Offer.Title;

    public string? ShipName => Observation?.Snapshot.Offer.ShipName;

    public string? DepartureDateText => Observation?.Snapshot.Offer.DepartureDate
        .ToString("d MMMM yyyy", DisplayCulture);

    public string? DeparturePort => Observation?.Snapshot.Offer.DeparturePort;

    public bool HasDeparturePort => !string.IsNullOrWhiteSpace(DeparturePort);

    public string? DurationText => Observation is null
        ? null
        : FormatDuration(Observation.Snapshot.Offer.DurationNights);

    public string? PriceText => Observation is null
        ? null
        : FormatPrice(Observation.Snapshot.Prices[0]);

    public string? PriceBasis => Observation?.Snapshot.Prices[0].Basis;

    public bool HasPriceBasis => !string.IsNullOrWhiteSpace(PriceBasis);

    public string? PromotionSummary => Observation?.Snapshot.PromotionSummary;

    public bool HasPromotionSummary => !string.IsNullOrWhiteSpace(PromotionSummary);

    public string? ProviderName => Observation?.Snapshot.Offer.Provider.Name;

    public string? SourceReference => Observation?.SourceReference;

    public bool HasSourceReference => !string.IsNullOrWhiteSpace(SourceReference);

    public string? ObservedAtText => Observation?.ObservedAt
        .ToString("d MMMM yyyy 'at' HH:mm zzz", DisplayCulture);

    public async Task RetrieveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;

        if (_skill is null)
        {
            ErrorMessage = "Cruise of the Week is currently unavailable.";
            return;
        }

        using var request = new CancellationTokenSource();
        _activeRequest = request;
        IsBusy = true;

        try
        {
            var requestedAt = _clock.Now;
            var result = await _skill.ExecuteAsync(
                new SkillRequest(GetCurrentOperation),
                new SkillContext(requestedAt),
                request.Token);

            if (!result.IsSuccess)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? DefaultErrorMessage
                    : result.Message;
                return;
            }

            if (result.Data is not CruiseObservation observation)
            {
                ErrorMessage = DefaultErrorMessage;
                return;
            }

            Observation = observation;
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested)
        {
            ErrorMessage = null;
        }
        catch (Exception)
        {
            ErrorMessage = DefaultErrorMessage;
        }
        finally
        {
            if (ReferenceEquals(_activeRequest, request))
            {
                _activeRequest = null;
            }

            IsBusy = false;
        }
    }

    private void Cancel()
    {
        _activeRequest?.Cancel();
    }

    private static string CreateSummary(CruiseObservation observation)
    {
        var offer = observation.Snapshot.Offer;
        var price = observation.Snapshot.Prices[0];
        var departure = offer.DeparturePort is null
            ? $"departing on {offer.DepartureDate.ToString("d MMMM yyyy", DisplayCulture)}"
            : $"departing {offer.DeparturePort} on {offer.DepartureDate.ToString("d MMMM yyyy", DisplayCulture)}";
        var basis = string.IsNullOrWhiteSpace(price.Basis)
            ? string.Empty
            : $" {price.Basis}";

        return $"Cruise of the Week is {offer.Title} on {offer.ShipName}, " +
               $"{departure} for {FormatDuration(offer.DurationNights)} " +
               $"from {FormatPrice(price)}{basis}.";
    }

    private static string FormatDuration(int durationNights)
    {
        return durationNights == 1
            ? "1 night"
            : $"{durationNights} nights";
    }

    private static string FormatPrice(CruisePrice price)
    {
        var amount = price.Amount.ToString("0.##", DisplayCulture);

        return string.Equals(price.Currency, "GBP", StringComparison.Ordinal)
            ? $"£{amount}"
            : $"{price.Currency} {amount}";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class AsyncCommand(
        Func<Task> execute,
        Func<bool> canExecute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => canExecute();

        public async void Execute(object? parameter)
        {
            await execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class DelegateCommand(
        Action execute,
        Func<bool> canExecute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => canExecute();

        public void Execute(object? parameter)
        {
            execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
