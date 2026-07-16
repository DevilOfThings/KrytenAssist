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

    public CruiseOfTheWeekViewModel(
        ISkillRegistry skillRegistry,
        IClock clock,
        CruiseDiscoverySourceCatalog? sourceCatalog = null,
        CruiseTrustedHostPolicy? trustedHostPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(skillRegistry);
        ArgumentNullException.ThrowIfNull(clock);

        _skill = skillRegistry.Find(SkillId);
        _clock = clock;
        BrowserFeasibility = new CruiseBrowserFeasibilityViewModel(
            sourceCatalog ?? new CruiseDiscoverySourceCatalog(),
            trustedHostPolicy ?? new CruiseTrustedHostPolicy());
        _retrieveCommand = new AsyncCommand(RetrieveAsync, () => CanRetrieve);
        _cancelCommand = new DelegateCommand(Cancel, () => IsBusy);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand RetrieveCommand => _retrieveCommand;

    public ICommand CancelCommand => _cancelCommand;

    public CruiseBrowserFeasibilityViewModel BrowserFeasibility { get; }

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
