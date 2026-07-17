namespace KrytenAssist.Infrastructure.Persistence;

public sealed class CruiseObservationPriceEntity
{
    public long Id { get; set; }
    public long CruiseObservationId { get; set; }
    public CruiseObservationEntity Observation { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Basis { get; set; }
    public int DisplayOrder { get; set; }
}
