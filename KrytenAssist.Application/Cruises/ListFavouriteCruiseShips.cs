using KrytenAssist.Application.Abstractions.Persistence;

namespace KrytenAssist.Application.Cruises;

public sealed class ListFavouriteCruiseShips(IFavouriteCruiseShipRepository repository)
{
    public async Task<FavouriteCruiseShipListResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return FavouriteCruiseShipListResult.Cancelled();
        try { return FavouriteCruiseShipListResult.Success((await repository.ListAsync(cancellationToken)).OrderBy(x => x.OperatorId, StringComparer.Ordinal).ThenBy(x => x.ShipName, StringComparer.Ordinal)); }
        catch (OperationCanceledException) { return FavouriteCruiseShipListResult.Cancelled(); }
        catch (Exception) { return FavouriteCruiseShipListResult.Failed(); }
    }
}
