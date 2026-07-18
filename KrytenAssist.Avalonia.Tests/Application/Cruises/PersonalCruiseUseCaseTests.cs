extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using DismissUseCase = KrytenApplication::KrytenAssist.Application.Cruises.DismissCruise;
using FavouriteShipUseCase = KrytenApplication::KrytenAssist.Application.Cruises.SetFavouriteCruiseShip;
using ListUseCase = KrytenApplication::KrytenAssist.Application.Cruises.ListSavedCruises;
using MutationStatus = KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseMutationStatus;
using PreferenceMutationStatus = KrytenApplication::KrytenAssist.Application.Cruises.PersonalCruisePreferenceMutationStatus;
using RemoveUseCase = KrytenApplication::KrytenAssist.Application.Cruises.RemoveSavedCruise;
using RestoreUseCase = KrytenApplication::KrytenAssist.Application.Cruises.RestoreCruise;
using SavePreferencesUseCase = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruisePreferences;
using SaveUseCase = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruise;
using UpdateUseCase = KrytenApplication::KrytenAssist.Application.Cruises.UpdateCruiseEvaluation;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class PersonalCruiseUseCaseTests
{
    [Fact]
    public async Task First_save_creates_shortlist_item_with_empty_evaluation()
    {
        var repository = new FakeSavedCruiseRepository();
        var result = await new SaveUseCase(repository).ExecuteAsync(Snapshot());
        result.Status.Should().Be(MutationStatus.Created);
        result.SavedCruise!.Status.Should().Be(SavedCruiseStatus.Shortlisted);
        result.SavedCruise.Evaluation.Should().Be(CruiseEvaluation.Empty);
    }

    [Fact]
    public async Task Repeat_save_refreshes_source_but_preserves_personal_state()
    {
        var repository = new FakeSavedCruiseRepository();
        var original = new SavedCruise(Snapshot(), SavedCruiseStatus.Dismissed, new CruiseEvaluation(CruiseInterestLevel.StrongCandidate, notes: "Keep"), true);
        repository.Items.Add(original.SailingKey, original);
        var changed = Snapshot("Updated", new CruiseSource("other", "Other"));

        var result = await new SaveUseCase(repository).ExecuteAsync(changed);

        result.Status.Should().Be(MutationStatus.Updated);
        result.SavedCruise!.Snapshot.Title.Should().Be("Updated");
        result.SavedCruise.Status.Should().Be(SavedCruiseStatus.Dismissed);
        result.SavedCruise.Evaluation.Should().Be(original.Evaluation);
        result.SavedCruise.IsFavourite.Should().BeTrue();
        repository.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Evaluation_update_changes_only_evaluation()
    {
        var repository = RepositoryWith(new SavedCruise(Snapshot(), SavedCruiseStatus.Dismissed, isFavourite: true));
        var evaluation = new CruiseEvaluation(CruiseInterestLevel.Maybe, 4, notes: "Consider");
        var result = await new UpdateUseCase(repository).ExecuteAsync(Key, evaluation);
        result.Status.Should().Be(MutationStatus.Updated);
        result.SavedCruise!.Evaluation.Should().Be(evaluation);
        result.SavedCruise.Status.Should().Be(SavedCruiseStatus.Dismissed);
        result.SavedCruise.IsFavourite.Should().BeTrue();
    }

    [Fact]
    public async Task Dismiss_restore_and_remove_have_explicit_outcomes()
    {
        var repository = RepositoryWith(new SavedCruise(Snapshot()));
        (await new DismissUseCase(repository).ExecuteAsync(Key)).Status.Should().Be(MutationStatus.Dismissed);
        (await new DismissUseCase(repository).ExecuteAsync(Key)).Status.Should().Be(MutationStatus.Unchanged);
        (await new RestoreUseCase(repository).ExecuteAsync(Key)).Status.Should().Be(MutationStatus.Restored);
        (await new RemoveUseCase(repository).ExecuteAsync(Key)).Status.Should().Be(MutationStatus.Removed);
        (await new RemoveUseCase(repository).ExecuteAsync(Key)).Status.Should().Be(MutationStatus.NotFound);
    }

    [Fact]
    public async Task List_is_ordered_by_sailing_and_does_not_contact_history()
    {
        var repository = new FakeSavedCruiseRepository();
        var later = Snapshot(key: new CruiseSailingKey("marella", "Explorer", new DateOnly(2028, 1, 1), 7));
        repository.Items.Add(later.SailingKey, new SavedCruise(later));
        repository.Items.Add(Key, new SavedCruise(Snapshot()));
        var result = await new ListUseCase(repository).ExecuteAsync();
        result.SavedCruises.Select(x => x.SailingKey).Should().Equal(Key, later.SailingKey);
    }

    [Fact]
    public async Task Cancellation_and_failure_are_controlled()
    {
        var cancelled = new CancellationToken(true);
        (await new SaveUseCase(new FakeSavedCruiseRepository()).ExecuteAsync(Snapshot(), cancelled)).Status.Should().Be(MutationStatus.Cancelled);
        var failing = new FakeSavedCruiseRepository { Exception = new InvalidOperationException() };
        (await new SaveUseCase(failing).ExecuteAsync(Snapshot())).Status.Should().Be(MutationStatus.Failed);
    }

    [Fact]
    public async Task Favourite_ship_state_is_independent_from_sailings()
    {
        var repository = new FakeFavouriteCruiseShipRepository();
        var useCase = new FavouriteShipUseCase(repository);
        var key = CruiseShipKey.From(Key);
        (await useCase.ExecuteAsync(key, true)).Status.Should().Be(PreferenceMutationStatus.Updated);
        (await useCase.ExecuteAsync(key, true)).Status.Should().Be(PreferenceMutationStatus.Unchanged);
        repository.Items.Should().ContainSingle().Which.Should().Be(key);
    }

    [Fact]
    public async Task Preference_save_distinguishes_unchanged_updated_and_failure()
    {
        var repository = new FakeCruisePreferencesRepository();
        var useCase = new SavePreferencesUseCase(repository);
        (await useCase.ExecuteAsync(new CruisePreferences())).Status.Should().Be(PreferenceMutationStatus.Unchanged);
        var changed = new CruisePreferences([8], [CruiseCabinType.Balcony], new CruiseBudget(2000, "GBP", CruiseBudgetBasis.PerPerson));
        (await useCase.ExecuteAsync(changed)).Status.Should().Be(PreferenceMutationStatus.Updated);
        repository.Value.Should().Be(changed);
        repository.Exception = new InvalidOperationException();
        (await useCase.ExecuteAsync(changed)).Status.Should().Be(PreferenceMutationStatus.Failed);
    }

    private static readonly CruiseSailingKey Key = new("marella", "Voyager", new DateOnly(2027, 8, 2), 7);
    private static SavedCruiseSnapshot Snapshot(string title = "Mediterranean Escape", CruiseSource? source = null, CruiseSailingKey? key = null) =>
        new(key ?? Key, title, "Marella Cruises", new CruisePrice(999, "GBP", "per person"), new DateTimeOffset(2026, 7, 18, 9, 0, 0, TimeSpan.Zero), "Palma", "Spain and Italy", source ?? new CruiseSource("tui", "TUI"), "https://www.tui.co.uk/cruise/example");
    private static FakeSavedCruiseRepository RepositoryWith(SavedCruise value) { var repository = new FakeSavedCruiseRepository(); repository.Items.Add(value.SailingKey, value); return repository; }
}
