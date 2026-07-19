extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using AlertQuery = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertQuery;
using AlertRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseAlertRepository;
using SettingsRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseAlertSettingsRepository;
using StateRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteSavedCruiseCriteriaStateRepository;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class CruiseAlertPersistenceTests
{
    [Fact]
    public async Task AllTypedAlerts_RoundTripExactlyAndListFiltersDeterministically()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        var time = new DateTimeOffset(2026, 7, 18, 20, 0, 0, TimeSpan.FromHours(2));
        var values = new[]
        {
            Alert(CruiseAlertType.PriceDrop, "price", time, new CruisePriceDropAlertDetails(new(1000.123456m, "GBP", "per person"), new(900.123456m, "GBP", "per person"), "price")),
            Alert(CruiseAlertType.Promotion, "promotion", time.AddMinutes(1), new CruisePromotionAlertDetails("Old", "New", "promotion")),
            Alert(CruiseAlertType.SavedCriteria, "criteria", time.AddMinutes(2), new CruiseSavedCriteriaAlertDetails(true, new(2000.123456m, "GBP", CruiseBudgetBasis.TotalBooking), new(1900.123456m, "GBP", "total booking"), "criteria-fingerprint", CruiseAlertEvidenceOrigin.SavedSnapshot, "criteria", time, false,
                [CruiseCabinType.Balcony, CruiseCabinType.Suite], [CruiseCabinType.Balcony], SavedCruiseCriteriaResult.Met,
                new string('a', 64), "cabin-evidence", time.AddMinutes(2))),
            Alert(CruiseAlertType.CabinAvailability, "ignored", time.AddMinutes(3), new CruiseCabinAvailabilityAlertDetails(
                CruiseCabinType.Balcony, CruiseCabinAvailabilityState.Unavailable, CruiseCabinAvailabilityState.Available,
                new string('b', 64), CruiseCabinEvidenceCoverage.Partial, new string('c', 64), "retailer-cabin", time.AddMinutes(3)))
        };
        await using (var context = database.CreateContext())
        {
            var repository = new AlertRepository(context);
            foreach (var value in values) (await repository.AddIfAbsentAsync(value)).Created.Should().BeTrue();
        }
        await using (var reopened = database.CreateContext())
        {
            var repository = new AlertRepository(reopened);
            var listed = await repository.ListAsync(new AlertQuery());
            listed.Should().BeEquivalentTo(values.Reverse(), options => options.WithStrictOrdering());
            (await repository.ListAsync(new AlertQuery(CruiseAlertType.Promotion))).Should().Equal(values[1]);
            (await repository.GetAsync(values[0].Id)).Should().BeEquivalentTo(values[0]);
            (await repository.GetAsync(Guid.NewGuid())).Should().BeNull();
        }
    }

    [Fact]
    public async Task DuplicateEventKey_ReturnsStoredAggregateAndOneDetail()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        var original = Promotion("same", DateTimeOffset.UtcNow);
        var duplicate = new CruiseAlert(Guid.NewGuid(), Candidate("same", original.EventTime), original.CreatedAt.AddDays(1));
        await using var context = database.CreateContext(); var repository = new AlertRepository(context);

        (await repository.AddIfAbsentAsync(original)).Created.Should().BeTrue();
        var result = await repository.AddIfAbsentAsync(duplicate);

        result.Created.Should().BeFalse(); result.Alert.Should().Be(original);
        (await context.CruiseAlerts.CountAsync()).Should().Be(1);
        (await context.CruisePromotionAlertDetails.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task LifecycleAndUnreadCount_ChangeOnlyStatus()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        var value = Promotion("status", DateTimeOffset.UtcNow);
        await using var context = database.CreateContext(); var repository = new AlertRepository(context);
        await repository.AddIfAbsentAsync(value);

        (await repository.CountUnreadAsync()).Should().Be(1);
        (await repository.UpdateStatusAsync(value.Id, CruiseAlertStatus.Dismissed)).Should().BeTrue();
        (await repository.CountUnreadAsync()).Should().Be(0);
        (await repository.GetAsync(value.Id)).Should().Be(value.WithStatus(CruiseAlertStatus.Dismissed));
        (await repository.UpdateStatusAsync(Guid.NewGuid(), CruiseAlertStatus.Read)).Should().BeFalse();
    }

    [Fact]
    public async Task Settings_DefaultReplaceAndRestartPreserveExactValue()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        await using (var context = database.CreateContext())
        {
            var repository = new SettingsRepository(context);
            (await repository.GetAsync()).Should().Be(new CruiseAlertSettings());
            await repository.SaveAsync(new(false, true, false, 12.3456m, false));
        }
        await using (var reopened = database.CreateContext())
            (await new SettingsRepository(reopened).GetAsync()).Should().Be(new CruiseAlertSettings(false, true, false, 12.3456m, false));
    }

    [Fact]
    public async Task CriteriaState_NewerWinsAndEqualTimeUsesEvidenceKey()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        var key = Key(); var fingerprint = "criteria"; var time = DateTimeOffset.UtcNow;
        await using var context = database.CreateContext(); var repository = new StateRepository(context);
        await repository.UpsertAsync(new(key, fingerprint, "b", time, SavedCruiseCriteriaResult.Met));
        await repository.UpsertAsync(new(key, fingerprint, "older", time.AddMinutes(-1), SavedCruiseCriteriaResult.NotMet));
        await repository.UpsertAsync(new(key, fingerprint, "a", time, SavedCruiseCriteriaResult.NotMet));
        (await repository.GetAsync(key, fingerprint))!.EvidenceKey.Should().Be("b");
        await repository.UpsertAsync(new(key, fingerprint, "c", time, SavedCruiseCriteriaResult.NotMet));
        var stored = await repository.GetAsync(key, fingerprint);
        stored!.EvidenceKey.Should().Be("c"); stored.Result.Should().Be(SavedCruiseCriteriaResult.NotMet);
    }

    [Fact]
    public async Task PreCancelledOperations_DoNotAccessDatabase()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        await using var context = database.CreateContext(); using var source = new CancellationTokenSource(); source.Cancel();
        var alerts = new AlertRepository(context); var settings = new SettingsRepository(context); var states = new StateRepository(context);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => alerts.ListAsync(new AlertQuery(), source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => settings.GetAsync(source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => states.GetAsync(Key(), "criteria", source.Token));
    }

    private static CruiseAlert Promotion(string evidence, DateTimeOffset time) => new(Guid.NewGuid(), Candidate(evidence, time), time.AddSeconds(1));
    private static CruiseAlertCandidate Candidate(string evidence, DateTimeOffset time) => new(CruiseAlertType.Promotion, Key(), new CruiseSource("retailer", "Retailer"), new CruisePromotionAlertDetails(null, "Offer", evidence), time, evidence);
    private static CruiseAlert Alert(CruiseAlertType type, string evidence, DateTimeOffset time, CruiseAlertDetails details)
    {
        var source = type == CruiseAlertType.SavedCriteria ? null : new CruiseSource("retailer", "Retailer");
        var fingerprint = (details as CruiseSavedCriteriaAlertDetails)?.CriteriaFingerprint;
        var triggeringEvidence = details is CruiseCabinAvailabilityAlertDetails cabin
            ? $"{cabin.StateFingerprint}:{(int)cabin.CabinType}"
            : evidence;
        return new(Guid.NewGuid(), new(type, Key(), source, details, time, triggeringEvidence, fingerprint), time.AddSeconds(1));
    }
    private static CruiseSailingKey Key() => new("operator", "ship", new DateOnly(2027, 1, 2), 7);
}
