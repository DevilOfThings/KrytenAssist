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
using GetHistory = KrytenApplication::KrytenAssist.Application.Cruises.GetCruiseHistory;
using ListHistories = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseHistories;
using ListStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseHistoryListStatus;
using RecordObservation = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseObservationAndEvaluateAlerts;
using RecordAndAlertResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseRecordAndAlertResult;
using AlertStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertOperationStatus;
using RecordResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRecordResult;
using RecordStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRecordStatus;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseHistoryViewModel : INotifyPropertyChanged
{
    private readonly RecordObservation _recordObservation;
    private readonly GetHistory _getHistory;
    private readonly ListHistories _listHistories;
    private readonly IClock _clock;
    private readonly AsyncCommand _recordCommand;
    private readonly DelegateCommand _cancelRecordingCommand;
    private readonly AsyncCommand _refreshHistoryCommand;
    private readonly DelegateCommand _cancelHistoryLoadingCommand;
    private CruiseObservation? _capturedObservation;
    private string? _completedFingerprint;
    private bool _isRecording;
    private string? _recordMessage;
    private bool _isLoadingHistory;
    private string? _historyMessage;
    private string? _historyErrorMessage;
    private bool _hasLoaded;
    private IReadOnlyList<CruiseHistoryItemViewModel> _histories = Array.Empty<CruiseHistoryItemViewModel>();
    private IReadOnlyList<CruiseHistoryGroupViewModel> _historyGroups = Array.Empty<CruiseHistoryGroupViewModel>();
    private CruiseHistoryGrouping _grouping;
    private CruiseHistoryItemViewModel? _selectedHistory;
    private CancellationTokenSource? _recordCancellation;
    private CancellationTokenSource? _loadCancellation;
    private int _captureGeneration;
    private int _loadGeneration;

    public CruiseHistoryViewModel(
        RecordObservation recordObservation,
        GetHistory getHistory,
        ListHistories listHistories,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(recordObservation);
        ArgumentNullException.ThrowIfNull(getHistory);
        ArgumentNullException.ThrowIfNull(listHistories);
        ArgumentNullException.ThrowIfNull(clock);
        _recordObservation = recordObservation;
        _getHistory = getHistory;
        _listHistories = listHistories;
        _clock = clock;
        _recordCommand = new AsyncCommand(RecordAsync, () => CanRecord);
        _cancelRecordingCommand = new DelegateCommand(CancelRecording, () => IsRecording);
        _refreshHistoryCommand = new AsyncCommand(RefreshHistoryAsync, () => !IsLoadingHistory);
        _cancelHistoryLoadingCommand = new DelegateCommand(CancelHistoryLoading, () => IsLoadingHistory);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand RecordObservationCommand => _recordCommand;
    public ICommand CancelRecordingCommand => _cancelRecordingCommand;
    public ICommand RefreshHistoryCommand => _refreshHistoryCommand;
    public ICommand CancelHistoryLoadingCommand => _cancelHistoryLoadingCommand;
    public CruiseObservation? CapturedObservation => _capturedObservation;
    public bool HasCapturedObservation => CapturedObservation is not null;
    public bool CanRecord => CapturedObservation is not null
        && !IsRecording
        && !string.Equals(
            _completedFingerprint,
            CruiseObservationFingerprint.From(CapturedObservation).PersistenceKey,
            StringComparison.Ordinal);

    public bool IsRecording
    {
        get => _isRecording;
        private set
        {
            if (SetField(ref _isRecording, value))
            {
                OnCommandStateChanged();
            }
        }
    }

    public string? RecordMessage
    {
        get => _recordMessage;
        private set
        {
            if (SetField(ref _recordMessage, value))
            {
                OnPropertyChanged(nameof(HasRecordMessage));
            }
        }
    }

    public bool HasRecordMessage => !string.IsNullOrWhiteSpace(RecordMessage);
    public bool IsRecordCompleted => _completedFingerprint is not null;
    public string ReviewStatusText => IsRecordCompleted
        ? "This captured observation has been checked against local history."
        : "Review only — choose Record Observation to add this evidence to local history.";

    public bool IsLoadingHistory
    {
        get => _isLoadingHistory;
        private set
        {
            if (SetField(ref _isLoadingHistory, value))
            {
                OnCommandStateChanged();
            }
        }
    }

    public string? HistoryMessage
    {
        get => _historyMessage;
        private set => SetField(ref _historyMessage, value);
    }

    public string? HistoryErrorMessage
    {
        get => _historyErrorMessage;
        private set
        {
            if (SetField(ref _historyErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasHistoryError));
            }
        }
    }

    public bool HasHistoryError => !string.IsNullOrWhiteSpace(HistoryErrorMessage);
    public bool HasLoaded => _hasLoaded;
    public IReadOnlyList<CruiseHistoryItemViewModel> Histories => _histories;
    public IReadOnlyList<CruiseHistoryGrouping> GroupingOptions { get; } =
        [CruiseHistoryGrouping.None, CruiseHistoryGrouping.Cruise, CruiseHistoryGrouping.Ship];
    public CruiseHistoryGrouping Grouping
    {
        get => _grouping;
        set
        {
            if (SetField(ref _grouping, value))
            {
                OnPropertyChanged(nameof(IsGroupedByNone));
                OnPropertyChanged(nameof(IsGroupedByCruise));
                OnPropertyChanged(nameof(IsGroupedByShip));
                RebuildHistoryGroups();
            }
        }
    }

    public bool IsGroupedByNone
    {
        get => Grouping == CruiseHistoryGrouping.None;
        set
        {
            if (value)
            {
                Grouping = CruiseHistoryGrouping.None;
            }
        }
    }

    public bool IsGroupedByCruise
    {
        get => Grouping == CruiseHistoryGrouping.Cruise;
        set
        {
            if (value)
            {
                Grouping = CruiseHistoryGrouping.Cruise;
            }
        }
    }

    public bool IsGroupedByShip
    {
        get => Grouping == CruiseHistoryGrouping.Ship;
        set
        {
            if (value)
            {
                Grouping = CruiseHistoryGrouping.Ship;
            }
        }
    }

    public IReadOnlyList<CruiseHistoryGroupViewModel> HistoryGroups => _historyGroups;
    public bool HasHistories => Histories.Count > 0;
    public bool IsHistoryEmpty => HasLoaded && !IsLoadingHistory && !HasHistories && !HasHistoryError;

    public CruiseHistoryItemViewModel? SelectedHistory
    {
        get => _selectedHistory;
        set
        {
            if (SetField(ref _selectedHistory, value))
            {
                OnPropertyChanged(nameof(HasSelectedHistory));
                foreach (var group in HistoryGroups)
                {
                    group.SynchronizeSelection(value);
                }
            }
        }
    }

    public bool HasSelectedHistory => SelectedHistory is not null;

    public void Activate()
    {
        if (!HasLoaded && !IsLoadingHistory && _refreshHistoryCommand.CanExecute(null))
        {
            _refreshHistoryCommand.Execute(null);
        }
    }

    public void Deactivate() => CancelHistoryLoading();

    public Task RefreshAfterBatchRecordingAsync(CruiseObservation preferredObservation)
    {
        ArgumentNullException.ThrowIfNull(preferredObservation);
        return LoadHistoryAsync(
            CruiseSailingKey.From(preferredObservation),
            preferredObservation.Source);
    }

    public void SetCapturedObservation(CruiseObservation? observation)
    {
        _captureGeneration++;
        _recordCancellation?.Cancel();
        _recordCancellation?.Dispose();
        _recordCancellation = null;
        _recordCommand.ResetExecution();
        _capturedObservation = observation;
        _completedFingerprint = null;
        IsRecording = false;
        RecordMessage = null;
        OnPropertyChanged(nameof(CapturedObservation));
        OnPropertyChanged(nameof(HasCapturedObservation));
        OnPropertyChanged(nameof(IsRecordCompleted));
        OnPropertyChanged(nameof(ReviewStatusText));
        OnCommandStateChanged();
    }

    private async Task RecordAsync()
    {
        var observation = CapturedObservation;
        if (observation is null)
        {
            return;
        }

        _recordCancellation?.Cancel();
        _recordCancellation?.Dispose();
        _recordCancellation = new CancellationTokenSource();
        var cancellation = _recordCancellation;
        var generation = _captureGeneration;
        IsRecording = true;
        RecordMessage = "Recording this observation...";
        try
        {
            var result = await _recordObservation.ExecuteAsync(
                observation,
                _clock.Now,
                cancellation.Token);
            if (generation != _captureGeneration)
            {
                return;
            }

            RecordMessage = CreateRecordMessage(result, observation.Source);
            if (result.Recording.Status is RecordStatus.FirstObservationRecorded
                or RecordStatus.ChangedObservationRecorded
                or RecordStatus.AlreadyCurrent)
            {
                _completedFingerprint = CruiseObservationFingerprint.From(observation).PersistenceKey;
                OnPropertyChanged(nameof(IsRecordCompleted));
                OnPropertyChanged(nameof(ReviewStatusText));
                await LoadHistoryAsync(
                    CruiseSailingKey.From(observation),
                    observation.Source);
            }
        }
        catch (Exception)
        {
            if (generation == _captureGeneration)
            {
                RecordMessage = "The observation could not be recorded. Please try again.";
            }
        }
        finally
        {
            if (generation == _captureGeneration)
            {
                IsRecording = false;
                OnCommandStateChanged();
            }
        }
    }

    private async Task RefreshHistoryAsync() =>
        await LoadHistoryAsync(
            SelectedHistory?.Details.History.SailingKey,
            SelectedHistory?.Details.History.Source);

    private async Task LoadHistoryAsync(
        CruiseSailingKey? preferredKey,
        CruiseSource? preferredSource)
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        var cancellation = _loadCancellation;
        var generation = ++_loadGeneration;
        IsLoadingHistory = true;
        HistoryErrorMessage = null;
        HistoryMessage = "Loading recorded cruise history...";
        try
        {
            var result = await _listHistories.ExecuteAsync(cancellation.Token);
            if (generation != _loadGeneration)
            {
                return;
            }

            if (result.Status == ListStatus.Success)
            {
                var items = new CruiseHistoryItemViewModel[result.Histories.Count];
                for (var index = 0; index < result.Histories.Count; index++)
                {
                    items[index] = new CruiseHistoryItemViewModel(result.Histories[index], _clock);
                }

                _histories = Array.AsReadOnly(items);
                _hasLoaded = true;
                OnPropertyChanged(nameof(Histories));
                OnPropertyChanged(nameof(HasHistories));
                OnPropertyChanged(nameof(HasLoaded));
                SelectedHistory = FindPreferred(items, preferredKey, preferredSource)
                    ?? (SelectedHistory is null ? items.FirstOrDefault() : FindMatching(items, SelectedHistory))
                    ?? items.FirstOrDefault();
                RebuildHistoryGroups();
                HistoryMessage = items.Length == 0
                    ? "No cruise observations have been recorded yet. Capture a cruise and choose Record Observation to begin its price history."
                    : $"{items.Length} recorded cruise histor{(items.Length == 1 ? "y" : "ies")}.";
            }
            else if (result.Status == ListStatus.Failed)
            {
                HistoryErrorMessage = "Recorded cruise history could not be loaded. Please try again.";
                HistoryMessage = null;
            }
            else
            {
                HistoryMessage = "Loading recorded cruise history was cancelled.";
            }
        }
        catch (Exception)
        {
            if (generation == _loadGeneration)
            {
                HistoryErrorMessage = "Recorded cruise history could not be loaded. Please try again.";
                HistoryMessage = null;
            }
        }
        finally
        {
            if (generation == _loadGeneration)
            {
                IsLoadingHistory = false;
                OnPropertyChanged(nameof(IsHistoryEmpty));
            }
        }
    }

    private void RebuildHistoryGroups()
    {
        IEnumerable<IGrouping<string, CruiseHistoryItemViewModel>> grouped = Grouping switch
        {
            CruiseHistoryGrouping.Cruise => Histories.GroupBy(
                item => item.Title,
                StringComparer.OrdinalIgnoreCase),
            CruiseHistoryGrouping.Ship => Histories.GroupBy(
                item => item.Ship,
                StringComparer.OrdinalIgnoreCase),
            _ => []
        };

        if (Grouping == CruiseHistoryGrouping.None)
        {
            _historyGroups =
            [
                new CruiseHistoryGroupViewModel(
                    null,
                    Histories,
                    SelectedHistory,
                    selected => SelectedHistory = selected)
            ];
        }
        else
        {
            var groups = new List<CruiseHistoryGroupViewModel>();
            foreach (var group in grouped)
            {
                var items = group.ToArray();
                groups.Add(new CruiseHistoryGroupViewModel(
                    $"{group.Key} · {items.Length} {(items.Length == 1 ? "sailing" : "sailings")}",
                    items,
                    SelectedHistory,
                    selected => SelectedHistory = selected));
            }

            _historyGroups = groups.AsReadOnly();
        }

        OnPropertyChanged(nameof(HistoryGroups));
    }

    private void CancelRecording()
    {
        _recordCancellation?.Cancel();
        RecordMessage = "Recording the cruise observation was cancelled.";
    }

    private void CancelHistoryLoading()
    {
        if (!IsLoadingHistory)
        {
            return;
        }

        _loadGeneration++;
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = null;
        _refreshHistoryCommand.ResetExecution();
        IsLoadingHistory = false;
        HistoryMessage = "Loading recorded cruise history was cancelled.";
        OnPropertyChanged(nameof(IsHistoryEmpty));
    }

    private static string CreateRecordMessage(RecordAndAlertResult result, CruiseSource? source)
    {
        var sourceName = source?.Name ?? "this source";
        var recordingMessage = result.Recording.Status switch
        {
            RecordStatus.FirstObservationRecorded =>
                $"Observation recorded. This is the first price seen for this sailing from {sourceName}.",
            RecordStatus.ChangedObservationRecorded => CreateChangedMessage(result.Recording),
            RecordStatus.AlreadyCurrent =>
                "No new snapshot was needed. This advertised observation matches the latest recorded values.",
            RecordStatus.Cancelled => "Recording the cruise observation was cancelled. You can try again.",
            _ => "The observation could not be recorded. Please try again."
        };

        if (!result.RecordingSucceeded || !result.AlertEvaluationWasAttempted)
        {
            return recordingMessage;
        }

        return (result.AnyAlertEvaluationCancelled, result.AnyAlertEvaluationFailed, result.CreatedAlertCount) switch
        {
            (true, _, > 0) =>
                $"{recordingMessage} {result.CreatedAlertCount} alert{(result.CreatedAlertCount == 1 ? string.Empty : "s")} {(result.CreatedAlertCount == 1 ? "was" : "were")} created, but some alert evaluation was cancelled.",
            (true, _, _) =>
                $"{recordingMessage} Alert evaluation was cancelled after the observation was recorded.",
            (_, true, > 0) =>
                $"{recordingMessage} {result.CreatedAlertCount} alert{(result.CreatedAlertCount == 1 ? string.Empty : "s")} {(result.CreatedAlertCount == 1 ? "was" : "were")} created, but some alerts could not be evaluated locally.",
            (_, true, _) =>
                $"{recordingMessage} Some alerts could not be evaluated locally after the observation was recorded.",
            (_, _, 1) =>
                $"{recordingMessage} 1 alert was created.",
            (_, _, > 1) =>
                $"{recordingMessage} {result.CreatedAlertCount} alerts were created.",
            _ => recordingMessage
        };
    }

    private static string CreateChangedMessage(RecordResult result)
    {
        var movement = result.Summary?.Movement;
        if (movement is null || movement.Direction == CruisePriceTrendDirection.Unavailable)
        {
            return "New observation recorded. Comparable price history is unavailable for this observation.";
        }

        return movement.Direction switch
        {
            CruisePriceTrendDirection.Lower =>
                $"New observation recorded. The comparable price is {CruiseHistoryItemViewModel.FormatPrice(new CruisePrice(movement.Delta!.Value, movement.CurrentPrice!.Currency))} lower than the previous recorded price.",
            CruisePriceTrendDirection.Higher =>
                $"New observation recorded. The comparable price is {CruiseHistoryItemViewModel.FormatPrice(new CruisePrice(movement.Delta!.Value, movement.CurrentPrice!.Currency))} higher than the previous recorded price.",
            CruisePriceTrendDirection.Unchanged =>
                "New observation recorded. The comparable price is unchanged; other advertised details changed.",
            _ => "New observation recorded. This is the first comparable price for the sailing."
        };
    }

    private static CruiseHistoryItemViewModel? FindPreferred(
        IEnumerable<CruiseHistoryItemViewModel> items,
        CruiseSailingKey? key,
        CruiseSource? source)
    {
        if (key is null)
        {
            return null;
        }

        return items.FirstOrDefault(item => item.Matches(key, source));
    }

    private static CruiseHistoryItemViewModel? FindMatching(
        IEnumerable<CruiseHistoryItemViewModel> items,
        CruiseHistoryItemViewModel selected) =>
        items.FirstOrDefault(item => item.Matches(
            selected.Details.History.SailingKey,
            selected.Details.History.Source));

    private void OnCommandStateChanged()
    {
        OnPropertyChanged(nameof(CanRecord));
        _recordCommand.RaiseCanExecuteChanged();
        _cancelRecordingCommand.RaiseCanExecuteChanged();
        _refreshHistoryCommand.RaiseCanExecuteChanged();
        _cancelHistoryLoadingCommand.RaiseCanExecuteChanged();
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed class AsyncCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private bool _isExecuting;
        private int _executionGeneration;

        public AsyncCommand(Func<Task> execute, Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => !_isExecuting && _canExecute();

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            _isExecuting = true;
            var generation = ++_executionGeneration;
            RaiseCanExecuteChanged();
            try
            {
                await _execute();
            }
            catch
            {
                RecordUnhandledFailure();
            }
            finally
            {
                if (generation == _executionGeneration)
                {
                    _isExecuting = false;
                    RaiseCanExecuteChanged();
                }
            }
        }

        private void RecordUnhandledFailure()
        {
            // Application use cases return controlled failures. This boundary prevents
            // unexpected presentation exceptions from escaping ICommand execution.
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        public void ResetExecution()
        {
            _executionGeneration++;
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    private sealed class DelegateCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public DelegateCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => _canExecute();
        public void Execute(object? parameter) => _execute();
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
