extern alias KrytenInfrastructure;

using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Repository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseObservationRepository;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class CruiseHistoryHardeningMigrationTests
{
    private const string CruisePersistenceMigration = "20260717082520_AddCruiseHistoryPersistence";

    [Fact]
    public async Task UpgradeFrom037c_PreservesRowsAndInitializesSequenceAndLatestEvidence()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAsync();
        await using (var oldContext = database.CreateContext())
        {
            await oldContext.Database.GetService<IMigrator>().MigrateAsync(CruisePersistenceMigration);
            await Seed037cHistoryAsync(oldContext);
            await oldContext.Database.MigrateAsync();
        }

        await using var context = database.CreateContext();
        var history = await context.CruiseHistories.AsNoTracking().SingleAsync();
        var observations = await context.CruiseObservations
            .AsNoTracking()
            .OrderBy(item => item.Sequence)
            .ToArrayAsync();

        Assert.Equal([1, 2], observations.Select(item => item.Sequence));
        Assert.Equal(["fingerprint-a", "fingerprint-b"], observations.Select(item => item.Fingerprint));
        Assert.Equal("offer-b", history.LatestProviderOfferId);
        Assert.Equal("https://example.test/b", history.LatestSourceReference);
        Assert.Equal(new DateTimeOffset(2026, 7, 17, 14, 35, 21, TimeSpan.FromHours(5.5)), history.LatestEvidenceObservedAt);
        Assert.Equal(2, await context.CruiseObservationPrices.CountAsync());

        var indexes = await ReadObservationIndexesAsync(database);
        Assert.Contains("IX_CruiseObservations_History_Fingerprint", indexes);
        Assert.Contains("UX_CruiseObservations_History_Sequence", indexes);
        Assert.DoesNotContain("UX_CruiseObservations_History_Fingerprint", indexes);
    }

    [Fact]
    public async Task UpgradeFrom037c_AllowsReturnToPriorState()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAsync();
        await using (var oldContext = database.CreateContext())
        {
            await oldContext.Database.GetService<IMigrator>().MigrateAsync(CruisePersistenceMigration);
            await Seed037cHistoryAsync(oldContext);
            await oldContext.Database.MigrateAsync();
        }

        var returned = CruisePersistenceTestData.Observation(
            amount: 988m,
            observedAt: new DateTimeOffset(2026, 7, 18, 14, 35, 21, TimeSpan.FromHours(5.5)),
            providerOfferId: "offer-returned",
            sourceReference: "https://example.test/returned");
        await using var context = database.CreateContext();
        var result = await new Repository(context).RecordAsync(
            CruiseSailingKey.From(returned),
            CruiseObservationFingerprint.From(returned),
            returned);
        var sequences = await context.CruiseObservations
            .OrderBy(item => item.Sequence)
            .Select(item => item.Sequence)
            .ToArrayAsync();

        Assert.Equal(3, result.History.Observations.Count);
        Assert.Equal([1, 2, 3], sequences);
    }

    private static Task Seed037cHistoryAsync(DbContext context) =>
        context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO CruiseHistories
                (Id, OperatorId, NormalizedShipName, DepartureDate, DurationNights,
                 RetailSourceId, RetailSourceName, FirstObservedAt, LastSeenAt)
            VALUES
                (1, 'marella', 'marella discovery 2', '2026-12-18', 7,
                 'tui', 'TUI', '2026-07-16T14:35:21.0000000+05:30',
                 '2026-07-17T14:35:21.0000000+05:30');

            INSERT INTO CruiseObservations
                (Id, CruiseHistoryId, Fingerprint, ProviderOfferId, OperatorName,
                 Title, ShipName, DepartureDate, DurationNights, DeparturePort,
                 ItinerarySummary, PromotionSummary, SourceReference, ObservedAt)
            VALUES
                (1, 1, 'fingerprint-a', 'offer-a', 'Marella Cruises',
                 'Canarian Flavours', 'Marella Discovery 2', '2026-12-18', 7,
                 'Santa Cruz, Tenerife', 'Tenerife and Madeira', 'First offer',
                 'https://example.test/a', '2026-07-16T14:35:21.0000000+05:30'),
                (2, 1, 'fingerprint-b', 'offer-b', 'Marella Cruises',
                 'Canarian Flavours', 'Marella Discovery 2', '2026-12-18', 7,
                 'Santa Cruz, Tenerife', 'Tenerife and Madeira', 'Second offer',
                 'https://example.test/b', '2026-07-17T14:35:21.0000000+05:30');

            INSERT INTO CruiseObservationPrices
                (Id, CruiseObservationId, Amount, Currency, Basis, DisplayOrder)
            VALUES
                (1, 1, '988', 'GBP', 'per person', 0),
                (2, 2, '949', 'GBP', 'per person', 0);
            """);

    private static async Task<string[]> ReadObservationIndexesAsync(CruisePersistenceTestDatabase database)
    {
        await using var command = database.Connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'index' AND tbl_name = 'CruiseObservations'";
        await using var reader = await command.ExecuteReaderAsync();
        var names = new List<string>();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names.ToArray();
    }
}
