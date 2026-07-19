using KrytenAssist.Application.Cruises;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Abstractions.Persistence;

public interface ICruiseDiscoveryRepository
{
    Task<CruiseDiscoveryRepositoryRecordResult> RecordAsync(CruiseDiscoveryCheck check, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CruiseItineraryCatalogueEntry>> ListFirstObservedAsync(CancellationToken cancellationToken = default);
    Task<CruiseItineraryCatalogueEntry?> GetAsync(string catalogueKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CruiseDiscoveryCheck>> ListChecksAsync(CancellationToken cancellationToken = default);
}
