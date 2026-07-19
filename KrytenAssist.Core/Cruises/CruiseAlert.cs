using System.Globalization;

namespace KrytenAssist.Core.Cruises;

public sealed record CruiseAlertCandidate
{
    public CruiseAlertCandidate(CruiseAlertType type, CruiseSailingKey sailingKey, CruiseSource? source, CruiseAlertDetails details, DateTimeOffset eventTime, string triggeringEvidenceKey, string? criteriaFingerprint = null)
    {
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        ArgumentNullException.ThrowIfNull(sailingKey); ArgumentNullException.ThrowIfNull(details);
        ArgumentException.ThrowIfNullOrWhiteSpace(triggeringEvidenceKey);
        var expectedType = details switch
        {
            CruisePriceDropAlertDetails => CruiseAlertType.PriceDrop,
            CruisePromotionAlertDetails => CruiseAlertType.Promotion,
            CruiseSavedCriteriaAlertDetails => CruiseAlertType.SavedCriteria,
            CruiseCabinAvailabilityAlertDetails => CruiseAlertType.CabinAvailability,
            _ => throw new ArgumentException("Unsupported alert details.", nameof(details))
        };
        if (type != expectedType)
            throw new ArgumentException("Alert type does not match its details.", nameof(type));
        if (type == CruiseAlertType.SavedCriteria && source is not null)
            throw new ArgumentException("Saved criteria alerts are not retail-source specific.", nameof(source));
        if (type != CruiseAlertType.SavedCriteria && source is null)
            throw new ArgumentException("Observation alerts require a retail source.", nameof(source));
        Type = type; SailingKey = sailingKey; Source = source; Details = details; EventTime = eventTime;
        EventKey = CruiseAlertEventKey.Create(type, sailingKey, source, triggeringEvidenceKey, criteriaFingerprint);
    }
    public CruiseAlertType Type { get; }
    public CruiseSailingKey SailingKey { get; }
    public CruiseSource? Source { get; }
    public CruiseAlertDetails Details { get; }
    public DateTimeOffset EventTime { get; }
    public string EventKey { get; }
}

public sealed record CruiseAlert
{
    public CruiseAlert(Guid id, CruiseAlertCandidate candidate, DateTimeOffset createdAt, CruiseAlertStatus status = CruiseAlertStatus.Unread)
    {
        if (id == Guid.Empty) throw new ArgumentException("Alert id is required.", nameof(id));
        ArgumentNullException.ThrowIfNull(candidate);
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        Id = id; Type = candidate.Type; SailingKey = candidate.SailingKey; Source = candidate.Source;
        Details = candidate.Details; EventTime = candidate.EventTime; EventKey = candidate.EventKey; CreatedAt = createdAt; Status = status;
    }
    public Guid Id { get; }
    public string EventKey { get; }
    public CruiseAlertType Type { get; }
    public CruiseAlertStatus Status { get; }
    public CruiseSailingKey SailingKey { get; }
    public CruiseSource? Source { get; }
    public CruiseAlertDetails Details { get; }
    public DateTimeOffset EventTime { get; }
    public DateTimeOffset CreatedAt { get; }
    public CruiseAlert WithStatus(CruiseAlertStatus status) => new(Id, new CruiseAlertCandidate(Type, SailingKey, Source, Details, EventTime, EvidenceKey(), CriteriaFingerprint()), CreatedAt, status);
    private string EvidenceKey() => Details switch { CruisePriceDropAlertDetails x => x.EvidenceKey, CruisePromotionAlertDetails x => x.EvidenceKey, CruiseSavedCriteriaAlertDetails x => x.EvidenceKey, CruiseCabinAvailabilityAlertDetails x => x.EvidenceKey, _ => throw new InvalidOperationException() };
    private string? CriteriaFingerprint() => (Details as CruiseSavedCriteriaAlertDetails)?.CriteriaFingerprint;
}

public static class CruiseAlertEventKey
{
    public static string Create(CruiseAlertType type, CruiseSailingKey key, CruiseSource? source, string evidenceKey, string? criteriaFingerprint = null)
    {
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        ArgumentNullException.ThrowIfNull(key); ArgumentException.ThrowIfNullOrWhiteSpace(evidenceKey);
        var canonical = string.Join('|', "cruise-alert:v1", (int)type, key.OperatorId, key.ShipName,
            key.DepartureDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), key.DurationNights.ToString(CultureInfo.InvariantCulture),
            CruiseHistoryText.NormalizeOptional(source?.Id) ?? "-", evidenceKey.Trim(), criteriaFingerprint?.Trim() ?? "-");
        return CruiseAlertSettings.Hash(canonical);
    }
}
