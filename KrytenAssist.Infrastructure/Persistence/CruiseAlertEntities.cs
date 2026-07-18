namespace KrytenAssist.Infrastructure.Persistence;

public sealed class CruiseAlertEntity
{
    public Guid Id { get; set; }
    public string EventKey { get; set; } = string.Empty;
    public int Type { get; set; }
    public int Status { get; set; }
    public string OperatorId { get; set; } = string.Empty;
    public string ShipName { get; set; } = string.Empty;
    public DateOnly DepartureDate { get; set; }
    public int DurationNights { get; set; }
    public string? RetailSourceId { get; set; }
    public string? RetailSourceName { get; set; }
    public DateTimeOffset EventTime { get; set; }
    public long EventTimeUtcTicks { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long CreatedAtUtcTicks { get; set; }
    public CruisePriceDropAlertDetailEntity? PriceDropDetails { get; set; }
    public CruisePromotionAlertDetailEntity? PromotionDetails { get; set; }
    public CruiseSavedCriteriaAlertDetailEntity? SavedCriteriaDetails { get; set; }
}

public sealed class CruisePriceDropAlertDetailEntity
{
    public Guid CruiseAlertId { get; set; }
    public CruiseAlertEntity Alert { get; set; } = null!;
    public decimal PreviousAmount { get; set; }
    public string PreviousCurrency { get; set; } = string.Empty;
    public string? PreviousBasis { get; set; }
    public decimal CurrentAmount { get; set; }
    public string CurrentCurrency { get; set; } = string.Empty;
    public string? CurrentBasis { get; set; }
    public decimal Reduction { get; set; }
    public decimal PercentageReduction { get; set; }
    public string EvidenceKey { get; set; } = string.Empty;
}

public sealed class CruisePromotionAlertDetailEntity
{
    public Guid CruiseAlertId { get; set; }
    public CruiseAlertEntity Alert { get; set; } = null!;
    public string? PreviousSummary { get; set; }
    public string CurrentSummary { get; set; } = string.Empty;
    public string EvidenceKey { get; set; } = string.Empty;
}

public sealed class CruiseSavedCriteriaAlertDetailEntity
{
    public Guid CruiseAlertId { get; set; }
    public CruiseAlertEntity Alert { get; set; } = null!;
    public bool MonthConfiguredAndMatched { get; set; }
    public decimal? ConfiguredBudgetAmount { get; set; }
    public string? ConfiguredBudgetCurrency { get; set; }
    public int? ConfiguredBudgetBasis { get; set; }
    public decimal? MatchedPriceAmount { get; set; }
    public string? MatchedPriceCurrency { get; set; }
    public string? MatchedPriceBasis { get; set; }
    public string CriteriaFingerprint { get; set; } = string.Empty;
    public int EvidenceOrigin { get; set; }
    public string EvidenceKey { get; set; } = string.Empty;
    public DateTimeOffset EvidenceTime { get; set; }
    public bool CabinPreferencesUnavailable { get; set; }
}

public sealed class CruiseAlertSettingsEntity
{
    public int Id { get; set; }
    public bool PriceDropEnabled { get; set; }
    public bool PromotionEnabled { get; set; }
    public bool SavedCriteriaEnabled { get; set; }
    public decimal MinimumPriceDropPercentage { get; set; }
}

public sealed class SavedCruiseCriteriaEvaluationStateEntity
{
    public long Id { get; set; }
    public string OperatorId { get; set; } = string.Empty;
    public string ShipName { get; set; } = string.Empty;
    public DateOnly DepartureDate { get; set; }
    public int DurationNights { get; set; }
    public string CriteriaFingerprint { get; set; } = string.Empty;
    public string EvidenceKey { get; set; } = string.Empty;
    public DateTimeOffset EvidenceTime { get; set; }
    public long EvidenceTimeUtcTicks { get; set; }
    public int Result { get; set; }
}
