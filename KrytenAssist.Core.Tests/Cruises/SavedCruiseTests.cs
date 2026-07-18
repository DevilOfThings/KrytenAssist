using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class SavedCruiseTests
{
    [Fact]
    public void New_saved_cruise_is_shortlisted_without_invented_evaluation()
    {
        var saved = new SavedCruise(Snapshot());
        saved.Status.Should().Be(SavedCruiseStatus.Shortlisted);
        saved.Evaluation.Should().Be(CruiseEvaluation.Empty);
        saved.IsFavourite.Should().BeFalse();
    }

    [Fact]
    public void Snapshot_normalizes_display_text_and_rejects_limits()
    {
        var value = Snapshot(title: "  Mediterranean Escape  ");
        value.Title.Should().Be("Mediterranean Escape");
        FluentActions.Invoking(() => Snapshot(title: new string('x', 1001))).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Snapshot(source: new CruiseSource(new string('x', 201), "TUI"))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Refreshing_snapshot_preserves_personal_state()
    {
        var original = new SavedCruise(Snapshot(), SavedCruiseStatus.Dismissed, new CruiseEvaluation(CruiseInterestLevel.StrongCandidate, notes: "Keep"), true);
        var refreshed = original.RefreshSnapshot(Snapshot(title: "Updated", source: new CruiseSource("other", "Other")));
        refreshed.Status.Should().Be(SavedCruiseStatus.Dismissed);
        refreshed.Evaluation.Should().Be(original.Evaluation);
        refreshed.IsFavourite.Should().BeTrue();
        refreshed.Snapshot.Title.Should().Be("Updated");
    }

    [Fact]
    public void Snapshot_for_different_sailing_cannot_replace_saved_identity() =>
        FluentActions.Invoking(() => new SavedCruise(Snapshot()).RefreshSnapshot(Snapshot(new CruiseSailingKey("marella", "Voyager", new DateOnly(2027, 8, 3), 7))))
            .Should().Throw<ArgumentException>();

    [Fact]
    public void Ship_key_ignores_formatting_and_sailing_date()
    {
        CruiseShipKey.From(Key()).Should().Be(new CruiseShipKey(" MARELLA ", " voyager "));
        CruiseShipKey.From(new CruiseSailingKey("marella", "Voyager", new DateOnly(2030, 1, 1), 14)).Should().Be(CruiseShipKey.From(Key()));
    }

    internal static CruiseSailingKey Key() => new("marella", "Voyager", new DateOnly(2027, 8, 2), 7);
    internal static SavedCruiseSnapshot Snapshot(CruiseSailingKey? key = null, string title = "Mediterranean Escape", CruiseSource? source = null) =>
        new(key ?? Key(), title, "Marella Cruises", new CruisePrice(999, "GBP", "per person"), new DateTimeOffset(2026, 7, 18, 9, 0, 0, TimeSpan.Zero), "Palma", "Spain and Italy", source ?? new CruiseSource("tui", "TUI"), "https://www.tui.co.uk/cruise/example");
}
