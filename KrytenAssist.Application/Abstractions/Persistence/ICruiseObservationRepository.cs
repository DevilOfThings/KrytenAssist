using KrytenAssist.Application.Cruises;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Abstractions.Persistence;

public interface ICruiseObservationRepository
{
    Task<CruiseObservationRepositoryRecordResult> RecordAsync(
        CruiseSailingKey sailingKey,
        CruiseObservationFingerprint fingerprint,
        CruiseObservation observation,
        CancellationToken cancellationToken = default);

    Task<CruiseRecordedHistory?> GetAsync(
        CruiseSailingKey sailingKey,
        CruiseSource? source,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CruiseRecordedHistory>> ListAsync(
        CancellationToken cancellationToken = default);
}
