using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public enum CruiseCabinOperationStatus
{
    Success, Found, NotFound, FirstObservationRecorded, ChangedObservationRecorded,
    AlreadyCurrent, Cancelled, Failed
}

public sealed record CruiseCabinRecordResult(CruiseCabinOperationStatus Status, CruiseCabinHistorySummary? Summary, string? Message)
{
    public static CruiseCabinRecordResult Recorded(CruiseCabinOperationStatus status, CruiseCabinHistorySummary summary) => new(status, summary, null);
    public static CruiseCabinRecordResult Cancelled() => new(CruiseCabinOperationStatus.Cancelled, null, "Cabin recording was cancelled.");
    public static CruiseCabinRecordResult Failed() => new(CruiseCabinOperationStatus.Failed, null, "Cabin evidence could not be recorded locally.");
}

public sealed record CruiseCabinHistoryDetails(CruiseCabinRecordedHistory History, CruiseCabinHistorySummary Summary);
public sealed record CruiseCabinHistoryQueryResult(CruiseCabinOperationStatus Status, CruiseCabinHistoryDetails? Details, string? Message);
public sealed record CruiseCabinHistoryListResult(CruiseCabinOperationStatus Status, IReadOnlyList<CruiseCabinHistoryDetails> Histories, string? Message);

public sealed record CruiseCabinAlertCandidateResult(CruiseCabinOperationStatus Status, IReadOnlyList<CruiseAlertCandidate> Candidates, string? Message)
{
    public static CruiseCabinAlertCandidateResult Success(IEnumerable<CruiseAlertCandidate> candidates) =>
        new(CruiseCabinOperationStatus.Success, Array.AsReadOnly(candidates.ToArray()), null);
    public static CruiseCabinAlertCandidateResult Cancelled() => new(CruiseCabinOperationStatus.Cancelled, [], "Cabin alert evaluation was cancelled.");
    public static CruiseCabinAlertCandidateResult Failed() => new(CruiseCabinOperationStatus.Failed, [], "Cabin alerts could not be evaluated locally.");
}

public sealed record CruiseCabinRecordAndAlertResult(
    CruiseCabinRecordResult Recording,
    CruiseAlertEvaluationResult? CabinAvailabilityAlerts,
    CruiseAlertEvaluationResult? SavedCriteriaAlerts)
{
    public bool RecordingSucceeded => Recording.Status is CruiseCabinOperationStatus.FirstObservationRecorded or
        CruiseCabinOperationStatus.ChangedObservationRecorded or CruiseCabinOperationStatus.AlreadyCurrent;
    public int CreatedAlertCount => (CabinAvailabilityAlerts?.CreatedAlerts.Count ?? 0) +
        (SavedCriteriaAlerts?.CreatedAlerts.Count ?? 0);
    public bool AnyAlertEvaluationFailed => CabinAvailabilityAlerts?.Status == CruiseAlertOperationStatus.Failed ||
        SavedCriteriaAlerts?.Status == CruiseAlertOperationStatus.Failed;
    public bool AnyAlertEvaluationCancelled => CabinAvailabilityAlerts?.Status == CruiseAlertOperationStatus.Cancelled ||
        SavedCriteriaAlerts?.Status == CruiseAlertOperationStatus.Cancelled;
    public bool AlertEvaluationRetryable => RecordingSucceeded &&
        (AnyAlertEvaluationFailed || AnyAlertEvaluationCancelled);
}
