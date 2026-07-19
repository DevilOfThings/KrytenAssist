namespace KrytenAssist.Core.Cruises;

public sealed record CruiseCabinAvailabilityChange
{
    public CruiseCabinAvailabilityChange(CruiseCabinType cabinType, CruiseCabinAvailabilityState previousState,
        CruiseCabinAvailabilityState currentState)
    {
        if (!Enum.IsDefined(cabinType)) throw new ArgumentOutOfRangeException(nameof(cabinType));
        if (!Enum.IsDefined(previousState)) throw new ArgumentOutOfRangeException(nameof(previousState));
        if (!Enum.IsDefined(currentState)) throw new ArgumentOutOfRangeException(nameof(currentState));
        if (previousState == currentState) throw new ArgumentException("A cabin change requires different states.");
        CabinType = cabinType; PreviousState = previousState; CurrentState = currentState;
    }
    public CruiseCabinType CabinType { get; }
    public CruiseCabinAvailabilityState PreviousState { get; }
    public CruiseCabinAvailabilityState CurrentState { get; }
    public bool IsExplicitInventoryTransition =>
        PreviousState is CruiseCabinAvailabilityState.Available or CruiseCabinAvailabilityState.Unavailable &&
        CurrentState is CruiseCabinAvailabilityState.Available or CruiseCabinAvailabilityState.Unavailable &&
        PreviousState != CurrentState;
}

public sealed record CruiseCabinHistorySummary(
    string SeriesKey,
    DateTimeOffset FirstObservedAt,
    DateTimeOffset LastSeenAt,
    CruiseCabinObservation LatestObservation,
    int ObservationCount,
    IReadOnlyList<CruiseCabinAvailabilityChange> LatestExplicitChanges);

public sealed class CruiseCabinHistoryAnalyzer
{
    public IReadOnlyList<CruiseCabinAvailabilityChange> Compare(CruiseCabinObservation previous, CruiseCabinObservation current)
    {
        ArgumentNullException.ThrowIfNull(previous); ArgumentNullException.ThrowIfNull(current);
        if (!string.Equals(previous.SeriesKey, current.SeriesKey, StringComparison.Ordinal))
            throw new ArgumentException("Cabin observations must belong to the same series.", nameof(current));
        if (previous.StateFingerprint == current.StateFingerprint) return [];
        return Enum.GetValues<CruiseCabinType>()
            .Where(type => previous.StateFor(type) != current.StateFor(type))
            .Select(type => new CruiseCabinAvailabilityChange(type, previous.StateFor(type), current.StateFor(type)))
            .ToArray();
    }

    public CruiseCabinHistorySummary Analyze(IEnumerable<CruiseCabinObservation> observations, DateTimeOffset? lastSeenAt = null)
    {
        ArgumentNullException.ThrowIfNull(observations);
        var ordered = observations.OrderBy(value => value.ObservedAt).ThenBy(value => value.StateFingerprint, StringComparer.Ordinal).ToArray();
        if (ordered.Length == 0) throw new ArgumentException("At least one observation is required.", nameof(observations));
        if (ordered.Any(value => value.SeriesKey != ordered[0].SeriesKey))
            throw new ArgumentException("All cabin observations must belong to the same series.", nameof(observations));
        var seen = lastSeenAt ?? ordered[^1].ObservedAt;
        if (seen < ordered[^1].ObservedAt) throw new ArgumentException("Last seen cannot precede the latest observation.", nameof(lastSeenAt));
        IReadOnlyList<CruiseCabinAvailabilityChange> changes = ordered.Length == 1 ? [] :
            Compare(ordered[^2], ordered[^1]).Where(change => change.IsExplicitInventoryTransition).ToArray();
        return new(ordered[0].SeriesKey, ordered[0].ObservedAt, seen, ordered[^1], ordered.Length, changes);
    }
}
