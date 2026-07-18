using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class RecordCruiseObservationAndEvaluateAlerts(
    RecordCruiseObservation recorder,
    GetCruiseHistory getHistory,
    EvaluateRecordedCruiseAlerts evaluateAlerts)
{
    public async Task<CruiseRecordAndAlertResult> ExecuteAsync(
        CruiseObservation observation,
        DateTimeOffset alertCreatedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observation);

        var recording = await recorder.ExecuteAsync(observation, cancellationToken);
        if (recording.Status is not CruiseObservationRecordStatus.ChangedObservationRecorded)
        {
            return new CruiseRecordAndAlertResult(recording, null);
        }

        var historyResult = await getHistory.ExecuteAsync(
            CruiseSailingKey.From(observation),
            observation.Source,
            cancellationToken);
        if (historyResult.Status == CruiseHistoryQueryStatus.Cancelled)
        {
            return new CruiseRecordAndAlertResult(recording, CruiseAlertEvaluationResult.Cancelled());
        }

        if (historyResult.Status != CruiseHistoryQueryStatus.Found ||
            historyResult.Details?.History is not { } history)
        {
            return new CruiseRecordAndAlertResult(recording, CruiseAlertEvaluationResult.Failed());
        }

        var observations = history.Observations;
        if (observations.Count < 2)
        {
            return new CruiseRecordAndAlertResult(recording, CruiseAlertEvaluationResult.Failed());
        }

        var current = observations[^1];
        if (current.ObservedAt != observation.ObservedAt ||
            !CruiseObservationFingerprint.From(current).Equals(
                CruiseObservationFingerprint.From(observation)))
        {
            return new CruiseRecordAndAlertResult(
                recording,
                CruiseAlertEvaluationResult.Success([], candidateCount: 0));
        }

        var alerts = await evaluateAlerts.ExecuteAsync(
            observations[^2],
            current,
            alertCreatedAt,
            cancellationToken);
        return new CruiseRecordAndAlertResult(recording, alerts);
    }
}
