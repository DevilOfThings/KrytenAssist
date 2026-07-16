namespace KrytenAssist.Core.Cruises;

public sealed class CruisePriceHistoryAnalyzer
{
    private const string PreferredCurrency = "GBP";
    private const string PreferredBasis = "per person";

    public CruisePrice? SelectComparablePrice(CruiseSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var distinct = snapshot.Prices
            .Select(CanonicalPrice)
            .Distinct()
            .ToArray();
        var preferred = distinct
            .Where(price => price.Currency == PreferredCurrency && price.Basis == PreferredBasis)
            .ToArray();

        if (preferred.Length == 1)
        {
            return preferred[0];
        }

        if (preferred.Length > 1)
        {
            return null;
        }

        return distinct.Length == 1 && distinct[0].Basis is not null
            ? distinct[0]
            : null;
    }

    public CruisePriceHistorySummary Analyze(IEnumerable<CruiseObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        var ordered = observations
            .Select(observation =>
            {
                ArgumentNullException.ThrowIfNull(observation);
                return new AnalyzedObservation(
                    observation,
                    CruiseObservationFingerprint.From(observation));
            })
            .OrderBy(item => item.Observation.ObservedAt)
            .ThenBy(item => item.Fingerprint.ComparisonKey, StringComparer.Ordinal)
            .ToArray();
        if (ordered.Length == 0)
        {
            throw new ArgumentException("At least one observation is required.", nameof(observations));
        }

        var sailingKey = ordered[0].Fingerprint.SailingKey;
        var sourceId = ordered[0].Fingerprint.RetailSourceId;
        if (ordered.Any(item => item.Fingerprint.SailingKey != sailingKey))
        {
            throw new ArgumentException("All observations must describe the same sailing.", nameof(observations));
        }

        if (ordered.Any(item => !string.Equals(
                item.Fingerprint.RetailSourceId,
                sourceId,
                StringComparison.Ordinal)))
        {
            throw new ArgumentException("All observations must have the same retail source.", nameof(observations));
        }

        var latest = ordered[^1];
        var currentPrice = SelectComparablePrice(latest.Observation.Snapshot);
        if (currentPrice is null)
        {
            return CreateSummary(ordered, sailingKey, null, null, null, CruisePriceMovement.Unavailable());
        }

        var matchingPrices = ordered
            .Select(item => SelectComparablePrice(item.Observation.Snapshot))
            .Where(price => price is not null && AreComparable(price, currentPrice))
            .Cast<CruisePrice>()
            .ToArray();
        var lowest = matchingPrices.MinBy(price => price.Amount)!;
        var highest = matchingPrices.MaxBy(price => price.Amount)!;
        CruisePriceMovement movement;
        if (ordered.Length == 1)
        {
            movement = CruisePriceMovement.First(currentPrice);
        }
        else
        {
            var previousPrice = SelectComparablePrice(ordered[^2].Observation.Snapshot);
            movement = previousPrice is null || !AreComparable(previousPrice, currentPrice)
                ? CruisePriceMovement.Unavailable(currentPrice)
                : CruisePriceMovement.Compare(previousPrice, currentPrice);
        }

        return CreateSummary(ordered, sailingKey, currentPrice, lowest, highest, movement);
    }

    internal static bool AreComparable(CruisePrice left, CruisePrice right) =>
        string.Equals(left.Currency, right.Currency, StringComparison.Ordinal) &&
        string.Equals(
            CruiseHistoryText.NormalizeOptional(left.Basis),
            CruiseHistoryText.NormalizeOptional(right.Basis),
            StringComparison.Ordinal);

    private static CruisePrice CanonicalPrice(CruisePrice price) =>
        new(price.Amount, price.Currency, CruiseHistoryText.NormalizeOptional(price.Basis));

    private static CruisePriceHistorySummary CreateSummary(
        IReadOnlyList<AnalyzedObservation> ordered,
        CruiseSailingKey sailingKey,
        CruisePrice? currentPrice,
        CruisePrice? lowestPrice,
        CruisePrice? highestPrice,
        CruisePriceMovement movement) =>
        new(
            sailingKey,
            ordered[^1].Observation.Source,
            ordered[0].Observation.ObservedAt,
            ordered[^1].Observation.ObservedAt,
            ordered.Count,
            currentPrice,
            lowestPrice,
            highestPrice,
            movement);

    private sealed record AnalyzedObservation(
        CruiseObservation Observation,
        CruiseObservationFingerprint Fingerprint);
}
