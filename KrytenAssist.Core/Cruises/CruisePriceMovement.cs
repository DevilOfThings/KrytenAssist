namespace KrytenAssist.Core.Cruises;

public sealed record CruisePriceMovement
{
    private CruisePriceMovement(
        CruisePriceTrendDirection direction,
        CruisePrice? previousPrice,
        CruisePrice? currentPrice,
        decimal? delta)
    {
        Direction = direction;
        PreviousPrice = previousPrice;
        CurrentPrice = currentPrice;
        Delta = delta;
    }

    public CruisePriceTrendDirection Direction { get; }

    public CruisePrice? PreviousPrice { get; }

    public CruisePrice? CurrentPrice { get; }

    public decimal? Delta { get; }

    public static CruisePriceMovement First(CruisePrice currentPrice)
    {
        ArgumentNullException.ThrowIfNull(currentPrice);
        return new CruisePriceMovement(
            CruisePriceTrendDirection.FirstObservation,
            null,
            currentPrice,
            null);
    }

    public static CruisePriceMovement Compare(CruisePrice previousPrice, CruisePrice currentPrice)
    {
        ArgumentNullException.ThrowIfNull(previousPrice);
        ArgumentNullException.ThrowIfNull(currentPrice);
        if (!CruisePriceHistoryAnalyzer.AreComparable(previousPrice, currentPrice))
        {
            return Unavailable(currentPrice);
        }

        var comparison = currentPrice.Amount.CompareTo(previousPrice.Amount);
        var direction = comparison switch
        {
            < 0 => CruisePriceTrendDirection.Lower,
            > 0 => CruisePriceTrendDirection.Higher,
            _ => CruisePriceTrendDirection.Unchanged
        };
        return new CruisePriceMovement(
            direction,
            previousPrice,
            currentPrice,
            Math.Abs(currentPrice.Amount - previousPrice.Amount));
    }

    public static CruisePriceMovement Unavailable(CruisePrice? currentPrice = null) =>
        new(CruisePriceTrendDirection.Unavailable, null, currentPrice, null);
}
