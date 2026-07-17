namespace KrytenAssist.Infrastructure.Persistence;

public sealed class CruiseObservationEntity
{
    public long Id { get; set; }
    public long CruiseHistoryId { get; set; }
    public CruiseHistoryEntity History { get; set; } = null!;
    public string Fingerprint { get; set; } = string.Empty;
    public string ProviderOfferId { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ShipName { get; set; } = string.Empty;
    public DateOnly DepartureDate { get; set; }
    public int DurationNights { get; set; }
    public string? DeparturePort { get; set; }
    public string? ItinerarySummary { get; set; }
    public string? PromotionSummary { get; set; }
    public string? SourceReference { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public List<CruiseObservationPriceEntity> Prices { get; set; } = [];
}
