using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class SqliteSavedCruiseRepository(KrytenAssistDbContext dbContext) : ISavedCruiseRepository
{
    private const int MaximumAttempts = 3;

    public async Task<SavedCruise?> GetAsync(CruiseSailingKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key); cancellationToken.ThrowIfCancellationRequested();
        var entity = await Query(key).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<SavedCruise>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var values = await dbContext.SavedCruises.AsNoTracking().OrderBy(x => x.DepartureDate).ThenBy(x => x.OperatorId).ThenBy(x => x.ShipName).ThenBy(x => x.DurationNights).ToArrayAsync(cancellationToken);
        return Array.AsReadOnly(values.Select(Map).ToArray());
    }

    public async Task UpsertAsync(SavedCruise value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value); cancellationToken.ThrowIfCancellationRequested();
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                var entity = await Query(value.SailingKey).SingleOrDefaultAsync(cancellationToken);
                if (entity is null) { entity = new SavedCruiseEntity(); dbContext.SavedCruises.Add(entity); }
                Copy(value, entity);
                await dbContext.SaveChangesAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                await transaction.CommitAsync(cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < MaximumAttempts && IsTransient(ex))
            {
                dbContext.ChangeTracker.Clear();
                await Task.Delay(TimeSpan.FromMilliseconds(10 * attempt), cancellationToken);
            }
        }
    }

    public async Task<bool> RemoveAsync(CruiseSailingKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key); cancellationToken.ThrowIfCancellationRequested();
        return await Query(key).ExecuteDeleteAsync(cancellationToken) > 0;
    }

    private IQueryable<SavedCruiseEntity> Query(CruiseSailingKey key) => dbContext.SavedCruises.Where(x => x.OperatorId == key.OperatorId && x.ShipName == key.ShipName && x.DepartureDate == key.DepartureDate && x.DurationNights == key.DurationNights);

    private static SavedCruise Map(SavedCruiseEntity x)
    {
        var key = new CruiseSailingKey(x.OperatorId, x.ShipName, x.DepartureDate, x.DurationNights);
        var source = x.RetailSourceId is null ? null : new CruiseSource(x.RetailSourceId, x.RetailSourceName!);
        var snapshot = new SavedCruiseSnapshot(key, x.Title, x.OperatorName, new CruisePrice(x.DisplayedPriceAmount, x.DisplayedPriceCurrency, x.DisplayedPriceBasis), x.SavedAt, x.DeparturePort, x.ItinerarySummary, source, x.SourceReference);
        var evaluation = new CruiseEvaluation((CruiseInterestLevel?)x.InterestLevel, x.OverallRating, x.ItineraryRating, x.ShipRating, x.ValueRating, x.Notes);
        return new SavedCruise(snapshot, (SavedCruiseStatus)x.Status, evaluation, x.IsFavourite);
    }

    private static void Copy(SavedCruise value, SavedCruiseEntity x)
    {
        var s = value.Snapshot; var e = value.Evaluation;
        x.OperatorId = value.SailingKey.OperatorId; x.ShipName = value.SailingKey.ShipName; x.DepartureDate = value.SailingKey.DepartureDate; x.DurationNights = value.SailingKey.DurationNights;
        x.Title = s.Title; x.OperatorName = s.OperatorName; x.DeparturePort = s.DeparturePort; x.ItinerarySummary = s.ItinerarySummary;
        x.DisplayedPriceAmount = s.DisplayedPrice.Amount; x.DisplayedPriceCurrency = s.DisplayedPrice.Currency; x.DisplayedPriceBasis = s.DisplayedPrice.Basis;
        x.RetailSourceId = s.RetailSource?.Id; x.RetailSourceName = s.RetailSource?.Name; x.SourceReference = s.SourceReference; x.SavedAt = s.SavedAt;
        x.Status = (int)value.Status; x.InterestLevel = (int?)e.InterestLevel; x.OverallRating = e.OverallRating; x.ItineraryRating = e.ItineraryRating; x.ShipRating = e.ShipRating; x.ValueRating = e.ValueRating; x.Notes = e.Notes; x.IsFavourite = value.IsFavourite;
    }

    private static bool IsTransient(Exception ex) => ex is DbUpdateException { InnerException: SqliteException sql } && sql.SqliteErrorCode is 5 or 6 or 19 || ex is SqliteException direct && direct.SqliteErrorCode is 5 or 6;
}
