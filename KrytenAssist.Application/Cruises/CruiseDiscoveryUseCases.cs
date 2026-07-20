using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class RecordCruiseDiscoveryCheck(ICruiseDiscoveryRepository repository)
{
    public async Task<CruiseDiscoveryRecordResult> ExecuteAsync(Core.Cruises.CruiseDiscoveryCheck check, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(check);
        if (token.IsCancellationRequested) return CruiseDiscoveryRecordResult.Cancelled();
        try
        {
            var result = await repository.RecordAsync(check, token);
            var status = result.State switch
            {
                CruiseDiscoveryRecordState.BaselineSeeded => CruiseDiscoveryOperationStatus.BaselineSeeded,
                CruiseDiscoveryRecordState.RecordedNoNewItineraries => CruiseDiscoveryOperationStatus.RecordedNoNewItineraries,
                CruiseDiscoveryRecordState.RecordedWithFirstObserved => CruiseDiscoveryOperationStatus.RecordedWithFirstObserved,
                CruiseDiscoveryRecordState.AlreadyRecorded => CruiseDiscoveryOperationStatus.AlreadyRecorded,
                _ => throw new InvalidOperationException("Unknown discovery repository state.")
            };
            return new(status, result.Check, result.FirstObservedEvents, null);
        }
        catch (OperationCanceledException) { return CruiseDiscoveryRecordResult.Cancelled(); }
        catch { return CruiseDiscoveryRecordResult.Failed(); }
    }
}

public sealed class RecordCruiseDiscoveryCheckAndEvaluateAlerts(
    RecordCruiseDiscoveryCheck record,
    ICruiseAlertSettingsRepository settings,
    CruiseNewItineraryAlertDetector detector,
    MaterializeCruiseAlertCandidates materialize)
{
    public async Task<CruiseDiscoveryRecordingAndAlertResult> ExecuteAsync(
        Core.Cruises.CruiseDiscoveryCheck check, DateTimeOffset createdAt, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(check);
        var recording = await record.ExecuteAsync(check, token);
        if (recording.Status is CruiseDiscoveryOperationStatus.Cancelled or CruiseDiscoveryOperationStatus.Failed)
            return new(recording, CruiseDiscoveryAlertEvaluationStatus.NotRequired, null);
        if (recording.Status is CruiseDiscoveryOperationStatus.BaselineSeeded or CruiseDiscoveryOperationStatus.RecordedNoNewItineraries ||
            recording.FirstObservedEvents.Count == 0)
            return new(recording, CruiseDiscoveryAlertEvaluationStatus.NotRequired, null);

        try
        {
            var currentSettings = await settings.GetAsync(token);
            if (!currentSettings.NewItineraryEnabled)
                return new(recording, CruiseDiscoveryAlertEvaluationStatus.Disabled, null);
            var candidates = detector.Detect(recording.FirstObservedEvents, currentSettings);
            var alerts = await materialize.ExecuteAsync(candidates, createdAt, token);
            var status = alerts.Status switch
            {
                CruiseAlertOperationStatus.Success => CruiseDiscoveryAlertEvaluationStatus.Success,
                CruiseAlertOperationStatus.Cancelled => CruiseDiscoveryAlertEvaluationStatus.Cancelled,
                _ => CruiseDiscoveryAlertEvaluationStatus.Failed
            };
            return new(recording, status, alerts);
        }
        catch (OperationCanceledException)
        {
            return new(recording, CruiseDiscoveryAlertEvaluationStatus.Cancelled, null);
        }
        catch
        {
            return new(recording, CruiseDiscoveryAlertEvaluationStatus.Failed, null);
        }
    }
}

public sealed class ListFirstObservedCruiseItineraries(ICruiseDiscoveryRepository repository)
{
    public async Task<CruiseItineraryListResult> ExecuteAsync(CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return new(CruiseDiscoveryOperationStatus.Cancelled, [], "Loading new itineraries was cancelled.");
        try
        {
            var entries = (await repository.ListFirstObservedAsync(token)).OrderByDescending(x => x.FirstSeenAt)
                .ThenBy(x => x.CatalogueKey.PersistenceKey, StringComparer.Ordinal).ToArray();
            return new(CruiseDiscoveryOperationStatus.Success, Array.AsReadOnly(entries), null);
        }
        catch (OperationCanceledException) { return new(CruiseDiscoveryOperationStatus.Cancelled, [], "Loading new itineraries was cancelled."); }
        catch { return new(CruiseDiscoveryOperationStatus.Failed, [], "New itineraries could not be loaded locally."); }
    }
}

public sealed class GetCruiseItineraryDiscovery(ICruiseDiscoveryRepository repository)
{
    public async Task<CruiseItineraryQueryResult> ExecuteAsync(string catalogueKey, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogueKey);
        if (token.IsCancellationRequested) return new(CruiseDiscoveryOperationStatus.Cancelled, null, "Loading itinerary evidence was cancelled.");
        try
        {
            var entry = await repository.GetAsync(catalogueKey, token);
            return new(entry is null ? CruiseDiscoveryOperationStatus.NotFound : CruiseDiscoveryOperationStatus.Found, entry, null);
        }
        catch (OperationCanceledException) { return new(CruiseDiscoveryOperationStatus.Cancelled, null, "Loading itinerary evidence was cancelled."); }
        catch { return new(CruiseDiscoveryOperationStatus.Failed, null, "Itinerary evidence could not be loaded locally."); }
    }
}

public sealed class ListCruiseDiscoveryChecks(ICruiseDiscoveryRepository repository)
{
    public async Task<CruiseDiscoveryCheckListResult> ExecuteAsync(CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return new(CruiseDiscoveryOperationStatus.Cancelled, [], "Loading discovery checks was cancelled.");
        try
        {
            var checks = (await repository.ListChecksAsync(token)).OrderByDescending(x => x.ObservedAt)
                .ThenBy(x => x.EvidenceKey, StringComparer.Ordinal).ToArray();
            return new(CruiseDiscoveryOperationStatus.Success, Array.AsReadOnly(checks), null);
        }
        catch (OperationCanceledException) { return new(CruiseDiscoveryOperationStatus.Cancelled, [], "Loading discovery checks was cancelled."); }
        catch { return new(CruiseDiscoveryOperationStatus.Failed, [], "Discovery checks could not be loaded locally."); }
    }
}
