extern alias KrytenInfrastructure;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using CabinRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseCabinObservationRepository;
using HistoryRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseObservationRepository;
using AlertRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseAlertRepository;
using SavedRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteSavedCruiseRepository;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class Prompt040BoundaryTests
{
    [Fact]
    public async Task CabinEvidence_RemainsWhenHistorySavedCruiseAndAlertRowsAreRemoved()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        var cabin = CruiseCabinPersistenceTestData.Observation();
        var price = CruisePersistenceTestData.Observation();
        await using var context = database.CreateContext();
        await new CabinRepository(context).RecordAsync(cabin);
        await new HistoryRepository(context).RecordAsync(CruiseSailingKey.From(price), CruiseObservationFingerprint.From(price), price);
        await new SavedRepository(context).UpsertAsync(new SavedCruise(new SavedCruiseSnapshot(cabin.SailingKey,
            "Independent cabin sailing", "Marella", new CruisePrice(900m, "GBP", "per person"), cabin.ObservedAt)));
        var candidate = new CruiseAlertCandidate(CruiseAlertType.Promotion, cabin.SailingKey, cabin.Source,
            new CruisePromotionAlertDetails(null, "Independent", "boundary"), cabin.ObservedAt, "boundary");
        await new AlertRepository(context).AddIfAbsentAsync(new CruiseAlert(Guid.NewGuid(), candidate, cabin.ObservedAt));

        context.CruiseHistories.RemoveRange(context.CruiseHistories);
        context.SavedCruises.RemoveRange(context.SavedCruises);
        context.CruiseAlerts.RemoveRange(context.CruiseAlerts);
        await context.SaveChangesAsync();

        context.CruiseCabinSeries.AsNoTracking().Should().ContainSingle();
        context.CruiseCabinObservations.AsNoTracking().Should().ContainSingle();
        context.CruiseCabinObservationStates.AsNoTracking().Should().HaveCount(5);
    }
}
