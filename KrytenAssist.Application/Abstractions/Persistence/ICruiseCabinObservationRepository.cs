using KrytenAssist.Application.Cruises;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Abstractions.Persistence;

public interface ICruiseCabinObservationRepository
{
    Task<CruiseCabinRepositoryRecordResult> RecordAsync(CruiseCabinObservation observation, CancellationToken cancellationToken = default);
    Task<CruiseCabinRecordedHistory?> GetAsync(string seriesKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CruiseCabinRecordedHistory>> ListAsync(CancellationToken cancellationToken = default);
}
