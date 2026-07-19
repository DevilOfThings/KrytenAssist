using System.Globalization;

namespace KrytenAssist.Core.Cruises;

public enum CruiseCabinAvailabilityState { Unknown, Available, Unavailable }
public enum CruiseCabinEvidenceCoverage { Partial, Complete }

public sealed record CruiseCabinState
{
    public CruiseCabinState(CruiseCabinType cabinType, CruiseCabinAvailabilityState availability)
    {
        if (!Enum.IsDefined(cabinType)) throw new ArgumentOutOfRangeException(nameof(cabinType));
        if (!Enum.IsDefined(availability)) throw new ArgumentOutOfRangeException(nameof(availability));
        CabinType = cabinType;
        Availability = availability;
    }
    public CruiseCabinType CabinType { get; }
    public CruiseCabinAvailabilityState Availability { get; }
}

public sealed record CruiseCabinObservation
{
    public const int MaximumEvidenceKeyLength = 500;
    public const int MaximumSourceReferenceLength = 4000;

    public CruiseCabinObservation(
        CruiseSailingKey sailingKey,
        CruiseSource source,
        CruiseCabinSearchContext searchContext,
        CruiseCabinEvidenceCoverage coverage,
        IEnumerable<CruiseCabinState> states,
        DateTimeOffset observedAt,
        string evidenceKey,
        string? sourceReference = null)
    {
        ArgumentNullException.ThrowIfNull(sailingKey);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(searchContext);
        if (!Enum.IsDefined(coverage)) throw new ArgumentOutOfRangeException(nameof(coverage));
        ArgumentNullException.ThrowIfNull(states);

        var values = states.ToArray();
        if (values.Any(value => value is null)) throw new ArgumentException("Cabin states cannot contain null.", nameof(states));
        var cabinTypes = Enum.GetValues<CruiseCabinType>();
        if (values.Length != cabinTypes.Length || values.Select(value => value.CabinType).Distinct().Count() != cabinTypes.Length ||
            cabinTypes.Any(type => values.All(value => value.CabinType != type)))
            throw new ArgumentException("Exactly one state is required for every cabin type.", nameof(states));
        var ordered = values.OrderBy(value => value.CabinType).ToArray();
        if (ordered.All(value => value.Availability == CruiseCabinAvailabilityState.Unknown))
            throw new ArgumentException("At least one cabin state must be known.", nameof(states));
        var hasUnknown = ordered.Any(value => value.Availability == CruiseCabinAvailabilityState.Unknown);
        if (coverage == CruiseCabinEvidenceCoverage.Partial && !hasUnknown)
            throw new ArgumentException("Partial evidence must contain an unknown cabin state.", nameof(coverage));
        if (coverage == CruiseCabinEvidenceCoverage.Complete && hasUnknown)
            throw new ArgumentException("Complete evidence cannot contain an unknown cabin state.", nameof(coverage));

        EvidenceKey = BoundedRequired(evidenceKey, MaximumEvidenceKeyLength, nameof(evidenceKey));
        SourceReference = BoundedOptional(sourceReference, MaximumSourceReferenceLength, nameof(sourceReference));
        SailingKey = sailingKey;
        Source = source;
        SearchContext = searchContext;
        Coverage = coverage;
        States = Array.AsReadOnly(ordered);
        ObservedAt = observedAt;
        SeriesKey = CreateSeriesKey(sailingKey, source, searchContext);
        StateFingerprint = CreateStateFingerprint(SeriesKey, coverage, ordered);
    }

    public CruiseSailingKey SailingKey { get; }
    public CruiseSource Source { get; }
    public CruiseCabinSearchContext SearchContext { get; }
    public CruiseCabinEvidenceCoverage Coverage { get; }
    public IReadOnlyList<CruiseCabinState> States { get; }
    public DateTimeOffset ObservedAt { get; }
    public string EvidenceKey { get; }
    public string? SourceReference { get; }
    public string SeriesKey { get; }
    public string StateFingerprint { get; }

    public CruiseCabinAvailabilityState StateFor(CruiseCabinType cabinType)
    {
        if (!Enum.IsDefined(cabinType)) throw new ArgumentOutOfRangeException(nameof(cabinType));
        return States[(int)cabinType].Availability;
    }

    private static string CreateSeriesKey(CruiseSailingKey key, CruiseSource source, CruiseCabinSearchContext context) =>
        CruiseAlertSettings.Hash(string.Join('|', "cabin-series:v1", key.OperatorId, key.ShipName,
            key.DepartureDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            key.DurationNights.ToString(CultureInfo.InvariantCulture), CruiseHistoryText.Normalize(source.Id), context.Fingerprint));

    private static string CreateStateFingerprint(string seriesKey, CruiseCabinEvidenceCoverage coverage, IEnumerable<CruiseCabinState> states) =>
        CruiseAlertSettings.Hash($"cabin-state:v1|{seriesKey}|{(int)coverage}|{string.Join(',', states.Select(state => $"{(int)state.CabinType}:{(int)state.Availability}"))}");

    private static string BoundedRequired(string value, int maximum, string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter);
        var trimmed = value.Trim();
        if (trimmed.Length > maximum) throw new ArgumentException("Value is too long.", parameter);
        return trimmed;
    }

    private static string? BoundedOptional(string? value, int maximum, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length > maximum) throw new ArgumentException("Value is too long.", parameter);
        return trimmed;
    }
}
