namespace KrytenAssist.Core.Cruises;

public sealed record SavedCruiseSnapshot
{
    public const int MaximumTitleLength = 1000;
    public const int MaximumOperatorNameLength = 500;
    public const int MaximumDeparturePortLength = 500;
    public const int MaximumItineraryLength = 4000;
    public const int MaximumSourceReferenceLength = 4000;
    public const int MaximumRetailSourceIdLength = 200;
    public const int MaximumRetailSourceNameLength = 500;

    public SavedCruiseSnapshot(
        CruiseSailingKey sailingKey,
        string title,
        string operatorName,
        CruisePrice displayedPrice,
        DateTimeOffset savedAt,
        string? departurePort = null,
        string? itinerarySummary = null,
        CruiseSource? retailSource = null,
        string? sourceReference = null)
    {
        ArgumentNullException.ThrowIfNull(sailingKey);
        ArgumentNullException.ThrowIfNull(displayedPrice);
        SailingKey = sailingKey;
        Title = Required(title, MaximumTitleLength, nameof(title));
        OperatorName = Required(operatorName, MaximumOperatorNameLength, nameof(operatorName));
        DeparturePort = Optional(departurePort, MaximumDeparturePortLength, nameof(departurePort));
        ItinerarySummary = Optional(itinerarySummary, MaximumItineraryLength, nameof(itinerarySummary));
        SourceReference = Optional(sourceReference, MaximumSourceReferenceLength, nameof(sourceReference));
        if (retailSource is not null)
        {
            Limit(retailSource.Id, MaximumRetailSourceIdLength, nameof(retailSource));
            Limit(retailSource.Name, MaximumRetailSourceNameLength, nameof(retailSource));
        }
        DisplayedPrice = displayedPrice;
        SavedAt = savedAt;
        RetailSource = retailSource;
    }

    public CruiseSailingKey SailingKey { get; }
    public string Title { get; }
    public string OperatorName { get; }
    public string? DeparturePort { get; }
    public string? ItinerarySummary { get; }
    public CruisePrice DisplayedPrice { get; }
    public CruiseSource? RetailSource { get; }
    public string? SourceReference { get; }
    public DateTimeOffset SavedAt { get; }

    private static string Required(string value, int maximum, string name)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        return Limit(value.Trim(), maximum, name);
    }

    private static string? Optional(string? value, int maximum, string name) =>
        string.IsNullOrWhiteSpace(value) ? null : Limit(value.Trim(), maximum, name);

    private static string Limit(string value, int maximum, string name)
    {
        if (value.Length > maximum) throw new ArgumentException($"{name} cannot exceed {maximum} characters.", name);
        return value;
    }
}
