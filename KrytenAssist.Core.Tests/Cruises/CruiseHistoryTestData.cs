using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

internal static class CruiseHistoryTestData
{
    internal static readonly DateTimeOffset FirstObserved =
        new(2026, 7, 16, 10, 30, 0, TimeSpan.FromHours(1));

    internal static CruiseObservation Observation(
        decimal perPersonPrice = 988m,
        DateTimeOffset? observedAt = null,
        string providerOfferId = "fictional-offer-101",
        string title = "Atlantic Discovery",
        string operatorId = "marella",
        string operatorName = "Marella Cruises",
        string shipName = "Marella Example",
        DateOnly? departureDate = null,
        int durationNights = 7,
        string? departurePort = "Santa Cruz, Tenerife",
        string? itinerary = "Tenerife, Gran Canaria and Lanzarote",
        string? promotion = "GBP 380 per person discount",
        CruiseSource? source = null,
        string? sourceReference = "https://www.tui.co.uk/cruise/bookitineraries/fictional-101",
        IEnumerable<CruisePrice>? prices = null) =>
        new(
            new CruiseSnapshot(
                new CruiseOffer(
                    new CruiseProvider(operatorId, operatorName),
                    providerOfferId,
                    title,
                    shipName,
                    departureDate ?? new DateOnly(2026, 12, 18),
                    durationNights,
                    departurePort,
                    itinerary),
                prices ??
                [
                    new CruisePrice(perPersonPrice, "GBP", "per person"),
                    new CruisePrice(1975m, "GBP", "total based on 2 sharing")
                ],
                promotion),
            observedAt ?? FirstObserved,
            sourceReference,
            source ?? new CruiseSource("tui", "TUI"));
}
