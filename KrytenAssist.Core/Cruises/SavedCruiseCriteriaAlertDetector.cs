using System.Globalization;

namespace KrytenAssist.Core.Cruises;

public sealed record CruiseCriteriaEvidence(CruiseAlertEvidenceOrigin Origin, string EvidenceKey, DateTimeOffset EvidenceTime, IReadOnlyList<CruisePrice> Prices)
{
    public CruiseCriteriaEvidence(CruiseAlertEvidenceOrigin origin, string evidenceKey, DateTimeOffset evidenceTime, IEnumerable<CruisePrice> prices)
        : this(origin, evidenceKey, evidenceTime, Array.AsReadOnly((prices ?? throw new ArgumentNullException(nameof(prices))).ToArray()))
    {
        if (!Enum.IsDefined(origin)) throw new ArgumentOutOfRangeException(nameof(origin));
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceKey);
        if (Prices.Any(x => x is null)) throw new ArgumentException("Prices cannot contain null.", nameof(prices));
    }
}

public sealed record SavedCruiseCriteriaEvaluationState
{
    public SavedCruiseCriteriaEvaluationState(
        CruiseSailingKey sailingKey,
        string criteriaFingerprint,
        string evidenceKey,
        DateTimeOffset evidenceTime,
        SavedCruiseCriteriaResult result)
    {
        ArgumentNullException.ThrowIfNull(sailingKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(criteriaFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceKey);
        if (!Enum.IsDefined(result)) throw new ArgumentOutOfRangeException(nameof(result));

        SailingKey = sailingKey;
        CriteriaFingerprint = criteriaFingerprint;
        EvidenceKey = evidenceKey;
        EvidenceTime = evidenceTime;
        Result = result;
    }

    public CruiseSailingKey SailingKey { get; }
    public string CriteriaFingerprint { get; }
    public string EvidenceKey { get; }
    public DateTimeOffset EvidenceTime { get; }
    public SavedCruiseCriteriaResult Result { get; }
}

public sealed record SavedCruiseCriteriaDetectionResult(SavedCruiseCriteriaEvaluationState State, CruiseAlertCandidate? Candidate);

public sealed class SavedCruiseCriteriaAlertDetector
{
    public SavedCruiseCriteriaDetectionResult Detect(SavedCruise saved, CruisePreferences preferences, CruiseAlertSettings settings, CruiseCriteriaEvidence evidence, SavedCruiseCriteriaEvaluationState? previousState = null)
    {
        ArgumentNullException.ThrowIfNull(saved); ArgumentNullException.ThrowIfNull(preferences); ArgumentNullException.ThrowIfNull(settings); ArgumentNullException.ThrowIfNull(evidence);
        var fingerprint = CriteriaFingerprint(preferences, settings);
        var hasMonth = preferences.DepartureMonths.Count > 0;
        var hasBudget = preferences.MaximumBudget is not null;
        var eligible = settings.SavedCriteriaEnabled && saved.Status == SavedCruiseStatus.Shortlisted && (hasMonth || hasBudget);
        var monthMatched = !hasMonth || preferences.DepartureMonths.Contains(saved.SailingKey.DepartureDate.Month);
        var matchedPrice = hasBudget ? SelectBudgetPrice(evidence.Prices, preferences.MaximumBudget!) : null;
        var met = eligible && monthMatched && (!hasBudget || matchedPrice is not null && matchedPrice.Amount <= preferences.MaximumBudget!.Amount);
        var result = eligible ? (met ? SavedCruiseCriteriaResult.Met : SavedCruiseCriteriaResult.NotMet) : SavedCruiseCriteriaResult.Unknown;
        var state = new SavedCruiseCriteriaEvaluationState(saved.SailingKey, fingerprint, evidence.EvidenceKey, evidence.EvidenceTime, result);
        var alreadyMet = previousState is not null && previousState.CriteriaFingerprint == fingerprint && previousState.Result == SavedCruiseCriteriaResult.Met;
        CruiseAlertCandidate? candidate = null;
        if (met && !alreadyMet)
        {
            var details = new CruiseSavedCriteriaAlertDetails(hasMonth && monthMatched, preferences.MaximumBudget, matchedPrice, fingerprint, evidence.Origin, evidence.EvidenceKey, evidence.EvidenceTime, preferences.PreferredCabins.Count > 0);
            candidate = new CruiseAlertCandidate(CruiseAlertType.SavedCriteria, saved.SailingKey, null, details, evidence.EvidenceTime, evidence.EvidenceKey, fingerprint);
        }
        return new(state, candidate);
    }

    public static string CriteriaFingerprint(CruisePreferences preferences, CruiseAlertSettings settings)
    {
        ArgumentNullException.ThrowIfNull(preferences); ArgumentNullException.ThrowIfNull(settings);
        var budget = preferences.MaximumBudget is null ? "-" : $"{preferences.MaximumBudget.Amount.ToString("G29", CultureInfo.InvariantCulture)}:{preferences.MaximumBudget.Currency}:{(int)preferences.MaximumBudget.Basis}";
        return CruiseAlertSettings.Hash($"criteria:v1|{string.Join(',', preferences.DepartureMonths)}|{budget}|cabins-unavailable:{string.Join(',', preferences.PreferredCabins.Select(x => (int)x))}|enabled:{settings.SavedCriteriaEnabled}");
    }

    private static CruisePrice? SelectBudgetPrice(IEnumerable<CruisePrice> prices, CruiseBudget budget)
    {
        var bases = budget.Basis == CruiseBudgetBasis.PerPerson ? new[] { "per person" } : new[] { "total", "total booking" };
        var matches = prices.Where(price => price.Currency == budget.Currency && price.Basis is not null && bases.Contains(CruiseHistoryText.Normalize(price.Basis), StringComparer.Ordinal))
            .Select(price => new CruisePrice(price.Amount, price.Currency, CruiseHistoryText.Normalize(price.Basis!))).Distinct().ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }
}
