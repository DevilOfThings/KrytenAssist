namespace KrytenAssist.Core.Cruises;

public sealed class CruiseCabinAvailabilityAlertDetector(CruiseCabinHistoryAnalyzer analyzer)
{
    public IReadOnlyList<CruiseAlertCandidate> Detect(
        CruiseCabinObservation previous,
        CruiseCabinObservation current,
        SavedCruise? savedCruise,
        CruisePreferences preferences,
        CruiseAlertSettings settings)
    {
        ArgumentNullException.ThrowIfNull(previous); ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(preferences); ArgumentNullException.ThrowIfNull(settings);
        var changes = analyzer.Compare(previous, current);
        if (!settings.CabinAvailabilityEnabled || savedCruise?.Status != SavedCruiseStatus.Shortlisted ||
            savedCruise.SailingKey != current.SailingKey || preferences.PreferredCabins.Count == 0)
            return [];

        return changes.Where(change => change.IsExplicitInventoryTransition && preferences.PreferredCabins.Contains(change.CabinType))
            .OrderBy(change => change.CabinType)
            .Select(change =>
            {
                var evidence = $"{current.StateFingerprint}:{(int)change.CabinType}";
                var details = new CruiseCabinAvailabilityAlertDetails(change.CabinType, change.PreviousState,
                    change.CurrentState, current.SearchContext.Fingerprint, current.Coverage,
                    current.StateFingerprint, current.EvidenceKey, current.ObservedAt);
                return new CruiseAlertCandidate(CruiseAlertType.CabinAvailability, current.SailingKey,
                    current.Source, details, current.ObservedAt, evidence);
            }).ToArray();
    }
}
