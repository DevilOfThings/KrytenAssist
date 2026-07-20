using System.Globalization;

namespace KrytenAssist.Core.Cruises;

public abstract record CruiseAlertSubject;
public sealed record CruiseSailingAlertSubject : CruiseAlertSubject
{
    public CruiseSailingAlertSubject(CruiseSailingKey sailingKey) => SailingKey = sailingKey ?? throw new ArgumentNullException(nameof(sailingKey));
    public CruiseSailingKey SailingKey { get; }
}
public sealed record CruiseItineraryAlertSubject : CruiseAlertSubject
{
    public CruiseItineraryAlertSubject(CruiseItineraryCatalogueKey catalogueKey) => CatalogueKey = catalogueKey ?? throw new ArgumentNullException(nameof(catalogueKey));
    public CruiseItineraryCatalogueKey CatalogueKey { get; }
}

public sealed record CruiseAlertCandidate
{
    public CruiseAlertCandidate(CruiseAlertType type, CruiseSailingKey sailingKey, CruiseSource? source, CruiseAlertDetails details, DateTimeOffset eventTime, string triggeringEvidenceKey, string? criteriaFingerprint = null)
        : this(type, new CruiseSailingAlertSubject(sailingKey), source, details, eventTime, triggeringEvidenceKey, criteriaFingerprint) { }

    public CruiseAlertCandidate(CruiseAlertType type, CruiseAlertSubject subject, CruiseSource? source, CruiseAlertDetails details, DateTimeOffset eventTime, string triggeringEvidenceKey, string? criteriaFingerprint = null)
    {
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        ArgumentNullException.ThrowIfNull(subject); ArgumentNullException.ThrowIfNull(details);
        ArgumentException.ThrowIfNullOrWhiteSpace(triggeringEvidenceKey);
        var expectedType = details switch
        {
            CruisePriceDropAlertDetails => CruiseAlertType.PriceDrop,
            CruisePromotionAlertDetails => CruiseAlertType.Promotion,
            CruiseSavedCriteriaAlertDetails => CruiseAlertType.SavedCriteria,
            CruiseCabinAvailabilityAlertDetails => CruiseAlertType.CabinAvailability,
            CruiseNewItineraryAlertDetails => CruiseAlertType.NewItinerary,
            _ => throw new ArgumentException("Unsupported alert details.", nameof(details))
        };
        if (type != expectedType)
            throw new ArgumentException("Alert type does not match its details.", nameof(type));
        if (type == CruiseAlertType.NewItinerary)
        {
            if (subject is not CruiseItineraryAlertSubject itinerarySubject)
                throw new ArgumentException("New itinerary alerts require an itinerary subject.", nameof(subject));
            if (source is null || !string.Equals(source.Id, itinerarySubject.CatalogueKey.RetailSourceId, StringComparison.Ordinal))
                throw new ArgumentException("New itinerary alert source must match its catalogue subject.", nameof(source));
            if (((CruiseNewItineraryAlertDetails)details).ItineraryKey != itinerarySubject.CatalogueKey.ItineraryKey)
                throw new ArgumentException("New itinerary details must match the alert subject.", nameof(details));
            if (((CruiseNewItineraryAlertDetails)details).FirstObservedAt != eventTime)
                throw new ArgumentException("New itinerary evidence time must match the alert event time.", nameof(details));
        }
        else if (subject is not CruiseSailingAlertSubject)
            throw new ArgumentException("This alert type requires a sailing subject.", nameof(subject));
        if (type == CruiseAlertType.SavedCriteria && source is not null)
            throw new ArgumentException("Saved criteria alerts are not retail-source specific.", nameof(source));
        if (type != CruiseAlertType.SavedCriteria && source is null)
            throw new ArgumentException("Observation alerts require a retail source.", nameof(source));
        Type = type; Subject = subject; Source = source; Details = details; EventTime = eventTime;
        EventKey = subject switch
        {
            CruiseSailingAlertSubject sailing => CruiseAlertEventKey.Create(type, sailing.SailingKey, source, triggeringEvidenceKey, criteriaFingerprint),
            CruiseItineraryAlertSubject itinerary => CruiseAlertEventKey.CreateNewItinerary(itinerary.CatalogueKey, triggeringEvidenceKey),
            _ => throw new ArgumentException("Unsupported alert subject.", nameof(subject))
        };
    }
    public CruiseAlertType Type { get; }
    public CruiseAlertSubject Subject { get; }
    public CruiseSailingKey? SailingKey => (Subject as CruiseSailingAlertSubject)?.SailingKey;
    public CruiseItineraryCatalogueKey? ItineraryCatalogueKey => (Subject as CruiseItineraryAlertSubject)?.CatalogueKey;
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
        Id = id; Type = candidate.Type; Subject = candidate.Subject; Source = candidate.Source;
        Details = candidate.Details; EventTime = candidate.EventTime; EventKey = candidate.EventKey; CreatedAt = createdAt; Status = status;
    }
    public Guid Id { get; }
    public string EventKey { get; }
    public CruiseAlertType Type { get; }
    public CruiseAlertStatus Status { get; }
    public CruiseAlertSubject Subject { get; }
    public CruiseSailingKey? SailingKey => (Subject as CruiseSailingAlertSubject)?.SailingKey;
    public CruiseItineraryCatalogueKey? ItineraryCatalogueKey => (Subject as CruiseItineraryAlertSubject)?.CatalogueKey;
    public CruiseSource? Source { get; }
    public CruiseAlertDetails Details { get; }
    public DateTimeOffset EventTime { get; }
    public DateTimeOffset CreatedAt { get; }
    public CruiseAlert WithStatus(CruiseAlertStatus status) => new(Id, new CruiseAlertCandidate(Type, Subject, Source, Details, EventTime, EvidenceKey(), CriteriaFingerprint()), CreatedAt, status);
    private string EvidenceKey() => Details switch { CruisePriceDropAlertDetails x => x.EvidenceKey, CruisePromotionAlertDetails x => x.EvidenceKey, CruiseSavedCriteriaAlertDetails x => x.EvidenceKey, CruiseCabinAvailabilityAlertDetails x => x.EvidenceKey, CruiseNewItineraryAlertDetails x => x.FirstObservedEventKey, _ => throw new InvalidOperationException() };
    private string? CriteriaFingerprint() => (Details as CruiseSavedCriteriaAlertDetails)?.CriteriaFingerprint;
}

public static class CruiseAlertEventKey
{
    public static string Create(CruiseAlertType type, CruiseSailingKey key, CruiseSource? source, string evidenceKey, string? criteriaFingerprint = null)
    {
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        if (type == CruiseAlertType.NewItinerary) throw new ArgumentException("New itinerary alerts require route-based event identity.", nameof(type));
        ArgumentNullException.ThrowIfNull(key); ArgumentException.ThrowIfNullOrWhiteSpace(evidenceKey);
        var canonical = string.Join('|', "cruise-alert:v1", (int)type, key.OperatorId, key.ShipName,
            key.DepartureDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), key.DurationNights.ToString(CultureInfo.InvariantCulture),
            CruiseHistoryText.NormalizeOptional(source?.Id) ?? "-", evidenceKey.Trim(), criteriaFingerprint?.Trim() ?? "-");
        return CruiseAlertSettings.Hash(canonical);
    }

    public static string CreateNewItinerary(CruiseItineraryCatalogueKey key, string firstObservedEventKey)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstObservedEventKey);
        return CruiseAlertSettings.Hash(string.Join('|', "cruise-alert-itinerary:v1", (int)CruiseAlertType.NewItinerary,
            key.RetailSourceId, key.ItineraryKey.OperatorId, key.ItineraryKey.ProviderItineraryId, firstObservedEventKey.Trim()));
    }
}
