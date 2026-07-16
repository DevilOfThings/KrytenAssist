namespace KrytenAssist.Core.Cruises;

public sealed record CruiseSailingKey
{
    public CruiseSailingKey(
        string operatorId,
        string shipName,
        DateOnly departureDate,
        int durationNights)
    {
        OperatorId = CruiseHistoryText.NormalizeRequired(operatorId, nameof(operatorId));
        ShipName = CruiseHistoryText.NormalizeRequired(shipName, nameof(shipName));
        ArgumentOutOfRangeException.ThrowIfLessThan(durationNights, 1);

        DepartureDate = departureDate;
        DurationNights = durationNights;
    }

    public string OperatorId { get; }

    public string ShipName { get; }

    public DateOnly DepartureDate { get; }

    public int DurationNights { get; }

    public static CruiseSailingKey From(CruiseObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var offer = observation.Snapshot.Offer;
        return new CruiseSailingKey(
            offer.Provider.Id,
            offer.ShipName,
            offer.DepartureDate,
            offer.DurationNights);
    }
}
