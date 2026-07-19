namespace KrytenAssist.Infrastructure.Persistence;

public sealed class CruiseDiscoveryScopeEntity
{
    public long Id { get; set; }
    public string ScopeFingerprint { get; set; } = string.Empty;
    public string RetailSourceId { get; set; } = string.Empty;
    public string RetailSourceName { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;
    public int Surface { get; set; }
    public int CaptureContractVersion { get; set; }
    public DateTimeOffset FirstCheckedAt { get; set; }
    public long FirstCheckedAtUtcTicks { get; set; }
    public DateTimeOffset LastCheckedAt { get; set; }
    public long LastCheckedAtUtcTicks { get; set; }
    public List<CruiseDiscoveryScopeCriterionEntity> Criteria { get; set; } = [];
    public List<CruiseDiscoveryCheckEntity> Checks { get; set; } = [];
}

public sealed class CruiseDiscoveryScopeCriterionEntity
{
    public long Id { get; set; }
    public long CruiseDiscoveryScopeId { get; set; }
    public CruiseDiscoveryScopeEntity Scope { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int State { get; set; }
    public List<CruiseDiscoveryScopeCriterionValueEntity> Values { get; set; } = [];
}

public sealed class CruiseDiscoveryScopeCriterionValueEntity
{
    public long Id { get; set; }
    public long CruiseDiscoveryScopeCriterionId { get; set; }
    public CruiseDiscoveryScopeCriterionEntity Criterion { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public string Value { get; set; } = string.Empty;
}

public sealed class CruiseDiscoveryCheckEntity
{
    public long Id { get; set; }
    public long CruiseDiscoveryScopeId { get; set; }
    public CruiseDiscoveryScopeEntity Scope { get; set; } = null!;
    public string EvidenceKey { get; set; } = string.Empty;
    public DateTimeOffset ObservedAt { get; set; }
    public long ObservedAtUtcTicks { get; set; }
    public bool WasTruncated { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
    public List<CruiseDiscoveryOccurrenceEntity> Occurrences { get; set; } = [];
    public List<CruiseDiscoveryRejectionEntity> Rejections { get; set; } = [];
}

public sealed class CruiseDiscoveryOccurrenceEntity
{
    public long Id { get; set; }
    public long CruiseDiscoveryCheckId { get; set; }
    public CruiseDiscoveryCheckEntity Check { get; set; } = null!;
    public string CatalogueKey { get; set; } = string.Empty;
    public string OccurrenceFingerprint { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;
    public string ProviderItineraryId { get; set; } = string.Empty;
    public string RetailSourceId { get; set; } = string.Empty;
    public string RetailSourceName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? ShipName { get; set; }
    public DateOnly? DepartureDate { get; set; }
    public int? DurationNights { get; set; }
    public string? DeparturePort { get; set; }
    public string? ItinerarySummary { get; set; }
    public string? ProviderOfferId { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public long ObservedAtUtcTicks { get; set; }
    public string EvidenceKey { get; set; } = string.Empty;
    public string? SourceReference { get; set; }
}

public sealed class CruiseDiscoveryRejectionEntity
{
    public long Id { get; set; }
    public long CruiseDiscoveryCheckId { get; set; }
    public CruiseDiscoveryCheckEntity Check { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public string CandidateKey { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class CruiseItineraryCatalogueEntity
{
    public long Id { get; set; }
    public string CatalogueKey { get; set; } = string.Empty;
    public string RetailSourceId { get; set; } = string.Empty;
    public string RetailSourceName { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;
    public string ProviderItineraryId { get; set; } = string.Empty;
    public long FirstOccurrenceId { get; set; }
    public CruiseDiscoveryOccurrenceEntity FirstOccurrence { get; set; } = null!;
    public long LatestOccurrenceId { get; set; }
    public CruiseDiscoveryOccurrenceEntity LatestOccurrence { get; set; } = null!;
    public DateTimeOffset FirstSeenAt { get; set; }
    public long FirstSeenAtUtcTicks { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public long LastSeenAtUtcTicks { get; set; }
    public string? FirstObservedEventKey { get; set; }
}
