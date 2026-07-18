using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class SqliteFavouriteCruiseShipRepository(KrytenAssistDbContext dbContext) : IFavouriteCruiseShipRepository
{
    public async Task<IReadOnlyList<CruiseShipKey>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var values = await dbContext.FavouriteCruiseShips.AsNoTracking().OrderBy(x => x.OperatorId).ThenBy(x => x.ShipName).ToArrayAsync(cancellationToken);
        return Array.AsReadOnly(values.Select(x => new CruiseShipKey(x.OperatorId, x.ShipName)).ToArray());
    }

    public async Task<bool> SetAsync(CruiseShipKey key, bool isFavourite, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key); cancellationToken.ThrowIfCancellationRequested();
        var query = dbContext.FavouriteCruiseShips.Where(x => x.OperatorId == key.OperatorId && x.ShipName == key.ShipName);
        if (!isFavourite) return await query.ExecuteDeleteAsync(cancellationToken) > 0;
        if (await query.AnyAsync(cancellationToken)) return false;
        dbContext.FavouriteCruiseShips.Add(new FavouriteCruiseShipEntity { OperatorId = key.OperatorId, ShipName = key.ShipName });
        try { await dbContext.SaveChangesAsync(cancellationToken); return true; }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 }) { dbContext.ChangeTracker.Clear(); return false; }
    }
}
