extern alias KrytenApplication;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using KrytenAssist.Avalonia.Cruises.Discovery;
using KrytenAssist.Core.Cruises;
using CabinCandidate = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinCaptureCandidateResult;
using CabinCaptureStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinCaptureStatus;
using CabinRecordResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRecordAndAlertResult;
using CabinOperationStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinOperationStatus;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseCabinCaptureReviewItemViewModel : INotifyPropertyChanged
{
    private readonly Func<CruiseCabinObservation, Task<CabinRecordResult>>? _record;
    private bool _isRecording;
    private string? _recordingMessage;

    public CruiseCabinCaptureReviewItemViewModel(CabinCandidate candidate,
        Func<CruiseCabinObservation, Task<CabinRecordResult>>? record)
    {
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        _record = record;
        RecordCommand = new DelegateCommand(() => _ = RecordAsync(), () => CanRecord);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public CabinCandidate Candidate { get; }
    public CruiseCabinObservation? Observation => Candidate.Observation;
    public string DisplayLabel => Candidate.DisplayLabel;
    public string StatusText => Candidate.Status switch
    {
        CabinCaptureStatus.Ready => "Ready",
        CabinCaptureStatus.Incomplete => "Incomplete",
        CabinCaptureStatus.Unsupported => "Unsupported",
        CabinCaptureStatus.Failed => "Failed",
        _ => "Unavailable"
    };
    public string? Message => Candidate.Message;
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public string SourceReference => Candidate.SourceReference;
    public string? Ship => Observation?.SailingKey.ShipName;
    public string? DepartureText => Observation?.SailingKey.DepartureDate.ToString("d MMMM yyyy");
    public string? DurationText => Observation is null ? null : $"{Observation.SailingKey.DurationNights} nights";
    public string? SourceText => Observation?.Source.Name;
    public string? CoverageText => Observation is null ? null : $"{Observation.Coverage} evidence";
    public string? ContextText => Observation is null ? null : BuildContext(Observation.SearchContext);
    public string? StatesText => Observation is null ? null : string.Join(Environment.NewLine,
        Observation.States.Select(state => $"{state.CabinType} — {StateText(state.Availability)}"));
    public string? EvidenceTimeText => Observation?.ObservedAt.ToString("d MMMM yyyy 'at' HH:mm zzz");
    public bool IsReady => Candidate.Status == CabinCaptureStatus.Ready;
    public bool IsRecording => _isRecording;
    public bool CanRecord => IsReady && Observation is not null && _record is not null && !IsRecording;
    public string? RecordingMessage => _recordingMessage;
    public bool HasRecordingMessage => !string.IsNullOrWhiteSpace(RecordingMessage);
    public ICommand RecordCommand { get; }

    private async Task RecordAsync()
    {
        if (!CanRecord || Observation is null) return;
        _isRecording = true;
        _recordingMessage = "Recording cabin observation…";
        NotifyRecording();
        try
        {
            var result = await _record!(Observation);
            var recorded = result.Recording.Status switch
            {
                CabinOperationStatus.FirstObservationRecorded => "First cabin observation recorded.",
                CabinOperationStatus.ChangedObservationRecorded => "Changed cabin observation recorded.",
                CabinOperationStatus.AlreadyCurrent => "This cabin observation is already current.",
                CabinOperationStatus.Cancelled => "Cabin recording was cancelled. You can try again.",
                _ => "The cabin observation could not be recorded. You can try again."
            };
            _recordingMessage = result.AlertEvaluationRetryable
                ? $"{recorded} Alert or criteria evaluation did not finish and can be retried."
                : result.CreatedAlertCount > 0
                    ? $"{recorded} {result.CreatedAlertCount} alert{(result.CreatedAlertCount == 1 ? " was" : "s were")} created."
                    : recorded;
        }
        catch { _recordingMessage = "The cabin observation could not be recorded. You can try again."; }
        finally { _isRecording = false; NotifyRecording(); }
    }

    private static string StateText(CruiseCabinAvailabilityState state) => state switch
    {
        CruiseCabinAvailabilityState.Available => "Available when captured for this search",
        CruiseCabinAvailabilityState.Unavailable => "Unavailable when captured for this search",
        _ => "Unknown"
    };

    private static string BuildContext(CruiseCabinSearchContext context)
    {
        var values = new List<string>();
        if (context.AdultCount is not null) values.Add($"{context.AdultCount} adult{(context.AdultCount == 1 ? "" : "s")}");
        if (context.ChildCount is not null) values.Add($"{context.ChildCount} child{(context.ChildCount == 1 ? "" : "ren")}");
        if (context.DepartureAirportId is not null) values.Add($"from {context.DepartureAirportId.ToUpperInvariant()}");
        if (context.PackageMode != CruiseCabinPackageMode.Unknown) values.Add(context.PackageMode.ToString());
        if (context.CabinQuantity is not null) values.Add($"{context.CabinQuantity} cabin{(context.CabinQuantity == 1 ? "" : "s")}");
        return values.Count == 0 ? "Search context unknown" : string.Join(" · ", values);
    }

    private void NotifyRecording()
    {
        OnPropertyChanged(nameof(IsRecording)); OnPropertyChanged(nameof(CanRecord));
        OnPropertyChanged(nameof(RecordingMessage)); OnPropertyChanged(nameof(HasRecordingMessage));
        ((DelegateCommand)RecordCommand).RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed class DelegateCommand(Action execute, Func<bool> canExecute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => canExecute();
        public void Execute(object? parameter) => execute();
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
