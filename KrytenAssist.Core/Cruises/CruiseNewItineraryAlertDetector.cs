namespace KrytenAssist.Core.Cruises;

public sealed class CruiseNewItineraryAlertDetector
{
    public IReadOnlyList<CruiseAlertCandidate> Detect(IEnumerable<CruiseItineraryFirstObservedEvent> events, CruiseAlertSettings settings)
    {
        ArgumentNullException.ThrowIfNull(events); ArgumentNullException.ThrowIfNull(settings);
        if (!settings.NewItineraryEnabled) return [];
        var candidates = events.DistinctBy(value => value.EventKey).OrderBy(value => value.EventKey, StringComparer.Ordinal).Select(value =>
        {
            var occurrence = value.Occurrence;
            var details = new CruiseNewItineraryAlertDetails(occurrence.ItineraryKey, value.ScopeFingerprint,
                value.CheckEvidenceKey, occurrence.Fingerprint, occurrence.EvidenceKey, value.EventKey, value.ObservedAt,
                occurrence.Title, occurrence.ShipName, occurrence.DepartureDate, occurrence.DurationNights,
                occurrence.DeparturePort, occurrence.ItinerarySummary, occurrence.SourceReference);
            return new CruiseAlertCandidate(CruiseAlertType.NewItinerary,
                new CruiseItineraryAlertSubject(occurrence.CatalogueKey), occurrence.Source, details,
                value.ObservedAt, value.EventKey);
        }).ToArray();
        return Array.AsReadOnly(candidates);
    }
}
