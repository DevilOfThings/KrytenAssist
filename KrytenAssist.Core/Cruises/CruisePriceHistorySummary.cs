namespace KrytenAssist.Core.Cruises;

public sealed record CruisePriceHistorySummary
{
    internal CruisePriceHistorySummary(
        CruiseSailingKey sailingKey,
        CruiseSource? source,
        DateTimeOffset firstObservedAt,
        DateTimeOffset lastObservedAt,
        int observationCount,
        CruisePrice? currentPrice,
        CruisePrice? lowestPrice,
        CruisePrice? highestPrice,
        CruisePriceMovement movement)
    {
        ArgumentNullException.ThrowIfNull(sailingKey);
        ArgumentOutOfRangeException.ThrowIfLessThan(observationCount, 1);
        ArgumentNullException.ThrowIfNull(movement);
        if (lastObservedAt < firstObservedAt)
        {
            throw new ArgumentException("Last observation cannot precede the first observation.", nameof(lastObservedAt));
        }

        SailingKey = sailingKey;
        Source = source;
        FirstObservedAt = firstObservedAt;
        LastObservedAt = lastObservedAt;
        ObservationCount = observationCount;
        CurrentPrice = currentPrice;
        LowestPrice = lowestPrice;
        HighestPrice = highestPrice;
        Movement = movement;
    }

    public CruiseSailingKey SailingKey { get; }
    public CruiseSource? Source { get; }
    public DateTimeOffset FirstObservedAt { get; }
    public DateTimeOffset LastObservedAt { get; }
    public int ObservationCount { get; }
    public CruisePrice? CurrentPrice { get; }
    public CruisePrice? LowestPrice { get; }
    public CruisePrice? HighestPrice { get; }
    public CruisePriceMovement Movement { get; }
}
