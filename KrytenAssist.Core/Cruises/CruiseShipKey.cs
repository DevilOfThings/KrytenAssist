namespace KrytenAssist.Core.Cruises;

public sealed record CruiseShipKey
{
    public CruiseShipKey(string operatorId, string shipName)
    {
        OperatorId = CruiseHistoryText.NormalizeRequired(operatorId, nameof(operatorId));
        ShipName = CruiseHistoryText.NormalizeRequired(shipName, nameof(shipName));
    }

    public string OperatorId { get; }
    public string ShipName { get; }

    public static CruiseShipKey From(CruiseSailingKey sailingKey)
    {
        ArgumentNullException.ThrowIfNull(sailingKey);
        return new(sailingKey.OperatorId, sailingKey.ShipName);
    }
}
