extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using Factory = KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseSnapshotFactory;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class SavedCruiseSnapshotFactoryTests
{
    [Fact]
    public void Create_maps_bounded_context_and_uses_first_advertised_price()
    {
        var offer = new CruiseOffer(new CruiseProvider("marella", "Marella Cruises"), "offer", "Escape", "Voyager", new DateOnly(2027, 8, 2), 7, "Palma", "Spain");
        var observation = new CruiseObservation(new CruiseSnapshot(offer, [new CruisePrice(999, "GBP", "per person"), new CruisePrice(1998, "GBP", "total")]), new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero), "https://www.tui.co.uk/cruise/example", new CruiseSource("tui", "TUI"));
        var savedAt = new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);

        var result = new Factory().Create(observation, savedAt);

        result.SailingKey.Should().Be(CruiseSailingKey.From(observation));
        result.DisplayedPrice.Should().Be(observation.Snapshot.Prices[0]);
        result.SavedAt.Should().Be(savedAt);
        result.Title.Should().Be("Escape"); result.RetailSource.Should().Be(observation.Source); result.SourceReference.Should().Be(observation.SourceReference);
    }
}
