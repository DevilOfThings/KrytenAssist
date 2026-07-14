using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public interface ICruiseOfTheWeekProvider
{
    Task<CruiseObservation> GetCurrentAsync(
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default);
}
