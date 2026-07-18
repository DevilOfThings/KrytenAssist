using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Abstractions.Persistence;

public interface IFavouriteCruiseShipRepository
{
    Task<IReadOnlyList<CruiseShipKey>> ListAsync(CancellationToken cancellationToken = default);
    Task<bool> SetAsync(CruiseShipKey shipKey, bool isFavourite, CancellationToken cancellationToken = default);
}
