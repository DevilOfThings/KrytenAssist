namespace KrytenAssist.Infrastructure.Persistence;

public sealed class SavedCruiseEntity
{
    public long Id { get; set; }
    public string OperatorId { get; set; } = string.Empty;
    public string ShipName { get; set; } = string.Empty;
    public DateOnly DepartureDate { get; set; }
    public int DurationNights { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string? DeparturePort { get; set; }
    public string? ItinerarySummary { get; set; }
    public decimal DisplayedPriceAmount { get; set; }
    public string DisplayedPriceCurrency { get; set; } = string.Empty;
    public string? DisplayedPriceBasis { get; set; }
    public string? RetailSourceId { get; set; }
    public string? RetailSourceName { get; set; }
    public string? SourceReference { get; set; }
    public DateTimeOffset SavedAt { get; set; }
    public int Status { get; set; }
    public int? InterestLevel { get; set; }
    public int? OverallRating { get; set; }
    public int? ItineraryRating { get; set; }
    public int? ShipRating { get; set; }
    public int? ValueRating { get; set; }
    public string? Notes { get; set; }
    public bool IsFavourite { get; set; }
}
