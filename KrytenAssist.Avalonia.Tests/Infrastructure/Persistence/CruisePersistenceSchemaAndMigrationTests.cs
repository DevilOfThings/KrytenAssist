extern alias KrytenInfrastructure;

using KrytenAssist.Core.Entities;
using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using CruiseHistoryEntity = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.CruiseHistoryEntity;
using CruisePriceEntity = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.CruiseObservationPriceEntity;
using Repository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseObservationRepository;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class CruisePersistenceSchemaAndMigrationTests
{
    private const string InitialMigration = "20260703152527_InitialCreate";

    [Fact]
    public async Task LatestMigration_CreatesNormalizedTablesIndexesAndConstraints()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();

        var schema = await ReadSchemaAsync(database, "table");
        var indexes = await ReadSchemaAsync(database, "index");

        Assert.Contains("CruiseHistories", schema.Keys);
        Assert.Contains("CruiseObservations", schema.Keys);
        Assert.Contains("CruiseObservationPrices", schema.Keys);
        Assert.Contains("CK_CruiseHistories_DurationNights", schema["CruiseHistories"]);
        Assert.Contains("CK_CruiseObservations_Fingerprint_Length", schema["CruiseObservations"]);
        Assert.Contains("CK_CruiseObservationPrices_Amount", schema["CruiseObservationPrices"]);
        Assert.Contains("UX_CruiseHistories_Sailing_Source", indexes.Keys);
        Assert.Contains("UX_CruiseObservations_History_Fingerprint", indexes.Keys);
        Assert.Contains("UX_CruiseObservationPrices_Observation_Order", indexes.Keys);
    }

    [Fact]
    public async Task MigrationFromInitialCreate_PreservesPromptCardAndSupportsReopen()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAsync();
        var promptId = Guid.Parse("62bd66c8-f0ae-4b13-a117-ec97e3f7dd19");
        await using (var initialContext = database.CreateContext())
        {
            var migrator = initialContext.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(InitialMigration);
            initialContext.PromptCards.Add(new PromptCard
            {
                Id = promptId,
                Title = "Existing prompt",
                Category = "Test",
                Description = "Must survive migration",
                PromptText = "Existing text",
                Tags = ["existing"],
                CreatedAt = CruisePersistenceTestData.ObservedAt,
                UpdatedAt = CruisePersistenceTestData.ObservedAt
            });
            await initialContext.SaveChangesAsync();
            await initialContext.Database.MigrateAsync();
        }

        await using var reopened = database.CreateContext();
        var prompt = await reopened.PromptCards.SingleAsync(card => card.Id == promptId);

        Assert.Equal("Existing prompt", prompt.Title);
        Assert.True(await reopened.Database.CanConnectAsync());
        Assert.Equal(0, await reopened.CruiseHistories.CountAsync());
    }

    [Fact]
    public async Task EmptyDatabase_MigratesThroughEveryCheckedInMigration()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAsync();
        await using var context = database.CreateContext();

        await context.Database.MigrateAsync();
        var applied = await context.Database.GetAppliedMigrationsAsync();

        Assert.Equal(2, applied.Count());
        Assert.Contains(InitialMigration, applied);
        Assert.Contains(applied, migration => migration.EndsWith("_AddCruiseHistoryPersistence", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DatabaseConstraints_RejectDuplicateHistoryAndPriceOrder()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        var observation = CruisePersistenceTestData.Observation();
        await using (var context = database.CreateContext())
        {
            var repository = new Repository(context);
            await repository.RecordAsync(
                CruiseSailingKey.From(observation),
                CruiseObservationFingerprint.From(observation),
                observation);
        }

        await using (var duplicateContext = database.CreateContext())
        {
            var stored = await duplicateContext.CruiseHistories.AsNoTracking().SingleAsync();
            duplicateContext.CruiseHistories.Add(new CruiseHistoryEntity
            {
                OperatorId = stored.OperatorId,
                NormalizedShipName = stored.NormalizedShipName,
                DepartureDate = stored.DepartureDate,
                DurationNights = stored.DurationNights,
                RetailSourceId = stored.RetailSourceId,
                RetailSourceName = stored.RetailSourceName,
                FirstObservedAt = stored.FirstObservedAt,
                LastSeenAt = stored.LastSeenAt
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => duplicateContext.SaveChangesAsync());
        }

        await using (var priceContext = database.CreateContext())
        {
            var storedPrice = await priceContext.CruiseObservationPrices.AsNoTracking().FirstAsync();
            priceContext.CruiseObservationPrices.Add(new CruisePriceEntity
            {
                CruiseObservationId = storedPrice.CruiseObservationId,
                Amount = 1m,
                Currency = "GBP",
                Basis = "test",
                DisplayOrder = storedPrice.DisplayOrder
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => priceContext.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task DeletingHistory_CascadesThroughObservationsAndPrices()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        var observation = CruisePersistenceTestData.Observation();
        await using var context = database.CreateContext();
        var repository = new Repository(context);
        await repository.RecordAsync(
            CruiseSailingKey.From(observation),
            CruiseObservationFingerprint.From(observation),
            observation);

        context.CruiseHistories.Remove(await context.CruiseHistories.SingleAsync());
        await context.SaveChangesAsync();

        Assert.Equal(0, await context.CruiseObservations.CountAsync());
        Assert.Equal(0, await context.CruiseObservationPrices.CountAsync());
    }

    private static async Task<Dictionary<string, string>> ReadSchemaAsync(
        CruisePersistenceTestDatabase database,
        string type)
    {
        await using var command = database.Connection.CreateCommand();
        command.CommandText = "SELECT name, sql FROM sqlite_master WHERE type = $type AND sql IS NOT NULL";
        command.Parameters.AddWithValue("$type", type);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        while (await reader.ReadAsync())
        {
            result.Add(reader.GetString(0), reader.GetString(1));
        }

        return result;
    }
}
