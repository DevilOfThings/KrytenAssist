extern alias KrytenInfrastructure;

using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using FavouriteRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteFavouriteCruiseShipRepository;
using SavedRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteSavedCruiseRepository;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class PersonalCruiseConcurrencyTests
{
    [Fact]
    public async Task Concurrent_first_saves_converge_to_one_sailing()
    {
        await using var database = new CruisePersistenceFileDatabase(); await database.MigrateAsync();
        var value = Saved(); var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = SaveAfterGate(database, value, gate.Task); var second = SaveAfterGate(database, value, gate.Task); gate.SetResult();
        await Task.WhenAll(first, second);
        await using var context = database.CreateContext();
        Assert.Equal(1, await context.SavedCruises.CountAsync());
        Assert.Equal(0, await context.CruiseHistories.CountAsync());
    }

    [Fact]
    public async Task Concurrent_favourites_converge_to_one_ship()
    {
        await using var database = new CruisePersistenceFileDatabase(); await database.MigrateAsync(); var key = new CruiseShipKey("marella", "voyager");
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = FavouriteAfterGate(database, key, gate.Task); var second = FavouriteAfterGate(database, key, gate.Task); gate.SetResult();
        var changes = await Task.WhenAll(first, second);
        Assert.Single(changes, changed => changed);
        await using var context = database.CreateContext(); Assert.Equal(1, await context.FavouriteCruiseShips.CountAsync());
    }

    private static async Task SaveAfterGate(CruisePersistenceFileDatabase database, SavedCruise value, Task gate)
    { await using var context = database.CreateContext(); await gate; await new SavedRepository(context).UpsertAsync(value); }
    private static async Task<bool> FavouriteAfterGate(CruisePersistenceFileDatabase database, CruiseShipKey key, Task gate)
    { await using var context = database.CreateContext(); await gate; return await new FavouriteRepository(context).SetAsync(key, true); }
    private static SavedCruise Saved()
    {
        var key = new CruiseSailingKey("marella", "voyager", new DateOnly(2027, 8, 2), 7);
        return new SavedCruise(new SavedCruiseSnapshot(key, "Escape", "Marella", new CruisePrice(999, "GBP"), new DateTimeOffset(2026, 7, 18, 9, 0, 0, TimeSpan.Zero)));
    }
}
