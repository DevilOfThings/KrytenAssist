extern alias KrytenInfrastructure;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using FavouriteRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteFavouriteCruiseShipRepository;
using PreferencesRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruisePreferencesRepository;
using SavedRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteSavedCruiseRepository;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class PersonalCruisePersistenceTests
{
    [Fact]
    public async Task Saved_cruise_round_trips_full_personal_state_after_new_context()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        var saved = Saved();
        await using (var context = database.CreateContext()) await new SavedRepository(context).UpsertAsync(saved);
        await using var reopened = database.CreateContext();
        (await new SavedRepository(reopened).GetAsync(saved.SailingKey)).Should().Be(saved);
    }

    [Fact]
    public async Task Upsert_refreshes_snapshot_without_duplicate_sailing()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        await using var context = database.CreateContext(); var repository = new SavedRepository(context); var original = Saved();
        await repository.UpsertAsync(original);
        var refreshed = original.RefreshSnapshot(Snapshot("Updated", new CruiseSource("other", "Other")));
        await repository.UpsertAsync(refreshed);
        (await repository.ListAsync()).Should().ContainSingle().Which.Should().Be(refreshed);
    }

    [Fact]
    public async Task Personal_remove_and_history_delete_are_independent()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        await using var context = database.CreateContext(); var savedRepository = new SavedRepository(context); var saved = Saved();
        await savedRepository.UpsertAsync(saved);
        var observation = CruisePersistenceTestData.Observation();
        await new KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseObservationRepository(context).RecordAsync(CruiseSailingKey.From(observation), CruiseObservationFingerprint.From(observation), observation);
        (await savedRepository.RemoveAsync(saved.SailingKey)).Should().BeTrue();
        context.CruiseHistories.Should().ContainSingle();
        await savedRepository.UpsertAsync(saved);
        context.CruiseHistories.Remove(await context.CruiseHistories.SingleAsync()); await context.SaveChangesAsync();
        (await savedRepository.GetAsync(saved.SailingKey)).Should().Be(saved);
    }

    [Fact]
    public async Task Favourite_ship_is_idempotent_independent_and_persistent()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync(); var key = new CruiseShipKey("marella", "Voyager");
        await using (var context = database.CreateContext())
        {
            var repository = new FavouriteRepository(context);
            (await repository.SetAsync(key, true)).Should().BeTrue();
            (await repository.SetAsync(key, true)).Should().BeFalse();
            context.SavedCruises.Should().BeEmpty();
        }
        await using var reopened = database.CreateContext(); var reloaded = new FavouriteRepository(reopened);
        (await reloaded.ListAsync()).Should().ContainSingle().Which.Should().Be(key);
        (await reloaded.SetAsync(key, false)).Should().BeTrue();
        (await reloaded.SetAsync(key, false)).Should().BeFalse();
    }

    [Fact]
    public async Task Preferences_round_trip_update_and_clear_atomically()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        await using var context = database.CreateContext(); var repository = new PreferencesRepository(context);
        (await repository.GetAsync()).Should().Be(new CruisePreferences());
        var value = new CruisePreferences([9, 5], [CruiseCabinType.Suite, CruiseCabinType.Balcony], new CruiseBudget(3000, "GBP", CruiseBudgetBasis.TotalBooking));
        await repository.SaveAsync(value); (await repository.GetAsync()).Should().Be(value);
        await repository.SaveAsync(new CruisePreferences()); (await repository.GetAsync()).Should().Be(new CruisePreferences());
    }

    [Fact]
    public async Task Pre_cancelled_operations_do_not_write()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync(); await using var context = database.CreateContext(); var token = new CancellationToken(true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SavedRepository(context).UpsertAsync(Saved(), token));
        context.SavedCruises.Should().BeEmpty();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new PreferencesRepository(context).SaveAsync(new CruisePreferences([5]), token));
        context.CruisePreferenceProfiles.Should().BeEmpty();
    }

    private static readonly CruiseSailingKey Key = new("marella", "voyager", new DateOnly(2027, 8, 2), 7);
    private static SavedCruise Saved() => new(Snapshot(), SavedCruiseStatus.Dismissed, new CruiseEvaluation(CruiseInterestLevel.StrongCandidate, 5, 4, 3, 2, "Great"), true);
    private static SavedCruiseSnapshot Snapshot(string title = "Mediterranean Escape", CruiseSource? source = null) => new(Key, title, "Marella Cruises", new CruisePrice(999, "GBP", "per person"), new DateTimeOffset(2026, 7, 18, 9, 0, 0, TimeSpan.Zero), "Palma", "Spain and Italy", source ?? new CruiseSource("tui", "TUI"), "https://www.tui.co.uk/cruise/example");
}
