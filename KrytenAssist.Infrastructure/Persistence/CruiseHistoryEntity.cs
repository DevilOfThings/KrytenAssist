namespace KrytenAssist.Infrastructure.Persistence;

public sealed class CruiseHistoryEntity
{
    public long Id { get; set; }
    public string OperatorId { get; set; } = string.Empty;
    public string NormalizedShipName { get; set; } = string.Empty;
    public DateOnly DepartureDate { get; set; }
    public int DurationNights { get; set; }
    public string RetailSourceId { get; set; } = string.Empty;
    public string? RetailSourceName { get; set; }
    public DateTimeOffset FirstObservedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public List<CruiseObservationEntity> Observations { get; set; } = [];
}
