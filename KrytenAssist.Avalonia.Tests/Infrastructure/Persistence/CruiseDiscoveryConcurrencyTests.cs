extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using Repository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseDiscoveryRepository;
using RecordResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseDiscoveryRepositoryRecordResult;
using RecordState = KrytenApplication::KrytenAssist.Application.Cruises.CruiseDiscoveryRecordState;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class CruiseDiscoveryConcurrencyTests
{
    private static readonly DateTimeOffset Time = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Concurrent_identical_first_check_seeds_one_baseline()
    {
        await using var database = new CruisePersistenceFileDatabase(); await database.MigrateAsync();
        var check = Check(Time, "known");
        var results = await RecordTogether(database, check, check);

        Assert.Equal([RecordState.BaselineSeeded, RecordState.AlreadyRecorded], results.Select(x => x.State).Order().ToArray());
        await using var context = database.CreateContext();
        Assert.Equal(1, await context.CruiseDiscoveryScopes.CountAsync()); Assert.Equal(1, await context.CruiseDiscoveryChecks.CountAsync()); Assert.Equal(1, await context.CruiseItineraryCatalogue.CountAsync());
    }

    [Fact]
    public async Task Concurrent_later_checks_detect_same_unseen_itinerary_once()
    {
        await using var database = new CruisePersistenceFileDatabase(); await database.MigrateAsync();
        await RecordOnce(database, Check(Time, "known"));
        var results = await RecordTogether(database, Check(Time.AddHours(1), "new"), Check(Time.AddHours(2), "new"));

        Assert.Equal(1, results.Sum(x => x.FirstObservedEvents.Count));
        Assert.Contains(results, x => x.State == RecordState.RecordedWithFirstObserved);
        await using var context = database.CreateContext();
        Assert.Equal(2, await context.CruiseItineraryCatalogue.CountAsync());
        Assert.Equal(1, await context.CruiseItineraryCatalogue.CountAsync(x => x.FirstObservedEventKey != null));
    }

    private static async Task<RecordResult[]> RecordTogether(CruisePersistenceFileDatabase database, CruiseDiscoveryCheck first, CruiseDiscoveryCheck second)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var one = RecordAfter(database, first, gate.Task); var two = RecordAfter(database, second, gate.Task); gate.SetResult(); return await Task.WhenAll(one, two);
    }
    private static async Task<RecordResult> RecordAfter(CruisePersistenceFileDatabase database, CruiseDiscoveryCheck check, Task gate) { await using var context = database.CreateContext(); await gate; return await new Repository(context).RecordAsync(check); }
    private static async Task RecordOnce(CruisePersistenceFileDatabase database, CruiseDiscoveryCheck check) { await using var context = database.CreateContext(); await new Repository(context).RecordAsync(check); }
    private static CruiseDiscoveryCheck Check(DateTimeOffset time, string id) => new(new CruiseDiscoveryScope(new("tui", "TUI"), "marella", CruiseDiscoverySurface.CruisePackages, 1), time, [new CruiseItineraryOccurrence(new("marella", id), new("tui", "TUI"), time, $"evidence-{id}")]);
}
