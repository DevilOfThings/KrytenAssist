using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class RecordCruiseObservationAndEvaluateAlerts(
    RecordCruiseObservation recorder,
    GetCruiseHistory getHistory,
    EvaluateRecordedCruiseAlerts evaluateAlerts,
    EvaluateSavedCruiseCriteriaForSailing evaluateSavedCriteria)
{
    public async Task<CruiseRecordAndAlertResult> ExecuteAsync(
        CruiseObservation observation,
        DateTimeOffset alertCreatedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observation);

        var recording = await recorder.ExecuteAsync(observation, cancellationToken);
        if (recording.Status is CruiseObservationRecordStatus.Cancelled or
            CruiseObservationRecordStatus.Failed)
        {
            return new CruiseRecordAndAlertResult(recording, null);
        }

        CruiseAlertEvaluationResult? observationAlerts = null;

        if (recording.Status == CruiseObservationRecordStatus.ChangedObservationRecorded)
        {
            var historyResult = await getHistory.ExecuteAsync(
                CruiseSailingKey.From(observation),
                observation.Source,
                cancellationToken);
            if (historyResult.Status == CruiseHistoryQueryStatus.Cancelled)
            {
                observationAlerts = CruiseAlertEvaluationResult.Cancelled();
            }
            else if (historyResult.Status != CruiseHistoryQueryStatus.Found ||
                     historyResult.Details?.History is not { } history ||
                     history.Observations.Count < 2)
            {
                observationAlerts = CruiseAlertEvaluationResult.Failed();
            }
            else
            {
                var current = history.Observations[^1];
                observationAlerts = current.ObservedAt != observation.ObservedAt ||
                                    !CruiseObservationFingerprint.From(current).Equals(
                                        CruiseObservationFingerprint.From(observation))
                    ? CruiseAlertEvaluationResult.Success([], candidateCount: 0)
                    : await evaluateAlerts.ExecuteAsync(
                        history.Observations[^2],
                        current,
                        alertCreatedAt,
                        cancellationToken);
            }
        }

        var savedCriteriaAlerts = await evaluateSavedCriteria.ExecuteAsync(
            CruiseSailingKey.From(observation),
            alertCreatedAt,
            cancellationToken: cancellationToken);
        return new CruiseRecordAndAlertResult(
            recording,
            observationAlerts,
            savedCriteriaAlerts);
    }
}
