using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

internal static class CruisePersistenceTestData
{
    public static readonly DateTimeOffset ObservedAt = new(2026, 7, 16, 14, 35, 21, TimeSpan.FromHours(5.5));

    public static CruiseObservation Observation(
        decimal amount = 987.46m,
        DateTimeOffset? observedAt = null,
        string providerOfferId = "offer-101842",
        string? sourceReference = "https://example.test/cruise/101842",
        CruiseSource? source = null,
        string title = "Canarian Flavours",
        DateOnly? departureDate = null,
        string? promotion = "£380 per person discount",
        IReadOnlyList<CruisePrice>? prices = null)
    {
        var provider = new CruiseProvider("marella", "Marella Cruises");
        var offer = new CruiseOffer(
            provider,
            providerOfferId,
            title,
            "Marella Discovery 2",
            departureDate ?? new DateOnly(2026, 12, 18),
            7,
            "Santa Cruz, Tenerife",
            "Tenerife, Gran Canaria and Madeira");
        return new CruiseObservation(
            new CruiseSnapshot(
                offer,
                prices ??
                [
                    new CruisePrice(amount, "GBP", "per person"),
                    new CruisePrice(1974.92m, "GBP", "total")
                ],
                promotion),
            observedAt ?? ObservedAt,
            sourceReference,
            source ?? new CruiseSource("tui", "TUI"));
    }

    public static CruiseObservation SourceLessObservation() =>
        new(
            Observation().Snapshot,
            ObservedAt,
            sourceReference: null,
            source: null);
}
