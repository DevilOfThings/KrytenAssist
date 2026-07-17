using System.Globalization;

namespace KrytenAssist.Core.Cruises;

public sealed class CruiseObservationFingerprint :
    IEquatable<CruiseObservationFingerprint>,
    IComparable<CruiseObservationFingerprint>
{
    private readonly string _comparisonKey;

    private CruiseObservationFingerprint(CruiseObservation observation)
    {
        SailingKey = CruiseSailingKey.From(observation);
        var offer = observation.Snapshot.Offer;
        var source = observation.Source;
        var prices = observation.Snapshot.Prices
            .Select(price => new PriceFingerprint(price))
            .Distinct()
            .OrderBy(price => price.ComparisonKey, StringComparer.Ordinal)
            .ToArray();

        OperatorName = CruiseHistoryText.NormalizeRequired(offer.Provider.Name, nameof(offer.Provider.Name));
        Title = CruiseHistoryText.NormalizeRequired(offer.Title, nameof(offer.Title));
        ShipName = CruiseHistoryText.NormalizeRequired(offer.ShipName, nameof(offer.ShipName));
        DeparturePort = CruiseHistoryText.NormalizeOptional(offer.DeparturePort);
        ItinerarySummary = CruiseHistoryText.NormalizeOptional(offer.ItinerarySummary);
        RetailSourceId = source is null ? null : CruiseHistoryText.NormalizeRequired(source.Id, nameof(source.Id));
        RetailSourceName = source is null ? null : CruiseHistoryText.NormalizeRequired(source.Name, nameof(source.Name));
        PromotionSummary = CruiseHistoryText.NormalizeOptional(observation.Snapshot.PromotionSummary);
        Prices = Array.AsReadOnly(prices.Select(price => price.ToCruisePrice()).ToArray());

        _comparisonKey = string.Join(
            '|',
            CruiseHistoryText.Component(SailingKey.OperatorId),
            CruiseHistoryText.Component(SailingKey.ShipName),
            SailingKey.DepartureDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            SailingKey.DurationNights.ToString(CultureInfo.InvariantCulture),
            CruiseHistoryText.Component(OperatorName),
            CruiseHistoryText.Component(Title),
            CruiseHistoryText.Component(ShipName),
            CruiseHistoryText.Component(DeparturePort),
            CruiseHistoryText.Component(ItinerarySummary),
            CruiseHistoryText.Component(RetailSourceId),
            CruiseHistoryText.Component(RetailSourceName),
            string.Join(';', prices.Select(price => price.ComparisonKey)),
            CruiseHistoryText.Component(PromotionSummary));
    }

    public CruiseSailingKey SailingKey { get; }

    public string OperatorName { get; }

    public string Title { get; }

    public string ShipName { get; }

    public string? DeparturePort { get; }

    public string? ItinerarySummary { get; }

    public string? RetailSourceId { get; }

    public string? RetailSourceName { get; }

    public IReadOnlyList<CruisePrice> Prices { get; }

    public string? PromotionSummary { get; }

    internal string ComparisonKey => _comparisonKey;

    public static CruiseObservationFingerprint From(CruiseObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        return new CruiseObservationFingerprint(observation);
    }

    public bool Equals(CruiseObservationFingerprint? other) =>
        other is not null && string.Equals(_comparisonKey, other._comparisonKey, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as CruiseObservationFingerprint);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_comparisonKey);

    public int CompareTo(CruiseObservationFingerprint? other) =>
        other is null
            ? 1
            : StringComparer.Ordinal.Compare(_comparisonKey, other._comparisonKey);

    private sealed class PriceFingerprint : IEquatable<PriceFingerprint>
    {
        public PriceFingerprint(CruisePrice price)
        {
            Amount = price.Amount;
            Currency = price.Currency.ToUpperInvariant();
            Basis = CruiseHistoryText.NormalizeOptional(price.Basis);
            ComparisonKey = string.Join(
                ':',
                Amount.ToString("G29", CultureInfo.InvariantCulture),
                CruiseHistoryText.Component(Currency),
                CruiseHistoryText.Component(Basis));
        }

        public decimal Amount { get; }
        public string Currency { get; }
        public string? Basis { get; }
        public string ComparisonKey { get; }

        public CruisePrice ToCruisePrice() => new(Amount, Currency, Basis);

        public bool Equals(PriceFingerprint? other) =>
            other is not null && string.Equals(ComparisonKey, other.ComparisonKey, StringComparison.Ordinal);

        public override bool Equals(object? obj) => Equals(obj as PriceFingerprint);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ComparisonKey);
    }
}
