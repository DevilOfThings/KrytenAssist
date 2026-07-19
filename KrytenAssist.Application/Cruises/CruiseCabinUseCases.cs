using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class RecordCruiseCabinObservation(ICruiseCabinObservationRepository repository, CruiseCabinHistoryAnalyzer analyzer)
{
    public async Task<CruiseCabinRecordResult> ExecuteAsync(CruiseCabinObservation observation, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if (token.IsCancellationRequested) return CruiseCabinRecordResult.Cancelled();
        try
        {
            var result = await repository.RecordAsync(observation, token);
            var status = result.State switch
            {
                CruiseCabinRepositoryRecordState.FirstObservationRecorded => CruiseCabinOperationStatus.FirstObservationRecorded,
                CruiseCabinRepositoryRecordState.ChangedObservationRecorded => CruiseCabinOperationStatus.ChangedObservationRecorded,
                CruiseCabinRepositoryRecordState.AlreadyCurrent => CruiseCabinOperationStatus.AlreadyCurrent,
                _ => throw new InvalidOperationException("Unknown cabin repository state.")
            };
            return CruiseCabinRecordResult.Recorded(status, analyzer.Analyze(result.History.Observations, result.History.LastSeenAt));
        }
        catch (OperationCanceledException) { return CruiseCabinRecordResult.Cancelled(); }
        catch { return CruiseCabinRecordResult.Failed(); }
    }
}

public sealed class GetCruiseCabinHistory(ICruiseCabinObservationRepository repository, CruiseCabinHistoryAnalyzer analyzer)
{
    public async Task<CruiseCabinHistoryQueryResult> ExecuteAsync(string seriesKey, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seriesKey);
        if (token.IsCancellationRequested) return new(CruiseCabinOperationStatus.Cancelled, null, "Loading cabin history was cancelled.");
        try
        {
            var history = await repository.GetAsync(seriesKey, token);
            return history is null
                ? new(CruiseCabinOperationStatus.NotFound, null, null)
                : new(CruiseCabinOperationStatus.Found, new(history, analyzer.Analyze(history.Observations, history.LastSeenAt)), null);
        }
        catch (OperationCanceledException) { return new(CruiseCabinOperationStatus.Cancelled, null, "Loading cabin history was cancelled."); }
        catch { return new(CruiseCabinOperationStatus.Failed, null, "Cabin history could not be loaded locally."); }
    }
}

public sealed class ListCruiseCabinHistories(ICruiseCabinObservationRepository repository, CruiseCabinHistoryAnalyzer analyzer)
{
    public async Task<CruiseCabinHistoryListResult> ExecuteAsync(CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return new(CruiseCabinOperationStatus.Cancelled, [], "Loading cabin histories was cancelled.");
        try
        {
            var histories = (await repository.ListAsync(token)).Select(history => new CruiseCabinHistoryDetails(
                history, analyzer.Analyze(history.Observations, history.LastSeenAt)))
                .OrderBy(value => value.History.LatestObservation.SailingKey.DepartureDate)
                .ThenBy(value => value.History.LatestObservation.SailingKey.OperatorId, StringComparer.Ordinal)
                .ThenBy(value => value.History.LatestObservation.SailingKey.ShipName, StringComparer.Ordinal)
                .ThenBy(value => value.History.SeriesKey, StringComparer.Ordinal).ToArray();
            return new(CruiseCabinOperationStatus.Success, Array.AsReadOnly(histories), null);
        }
        catch (OperationCanceledException) { return new(CruiseCabinOperationStatus.Cancelled, [], "Loading cabin histories was cancelled."); }
        catch { return new(CruiseCabinOperationStatus.Failed, [], "Cabin histories could not be loaded locally."); }
    }
}

public sealed class EvaluateCruiseCabinAvailabilityAlerts(
    CruiseCabinAvailabilityAlertDetector detector,
    ICruiseAlertSettingsRepository settingsRepository)
{
    public async Task<CruiseCabinAlertCandidateResult> ExecuteAsync(CruiseCabinObservation previous,
        CruiseCabinObservation current, SavedCruise? savedCruise, CruisePreferences preferences,
        CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return CruiseCabinAlertCandidateResult.Cancelled();
        try
        {
            var settings = await settingsRepository.GetAsync(token);
            return CruiseCabinAlertCandidateResult.Success(detector.Detect(previous, current, savedCruise, preferences, settings));
        }
        catch (OperationCanceledException) { return CruiseCabinAlertCandidateResult.Cancelled(); }
        catch { return CruiseCabinAlertCandidateResult.Failed(); }
    }
}

public sealed class EvaluateRecordedCruiseCabinAlerts(
    CruiseCabinAvailabilityAlertDetector detector,
    ICruiseAlertSettingsRepository settingsRepository,
    MaterializeCruiseAlertCandidates materialize)
{
    public async Task<CruiseAlertEvaluationResult> ExecuteAsync(
        CruiseCabinObservation previous,
        CruiseCabinObservation current,
        SavedCruise? savedCruise,
        CruisePreferences preferences,
        DateTimeOffset alertCreatedAt,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(preferences);
        if (token.IsCancellationRequested) return CruiseAlertEvaluationResult.Cancelled();
        try
        {
            var settings = await settingsRepository.GetAsync(token);
            var candidates = detector.Detect(previous, current, savedCruise, preferences, settings);
            return await materialize.ExecuteAsync(candidates, alertCreatedAt, token);
        }
        catch (OperationCanceledException) { return CruiseAlertEvaluationResult.Cancelled(); }
        catch { return CruiseAlertEvaluationResult.Failed(); }
    }
}

public sealed class RecordCruiseCabinObservationAndEvaluateAlerts(
    ICruiseCabinObservationRepository repository,
    RecordCruiseCabinObservation record,
    GetSavedCruise getSavedCruise,
    GetCruisePreferences getPreferences,
    EvaluateRecordedCruiseCabinAlerts evaluateCabinAlerts,
    EvaluateSavedCruiseCriteriaForSailing evaluateSavedCriteria)
{
    public async Task<CruiseCabinRecordAndAlertResult> ExecuteAsync(
        CruiseCabinObservation observation,
        DateTimeOffset alertCreatedAt,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var recording = await record.ExecuteAsync(observation, token);
        if (!recording.Status.Equals(CruiseCabinOperationStatus.FirstObservationRecorded) &&
            !recording.Status.Equals(CruiseCabinOperationStatus.ChangedObservationRecorded) &&
            !recording.Status.Equals(CruiseCabinOperationStatus.AlreadyCurrent))
            return new(recording, null, null);

        CruiseCabinRecordedHistory? committedHistory;
        try { committedHistory = await repository.GetAsync(observation.SeriesKey, token); }
        catch (OperationCanceledException) { return new(recording, CruiseAlertEvaluationResult.Cancelled(), CruiseAlertEvaluationResult.Cancelled()); }
        catch { return new(recording, CruiseAlertEvaluationResult.Failed(), CruiseAlertEvaluationResult.Failed()); }

        if (committedHistory is null || committedHistory.Observations.Count == 0)
            return new(recording, CruiseAlertEvaluationResult.Failed(), CruiseAlertEvaluationResult.Failed());
        var committedCurrent = committedHistory.LatestObservation;
        var incomingIsCurrent = committedCurrent.SeriesKey == observation.SeriesKey &&
            committedCurrent.StateFingerprint == observation.StateFingerprint;

        CruiseAlertEvaluationResult? cabinAlerts = null;
        if (incomingIsCurrent && committedHistory.Observations.Count >= 2)
        {
            if (token.IsCancellationRequested)
                cabinAlerts = CruiseAlertEvaluationResult.Cancelled();
            else
            {
                var savedResult = await getSavedCruise.ExecuteAsync(committedCurrent.SailingKey, token);
                var preferencesResult = await getPreferences.ExecuteAsync(token);
                if (savedResult.Status == SavedCruiseQueryStatus.Cancelled ||
                    preferencesResult.Status == PersonalCruisePreferenceQueryStatus.Cancelled)
                    cabinAlerts = CruiseAlertEvaluationResult.Cancelled();
                else if (savedResult.Status == SavedCruiseQueryStatus.Failed ||
                    preferencesResult.Status != PersonalCruisePreferenceQueryStatus.Success ||
                    preferencesResult.Preferences is null)
                    cabinAlerts = CruiseAlertEvaluationResult.Failed();
                else
                    cabinAlerts = await evaluateCabinAlerts.ExecuteAsync(
                        committedHistory.Observations[^2], committedCurrent,
                        savedResult.SavedCruise, preferencesResult.Preferences,
                        alertCreatedAt, token);
            }
        }

        CruiseAlertEvaluationResult? savedCriteria;
        if (token.IsCancellationRequested)
            savedCriteria = CruiseAlertEvaluationResult.Cancelled();
        else
            savedCriteria = await evaluateSavedCriteria.ExecuteAsync(
                committedCurrent.SailingKey, alertCreatedAt, committedCurrent, token);
        return new(recording, cabinAlerts, savedCriteria);
    }
}
