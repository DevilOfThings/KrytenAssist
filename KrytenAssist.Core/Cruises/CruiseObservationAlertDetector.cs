namespace KrytenAssist.Core.Cruises;

public sealed class CruiseObservationAlertDetector(CruisePriceHistoryAnalyzer analyzer)
{
    public IReadOnlyList<CruiseAlertCandidate> Detect(CruiseObservation previous, CruiseObservation current, CruiseAlertSettings settings)
    {
        ArgumentNullException.ThrowIfNull(previous); ArgumentNullException.ThrowIfNull(current); ArgumentNullException.ThrowIfNull(settings);
        var previousFingerprint = CruiseObservationFingerprint.From(previous);
        var currentFingerprint = CruiseObservationFingerprint.From(current);
        if (previousFingerprint.SailingKey != currentFingerprint.SailingKey ||
            !string.Equals(previousFingerprint.RetailSourceId, currentFingerprint.RetailSourceId, StringComparison.Ordinal))
            throw new ArgumentException("Alert comparison requires one sailing and retail source.");
        var candidates = new List<CruiseAlertCandidate>();
        if (settings.PriceDropEnabled)
        {
            var oldPrice = analyzer.SelectComparablePrice(previous.Snapshot);
            var newPrice = analyzer.SelectComparablePrice(current.Snapshot);
            if (oldPrice is not null && newPrice is not null && CruisePriceHistoryAnalyzer.AreComparable(oldPrice, newPrice) && newPrice.Amount < oldPrice.Amount)
            {
                var details = new CruisePriceDropAlertDetails(oldPrice, newPrice, currentFingerprint.PersistenceKey);
                if (details.PercentageReduction >= settings.MinimumPriceDropPercentage)
                    candidates.Add(new(CruiseAlertType.PriceDrop, currentFingerprint.SailingKey, current.Source, details, current.ObservedAt, currentFingerprint.PersistenceKey));
            }
        }
        if (settings.PromotionEnabled && current.Snapshot.PromotionSummary is not null &&
            !string.Equals(CruiseHistoryText.NormalizeOptional(previous.Snapshot.PromotionSummary), CruiseHistoryText.NormalizeOptional(current.Snapshot.PromotionSummary), StringComparison.Ordinal))
        {
            var details = new CruisePromotionAlertDetails(previous.Snapshot.PromotionSummary, current.Snapshot.PromotionSummary, currentFingerprint.PersistenceKey);
            candidates.Add(new(CruiseAlertType.Promotion, currentFingerprint.SailingKey, current.Source, details, current.ObservedAt, currentFingerprint.PersistenceKey));
        }
        return Array.AsReadOnly(candidates.ToArray());
    }
}
