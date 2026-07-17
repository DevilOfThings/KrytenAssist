using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed record CruiseRecordedHistory
{
    public CruiseRecordedHistory(
        CruiseSailingKey sailingKey,
        DateTimeOffset lastSeenAt,
        IEnumerable<CruiseObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(sailingKey);
        ArgumentNullException.ThrowIfNull(observations);
        var ordered = observations
            .Select(observation =>
            {
                ArgumentNullException.ThrowIfNull(observation);
                return new ObservationWithFingerprint(
                    observation,
                    CruiseObservationFingerprint.From(observation));
            })
            .OrderBy(item => item.Observation.ObservedAt)
            .ThenBy(item => item.Fingerprint)
            .ToArray();
        if (ordered.Length == 0)
        {
            throw new ArgumentException("At least one observation is required.", nameof(observations));
        }

        if (ordered.Any(item => item.Fingerprint.SailingKey != sailingKey))
        {
            throw new ArgumentException("All observations must match the sailing key.", nameof(observations));
        }

        var sourceId = ordered[0].Fingerprint.RetailSourceId;
        if (ordered.Any(item => !string.Equals(
                item.Fingerprint.RetailSourceId,
                sourceId,
                StringComparison.Ordinal)))
        {
            throw new ArgumentException("All observations must have the same retail source.", nameof(observations));
        }

        if (lastSeenAt < ordered[^1].Observation.ObservedAt)
        {
            throw new ArgumentException(
                "Last seen time cannot precede the latest observation.",
                nameof(lastSeenAt));
        }

        SailingKey = sailingKey;
        LastSeenAt = lastSeenAt;
        Observations = Array.AsReadOnly(ordered.Select(item => item.Observation).ToArray());
        Source = Observations[^1].Source;
    }

    public CruiseSailingKey SailingKey { get; }
    public CruiseSource? Source { get; }
    public DateTimeOffset LastSeenAt { get; }
    public IReadOnlyList<CruiseObservation> Observations { get; }

    public CruisePriceHistorySummary Analyze(CruisePriceHistoryAnalyzer analyzer)
    {
        ArgumentNullException.ThrowIfNull(analyzer);
        return analyzer.Analyze(Observations);
    }

    private sealed record ObservationWithFingerprint(
        CruiseObservation Observation,
        CruiseObservationFingerprint Fingerprint);
}
