using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class MaterializeCruiseAlertCandidates(ICruiseAlertRepository repository)
{
    public async Task<CruiseAlertEvaluationResult> ExecuteAsync(IEnumerable<CruiseAlertCandidate> candidates, DateTimeOffset createdAt, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(candidates); if (token.IsCancellationRequested) return CruiseAlertEvaluationResult.Cancelled();
        try { var candidateArray = candidates.ToArray(); var created = new List<CruiseAlert>(); var existing = 0; foreach (var candidate in candidateArray) { var alert = new CruiseAlert(Guid.NewGuid(), candidate, createdAt); var result = await repository.AddIfAbsentAsync(alert, token); if (result.Created) created.Add(result.Alert); else existing++; } return CruiseAlertEvaluationResult.Success(created, existing, candidateCount: candidateArray.Length); }
        catch (OperationCanceledException) { return CruiseAlertEvaluationResult.Cancelled(); } catch { return CruiseAlertEvaluationResult.Failed(); }
    }
}

public sealed class EvaluateRecordedCruiseAlerts(CruiseObservationAlertDetector detector, ICruiseAlertSettingsRepository settings, MaterializeCruiseAlertCandidates materialize)
{
    public async Task<CruiseAlertEvaluationResult> ExecuteAsync(CruiseObservation previous, CruiseObservation current, DateTimeOffset createdAt, CancellationToken token = default)
    { if (token.IsCancellationRequested) return CruiseAlertEvaluationResult.Cancelled(); try { var candidates = detector.Detect(previous, current, await settings.GetAsync(token)); return await materialize.ExecuteAsync(candidates, createdAt, token); } catch (OperationCanceledException) { return CruiseAlertEvaluationResult.Cancelled(); } catch { return CruiseAlertEvaluationResult.Failed(); } }
}

public sealed class EvaluateSavedCruiseCriteriaAlerts(SavedCruiseCriteriaAlertDetector detector, ICruiseAlertSettingsRepository settings, ISavedCruiseCriteriaStateRepository states, MaterializeCruiseAlertCandidates materialize)
{
    public async Task<CruiseAlertEvaluationResult> ExecuteAsync(SavedCruise saved, CruisePreferences preferences, CruiseCriteriaEvidence evidence, DateTimeOffset createdAt, CancellationToken token = default)
    {
        if (token.IsCancellationRequested)
        {
            return CruiseAlertEvaluationResult.Cancelled();
        }

        try
        {
            var currentSettings = await settings.GetAsync(token);
            var fingerprint = SavedCruiseCriteriaAlertDetector.CriteriaFingerprint(
                preferences,
                currentSettings,
                evidence.CabinObservation?.SailingKey == saved.SailingKey
                    ? evidence.CabinObservation.SearchContext.Fingerprint
                    : null);
            var prior = await states.GetAsync(saved.SailingKey, fingerprint, token);
            var detected = detector.Detect(
                saved,
                preferences,
                currentSettings,
                evidence,
                prior);
            if (detected.Candidate is null)
            {
                await states.UpsertAsync(detected.State, token);
                return CruiseAlertEvaluationResult.Success([], state: detected.State);
            }

            var result = await materialize.ExecuteAsync(
                [detected.Candidate],
                createdAt,
                token);
            if (result.Status == CruiseAlertOperationStatus.Success)
            {
                await states.UpsertAsync(detected.State, token);
            }

            return result with { CriteriaState = detected.State };
        }
        catch (OperationCanceledException)
        {
            return CruiseAlertEvaluationResult.Cancelled();
        }
        catch
        {
            return CruiseAlertEvaluationResult.Failed();
        }
    }
}
