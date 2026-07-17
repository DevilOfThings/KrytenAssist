extern alias KrytenApplication;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using KrytenAssist.Core.Cruises;
using CruiseCaptureCandidateResult =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureCandidateResult;
using CruiseCaptureCandidateStatus =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureCandidateStatus;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseCaptureCandidateReviewItemViewModel : INotifyPropertyChanged
{
    private readonly Action<CruiseCaptureCandidateReviewItemViewModel> _selectionChanged;
    private bool _isSelected;

    public CruiseCaptureCandidateReviewItemViewModel(
        CruiseCaptureCandidateResult candidate,
        bool canOpenAtTui,
        Action<Uri> openAtTui,
        Action<CruiseCaptureCandidateReviewItemViewModel> selectionChanged)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(openAtTui);
        ArgumentNullException.ThrowIfNull(selectionChanged);

        Candidate = candidate;
        _selectionChanged = selectionChanged;
        CanOpenAtTui = canOpenAtTui &&
                       Uri.TryCreate(candidate.SourceReference, UriKind.Absolute, out _);
        OpenAtTuiCommand = new DelegateCommand(
            () =>
            {
                if (Uri.TryCreate(SourceReference, UriKind.Absolute, out var address))
                {
                    openAtTui(address);
                }
            },
            () => CanOpenAtTui);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CruiseCaptureCandidateResult Candidate { get; }

    public CruiseCaptureCandidateStatus Status => Candidate.Status;

    public string StatusText => Status switch
    {
        CruiseCaptureCandidateStatus.Ready => "Ready",
        CruiseCaptureCandidateStatus.Incomplete => "Incomplete",
        CruiseCaptureCandidateStatus.Failed => "Failed",
        _ => "Unavailable"
    };

    public string DisplayLabel => Candidate.DisplayLabel;

    public CruiseObservation? Observation => Candidate.Observation;

    public string? Message => Candidate.Message;

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    public string SourceReference => Candidate.SourceReference;

    public string? Operator => Observation?.Snapshot.Offer.Provider.Name;

    public string? RetailSource => Observation?.Source?.Name;

    public string? Ship => Observation?.Snapshot.Offer.ShipName;

    public string? DepartureDateText =>
        Observation?.Snapshot.Offer.DepartureDate.ToString("d MMMM yyyy");

    public string? DurationText => Observation is null
        ? null
        : $"{Observation.Snapshot.Offer.DurationNights} nights";

    public string? DeparturePort => Observation?.Snapshot.Offer.DeparturePort;

    public bool HasDeparturePort => !string.IsNullOrWhiteSpace(DeparturePort);

    public string? PricesText => Observation is null
        ? null
        : string.Join(
            Environment.NewLine,
            Observation.Snapshot.Prices.Select(price =>
                $"{price.Currency} {price.Amount:0.##}" +
                (price.Basis is null ? string.Empty : $" {price.Basis}")));

    public bool HasPrices => !string.IsNullOrWhiteSpace(PricesText);

    public string? PromotionSummary => Observation?.Snapshot.PromotionSummary;

    public bool HasPromotion => !string.IsNullOrWhiteSpace(PromotionSummary);

    public IReadOnlyList<string> MissingFields => Candidate.MissingFields;

    public string MissingFieldsText => string.Join(", ", MissingFields);

    public bool HasMissingFields => MissingFields.Count > 0;

    public bool IsReady => Candidate.IsReady;

    public bool IsIncomplete => Status == CruiseCaptureCandidateStatus.Incomplete;

    public bool IsFailed => Status == CruiseCaptureCandidateStatus.Failed;

    public bool CanSelect => IsReady;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            var selected = CanSelect && value;
            if (_isSelected == selected)
            {
                return;
            }

            _isSelected = selected;
            OnPropertyChanged();
            _selectionChanged(this);
        }
    }

    public bool CanOpenAtTui { get; }

    public ICommand OpenAtTuiCommand { get; }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed class DelegateCommand(Action execute, Func<bool> canExecute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => canExecute();

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                execute();
            }
        }
    }
}
