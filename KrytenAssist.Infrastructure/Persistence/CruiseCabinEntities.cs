namespace KrytenAssist.Infrastructure.Persistence;

public sealed class CruiseCabinSeriesEntity
{
    public long Id { get; set; }
    public string SeriesKey { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;
    public string ShipName { get; set; } = string.Empty;
    public DateOnly DepartureDate { get; set; }
    public int DurationNights { get; set; }
    public string RetailSourceId { get; set; } = string.Empty;
    public string RetailSourceName { get; set; } = string.Empty;
    public string ContextFingerprint { get; set; } = string.Empty;
    public int? AdultCount { get; set; }
    public int? ChildCount { get; set; }
    public bool ChildAgesKnown { get; set; }
    public int PackageMode { get; set; }
    public string? DepartureAirportId { get; set; }
    public int? CabinQuantity { get; set; }
    public DateTimeOffset FirstObservedAt { get; set; }
    public long FirstObservedAtUtcTicks { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public long LastSeenAtUtcTicks { get; set; }
    public string LatestEvidenceKey { get; set; } = string.Empty;
    public string? LatestSourceReference { get; set; }
    public DateTimeOffset LatestEvidenceObservedAt { get; set; }
    public long LatestEvidenceObservedAtUtcTicks { get; set; }
    public List<CruiseCabinContextChildAgeEntity> ChildAges { get; set; } = [];
    public List<CruiseCabinObservationEntity> Observations { get; set; } = [];
}

public sealed class CruiseCabinContextChildAgeEntity
{
    public long Id { get; set; }
    public long CruiseCabinSeriesId { get; set; }
    public CruiseCabinSeriesEntity Series { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public int Age { get; set; }
}

public sealed class CruiseCabinObservationEntity
{
    public long Id { get; set; }
    public long CruiseCabinSeriesId { get; set; }
    public CruiseCabinSeriesEntity Series { get; set; } = null!;
    public int Sequence { get; set; }
    public string StateFingerprint { get; set; } = string.Empty;
    public int Coverage { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public long ObservedAtUtcTicks { get; set; }
    public string EvidenceKey { get; set; } = string.Empty;
    public string? SourceReference { get; set; }
    public List<CruiseCabinObservationStateEntity> States { get; set; } = [];
}

public sealed class CruiseCabinObservationStateEntity
{
    public long Id { get; set; }
    public long CruiseCabinObservationId { get; set; }
    public CruiseCabinObservationEntity Observation { get; set; } = null!;
    public int CabinType { get; set; }
    public int Availability { get; set; }
}
