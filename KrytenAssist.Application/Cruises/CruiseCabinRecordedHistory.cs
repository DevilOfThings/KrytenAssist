using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed record CruiseCabinLatestEvidence(string EvidenceKey, string? SourceReference, DateTimeOffset ObservedAt);

public sealed record CruiseCabinRecordedHistory
{
    public CruiseCabinRecordedHistory(
        string seriesKey,
        DateTimeOffset lastSeenAt,
        IEnumerable<CruiseCabinObservation> observations,
        CruiseCabinLatestEvidence? latestEvidence = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seriesKey);
        ArgumentNullException.ThrowIfNull(observations);
        var ordered = observations.OrderBy(value => value.ObservedAt).ThenBy(value => value.StateFingerprint, StringComparer.Ordinal).ToArray();
        if (ordered.Length == 0 || ordered.Any(value => value is null))
            throw new ArgumentException("At least one cabin observation is required.", nameof(observations));
        if (ordered.Any(value => value.SeriesKey != seriesKey))
            throw new ArgumentException("All observations must match the series key.", nameof(observations));
        if (lastSeenAt < ordered[^1].ObservedAt)
            throw new ArgumentException("Last seen cannot precede the latest observation.", nameof(lastSeenAt));
        latestEvidence ??= new(ordered[^1].EvidenceKey, ordered[^1].SourceReference, ordered[^1].ObservedAt);
        if (latestEvidence.ObservedAt > lastSeenAt)
            throw new ArgumentException("Latest evidence cannot follow last seen.", nameof(latestEvidence));
        SeriesKey = seriesKey;
        LastSeenAt = lastSeenAt;
        Observations = Array.AsReadOnly(ordered);
        LatestEvidence = latestEvidence;
    }
    public string SeriesKey { get; }
    public DateTimeOffset LastSeenAt { get; }
    public IReadOnlyList<CruiseCabinObservation> Observations { get; }
    public CruiseCabinLatestEvidence LatestEvidence { get; }
    public CruiseCabinObservation LatestObservation => Observations[^1];
}

public enum CruiseCabinRepositoryRecordState { FirstObservationRecorded, ChangedObservationRecorded, AlreadyCurrent }

public sealed record CruiseCabinRepositoryRecordResult
{
    public CruiseCabinRepositoryRecordResult(CruiseCabinRepositoryRecordState state, CruiseCabinRecordedHistory history)
    {
        if (!Enum.IsDefined(state)) throw new ArgumentOutOfRangeException(nameof(state));
        State = state;
        History = history ?? throw new ArgumentNullException(nameof(history));
    }
    public CruiseCabinRepositoryRecordState State { get; }
    public CruiseCabinRecordedHistory History { get; }
}
