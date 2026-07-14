namespace KrytenAssist.Core.Cruises;

public sealed record CruiseSnapshot
{
    public CruiseSnapshot(
        CruiseOffer offer,
        IEnumerable<CruisePrice> prices,
        string? promotionSummary = null)
    {
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentNullException.ThrowIfNull(prices);

        var priceList = prices.ToList();

        if (priceList.Count == 0)
        {
            throw new ArgumentException(
                "At least one cruise price is required.",
                nameof(prices));
        }

        if (priceList.Any(price => price is null))
        {
            throw new ArgumentException(
                "Cruise prices cannot contain null values.",
                nameof(prices));
        }

        if (promotionSummary is not null && string.IsNullOrWhiteSpace(promotionSummary))
        {
            throw new ArgumentException(
                "Promotion summary cannot be empty or whitespace.",
                nameof(promotionSummary));
        }

        Offer = offer;
        Prices = priceList.AsReadOnly();
        PromotionSummary = promotionSummary;
    }

    public CruiseOffer Offer { get; }

    public IReadOnlyList<CruisePrice> Prices { get; }

    public string? PromotionSummary { get; }
}
