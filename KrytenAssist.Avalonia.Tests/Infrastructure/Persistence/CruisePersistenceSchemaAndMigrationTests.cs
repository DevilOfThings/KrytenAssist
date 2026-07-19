extern alias KrytenInfrastructure;

using KrytenAssist.Core.Entities;
using KrytenAssist.Core.Cruises;
using FluentAssertions;
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
    private const string PreviousMigration = "20260718205146_AddCruiseAlertPersistence";

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
        Assert.Contains("SavedCruises", schema.Keys);
        Assert.Contains("FavouriteCruiseShips", schema.Keys);
        Assert.Contains("CruisePreferenceProfiles", schema.Keys);
        Assert.Contains("CruisePreferenceMonths", schema.Keys);
        Assert.Contains("CruisePreferenceCabins", schema.Keys);
        Assert.Contains("CruiseAlerts", schema.Keys);
        Assert.Contains("CruisePriceDropAlertDetails", schema.Keys);
        Assert.Contains("CruisePromotionAlertDetails", schema.Keys);
        Assert.Contains("CruiseSavedCriteriaAlertDetails", schema.Keys);
        Assert.Contains("CruiseAlertSettings", schema.Keys);
        Assert.Contains("SavedCruiseCriteriaEvaluationStates", schema.Keys);
        Assert.Contains("CruiseCabinSeries", schema.Keys);
        Assert.Contains("CruiseCabinContextChildAges", schema.Keys);
        Assert.Contains("CruiseCabinObservations", schema.Keys);
        Assert.Contains("CruiseCabinObservationStates", schema.Keys);
        Assert.Contains("CruiseCabinAvailabilityAlertDetails", schema.Keys);
        Assert.Contains("CruiseSavedCriteriaAlertCabins", schema.Keys);
        Assert.Contains("CK_CruiseHistories_DurationNights", schema["CruiseHistories"]);
        Assert.Contains("CK_CruiseObservations_Fingerprint_Length", schema["CruiseObservations"]);
        Assert.Contains("CK_CruiseObservationPrices_Amount", schema["CruiseObservationPrices"]);
        Assert.Contains("UX_CruiseHistories_Sailing_Source", indexes.Keys);
        Assert.Contains("IX_CruiseObservations_History_Fingerprint", indexes.Keys);
        Assert.Contains("UX_CruiseObservations_History_Sequence", indexes.Keys);
        Assert.DoesNotContain("UX_CruiseObservations_History_Fingerprint", indexes.Keys);
        Assert.Contains("UX_CruiseObservationPrices_Observation_Order", indexes.Keys);
        Assert.Contains("UX_SavedCruises_Sailing", indexes.Keys);
        Assert.Contains("UX_FavouriteCruiseShips_Operator_Ship", indexes.Keys);
        Assert.Contains("UX_CruisePreferenceMonths_Profile_Month", indexes.Keys);
        Assert.Contains("UX_CruisePreferenceCabins_Profile_Cabin", indexes.Keys);
        Assert.Contains("UX_CruiseAlerts_EventKey", indexes.Keys);
        Assert.Contains("UX_SavedCriteriaStates_Sailing_Fingerprint", indexes.Keys);
        Assert.Contains("UX_CruiseCabinSeries_SeriesKey", indexes.Keys);
        Assert.Contains("UX_CruiseCabinObservations_Series_Sequence", indexes.Keys);
        Assert.Contains("IX_CruiseCabinObservations_Series_State", indexes.Keys);
        Assert.DoesNotContain("UX_CruiseCabinObservations_Series_State", indexes.Keys);
        Assert.Contains("UX_CruiseCabinObservationStates_Observation_Cabin", indexes.Keys);
        Assert.Contains("UX_CruiseCabinContextChildAges_Series_Order", indexes.Keys);
        Assert.Contains("UX_CruiseSavedCriteriaAlertCabins_Alert_Cabin", indexes.Keys);
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

        Assert.Equal(6, applied.Count());
        Assert.Contains(InitialMigration, applied);
        Assert.Contains(applied, migration => migration.EndsWith("_AddCruiseHistoryPersistence", StringComparison.Ordinal));
        Assert.Contains(applied, migration => migration.EndsWith("_HardenCruiseHistoryRecording", StringComparison.Ordinal));
        Assert.Contains(applied, migration => migration.EndsWith("_AddPersonalCruiseState", StringComparison.Ordinal));
        Assert.Contains(applied, migration => migration.EndsWith("_AddCruiseAlertPersistence", StringComparison.Ordinal));
        Assert.Contains(applied, migration => migration.EndsWith("_AddCruiseCabinPersistence", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MigrationFromPreviousAlertSchema_PreservesSettingsAndDefaultsCabinAlertsEnabled()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAsync();
        var time = new DateTimeOffset(2026, 7, 18, 20, 0, 0, TimeSpan.FromHours(1));
        var key = new CruiseSailingKey("operator", "ship", new DateOnly(2027, 1, 2), 7);
        var legacyDetails = new CruiseSavedCriteriaAlertDetails(true, null, null, "legacy-criteria",
            CruiseAlertEvidenceOrigin.SavedSnapshot, "legacy-evidence", time, true);
        var legacyAlert = new CruiseAlert(Guid.NewGuid(), new CruiseAlertCandidate(CruiseAlertType.SavedCriteria,
            key, null, legacyDetails, time, "legacy-evidence", "legacy-criteria"), time.AddMinutes(1));
        await using (var oldContext = database.CreateContext())
        {
            await oldContext.Database.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            await oldContext.Database.ExecuteSqlRawAsync("""
                INSERT INTO CruiseAlertSettings (Id, PriceDropEnabled, PromotionEnabled, SavedCriteriaEnabled, MinimumPriceDropPercentage)
                VALUES (1, 0, 1, 0, '12.5')
                """);
            await oldContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO CruiseAlerts (Id, EventKey, Type, Status, OperatorId, ShipName, DepartureDate,
                    DurationNights, RetailSourceId, RetailSourceName, EventTime, EventTimeUtcTicks, CreatedAt, CreatedAtUtcTicks)
                VALUES ({legacyAlert.Id}, {legacyAlert.EventKey}, 2, 0, {key.OperatorId}, {key.ShipName},
                    {key.DepartureDate.ToString("yyyy-MM-dd")}, {key.DurationNights}, NULL, NULL,
                    {time.ToString("O")}, {time.UtcTicks}, {legacyAlert.CreatedAt.ToString("O")}, {legacyAlert.CreatedAt.UtcTicks})
                """);
            await oldContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO CruiseSavedCriteriaAlertDetails (CruiseAlertId, MonthConfiguredAndMatched,
                    ConfiguredBudgetAmount, ConfiguredBudgetCurrency, ConfiguredBudgetBasis, MatchedPriceAmount,
                    MatchedPriceCurrency, MatchedPriceBasis, CriteriaFingerprint, EvidenceOrigin, EvidenceKey,
                    EvidenceTime, CabinPreferencesUnavailable)
                VALUES ({legacyAlert.Id}, 1, NULL, NULL, NULL, NULL, NULL, NULL, 'legacy-criteria', 1,
                    'legacy-evidence', {time.ToString("O")}, 1)
                """);
            await oldContext.Database.MigrateAsync();
        }
        await using var reopened = database.CreateContext();
        var settings = await new KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseAlertSettingsRepository(reopened).GetAsync();
        settings.Should().Be(new CruiseAlertSettings(false, true, false, 12.5m, true));
        var reconstructed = await new KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseAlertRepository(reopened).GetAsync(legacyAlert.Id);
        reconstructed.Should().BeEquivalentTo(legacyAlert);
        var reconstructedDetails = (CruiseSavedCriteriaAlertDetails)reconstructed!.Details;
        reconstructedDetails.ConfiguredCabins.Should().BeEmpty();
        reconstructedDetails.CabinCriterionResult.Should().Be(SavedCruiseCriteriaResult.Unknown);
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
                LastSeenAt = stored.LastSeenAt,
                LatestProviderOfferId = stored.LatestProviderOfferId,
                LatestSourceReference = stored.LatestSourceReference,
                LatestEvidenceObservedAt = stored.LatestEvidenceObservedAt
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
    public async Task AlertAndCriteriaState_HaveNoRelationshipsToHistoryOrSavedCruises()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();

        foreach (var table in new[] { "CruiseAlerts", "CruiseAlertSettings", "SavedCruiseCriteriaEvaluationStates" })
        {
            await using var command = database.Connection.CreateCommand();
            command.CommandText = $"PRAGMA foreign_key_list(\"{table}\")";
            await using var reader = await command.ExecuteReaderAsync();
            Assert.False(await reader.ReadAsync());
        }

        foreach (var table in new[] { "CruisePriceDropAlertDetails", "CruisePromotionAlertDetails", "CruiseSavedCriteriaAlertDetails" })
        {
            await using var command = database.Connection.CreateCommand();
            command.CommandText = $"PRAGMA foreign_key_list(\"{table}\")";
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("CruiseAlerts", reader.GetString(reader.GetOrdinal("table")));
            Assert.False(await reader.ReadAsync());
        }
    }

    [Fact]
    public async Task CabinAggregate_HasOnlyInternalCascadingRelationships()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        foreach (var table in new[] { "CruiseCabinSeries" })
        {
            await using var command = database.Connection.CreateCommand(); command.CommandText = $"PRAGMA foreign_key_list(\"{table}\")";
            await using var reader = await command.ExecuteReaderAsync(); Assert.False(await reader.ReadAsync());
        }

        var observation = CruiseCabinPersistenceTestData.Observation();
        await using var context = database.CreateContext();
        await new KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseCabinObservationRepository(context).RecordAsync(observation);
        context.CruiseCabinSeries.Remove(await context.CruiseCabinSeries.SingleAsync());
        await context.SaveChangesAsync();
        (await context.CruiseCabinContextChildAges.CountAsync()).Should().Be(0);
        (await context.CruiseCabinObservations.CountAsync()).Should().Be(0);
        (await context.CruiseCabinObservationStates.CountAsync()).Should().Be(0);
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
