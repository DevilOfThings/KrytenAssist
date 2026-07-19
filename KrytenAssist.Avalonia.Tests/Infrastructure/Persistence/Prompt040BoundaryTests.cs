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

    [Fact]
    public async Task Removing_cabin_history_leaves_price_personal_and_alert_state_intact()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        var priceObservation = CruisePersistenceTestData.Observation();
        var cabinObservation = CruiseCabinPersistenceTestData.Observation();
        var key = CruiseSailingKey.From(priceObservation);
        CruiseAlert alert;

        await using (var context = database.CreateContext())
        {
            await new HistoryRepository(context).RecordAsync(
                key,
                CruiseObservationFingerprint.From(priceObservation),
                priceObservation);
            var saved = new SavedCruise(
                new SavedCruiseSnapshot(key, "Retained sailing", "Marella", new CruisePrice(900m, "GBP", "per person"), priceObservation.ObservedAt),
                evaluation: new CruiseEvaluation(CruiseInterestLevel.StrongCandidate, notes: "Keep personal state"));
            await new SavedRepository(context).UpsertAsync(saved);
            await new CabinRepository(context).RecordAsync(cabinObservation);

            alert = new CruiseAlert(
                Guid.NewGuid(),
                new CruiseAlertCandidate(
                    CruiseAlertType.Promotion,
                    key,
                    priceObservation.Source,
                    new CruisePromotionAlertDetails(null, "Retained promotion", "retained-alert-evidence"),
                    priceObservation.ObservedAt,
                    "retained-alert-evidence"),
                priceObservation.ObservedAt.AddMinutes(1));
            await new AlertRepository(context).AddIfAbsentAsync(alert);

            context.CruiseCabinSeries.Remove(await context.CruiseCabinSeries.SingleAsync());
            await context.SaveChangesAsync();
        }

        await using var reopened = database.CreateContext();
        reopened.CruiseCabinSeries.Should().BeEmpty();
        reopened.CruiseCabinObservations.Should().BeEmpty();
        reopened.CruiseHistories.Should().ContainSingle();
        reopened.CruiseObservations.Should().ContainSingle();
        (await new SavedRepository(reopened).GetAsync(key))!.Evaluation.Notes.Should().Be("Keep personal state");
        (await new AlertRepository(reopened).GetAsync(alert.Id)).Should().Be(alert);
    }
}
