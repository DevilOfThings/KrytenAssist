extern alias KrytenApplication;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using KrytenAssist.Core.Cruises;
using CruiseCaptureCandidateResult =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureCandidateResult;
using CruiseCaptureCandidateStatus =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureCandidateStatus;
using RecordResult =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseRecordAndAlertResult;
using RecordStatus =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRecordStatus;
using AlertStatus =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertOperationStatus;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseCaptureCandidateReviewItemViewModel : INotifyPropertyChanged
{
    private readonly Action<CruiseCaptureCandidateReviewItemViewModel> _selectionChanged;
    private bool _isSelected;
    private bool _isSelectionLocked;
    private CruiseBatchRecordingStatus _recordingStatus;
    private string? _recordingMessage;
    private int _createdAlertCount;
    private AlertStatus? _alertEvaluationStatus;
    private readonly Func<CruiseObservation, Task<string>>? _saveCruise;
    private bool _isSaving;
    private string? _saveMessage;

    public CruiseCaptureCandidateReviewItemViewModel(
        CruiseCaptureCandidateResult candidate,
        bool canOpenAtTui,
        Action<Uri> openAtTui,
        Action<CruiseCaptureCandidateReviewItemViewModel> selectionChanged,
        Func<CruiseObservation, Task<string>>? saveCruise = null)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(openAtTui);
        ArgumentNullException.ThrowIfNull(selectionChanged);

        Candidate = candidate;
        _selectionChanged = selectionChanged;
        _saveCruise = saveCruise;
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
        SaveCruiseCommand = new DelegateCommand(StartSaveCruise, () => CanSaveCruise);
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

    public bool CanSelect => IsReady && !_isSelectionLocked;

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
    public ICommand SaveCruiseCommand { get; }
    public bool IsSaving => _isSaving;
    public bool CanSaveCruise => IsReady && Observation is not null && _saveCruise is not null && !IsSaving;
    public string? SaveMessage => _saveMessage;
    public bool HasSaveMessage => !string.IsNullOrWhiteSpace(SaveMessage);

    public CruiseBatchRecordingStatus RecordingStatus => _recordingStatus;

    public string RecordingStatusText => RecordingStatus switch
    {
        CruiseBatchRecordingStatus.NotAttempted => "Not recorded",
        CruiseBatchRecordingStatus.Recording => "Recording…",
        CruiseBatchRecordingStatus.FirstObservationRecorded => "First observation recorded",
        CruiseBatchRecordingStatus.ChangedObservationRecorded => "Changed observation recorded",
        CruiseBatchRecordingStatus.AlreadyCurrent => "Already current",
        CruiseBatchRecordingStatus.Cancelled => "Recording cancelled",
        CruiseBatchRecordingStatus.Failed => "Recording failed",
        _ => "Not recorded"
    };

    public string? RecordingMessage => _recordingMessage;

    public bool HasRecordingMessage => !string.IsNullOrWhiteSpace(RecordingMessage);

    public int CreatedAlertCount => _createdAlertCount;

    public AlertStatus? AlertEvaluationStatus => _alertEvaluationStatus;

    public bool AlertEvaluationCancelled => AlertEvaluationStatus == AlertStatus.Cancelled;

    public bool AlertEvaluationFailed => AlertEvaluationStatus == AlertStatus.Failed;

    public bool IsRecording => RecordingStatus == CruiseBatchRecordingStatus.Recording;

    public bool IsRecordingComplete => RecordingStatus is
        CruiseBatchRecordingStatus.FirstObservationRecorded or
        CruiseBatchRecordingStatus.ChangedObservationRecorded or
        CruiseBatchRecordingStatus.AlreadyCurrent;

    public bool IsRetryable => IsReady && RecordingStatus is
        CruiseBatchRecordingStatus.NotAttempted or
        CruiseBatchRecordingStatus.Cancelled or
        CruiseBatchRecordingStatus.Failed;

    public bool CanRecord => Observation is not null && IsRetryable;

    public void SetSelectionLocked(bool isLocked)
    {
        if (_isSelectionLocked == isLocked)
        {
            return;
        }

        _isSelectionLocked = isLocked;
        OnPropertyChanged(nameof(CanSelect));
    }

    private async void StartSaveCruise()
    {
        if (!CanSaveCruise || Observation is null) return;
        _isSaving = true; _saveMessage = "Saving cruise…"; NotifySaveState();
        try { _saveMessage = await _saveCruise!(Observation); }
        catch { _saveMessage = "This cruise could not be saved locally. Please try again."; }
        finally { _isSaving = false; NotifySaveState(); }
    }

    private void NotifySaveState()
    {
        OnPropertyChanged(nameof(IsSaving)); OnPropertyChanged(nameof(CanSaveCruise));
        OnPropertyChanged(nameof(SaveMessage)); OnPropertyChanged(nameof(HasSaveMessage));
    }

    public void MarkRecording()
    {
        if (!CanRecord)
        {
            return;
        }

        SetRecordingState(CruiseBatchRecordingStatus.Recording, "Recording this observation…");
    }

    public void ApplyRecordingResult(RecordResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var status = result.Recording.Status switch
        {
            RecordStatus.FirstObservationRecorded =>
                CruiseBatchRecordingStatus.FirstObservationRecorded,
            RecordStatus.ChangedObservationRecorded =>
                CruiseBatchRecordingStatus.ChangedObservationRecorded,
            RecordStatus.AlreadyCurrent => CruiseBatchRecordingStatus.AlreadyCurrent,
            RecordStatus.Cancelled => CruiseBatchRecordingStatus.Cancelled,
            _ => CruiseBatchRecordingStatus.Failed
        };
        var recordingMessage = status switch
        {
            CruiseBatchRecordingStatus.FirstObservationRecorded =>
                "Observation recorded as the first evidence for this sailing.",
            CruiseBatchRecordingStatus.ChangedObservationRecorded =>
                "A changed observation was recorded.",
            CruiseBatchRecordingStatus.AlreadyCurrent =>
                "No new snapshot was needed; this observation is already current.",
            CruiseBatchRecordingStatus.Cancelled =>
                "Recording was cancelled. You can try this observation again.",
            _ => "The observation could not be recorded. You can try it again."
        };
        _createdAlertCount = result.CreatedAlertCount;
        _alertEvaluationStatus = result.AnyAlertEvaluationFailed
            ? AlertStatus.Failed
            : result.AnyAlertEvaluationCancelled
                ? AlertStatus.Cancelled
                : result.AlertEvaluationWasAttempted
                    ? AlertStatus.Success
                    : null;
        var message = _alertEvaluationStatus switch
        {
            AlertStatus.Cancelled when _createdAlertCount > 0 =>
                $"{recordingMessage} {_createdAlertCount} alert{(_createdAlertCount == 1 ? string.Empty : "s")} {(_createdAlertCount == 1 ? "was" : "were")} created, but some alert evaluation was cancelled.",
            AlertStatus.Cancelled =>
                $"{recordingMessage} Alert evaluation was cancelled after recording.",
            AlertStatus.Failed when _createdAlertCount > 0 =>
                $"{recordingMessage} {_createdAlertCount} alert{(_createdAlertCount == 1 ? string.Empty : "s")} {(_createdAlertCount == 1 ? "was" : "were")} created, but some alerts could not be evaluated locally.",
            AlertStatus.Failed =>
                $"{recordingMessage} Alerts could not be evaluated locally after recording.",
            _ when _createdAlertCount == 1 => $"{recordingMessage} 1 alert was created.",
            _ when _createdAlertCount > 1 =>
                $"{recordingMessage} {_createdAlertCount} alerts were created.",
            _ => recordingMessage
        };
        SetRecordingState(status, message);
        OnPropertyChanged(nameof(CreatedAlertCount));
        OnPropertyChanged(nameof(AlertEvaluationStatus));
        OnPropertyChanged(nameof(AlertEvaluationCancelled));
        OnPropertyChanged(nameof(AlertEvaluationFailed));
    }

    private void SetRecordingState(CruiseBatchRecordingStatus status, string message)
    {
        _recordingStatus = status;
        _recordingMessage = message;
        OnPropertyChanged(nameof(RecordingStatus));
        OnPropertyChanged(nameof(RecordingStatusText));
        OnPropertyChanged(nameof(RecordingMessage));
        OnPropertyChanged(nameof(HasRecordingMessage));
        OnPropertyChanged(nameof(IsRecording));
        OnPropertyChanged(nameof(IsRecordingComplete));
        OnPropertyChanged(nameof(IsRetryable));
        OnPropertyChanged(nameof(CanRecord));
    }

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
