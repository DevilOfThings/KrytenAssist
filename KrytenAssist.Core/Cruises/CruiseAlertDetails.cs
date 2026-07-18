namespace KrytenAssist.Core.Cruises;

public abstract record CruiseAlertDetails;

public sealed record CruisePriceDropAlertDetails : CruiseAlertDetails
{
    public CruisePriceDropAlertDetails(CruisePrice previousPrice, CruisePrice currentPrice, string evidenceKey)
    {
        ArgumentNullException.ThrowIfNull(previousPrice);
        ArgumentNullException.ThrowIfNull(currentPrice);
        if (!CruisePriceHistoryAnalyzer.AreComparable(previousPrice, currentPrice) || currentPrice.Amount >= previousPrice.Amount)
            throw new ArgumentException("Price Drop details require a lower comparable current price.");
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceKey);
        PreviousPrice = previousPrice;
        CurrentPrice = currentPrice;
        Reduction = previousPrice.Amount - currentPrice.Amount;
        PercentageReduction = decimal.Round(Reduction / previousPrice.Amount * 100m, 4, MidpointRounding.AwayFromZero);
        EvidenceKey = evidenceKey;
    }
    public CruisePrice PreviousPrice { get; }
    public CruisePrice CurrentPrice { get; }
    public decimal Reduction { get; }
    public decimal PercentageReduction { get; }
    public string EvidenceKey { get; }
}

public sealed record CruisePromotionAlertDetails : CruiseAlertDetails
{
    public const int MaximumSummaryLength = 4000;
    public CruisePromotionAlertDetails(string? previousSummary, string currentSummary, string evidenceKey)
    {
        PreviousSummary = Optional(previousSummary);
        CurrentSummary = Required(currentSummary);
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceKey);
        EvidenceKey = evidenceKey;
    }
    public string? PreviousSummary { get; }
    public string CurrentSummary { get; }
    public string EvidenceKey { get; }
    private static string Required(string value) => Optional(value) ?? throw new ArgumentException("Current promotion is required.", nameof(value));
    private static string? Optional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length > MaximumSummaryLength) throw new ArgumentException("Promotion summary is too long.", nameof(value));
        return trimmed;
    }
}

public sealed record CruiseSavedCriteriaAlertDetails : CruiseAlertDetails
{
    public CruiseSavedCriteriaAlertDetails(
        bool monthConfiguredAndMatched,
        CruiseBudget? configuredBudget,
        CruisePrice? matchedPrice,
        string criteriaFingerprint,
        CruiseAlertEvidenceOrigin evidenceOrigin,
        string evidenceKey,
        DateTimeOffset evidenceTime,
        bool cabinPreferencesUnavailable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(criteriaFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceKey);
        if (!Enum.IsDefined(evidenceOrigin)) throw new ArgumentOutOfRangeException(nameof(evidenceOrigin));
        if ((configuredBudget is null) != (matchedPrice is null))
            throw new ArgumentException("Configured budget and matched price must be supplied together.");

        MonthConfiguredAndMatched = monthConfiguredAndMatched;
        ConfiguredBudget = configuredBudget;
        MatchedPrice = matchedPrice;
        CriteriaFingerprint = criteriaFingerprint;
        EvidenceOrigin = evidenceOrigin;
        EvidenceKey = evidenceKey;
        EvidenceTime = evidenceTime;
        CabinPreferencesUnavailable = cabinPreferencesUnavailable;
    }

    public bool MonthConfiguredAndMatched { get; }
    public CruiseBudget? ConfiguredBudget { get; }
    public CruisePrice? MatchedPrice { get; }
    public string CriteriaFingerprint { get; }
    public CruiseAlertEvidenceOrigin EvidenceOrigin { get; }
    public string EvidenceKey { get; }
    public DateTimeOffset EvidenceTime { get; }
    public bool CabinPreferencesUnavailable { get; }
}
