namespace KrytenAssist.Infrastructure.Persistence;

public sealed class FavouriteCruiseShipEntity
{
    public long Id { get; set; }
    public string OperatorId { get; set; } = string.Empty;
    public string ShipName { get; set; } = string.Empty;
}
