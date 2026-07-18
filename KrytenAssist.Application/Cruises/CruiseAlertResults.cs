using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed record CruiseAlertQuery
{
    public CruiseAlertQuery(CruiseAlertType? type = null, CruiseAlertStatus? status = null)
    {
        if (type is not null && !Enum.IsDefined(type.Value)) throw new ArgumentOutOfRangeException(nameof(type));
        if (status is not null && !Enum.IsDefined(status.Value)) throw new ArgumentOutOfRangeException(nameof(status));
        Type = type;
        Status = status;
    }

    public CruiseAlertType? Type { get; }
    public CruiseAlertStatus? Status { get; }
}

public enum CruiseAlertOperationStatus { Success, Found, NotFound, Created, AlreadyExists, Updated, Unchanged, Cancelled, Failed }
public sealed record CruiseAlertAddRepositoryResult(bool Created, CruiseAlert Alert);
public sealed record CruiseAlertQueryResult(CruiseAlertOperationStatus Status, IReadOnlyList<CruiseAlert> Alerts, string? Message)
{
    public static CruiseAlertQueryResult Success(IEnumerable<CruiseAlert> alerts) => new(CruiseAlertOperationStatus.Success, Array.AsReadOnly(alerts.ToArray()), null);
    public static CruiseAlertQueryResult Cancelled() => new(CruiseAlertOperationStatus.Cancelled, [], "Loading cruise alerts was cancelled.");
    public static CruiseAlertQueryResult Failed() => new(CruiseAlertOperationStatus.Failed, [], "Cruise alerts could not be loaded locally.");
}
public sealed record CruiseAlertItemResult(CruiseAlertOperationStatus Status, CruiseAlert? Alert, string? Message);
public sealed record CruiseAlertCountResult(CruiseAlertOperationStatus Status, int Count, string? Message);
public sealed record CruiseAlertMutationResult(CruiseAlertOperationStatus Status, CruiseAlert? Alert, string? Message);
public sealed record CruiseAlertSettingsResult(CruiseAlertOperationStatus Status, CruiseAlertSettings? Settings, string? Message);
public sealed record CruiseAlertEvaluationResult(CruiseAlertOperationStatus Status, int CandidateCount, IReadOnlyList<CruiseAlert> CreatedAlerts, int ExistingCount, SavedCruiseCriteriaEvaluationState? CriteriaState, string? Message)
{
    public static CruiseAlertEvaluationResult Success(IEnumerable<CruiseAlert> created, int existing = 0, SavedCruiseCriteriaEvaluationState? state = null, int? candidateCount = null)
    {
        var alerts = Array.AsReadOnly(created.ToArray());
        return new(CruiseAlertOperationStatus.Success, candidateCount ?? alerts.Count + existing, alerts, existing, state, null);
    }
    public static CruiseAlertEvaluationResult Cancelled() => new(CruiseAlertOperationStatus.Cancelled, 0, [], 0, null, "Alert evaluation was cancelled.");
    public static CruiseAlertEvaluationResult Failed() => new(CruiseAlertOperationStatus.Failed, 0, [], 0, null, "Alerts could not be evaluated locally.");
}

public sealed record CruiseRecordAndAlertResult(CruiseObservationRecordResult Recording, CruiseAlertEvaluationResult? Alerts)
{
    public bool RecordingSucceeded => Recording.Status is CruiseObservationRecordStatus.FirstObservationRecorded or CruiseObservationRecordStatus.ChangedObservationRecorded or CruiseObservationRecordStatus.AlreadyCurrent;
    public bool AlertEvaluationWasAttempted => Alerts is not null;
}
