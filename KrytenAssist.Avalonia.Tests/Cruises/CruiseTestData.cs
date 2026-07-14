using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Avalonia.Tests.Cruises;

internal static class CruiseTestData
{
    internal static readonly DateTimeOffset ObservedAt = new(
        2026,
        7,
        14,
        10,
        30,
        0,
        TimeSpan.FromHours(1));

    internal const string SourceUrl = "https://example.test/cruise-of-the-week";

    internal static CruiseObservation CreateObservation(
        DateTimeOffset? observedAt = null,
        string sourceReference = SourceUrl)
    {
        var provider = new CruiseProvider("marella", "Marella Cruises");
        var offer = new CruiseOffer(
            provider,
            "offer-123",
            "Mediterranean Medley",
            "Marella Explorer",
            new DateOnly(2026, 10, 27),
            7,
            "Palma",
            "Western Mediterranean");
        var snapshot = new CruiseSnapshot(
            offer,
            [new CruisePrice(903m, "GBP", "per person")],
            "This week's deal: Save £300 per booking with code WEEK300");

        return new CruiseObservation(
            snapshot,
            observedAt ?? ObservedAt,
            sourceReference);
    }

    internal static string CreateHtml(
        string title = "Mediterranean Medley",
        string ship = "Marella Explorer",
        string departure = "27 Oct 2026 - 7 nights",
        string perPersonPrice = "£903 pp",
        string? departurePort = "From Palma on 27 Oct 2026",
        string? totalPrice = "£1,806 Total price based on 2 sharing",
        string? itinerarySummary = "Western Mediterranean",
        string? providerId = "offer-123",
        string promotion = "This week's deal: Save £300 on “Mediterranean Medley” with code WEEK300")
    {
        var port = departurePort is null ? string.Empty : $"<p>{departurePort}</p>";
        var total = totalPrice is null ? string.Empty : $"<p>{totalPrice}</p>";
        var itinerary = itinerarySummary is null
            ? string.Empty
            : $"<p data-itinerary-summary=\"{itinerarySummary}\"></p>";
        var identifier = providerId is null
            ? string.Empty
            : $" data-offer-id=\"{providerId}\"";

        return $$"""
            <html><body>
              <h2>{{promotion}}</h2>
              <article>
                <h3{{identifier}}>{{title}}</h3>
                <p data-ship-name="{{ship}}"></p>
                {{port}}
                <span>Departure date and trip duration</span>
                <p>{{departure}}</p>
                <p>{{perPersonPrice}}</p>
                {{total}}
                {{itinerary}}
              </article>
            </body></html>
            """;
    }
}
