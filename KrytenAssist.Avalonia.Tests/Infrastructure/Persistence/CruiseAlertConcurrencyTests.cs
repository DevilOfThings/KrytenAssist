extern alias KrytenInfrastructure;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using AlertRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseAlertRepository;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class CruiseAlertConcurrencyTests
{
    [Fact]
    public async Task ConcurrentFirstInsert_ConvergesToOneAlertAndOneCreatedResult()
    {
        await using var database = new CruisePersistenceFileDatabase(); await database.MigrateAsync();
        var time = DateTimeOffset.UtcNow; var key = new CruiseSailingKey("operator", "ship", new DateOnly(2027, 1, 2), 7);
        var candidate = new CruiseAlertCandidate(CruiseAlertType.Promotion, key, new CruiseSource("retailer", "Retailer"), new CruisePromotionAlertDetails(null, "Offer", "evidence"), time, "evidence");
        var firstAlert = new CruiseAlert(Guid.NewGuid(), candidate, time); var secondAlert = new CruiseAlert(Guid.NewGuid(), candidate, time.AddSeconds(1));
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = Insert(database, firstAlert, gate.Task); var second = Insert(database, secondAlert, gate.Task); gate.SetResult();

        var results = await Task.WhenAll(first, second);

        results.Count(x => x).Should().Be(1);
        await using var context = database.CreateContext();
        (await context.CruiseAlerts.CountAsync()).Should().Be(1); (await context.CruisePromotionAlertDetails.CountAsync()).Should().Be(1);
    }

    private static async Task<bool> Insert(CruisePersistenceFileDatabase database, CruiseAlert alert, Task gate)
    {
        await using var context = database.CreateContext(); await gate;
        return (await new AlertRepository(context).AddIfAbsentAsync(alert)).Created;
    }
}
