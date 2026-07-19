using System.Globalization;

namespace KrytenAssist.Core.Cruises;

public sealed record CruiseCriteriaEvidence(CruiseAlertEvidenceOrigin Origin, string EvidenceKey, DateTimeOffset EvidenceTime, IReadOnlyList<CruisePrice> Prices, CruiseCabinObservation? CabinObservation)
{
    public CruiseCriteriaEvidence(CruiseAlertEvidenceOrigin origin, string evidenceKey, DateTimeOffset evidenceTime, IEnumerable<CruisePrice> prices, CruiseCabinObservation? cabinObservation = null)
        : this(origin, evidenceKey, evidenceTime, Array.AsReadOnly((prices ?? throw new ArgumentNullException(nameof(prices))).ToArray()), cabinObservation)
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
        var compatibleCabin = evidence.CabinObservation is not null && evidence.CabinObservation.SailingKey == saved.SailingKey
            ? evidence.CabinObservation : null;
        var fingerprint = CriteriaFingerprint(preferences, settings, compatibleCabin?.SearchContext.Fingerprint);
        var hasMonth = preferences.DepartureMonths.Count > 0;
        var hasBudget = preferences.MaximumBudget is not null;
        var hasCabins = preferences.PreferredCabins.Count > 0;
        var eligible = settings.SavedCriteriaEnabled && saved.Status == SavedCruiseStatus.Shortlisted && (hasMonth || hasBudget || hasCabins);
        var monthMatched = !hasMonth || preferences.DepartureMonths.Contains(saved.SailingKey.DepartureDate.Month);
        var matchedPrice = hasBudget ? SelectBudgetPrice(evidence.Prices, preferences.MaximumBudget!) : null;
        var monthResult = !hasMonth ? SavedCruiseCriteriaResult.Met : monthMatched ? SavedCruiseCriteriaResult.Met : SavedCruiseCriteriaResult.NotMet;
        var budgetResult = !hasBudget ? SavedCruiseCriteriaResult.Met : matchedPrice is not null && matchedPrice.Amount <= preferences.MaximumBudget!.Amount
            ? SavedCruiseCriteriaResult.Met : SavedCruiseCriteriaResult.NotMet;
        var matchedCabins = compatibleCabin is null ? [] : preferences.PreferredCabins
            .Where(cabin => compatibleCabin.StateFor(cabin) == CruiseCabinAvailabilityState.Available).ToArray();
        var cabinResult = !hasCabins ? SavedCruiseCriteriaResult.Met : matchedCabins.Length > 0
            ? SavedCruiseCriteriaResult.Met : compatibleCabin is null || preferences.PreferredCabins.Any(cabin => compatibleCabin.StateFor(cabin) == CruiseCabinAvailabilityState.Unknown)
                ? SavedCruiseCriteriaResult.Unknown : SavedCruiseCriteriaResult.NotMet;
        var configuredResults = new SavedCruiseCriteriaResult?[] { hasMonth ? monthResult : null, hasBudget ? budgetResult : null, hasCabins ? cabinResult : null }
            .Where(value => value is not null).Select(value => value!.Value).ToArray();
        var result = !eligible ? SavedCruiseCriteriaResult.Unknown : configuredResults.Contains(SavedCruiseCriteriaResult.NotMet)
            ? SavedCruiseCriteriaResult.NotMet : configuredResults.All(value => value == SavedCruiseCriteriaResult.Met)
                ? SavedCruiseCriteriaResult.Met : SavedCruiseCriteriaResult.Unknown;
        var evidenceKey = CompositeEvidenceKey(evidence, compatibleCabin);
        var evidenceTime = compatibleCabin is null || compatibleCabin.ObservedAt <= evidence.EvidenceTime ? evidence.EvidenceTime : compatibleCabin.ObservedAt;
        var state = new SavedCruiseCriteriaEvaluationState(saved.SailingKey, fingerprint, evidenceKey, evidenceTime, result);
        var alreadyMet = previousState is not null && previousState.CriteriaFingerprint == fingerprint && previousState.Result == SavedCruiseCriteriaResult.Met;
        CruiseAlertCandidate? candidate = null;
        if (result == SavedCruiseCriteriaResult.Met && !alreadyMet)
        {
            var details = new CruiseSavedCriteriaAlertDetails(hasMonth && monthMatched, preferences.MaximumBudget, matchedPrice,
                fingerprint, evidence.Origin, evidenceKey, evidenceTime, false, preferences.PreferredCabins, matchedCabins,
                hasCabins ? cabinResult : SavedCruiseCriteriaResult.Unknown, compatibleCabin?.SearchContext.Fingerprint,
                compatibleCabin?.EvidenceKey, compatibleCabin?.ObservedAt);
            candidate = new CruiseAlertCandidate(CruiseAlertType.SavedCriteria, saved.SailingKey, null, details, evidenceTime, evidenceKey, fingerprint);
        }
        return new(state, candidate);
    }

    public static string CriteriaFingerprint(CruisePreferences preferences, CruiseAlertSettings settings, string? cabinContextFingerprint = null)
    {
        ArgumentNullException.ThrowIfNull(preferences); ArgumentNullException.ThrowIfNull(settings);
        var budget = preferences.MaximumBudget is null ? "-" : $"{preferences.MaximumBudget.Amount.ToString("G29", CultureInfo.InvariantCulture)}:{preferences.MaximumBudget.Currency}:{(int)preferences.MaximumBudget.Basis}";
        return CruiseAlertSettings.Hash($"criteria:v2|{string.Join(',', preferences.DepartureMonths)}|{budget}|cabins:{string.Join(',', preferences.PreferredCabins.Select(x => (int)x))}|enabled:{settings.SavedCriteriaEnabled}|context:{cabinContextFingerprint ?? "?"}");
    }

    private static string CompositeEvidenceKey(CruiseCriteriaEvidence evidence, CruiseCabinObservation? cabin) => cabin is null
        ? evidence.EvidenceKey
        : CruiseAlertSettings.Hash($"criteria-evidence:v2|{evidence.EvidenceKey.Trim()}|{cabin.StateFingerprint}|{cabin.EvidenceKey}");

    private static CruisePrice? SelectBudgetPrice(IEnumerable<CruisePrice> prices, CruiseBudget budget)
    {
        var bases = budget.Basis == CruiseBudgetBasis.PerPerson ? new[] { "per person" } : new[] { "total", "total booking" };
        var matches = prices.Where(price => price.Currency == budget.Currency && price.Basis is not null && bases.Contains(CruiseHistoryText.Normalize(price.Basis), StringComparer.Ordinal))
            .Select(price => new CruisePrice(price.Amount, price.Currency, CruiseHistoryText.Normalize(price.Basis!))).Distinct().ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }
}
