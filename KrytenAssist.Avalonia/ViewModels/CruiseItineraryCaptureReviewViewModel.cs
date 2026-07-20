extern alias KrytenApplication;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using KrytenAssist.Avalonia.Tools;
using KrytenAssist.Core.Cruises;
using CaptureService = KrytenApplication::KrytenAssist.Application.Cruises.ICruiseItineraryPageCaptureService;
using CaptureRequest = KrytenApplication::KrytenAssist.Application.Cruises.CruiseItineraryPageCaptureRequest;
using BatchResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseItineraryCaptureBatchResult;
using BatchStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureBatchStatus;
using RecordUseCase = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseDiscoveryCheckAndEvaluateAlerts;
using RecordStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseDiscoveryOperationStatus;
using AlertStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseDiscoveryAlertEvaluationStatus;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseItineraryCaptureReviewViewModel : INotifyPropertyChanged
{
    private readonly CaptureService _capture;
    private readonly RecordUseCase _record;
    private readonly IClock _clock;
    private readonly CruiseNewItinerariesViewModel _newItineraries;
    private readonly CruiseAlertCoordinator _alerts;
    private readonly AsyncCommand _recordCommand;
    private readonly DelegateCommand _cancelCommand;
    private BatchResult? _result;
    private IReadOnlyList<CruiseItineraryCaptureReviewItemViewModel> _items = [];
    private bool _isRecording;
    private string? _message;
    private string? _errorMessage;
    private CancellationTokenSource? _recordCancellation;
    private int _generation;

    public CruiseItineraryCaptureReviewViewModel(CaptureService capture, RecordUseCase record, IClock clock,
        CruiseNewItinerariesViewModel newItineraries, CruiseAlertCoordinator alerts)
    {
        _capture = capture; _record = record; _clock = clock; _newItineraries = newItineraries; _alerts = alerts;
        _recordCommand = new AsyncCommand(RecordAsync, () => CanRecord);
        _cancelCommand = new DelegateCommand(CancelRecording, () => IsRecording);
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public ICommand RecordCommand => _recordCommand;
    public ICommand CancelRecordingCommand => _cancelCommand;
    public IReadOnlyList<CruiseItineraryCaptureReviewItemViewModel> Items => _items;
    public bool HasReview => _result?.Status == BatchStatus.Completed;
    public int ReadyCount => Items.Count(item => item.IsReady);
    public int RejectedCount => Items.Count - ReadyCount;
    public bool WasTruncated => _result?.WasTruncated == true;
    public string ScopeSummary => _result?.Scope is null ? string.Empty : string.Join(Environment.NewLine,
        _result.Scope.Criteria.Select(value => $"{value.Name}: {(value.State == CruiseDiscoveryCriterionState.Unknown ? "Unknown" : string.Join(", ", value.Values))}"));
    public string ScopeIdentityText => _result?.Scope is null ? string.Empty : $"Source: {_result.Scope.Source.Name} · Operator: {_result.Scope.OperatorId} · Surface: {_result.Scope.Surface}";
    public string ObservedAtText => _result?.Candidates.FirstOrDefault(value => value.Occurrence is not null)?.Occurrence?.ObservedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm zzz") ?? string.Empty;
    public string BoundsText => WasTruncated
        ? "Only the bounded displayed results were captured and the result was truncated. Sort/order may expose a previously unseen older route."
        : "Only routes present in this bounded capture are evidence.";
    public string EvidenceDisclaimer => "First observed by Kryten. This does not prove when TUI published the itinerary. Absence does not mean withdrawn, cancelled or sold out.";
    public bool IsRecording { get => _isRecording; private set { if (Set(ref _isRecording, value)) { Changed(nameof(CanRecord)); _recordCommand.Raise(); _cancelCommand.Raise(); } } }
    public bool CanRecord => HasReview && ReadyCount > 0 && !IsRecording;
    public string? Message { get => _message; private set { if (Set(ref _message, value)) { Changed(nameof(HasMessage)); Changed(nameof(HasStatus)); } } }
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public string? ErrorMessage { get => _errorMessage; private set { if (Set(ref _errorMessage, value)) { Changed(nameof(HasError)); Changed(nameof(HasStatus)); } } }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasStatus => HasMessage || HasError;

    public async Task CaptureAsync(CaptureRequest request, CancellationToken token)
    {
        var generation = ++_generation;
        try
        {
            var result = await _capture.CaptureAsync(request, token);
            if (generation != _generation || token.IsCancellationRequested) return;
            _result = result; _items = result.Candidates.Select(value => new CruiseItineraryCaptureReviewItemViewModel(value)).ToArray();
            Message = result.Status == BatchStatus.Completed ? $"Itinerary review ready: {ReadyCount} route(s) Ready and {RejectedCount} rejected." : result.Message;
            ErrorMessage = result.Status == BatchStatus.Failed ? result.Message : null;
            NotifyReview();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch { if (generation == _generation) { Clear(); ErrorMessage = "Itinerary evidence could not be captured. Price and cabin evidence may still be reviewed."; } }
    }
    public void Clear()
    {
        ++_generation; CancelRecording(); _recordCancellation?.Dispose(); _recordCancellation = null; IsRecording = false;
        _result = null; _items = []; Message = null; ErrorMessage = null; NotifyReview();
    }
    private async Task RecordAsync()
    {
        if (!CanRecord || _result?.Scope is null) return;
        _recordCancellation?.Dispose(); _recordCancellation = new(); var token = _recordCancellation.Token; var generation = _generation;
        IsRecording = true; ErrorMessage = null; Message = "Recording discovery check…";
        try
        {
            var occurrences = _result.Candidates.Where(value => value.Occurrence is not null).Select(value => value.Occurrence!).ToArray();
            var rejections = _result.Candidates.Select((value, index) => new { value, index }).Where(x => x.value.Occurrence is null)
                .Select(x => new CruiseDiscoveryRejection($"{x.index:D2}:{Bound(x.value.DisplayLabel, 960)}", Bound(x.value.Message ?? $"{x.value.Status}: {string.Join(", ", x.value.MissingFields)}", 1000))).ToArray();
            var check = new CruiseDiscoveryCheck(_result.Scope, occurrences[0].ObservedAt, occurrences, rejections, _result.WasTruncated);
            var result = await _record.ExecuteAsync(check, _clock.Now, token);
            if (generation != _generation) return;
            Message = RecordingMessage(result.Recording.Status, result.Recording.FirstObservedEvents.Count) + " " + AlertMessage(result.AlertEvaluation, result.Alerts);
            if (result.Recording.Status is RecordStatus.BaselineSeeded or RecordStatus.RecordedNoNewItineraries or RecordStatus.RecordedWithFirstObserved or RecordStatus.AlreadyRecorded)
            {
                await _newItineraries.RefreshAfterRecordingAsync();
                await _alerts.NotifyAlertsCreatedAsync(result.Alerts?.CreatedAlerts.Count ?? 0);
            }
            else if (result.Recording.Status == RecordStatus.Failed) ErrorMessage = result.Recording.Message;
        }
        catch (OperationCanceledException) { Message = "Discovery recording was cancelled."; }
        catch { ErrorMessage = "Discovery evidence could not be recorded locally."; }
        finally { if (generation == _generation) { _recordCancellation?.Dispose(); _recordCancellation = null; IsRecording = false; } }
    }
    private void CancelRecording() => _recordCancellation?.Cancel();
    private static string RecordingMessage(RecordStatus status, int count) => status switch
    {
        RecordStatus.BaselineSeeded => "Discovery baseline recorded. No New Itinerary alerts are created from the first accepted check for this scope.",
        RecordStatus.RecordedNoNewItineraries => "Discovery check recorded. Every eligible route was already known to Kryten.",
        RecordStatus.RecordedWithFirstObserved => $"Discovery check recorded. Kryten first observed {count} itinerary/itineraries in this check.",
        RecordStatus.AlreadyRecorded => "This exact discovery check was already recorded.",
        RecordStatus.Cancelled => "Discovery recording was cancelled.",
        _ => "Discovery evidence could not be recorded locally."
    };
    private static string AlertMessage(AlertStatus status, KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertEvaluationResult? alerts) => status switch
    {
        AlertStatus.NotRequired => "No New Itinerary alert evaluation was required.",
        AlertStatus.Disabled => "New Itinerary alerts are disabled for future evaluations.",
        AlertStatus.Success => $"Alerts: {alerts?.CreatedAlerts.Count ?? 0} created, {alerts?.ExistingCount ?? 0} already present.",
        AlertStatus.Cancelled => "Alert evaluation was cancelled. The discovery check remains recorded.",
        _ => "Alert evaluation failed. The discovery check remains recorded."
    };
    private static string Bound(string value, int maximum) { var trimmed = value.Trim(); return trimmed.Length <= maximum ? trimmed : trimmed[..maximum]; }
    private void NotifyReview() { Changed(nameof(Items)); Changed(nameof(HasReview)); Changed(nameof(ReadyCount)); Changed(nameof(RejectedCount)); Changed(nameof(WasTruncated)); Changed(nameof(ScopeSummary)); Changed(nameof(ScopeIdentityText)); Changed(nameof(ObservedAtText)); Changed(nameof(BoundsText)); Changed(nameof(CanRecord)); _recordCommand.Raise(); }
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; Changed(name); return true; }
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    private sealed class AsyncCommand(Func<Task> execute, Func<bool> canExecute) : ICommand { public event EventHandler? CanExecuteChanged; public bool CanExecute(object? p) => canExecute(); public async void Execute(object? p) => await execute(); public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    private sealed class DelegateCommand(Action execute, Func<bool> canExecute) : ICommand { public event EventHandler? CanExecuteChanged; public bool CanExecute(object? p) => canExecute(); public void Execute(object? p) => execute(); public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
}
