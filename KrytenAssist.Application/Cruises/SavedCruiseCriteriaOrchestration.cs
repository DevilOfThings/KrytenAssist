using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class EvaluateSavedCruiseCriteriaForSavedCruise(
    CruiseCriteriaEvidenceSelector evidenceSelector,
    EvaluateSavedCruiseCriteriaAlerts evaluate,
    GetCruisePreferences getPreferences,
    ListCruiseHistories listHistories)
{
    public Task<CruiseAlertEvaluationResult> ExecuteAsync(
        SavedCruise savedCruise,
        CruisePreferences preferences,
        IReadOnlyList<CruiseRecordedHistory> histories,
        DateTimeOffset alertCreatedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(savedCruise);
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(histories);
        var evidence = evidenceSelector.Select(savedCruise, histories);
        return evaluate.ExecuteAsync(
            savedCruise,
            preferences,
            evidence,
            alertCreatedAt,
            cancellationToken);
    }

    public async Task<CruiseAlertEvaluationResult> ExecuteAsync(
        SavedCruise savedCruise,
        DateTimeOffset alertCreatedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(savedCruise);
        var preferences = await getPreferences.ExecuteAsync(cancellationToken);
        if (preferences.Status == PersonalCruisePreferenceQueryStatus.Cancelled)
        {
            return CruiseAlertEvaluationResult.Cancelled();
        }

        if (preferences.Status != PersonalCruisePreferenceQueryStatus.Success ||
            preferences.Preferences is null)
        {
            return CruiseAlertEvaluationResult.Failed();
        }

        var histories = await listHistories.ExecuteAsync(cancellationToken);
        if (histories.Status == CruiseHistoryListStatus.Cancelled)
        {
            return CruiseAlertEvaluationResult.Cancelled();
        }

        if (histories.Status != CruiseHistoryListStatus.Success)
        {
            return CruiseAlertEvaluationResult.Failed();
        }

        return await ExecuteAsync(
            savedCruise,
            preferences.Preferences,
            histories.Histories.Select(item => item.History).ToArray(),
            alertCreatedAt,
            cancellationToken);
    }
}

public sealed class EvaluateSavedCruiseCriteriaForSailing(
    GetSavedCruise getSavedCruise,
    EvaluateSavedCruiseCriteriaForSavedCruise evaluate)
{
    public async Task<CruiseAlertEvaluationResult?> ExecuteAsync(
        CruiseSailingKey sailingKey,
        DateTimeOffset alertCreatedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sailingKey);
        var saved = await getSavedCruise.ExecuteAsync(sailingKey, cancellationToken);
        return saved.Status switch
        {
            SavedCruiseQueryStatus.NotFound => null,
            SavedCruiseQueryStatus.Cancelled => CruiseAlertEvaluationResult.Cancelled(),
            SavedCruiseQueryStatus.Found when saved.SavedCruise?.Status == SavedCruiseStatus.Shortlisted =>
                await evaluate.ExecuteAsync(saved.SavedCruise, alertCreatedAt, cancellationToken),
            SavedCruiseQueryStatus.Found => null,
            _ => CruiseAlertEvaluationResult.Failed()
        };
    }
}

public sealed class SaveCruiseAndEvaluateCriteria(
    SaveCruise saveCruise,
    EvaluateSavedCruiseCriteriaForSavedCruise evaluate)
{
    public async Task<SavedCruiseMutationAndAlertResult> ExecuteAsync(
        SavedCruiseSnapshot snapshot,
        DateTimeOffset alertCreatedAt,
        CancellationToken cancellationToken = default)
    {
        var mutation = await saveCruise.ExecuteAsync(snapshot, cancellationToken);
        if (mutation.Status is not (SavedCruiseMutationStatus.Created or
            SavedCruiseMutationStatus.Updated or SavedCruiseMutationStatus.Unchanged) ||
            mutation.SavedCruise?.Status != SavedCruiseStatus.Shortlisted)
        {
            return new SavedCruiseMutationAndAlertResult(mutation, null);
        }

        var alerts = await evaluate.ExecuteAsync(
            mutation.SavedCruise,
            alertCreatedAt,
            cancellationToken);
        return new SavedCruiseMutationAndAlertResult(mutation, alerts);
    }
}

public sealed class RestoreCruiseAndEvaluateCriteria(
    RestoreCruise restoreCruise,
    EvaluateSavedCruiseCriteriaForSavedCruise evaluate)
{
    public async Task<SavedCruiseMutationAndAlertResult> ExecuteAsync(
        CruiseSailingKey sailingKey,
        DateTimeOffset alertCreatedAt,
        CancellationToken cancellationToken = default)
    {
        var mutation = await restoreCruise.ExecuteAsync(sailingKey, cancellationToken);
        if (mutation.Status is not (SavedCruiseMutationStatus.Restored or
            SavedCruiseMutationStatus.Unchanged) ||
            mutation.SavedCruise?.Status != SavedCruiseStatus.Shortlisted)
        {
            return new SavedCruiseMutationAndAlertResult(mutation, null);
        }

        var alerts = await evaluate.ExecuteAsync(
            mutation.SavedCruise,
            alertCreatedAt,
            cancellationToken);
        return new SavedCruiseMutationAndAlertResult(mutation, alerts);
    }
}

public sealed class SaveCruisePreferencesAndEvaluateCriteria(
    SaveCruisePreferences savePreferences,
    ListSavedCruises listSavedCruises,
    ListCruiseHistories listHistories,
    EvaluateSavedCruiseCriteriaForSavedCruise evaluate)
{
    public async Task<CruisePreferencesMutationAndAlertResult> ExecuteAsync(
        CruisePreferences preferences,
        DateTimeOffset alertCreatedAt,
        CancellationToken cancellationToken = default)
    {
        var mutation = await savePreferences.ExecuteAsync(preferences, cancellationToken);
        if (mutation.Status is not (PersonalCruisePreferenceMutationStatus.Updated or
            PersonalCruisePreferenceMutationStatus.Unchanged))
        {
            return new CruisePreferencesMutationAndAlertResult(mutation, null);
        }

        var savedResult = await listSavedCruises.ExecuteAsync(cancellationToken);
        if (savedResult.Status == SavedCruiseListStatus.Cancelled)
        {
            return new CruisePreferencesMutationAndAlertResult(
                mutation,
                CruiseCriteriaBulkEvaluationResult.Cancelled());
        }

        if (savedResult.Status != SavedCruiseListStatus.Success)
        {
            return new CruisePreferencesMutationAndAlertResult(
                mutation,
                CruiseCriteriaBulkEvaluationResult.Failed());
        }

        var eligible = savedResult.SavedCruises
            .Where(saved => saved.Status == SavedCruiseStatus.Shortlisted)
            .OrderBy(saved => saved.SailingKey.DepartureDate)
            .ThenBy(saved => saved.SailingKey.OperatorId, StringComparer.Ordinal)
            .ThenBy(saved => saved.SailingKey.ShipName, StringComparer.Ordinal)
            .ThenBy(saved => saved.SailingKey.DurationNights)
            .ToArray();
        var historyResult = await listHistories.ExecuteAsync(cancellationToken);
        if (historyResult.Status == CruiseHistoryListStatus.Cancelled)
        {
            return new CruisePreferencesMutationAndAlertResult(
                mutation,
                CruiseCriteriaBulkEvaluationResult.Cancelled(
                    eligible.Length,
                    unprocessedCount: eligible.Length));
        }

        if (historyResult.Status != CruiseHistoryListStatus.Success)
        {
            return new CruisePreferencesMutationAndAlertResult(
                mutation,
                CruiseCriteriaBulkEvaluationResult.Failed());
        }

        var histories = historyResult.Histories.Select(item => item.History).ToArray();
        var completed = new List<CruiseCriteriaSailingEvaluationResult>();
        foreach (var saved in eligible)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new CruisePreferencesMutationAndAlertResult(
                    mutation,
                    CruiseCriteriaBulkEvaluationResult.Cancelled(
                        eligible.Length,
                        completed,
                        eligible.Length - completed.Count));
            }

            var result = await evaluate.ExecuteAsync(
                saved,
                preferences,
                histories,
                alertCreatedAt,
                cancellationToken);
            completed.Add(new CruiseCriteriaSailingEvaluationResult(saved.SailingKey, result));
            if (result.Status == CruiseAlertOperationStatus.Cancelled)
            {
                return new CruisePreferencesMutationAndAlertResult(
                    mutation,
                    CruiseCriteriaBulkEvaluationResult.Cancelled(
                        eligible.Length,
                        completed,
                        eligible.Length - completed.Count));
            }
        }

        return new CruisePreferencesMutationAndAlertResult(
            mutation,
            CruiseCriteriaBulkEvaluationResult.Success(eligible.Length, completed));
    }
}
