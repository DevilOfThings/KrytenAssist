extern alias KrytenInfrastructure;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using AlertRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseAlertRepository;
using CabinRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseCabinObservationRepository;
using DiscoveryRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseDiscoveryRepository;
using HistoryRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseObservationRepository;
using SavedRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteSavedCruiseRepository;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class Prompt041BoundaryTests
{
    private static readonly DateTimeOffset CheckedAt = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Discovery_evidence_remains_when_other_cruise_feature_rows_are_removed()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        var price = CruisePersistenceTestData.Observation();
        var cabin = CruiseCabinPersistenceTestData.Observation();
        var sailingKey = CruiseSailingKey.From(price);

        await using var context = database.CreateContext();
        await new DiscoveryRepository(context).RecordAsync(Check());
        await new HistoryRepository(context).RecordAsync(sailingKey, CruiseObservationFingerprint.From(price), price);
        await new CabinRepository(context).RecordAsync(cabin);
        await new SavedRepository(context).UpsertAsync(new SavedCruise(new SavedCruiseSnapshot(
            sailingKey, "Independent sailing", "Marella", new CruisePrice(900m, "GBP", "per person"), CheckedAt)));
        var candidate = new CruiseAlertCandidate(CruiseAlertType.Promotion, sailingKey, price.Source,
            new CruisePromotionAlertDetails(null, "Independent", "prompt-041-boundary"), CheckedAt, "prompt-041-boundary");
        await new AlertRepository(context).AddIfAbsentAsync(new CruiseAlert(Guid.NewGuid(), candidate, CheckedAt));

        context.CruiseHistories.RemoveRange(context.CruiseHistories);
        context.CruiseCabinSeries.RemoveRange(context.CruiseCabinSeries);
        context.SavedCruises.RemoveRange(context.SavedCruises);
        context.CruiseAlerts.RemoveRange(context.CruiseAlerts);
        await context.SaveChangesAsync();

        context.CruiseDiscoveryScopes.AsNoTracking().Should().ContainSingle();
        context.CruiseDiscoveryChecks.AsNoTracking().Should().ContainSingle();
        context.CruiseDiscoveryOccurrences.AsNoTracking().Should().ContainSingle();
        context.CruiseItineraryCatalogue.AsNoTracking().Should().ContainSingle();
    }

    [Fact]
    public async Task Removing_discovery_evidence_leaves_history_saved_cabin_and_alert_state_intact()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        var price = CruisePersistenceTestData.Observation();
        var cabin = CruiseCabinPersistenceTestData.Observation();
        var sailingKey = CruiseSailingKey.From(price);
        Guid alertId;

        await using (var context = database.CreateContext())
        {
            await new DiscoveryRepository(context).RecordAsync(Check());
            await new HistoryRepository(context).RecordAsync(sailingKey, CruiseObservationFingerprint.From(price), price);
            await new CabinRepository(context).RecordAsync(cabin);
            await new SavedRepository(context).UpsertAsync(new SavedCruise(new SavedCruiseSnapshot(
                sailingKey, "Retained sailing", "Marella", new CruisePrice(900m, "GBP", "per person"), CheckedAt)));
            var candidate = new CruiseAlertCandidate(CruiseAlertType.Promotion, sailingKey, price.Source,
                new CruisePromotionAlertDetails(null, "Retained", "prompt-041-retained"), CheckedAt, "prompt-041-retained");
            alertId = Guid.NewGuid();
            await new AlertRepository(context).AddIfAbsentAsync(new CruiseAlert(alertId, candidate, CheckedAt));

            context.CruiseItineraryCatalogue.RemoveRange(context.CruiseItineraryCatalogue);
            await context.SaveChangesAsync();
            context.CruiseDiscoveryScopes.Remove(await context.CruiseDiscoveryScopes.SingleAsync());
            await context.SaveChangesAsync();
        }

        await using var reopened = database.CreateContext();
        reopened.CruiseDiscoveryScopes.Should().BeEmpty();
        reopened.CruiseDiscoveryChecks.Should().BeEmpty();
        reopened.CruiseDiscoveryOccurrences.Should().BeEmpty();
        reopened.CruiseItineraryCatalogue.Should().BeEmpty();
        reopened.CruiseHistories.Should().ContainSingle();
        reopened.CruiseCabinSeries.Should().ContainSingle();
        (await new SavedRepository(reopened).GetAsync(sailingKey)).Should().NotBeNull();
        (await new AlertRepository(reopened).GetAsync(alertId)).Should().NotBeNull();
    }

    private static CruiseDiscoveryCheck Check() => new(
        new CruiseDiscoveryScope(new CruiseSource("tui", "TUI"), "marella", CruiseDiscoverySurface.CruisePackages, 3,
            [new CruiseDiscoveryCriterion("departure-airport", CruiseDiscoveryCriterionState.Known, ["stn"])]),
        CheckedAt,
        [new CruiseItineraryOccurrence(new CruiseItineraryKey("marella", "itinerary-041"), new CruiseSource("tui", "TUI"),
            CheckedAt, "prompt-041-evidence", "Boundary itinerary", "Explorer", new DateOnly(2027, 4, 1), 7,
            "Palma", "Mediterranean", "package-041", "https://www.tui.co.uk/cruise/bookitineraries/example?itineraryCode=itinerary-041")]);
}
