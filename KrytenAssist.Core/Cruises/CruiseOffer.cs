namespace KrytenAssist.Core.Cruises;

public sealed record CruiseOffer
{
    public CruiseOffer(
        CruiseProvider provider,
        string providerOfferId,
        string title,
        string shipName,
        DateOnly departureDate,
        int durationNights,
        string? departurePort = null,
        string? itinerarySummary = null)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(providerOfferId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerOfferId);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(shipName);
        ArgumentException.ThrowIfNullOrWhiteSpace(shipName);
        ArgumentOutOfRangeException.ThrowIfLessThan(durationNights, 1);

        if (departurePort is not null && string.IsNullOrWhiteSpace(departurePort))
        {
            throw new ArgumentException(
                "Departure port cannot be empty or whitespace.",
                nameof(departurePort));
        }

        if (itinerarySummary is not null && string.IsNullOrWhiteSpace(itinerarySummary))
        {
            throw new ArgumentException(
                "Itinerary summary cannot be empty or whitespace.",
                nameof(itinerarySummary));
        }

        Provider = provider;
        ProviderOfferId = providerOfferId;
        Title = title;
        ShipName = shipName;
        DepartureDate = departureDate;
        DurationNights = durationNights;
        DeparturePort = departurePort;
        ItinerarySummary = itinerarySummary;
    }

    public CruiseProvider Provider { get; }

    public string ProviderOfferId { get; }

    public string Title { get; }

    public string ShipName { get; }

    public DateOnly DepartureDate { get; }

    public int DurationNights { get; }

    public string? DeparturePort { get; }

    public string? ItinerarySummary { get; }
}
