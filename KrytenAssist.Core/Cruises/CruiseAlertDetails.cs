namespace KrytenAssist.Core.Cruises;

public abstract record CruiseAlertDetails;

public sealed record CruiseNewItineraryAlertDetails : CruiseAlertDetails
{
    public CruiseNewItineraryAlertDetails(CruiseItineraryKey itineraryKey, string scopeFingerprint,
        string checkEvidenceKey, string occurrenceFingerprint, string providerEvidenceKey,
        string firstObservedEventKey, DateTimeOffset firstObservedAt, string? title = null,
        string? shipName = null, DateOnly? departureDate = null, int? durationNights = null,
        string? departurePort = null, string? itinerarySummary = null, string? sourceReference = null)
    {
        ItineraryKey = itineraryKey ?? throw new ArgumentNullException(nameof(itineraryKey));
        ScopeFingerprint = Hash(scopeFingerprint, nameof(scopeFingerprint));
        CheckEvidenceKey = Hash(checkEvidenceKey, nameof(checkEvidenceKey));
        OccurrenceFingerprint = Hash(occurrenceFingerprint, nameof(occurrenceFingerprint));
        ProviderEvidenceKey = Required(providerEvidenceKey, CruiseItineraryOccurrence.MaximumSummaryLength, nameof(providerEvidenceKey), false);
        FirstObservedEventKey = Hash(firstObservedEventKey, nameof(firstObservedEventKey));
        if (durationNights is < 1 or > CruiseItineraryOccurrence.MaximumDurationNights) throw new ArgumentOutOfRangeException(nameof(durationNights));
        FirstObservedAt = firstObservedAt; Title = Optional(title, CruiseItineraryOccurrence.MaximumDisplayLength, nameof(title));
        ShipName = Optional(shipName, CruiseItineraryOccurrence.MaximumDisplayLength, nameof(shipName)); DepartureDate = departureDate;
        DurationNights = durationNights; DeparturePort = Optional(departurePort, CruiseItineraryOccurrence.MaximumDisplayLength, nameof(departurePort));
        ItinerarySummary = Optional(itinerarySummary, CruiseItineraryOccurrence.MaximumSummaryLength, nameof(itinerarySummary));
        SourceReference = Optional(sourceReference, CruiseItineraryOccurrence.MaximumSourceReferenceLength, nameof(sourceReference), false);
    }
    public CruiseItineraryKey ItineraryKey { get; }
    public string ScopeFingerprint { get; }
    public string CheckEvidenceKey { get; }
    public string OccurrenceFingerprint { get; }
    public string ProviderEvidenceKey { get; }
    public string FirstObservedEventKey { get; }
    public DateTimeOffset FirstObservedAt { get; }
    public string? Title { get; }
    public string? ShipName { get; }
    public DateOnly? DepartureDate { get; }
    public int? DurationNights { get; }
    public string? DeparturePort { get; }
    public string? ItinerarySummary { get; }
    public string? SourceReference { get; }
    private static string Hash(string value, string name)
    { var result = Required(value, 64, name, false); return result.Length == 64 && result.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f') ? result : throw new ArgumentException($"{name} must be a lowercase SHA-256 value.", name); }
    private static string Required(string value, int maximum, string name, bool normalize = true)
    { ArgumentException.ThrowIfNullOrWhiteSpace(value); var result = normalize ? CruiseHistoryText.NormalizeRequired(value, name) : value.Trim(); if (result.Length > maximum) throw new ArgumentException($"{name} is too long.", name); return result; }
    private static string? Optional(string? value, int maximum, string name, bool normalize = true)
    { if (string.IsNullOrWhiteSpace(value)) return null; return Required(value, maximum, name, normalize); }
}

public sealed record CruiseCabinAvailabilityAlertDetails : CruiseAlertDetails
{
    public CruiseCabinAvailabilityAlertDetails(
        CruiseCabinType cabinType,
        CruiseCabinAvailabilityState previousState,
        CruiseCabinAvailabilityState currentState,
        string contextFingerprint,
        CruiseCabinEvidenceCoverage coverage,
        string stateFingerprint,
        string evidenceKey,
        DateTimeOffset evidenceTime)
    {
        if (!Enum.IsDefined(cabinType)) throw new ArgumentOutOfRangeException(nameof(cabinType));
        if (!Enum.IsDefined(previousState)) throw new ArgumentOutOfRangeException(nameof(previousState));
        if (!Enum.IsDefined(currentState)) throw new ArgumentOutOfRangeException(nameof(currentState));
        if (!Enum.IsDefined(coverage)) throw new ArgumentOutOfRangeException(nameof(coverage));
        if (previousState == CruiseCabinAvailabilityState.Unavailable && currentState == CruiseCabinAvailabilityState.Available)
            Direction = CruiseCabinAlertDirection.BecameAvailable;
        else if (previousState == CruiseCabinAvailabilityState.Available && currentState == CruiseCabinAvailabilityState.Unavailable)
            Direction = CruiseCabinAlertDirection.BecameUnavailable;
        else
            throw new ArgumentException("Cabin alert details require an explicit opposite-state transition.");
        ArgumentException.ThrowIfNullOrWhiteSpace(contextFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceKey);
        CabinType = cabinType;
        PreviousState = previousState;
        CurrentState = currentState;
        ContextFingerprint = contextFingerprint.Trim();
        Coverage = coverage;
        StateFingerprint = stateFingerprint.Trim();
        EvidenceKey = evidenceKey.Trim();
        EvidenceTime = evidenceTime;
    }
    public CruiseCabinType CabinType { get; }
    public CruiseCabinAvailabilityState PreviousState { get; }
    public CruiseCabinAvailabilityState CurrentState { get; }
    public CruiseCabinAlertDirection Direction { get; }
    public string ContextFingerprint { get; }
    public CruiseCabinEvidenceCoverage Coverage { get; }
    public string StateFingerprint { get; }
    public string EvidenceKey { get; }
    public DateTimeOffset EvidenceTime { get; }
}

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
        bool cabinPreferencesUnavailable,
        IEnumerable<CruiseCabinType>? configuredCabins = null,
        IEnumerable<CruiseCabinType>? matchedCabins = null,
        SavedCruiseCriteriaResult cabinCriterionResult = SavedCruiseCriteriaResult.Unknown,
        string? cabinContextFingerprint = null,
        string? cabinEvidenceKey = null,
        DateTimeOffset? cabinEvidenceTime = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(criteriaFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceKey);
        if (!Enum.IsDefined(evidenceOrigin)) throw new ArgumentOutOfRangeException(nameof(evidenceOrigin));
        if ((configuredBudget is null) != (matchedPrice is null))
            throw new ArgumentException("Configured budget and matched price must be supplied together.");
        if (!Enum.IsDefined(cabinCriterionResult)) throw new ArgumentOutOfRangeException(nameof(cabinCriterionResult));
        var configured = (configuredCabins ?? []).Distinct().Order().ToArray();
        var matched = (matchedCabins ?? []).Distinct().Order().ToArray();
        if (configured.Any(value => !Enum.IsDefined(value)) || matched.Any(value => !Enum.IsDefined(value)) || matched.Except(configured).Any())
            throw new ArgumentException("Matched cabins must be configured cabin types.");
        if ((cabinEvidenceKey is null) != (cabinEvidenceTime is null))
            throw new ArgumentException("Cabin evidence key and time must be supplied together.");

        MonthConfiguredAndMatched = monthConfiguredAndMatched;
        ConfiguredBudget = configuredBudget;
        MatchedPrice = matchedPrice;
        CriteriaFingerprint = criteriaFingerprint;
        EvidenceOrigin = evidenceOrigin;
        EvidenceKey = evidenceKey;
        EvidenceTime = evidenceTime;
        CabinPreferencesUnavailable = cabinPreferencesUnavailable;
        ConfiguredCabins = Array.AsReadOnly(configured);
        MatchedCabins = Array.AsReadOnly(matched);
        CabinCriterionResult = cabinCriterionResult;
        CabinContextFingerprint = cabinContextFingerprint;
        CabinEvidenceKey = cabinEvidenceKey;
        CabinEvidenceTime = cabinEvidenceTime;
    }

    public bool MonthConfiguredAndMatched { get; }
    public CruiseBudget? ConfiguredBudget { get; }
    public CruisePrice? MatchedPrice { get; }
    public string CriteriaFingerprint { get; }
    public CruiseAlertEvidenceOrigin EvidenceOrigin { get; }
    public string EvidenceKey { get; }
    public DateTimeOffset EvidenceTime { get; }
    public bool CabinPreferencesUnavailable { get; }
    public IReadOnlyList<CruiseCabinType> ConfiguredCabins { get; }
    public IReadOnlyList<CruiseCabinType> MatchedCabins { get; }
    public SavedCruiseCriteriaResult CabinCriterionResult { get; }
    public string? CabinContextFingerprint { get; }
    public string? CabinEvidenceKey { get; }
    public DateTimeOffset? CabinEvidenceTime { get; }
}
