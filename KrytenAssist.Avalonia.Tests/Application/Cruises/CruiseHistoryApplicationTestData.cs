extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using RecordedHistory = KrytenApplication::KrytenAssist.Application.Cruises.CruiseRecordedHistory;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

internal static class CruiseHistoryApplicationTestData
{
    internal static readonly DateTimeOffset FirstObserved =
        new(2026, 7, 16, 10, 30, 0, TimeSpan.FromHours(1));

    internal static CruiseObservation Observation(
        decimal price = 988m,
        DateTimeOffset? observedAt = null,
        string title = "Atlantic Discovery",
        string ship = "Marella Example",
        DateOnly? departure = null,
        string? promotion = "GBP 380 per person discount",
        CruiseSource? source = null) =>
        new(
            new CruiseSnapshot(
                new CruiseOffer(
                    new CruiseProvider("marella", "Marella Cruises"),
                    "fictional-offer",
                    title,
                    ship,
                    departure ?? new DateOnly(2026, 12, 18),
                    7,
                    "Santa Cruz, Tenerife",
                    "Tenerife and Gran Canaria"),
                [new CruisePrice(price, "GBP", "per person")],
                promotion),
            observedAt ?? FirstObserved,
            "https://www.tui.co.uk/cruise/bookitineraries/fictional",
            source ?? new CruiseSource("tui", "TUI"));

    internal static RecordedHistory History(
        params CruiseObservation[] observations) =>
        new(
            CruiseSailingKey.From(observations[0]),
            observations.Max(observation => observation.ObservedAt).AddHours(1),
            observations);
}
