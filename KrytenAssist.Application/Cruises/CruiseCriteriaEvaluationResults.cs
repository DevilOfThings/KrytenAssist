using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed record SavedCruiseMutationAndAlertResult(
    SavedCruiseMutationResult Mutation,
    CruiseAlertEvaluationResult? SavedCriteriaAlerts)
{
    public bool EvaluationWasAttempted => SavedCriteriaAlerts is not null;
}

public sealed record CruiseCriteriaSailingEvaluationResult(
    CruiseSailingKey SailingKey,
    CruiseAlertEvaluationResult Evaluation);

public sealed record CruiseCriteriaBulkEvaluationResult(
    CruiseAlertOperationStatus Status,
    int EligibleCount,
    IReadOnlyList<CruiseCriteriaSailingEvaluationResult> Completed,
    int UnprocessedCount,
    string? Message)
{
    public int AttemptedCount => Completed.Count;
    public int CreatedAlertCount => Completed.Sum(item => item.Evaluation.CreatedAlerts.Count);
    public int ExistingCount => Completed.Sum(item => item.Evaluation.ExistingCount);
    public int FailedCount => Completed.Count(item => item.Evaluation.Status == CruiseAlertOperationStatus.Failed);
    public int CancelledCount => Completed.Count(item => item.Evaluation.Status == CruiseAlertOperationStatus.Cancelled);

    public static CruiseCriteriaBulkEvaluationResult Success(
        int eligibleCount,
        IEnumerable<CruiseCriteriaSailingEvaluationResult> completed,
        int unprocessedCount = 0) =>
        new(
            CruiseAlertOperationStatus.Success,
            eligibleCount,
            Array.AsReadOnly(completed.ToArray()),
            unprocessedCount,
            null);

    public static CruiseCriteriaBulkEvaluationResult Cancelled(
        int eligibleCount = 0,
        IEnumerable<CruiseCriteriaSailingEvaluationResult>? completed = null,
        int unprocessedCount = 0) =>
        new(
            CruiseAlertOperationStatus.Cancelled,
            eligibleCount,
            Array.AsReadOnly((completed ?? []).ToArray()),
            unprocessedCount,
            "Saved criteria evaluation was cancelled.");

    public static CruiseCriteriaBulkEvaluationResult Failed() =>
        new(
            CruiseAlertOperationStatus.Failed,
            0,
            [],
            0,
            "Saved criteria could not be evaluated locally.");
}

public sealed record CruisePreferencesMutationAndAlertResult(
    PersonalCruisePreferenceMutationResult Mutation,
    CruiseCriteriaBulkEvaluationResult? SavedCriteriaAlerts)
{
    public bool EvaluationWasAttempted => SavedCriteriaAlerts is not null;
}
