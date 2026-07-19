extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using Selector = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCriteriaEvidenceSelector;
using CabinHistory = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRecordedHistory;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class CruiseCriteriaCabinEvidenceSelectorTests
{
    [Fact]
    public void Select_UsesLatestExactSailingAndRetailerWithoutMergingContexts()
    {
        var saved = Saved();
        var older = Cabin(saved.SailingKey, "tui", 2, new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero));
        var latest = Cabin(saved.SailingKey, "TUI", 1, older.ObservedAt.AddHours(1));
        var foreign = Cabin(saved.SailingKey, "other", 3, latest.ObservedAt.AddHours(1));
        var histories = new[]
        {
            new CabinHistory(older.SeriesKey, older.ObservedAt, [older]),
            new CabinHistory(latest.SeriesKey, latest.ObservedAt, [latest]),
            new CabinHistory(foreign.SeriesKey, foreign.ObservedAt, [foreign])
        };

        var evidence = new Selector().Select(saved, [], histories);

        evidence.CabinObservation.Should().BeSameAs(latest);
        evidence.CabinObservation!.SearchContext.AdultCount.Should().Be(1);
    }

    [Fact]
    public void Select_UsesCompatibleExplicitCommittedObservationAndRejectsForeignRetailer()
    {
        var saved = Saved();
        var compatible = Cabin(saved.SailingKey, "tui", 2, saved.Snapshot.SavedAt.AddHours(1));
        var foreign = Cabin(saved.SailingKey, "other", 1, compatible.ObservedAt.AddHours(1));

        new Selector().Select(saved, [], [], compatible).CabinObservation.Should().BeSameAs(compatible);
        new Selector().Select(saved, [], [], foreign).CabinObservation.Should().BeNull();
    }

    private static SavedCruise Saved()
    {
        var key = new CruiseSailingKey("marella", "Marella Voyager", new DateOnly(2026, 10, 2), 7);
        return new SavedCruise(new SavedCruiseSnapshot(key, "Cruise", "Marella Cruises",
            new CruisePrice(900m, "GBP", "per person"),
            new DateTimeOffset(2026, 7, 19, 9, 0, 0, TimeSpan.Zero), retailSource: new CruiseSource("tui", "TUI")));
    }

    private static CruiseCabinObservation Cabin(CruiseSailingKey key, string source, int adults, DateTimeOffset time)
    {
        var states = Enum.GetValues<CruiseCabinType>().Select(type => new CruiseCabinState(type,
            type == CruiseCabinType.Inside ? CruiseCabinAvailabilityState.Available : CruiseCabinAvailabilityState.Unknown));
        return new(key, new CruiseSource(source, source), new CruiseCabinSearchContext(adults, 0, [], true),
            CruiseCabinEvidenceCoverage.Partial, states, time, $"evidence-{source}-{adults}");
    }
}
