namespace KrytenAssist.Avalonia.ViewModels;

public enum CruiseBatchRecordingStatus
{
    NotAttempted,
    Recording,
    FirstObservationRecorded,
    ChangedObservationRecorded,
    AlreadyCurrent,
    Cancelled,
    Failed
}
