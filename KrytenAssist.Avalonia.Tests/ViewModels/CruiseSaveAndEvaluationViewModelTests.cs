extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Avalonia.Tests.Application.Cruises;
using KrytenAssist.Avalonia.Tools;
using KrytenAssist.Avalonia.ViewModels;
using KrytenAssist.Core.Cruises;
using GetSaved = KrytenApplication::KrytenAssist.Application.Cruises.GetSavedCruise;
using ListShips = KrytenApplication::KrytenAssist.Application.Cruises.ListFavouriteCruiseShips;
using SaveUseCase = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruise;
using SetFavourite = KrytenApplication::KrytenAssist.Application.Cruises.SetSavedCruiseFavourite;
using SetShip = KrytenApplication::KrytenAssist.Application.Cruises.SetFavouriteCruiseShip;
using Factory = KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseSnapshotFactory;
using UpdateEvaluation = KrytenApplication::KrytenAssist.Application.Cruises.UpdateCruiseEvaluation;

namespace KrytenAssist.Avalonia.Tests.ViewModels;

public sealed class CruiseSaveAndEvaluationViewModelTests
{
    [Fact]
    public async Task Save_without_recording_opens_empty_editor_and_uses_clock()
    {
        var saved = new FakeSavedCruiseRepository(); var vm = Create(saved);
        var message = await vm.SaveAndEditAsync(Observation());
        message.Should().Be("Cruise saved to your shortlist.");
        vm.IsEditorOpen.Should().BeTrue(); vm.IsSaved.Should().BeTrue(); vm.Interest.Should().Be("Unrated");
        saved.Items.Values.Single().Snapshot.SavedAt.Should().Be(FixedClock.Value);
    }

    [Fact]
    public async Task Re_save_preserves_existing_evaluation_and_favourite()
    {
        var repository = new FakeSavedCruiseRepository(); var observation = Observation(); var snapshot = new Factory().Create(observation, FixedClock.Value.AddDays(-1));
        repository.Items.Add(snapshot.SailingKey, new SavedCruise(snapshot, evaluation: new CruiseEvaluation(CruiseInterestLevel.StrongCandidate, 5, notes: "Keep"), isFavourite: true));
        var vm = Create(repository); await vm.SaveAndEditAsync(observation);
        vm.Interest.Should().Be("Strong candidate"); vm.OverallRating.Should().Be("5"); vm.Notes.Should().Be("Keep"); vm.IsFavourite.Should().BeTrue();
    }

    [Fact]
    public async Task Inspect_distinguishes_saved_and_unsaved_history_targets()
    {
        var repository = new FakeSavedCruiseRepository(); var observation = Observation(); var vm = Create(repository);
        await vm.InspectAsync(observation); vm.IsSaved.Should().BeFalse(); vm.IsEditorOpen.Should().BeFalse();
        var snapshot = new Factory().Create(observation, FixedClock.Value); repository.Items.Add(snapshot.SailingKey, new SavedCruise(snapshot));
        await vm.InspectAsync(observation); vm.IsSaved.Should().BeTrue(); vm.IsEditorOpen.Should().BeFalse(); vm.OpenEditor(); vm.IsEditorOpen.Should().BeTrue();
    }

    private static CruiseSaveAndEvaluationViewModel Create(FakeSavedCruiseRepository saved)
    { var ships=new FakeFavouriteCruiseShipRepository(); return new(new SaveUseCase(saved),new GetSaved(saved),new UpdateEvaluation(saved),new SetFavourite(saved),new SetShip(ships),new ListShips(ships),new Factory(),new FixedClock()); }
    private static CruiseObservation Observation()
    { var offer=new CruiseOffer(new CruiseProvider("marella","Marella Cruises"),"offer","Escape","Voyager",new DateOnly(2027,8,2),7);return new CruiseObservation(new CruiseSnapshot(offer,[new CruisePrice(999,"GBP")]),FixedClock.Value,"https://www.tui.co.uk/cruise/example",new CruiseSource("tui","TUI")); }
    private sealed class FixedClock:IClock { internal static readonly DateTimeOffset Value=new(2026,7,18,10,0,0,TimeSpan.Zero); public DateTimeOffset Now=>Value; }
}
