extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Avalonia.Tests.Application.Cruises;
using KrytenAssist.Avalonia.Tools;
using KrytenAssist.Avalonia.ViewModels;
using KrytenAssist.Core.Cruises;
using DismissUseCase = KrytenApplication::KrytenAssist.Application.Cruises.DismissCruise;
using GetSaved = KrytenApplication::KrytenAssist.Application.Cruises.GetSavedCruise;
using ListDetails = KrytenApplication::KrytenAssist.Application.Cruises.ListSavedCruiseDetails;
using ListShips = KrytenApplication::KrytenAssist.Application.Cruises.ListFavouriteCruiseShips;
using RemoveUseCase = KrytenApplication::KrytenAssist.Application.Cruises.RemoveSavedCruise;
using RestoreUseCase = KrytenApplication::KrytenAssist.Application.Cruises.RestoreCruise;
using SaveUseCase = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruise;
using SetFavourite = KrytenApplication::KrytenAssist.Application.Cruises.SetSavedCruiseFavourite;
using SetShip = KrytenApplication::KrytenAssist.Application.Cruises.SetFavouriteCruiseShip;
using Factory = KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseSnapshotFactory;
using UpdateEvaluation = KrytenApplication::KrytenAssist.Application.Cruises.UpdateCruiseEvaluation;
using GetPreferences = KrytenApplication::KrytenAssist.Application.Cruises.GetCruisePreferences;
using SavePreferences = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruisePreferences;

namespace KrytenAssist.Avalonia.Tests.ViewModels;

public sealed class SavedCruisesViewModelTests
{
    [Fact]
    public async Task Filters_have_exact_membership_and_counts()
    {
        var context = CreateContext();
        var ordinary = Saved("Ordinary", "Voyager", new DateOnly(2027, 8, 1));
        var strong = Saved("Strong", "Explorer", new DateOnly(2027, 8, 2),
            evaluation: new CruiseEvaluation(CruiseInterestLevel.StrongCandidate));
        var favouriteShip = Saved("Ship favourite", "Discovery", new DateOnly(2027, 8, 3));
        var dismissed = Saved("Dismissed", "Dream", new DateOnly(2027, 8, 4),
            SavedCruiseStatus.Dismissed, isFavourite: true);
        foreach (var saved in new[] { ordinary, strong, favouriteShip, dismissed })
            context.Saved.Items.Add(saved.SailingKey, saved);
        context.Ships.Items.Add(CruiseShipKey.From(favouriteShip.SailingKey));

        await context.ViewModel.ActivateAsync();

        context.ViewModel.ShortlistCount.Should().Be(3);
        context.ViewModel.StrongCandidatesCount.Should().Be(1);
        context.ViewModel.FavouritesCount.Should().Be(1);
        context.ViewModel.NotForUsCount.Should().Be(1);
        context.ViewModel.Filter = SavedCruiseFilter.StrongCandidates;
        context.ViewModel.Items.Should().ContainSingle(item => item.Title == "Strong");
        context.ViewModel.Filter = SavedCruiseFilter.Favourites;
        context.ViewModel.Items.Should().ContainSingle(item => item.Title == "Ship favourite");
        context.ViewModel.Filter = SavedCruiseFilter.NotForUs;
        context.ViewModel.Items.Should().ContainSingle(item => item.Title == "Dismissed");
    }

    [Fact]
    public async Task Dismiss_and_restore_move_selection_between_filters_without_losing_evaluation()
    {
        var context = CreateContext();
        var evaluation = new CruiseEvaluation(CruiseInterestLevel.Maybe, 4, notes: "Keep this");
        var saved = Saved("Consider", "Voyager", new DateOnly(2027, 8, 1), evaluation: evaluation);
        context.Saved.Items.Add(saved.SailingKey, saved);
        await context.ViewModel.ActivateAsync();

        await context.ViewModel.ChangeLifecycleAsync();

        context.ViewModel.Items.Should().BeEmpty();
        context.Saved.Items[saved.SailingKey].Status.Should().Be(SavedCruiseStatus.Dismissed);
        context.Saved.Items[saved.SailingKey].Evaluation.Should().Be(evaluation);
        context.ViewModel.Filter = SavedCruiseFilter.NotForUs;
        await context.ViewModel.ChangeLifecycleAsync();
        context.Saved.Items[saved.SailingKey].Status.Should().Be(SavedCruiseStatus.Shortlisted);
        context.Saved.Items[saved.SailingKey].Evaluation.Should().Be(evaluation);
    }

    [Fact]
    public async Task Remove_requires_confirmation_and_does_not_touch_history_or_ship_favourite()
    {
        var context = CreateContext();
        var saved = Saved("Remove me", "Voyager", new DateOnly(2027, 8, 1));
        context.Saved.Items.Add(saved.SailingKey, saved);
        context.Ships.Items.Add(CruiseShipKey.From(saved.SailingKey));
        await context.ViewModel.ActivateAsync();

        await context.ViewModel.RemoveAsync();
        context.Saved.Items.Should().ContainKey(saved.SailingKey);
        context.ViewModel.RequestRemove();
        await context.ViewModel.RemoveAsync();

        context.Saved.Items.Should().NotContainKey(saved.SailingKey);
        context.Ships.Items.Should().Contain(CruiseShipKey.From(saved.SailingKey));
        context.Observations.RecordCalls.Should().Be(0);
        context.ViewModel.Message.Should().Contain("Recorded History was not changed");
    }

    [Fact]
    public async Task Selection_opens_existing_saved_cruise_without_saving_again()
    {
        var context = CreateContext();
        var saved = Saved("Open me", "Voyager", new DateOnly(2027, 8, 1),
            evaluation: new CruiseEvaluation(CruiseInterestLevel.Maybe, notes: "Existing"));
        context.Saved.Items.Add(saved.SailingKey, saved);

        await context.ViewModel.ActivateAsync();

        context.Evaluation.IsEditorOpen.Should().BeTrue();
        context.Evaluation.Notes.Should().Be("Existing");
        context.Saved.UpsertCalls.Should().Be(0);
        context.Observations.RecordCalls.Should().Be(0);
    }

    [Fact]
    public async Task Preferences_secondary_mode_preserves_unsaved_draft_and_does_not_change_saved_filters()
    {
        var context = CreateContext();
        var saved = Saved("Keep listed", "Voyager", new DateOnly(2027, 8, 1));
        context.Saved.Items.Add(saved.SailingKey, saved);
        await context.ViewModel.ActivateAsync();
        context.ViewModel.ShortlistCount.Should().Be(1);

        context.ViewModel.IsPreferencesMode = true;
        await context.Preferences.ActivateAsync();
        context.Preferences.MonthOptions.Single(option => option.Month == 9).IsSelected = true;
        context.ViewModel.IsOrganisationMode = true;
        context.ViewModel.IsPreferencesMode = true;
        await context.Preferences.ActivateAsync();

        context.Preferences.MonthOptions.Single(option => option.Month == 9).IsSelected.Should().BeTrue();
        context.Preferences.HasUnsavedChanges.Should().BeTrue();
        context.PreferencesRepository.SaveCalls.Should().Be(0);
        context.ViewModel.ShortlistCount.Should().Be(1);
    }

    private static TestContext CreateContext()
    {
        var saved = new FakeSavedCruiseRepository();
        var ships = new FakeFavouriteCruiseShipRepository();
        var observations = new FakeCruiseObservationRepository();
        var preferencesRepository = new FakeCruisePreferencesRepository();
        var preferences = new CruisePreferencesViewModel(
            new GetPreferences(preferencesRepository),
            new SavePreferences(preferencesRepository));
        var evaluation = new CruiseSaveAndEvaluationViewModel(
            new SaveUseCase(saved), new GetSaved(saved), new UpdateEvaluation(saved),
            new SetFavourite(saved), new SetShip(ships), new ListShips(ships),
            new Factory(), new FixedClock());
        var viewModel = new SavedCruisesViewModel(
            new ListDetails(saved, ships, observations, new CruisePriceHistoryAnalyzer()),
            new DismissUseCase(saved), new RestoreUseCase(saved), new RemoveUseCase(saved), evaluation, preferences);
        return new(viewModel, evaluation, preferences, saved, ships, observations, preferencesRepository);
    }

    private static SavedCruise Saved(
        string title,
        string ship,
        DateOnly departure,
        SavedCruiseStatus status = SavedCruiseStatus.Shortlisted,
        CruiseEvaluation? evaluation = null,
        bool isFavourite = false)
    {
        var key = new CruiseSailingKey("marella", ship, departure, 7);
        var snapshot = new SavedCruiseSnapshot(key, title, "Marella Cruises",
            new CruisePrice(999, "GBP", "per person"), FixedClock.Value);
        return new(snapshot, status, evaluation, isFavourite);
    }

    private sealed record TestContext(
        SavedCruisesViewModel ViewModel,
        CruiseSaveAndEvaluationViewModel Evaluation,
        CruisePreferencesViewModel Preferences,
        FakeSavedCruiseRepository Saved,
        FakeFavouriteCruiseShipRepository Ships,
        FakeCruiseObservationRepository Observations,
        FakeCruisePreferencesRepository PreferencesRepository);

    private sealed class FixedClock : IClock
    {
        internal static readonly DateTimeOffset Value = new(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);
        public DateTimeOffset Now => Value;
    }
}
