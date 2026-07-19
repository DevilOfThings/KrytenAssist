using KrytenAssist.Application.Abstractions.Persistence;

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
