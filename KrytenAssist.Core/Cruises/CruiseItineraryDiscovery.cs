using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace KrytenAssist.Core.Cruises;

public sealed record CruiseItineraryKey
{
    public const int MaximumProviderItineraryIdLength = 1000;

    public CruiseItineraryKey(string operatorId, string providerItineraryId)
    {
        OperatorId = Required(operatorId, 200, nameof(operatorId));
        ProviderItineraryId = Required(providerItineraryId, MaximumProviderItineraryIdLength, nameof(providerItineraryId));
    }

    public string OperatorId { get; }
    public string ProviderItineraryId { get; }
    public string PersistenceKey => CruiseDiscoveryIdentity.Hash($"itinerary:v1|{CruiseHistoryText.Component(OperatorId)}|{CruiseHistoryText.Component(ProviderItineraryId)}");

    private static string Required(string value, int maximum, string name)
    {
        var normalized = CruiseHistoryText.NormalizeRequired(value, name);
        if (normalized.Length > maximum) throw new ArgumentException($"{name} is too long.", name);
        return normalized;
    }
}

public sealed record CruiseItineraryCatalogueKey
{
    public CruiseItineraryCatalogueKey(CruiseSource source, CruiseItineraryKey itineraryKey)
    {
        ArgumentNullException.ThrowIfNull(source);
        ItineraryKey = itineraryKey ?? throw new ArgumentNullException(nameof(itineraryKey));
        RetailSourceId = CruiseHistoryText.NormalizeRequired(source.Id, nameof(source));
        if (RetailSourceId.Length > 200) throw new ArgumentException("Retail source id is too long.", nameof(source));
    }

    public string RetailSourceId { get; }
    public CruiseItineraryKey ItineraryKey { get; }
    public string PersistenceKey => CruiseDiscoveryIdentity.Hash($"catalogue:v1|{CruiseHistoryText.Component(RetailSourceId)}|{ItineraryKey.PersistenceKey}");
}

public enum CruiseDiscoverySurface { CruisePackages }
public enum CruiseDiscoveryCriterionState { Unknown, Known }

public sealed record CruiseDiscoveryCriterion
{
    public const int MaximumNameLength = 100;
    public const int MaximumValueLength = 500;
    public const int MaximumValueCount = 32;

    public CruiseDiscoveryCriterion(string name, CruiseDiscoveryCriterionState state, IEnumerable<string>? values = null)
    {
        if (!Enum.IsDefined(state)) throw new ArgumentOutOfRangeException(nameof(state));
        Name = CruiseHistoryText.NormalizeRequired(name, nameof(name));
        if (Name.Length > MaximumNameLength) throw new ArgumentException("Criterion name is too long.", nameof(name));
        var normalized = (values ?? []).Select((value, index) =>
        {
            var item = CruiseHistoryText.NormalizeRequired(value, $"{nameof(values)}[{index}]");
            if (item.Length > MaximumValueLength) throw new ArgumentException("Criterion value is too long.", nameof(values));
            return item;
        }).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (normalized.Length > MaximumValueCount) throw new ArgumentException("Too many criterion values.", nameof(values));
        if (state == CruiseDiscoveryCriterionState.Known && normalized.Length == 0)
            throw new ArgumentException("Known criteria require values.", nameof(values));
        if (state == CruiseDiscoveryCriterionState.Unknown && normalized.Length != 0)
            throw new ArgumentException("Unknown criteria cannot contain values.", nameof(values));
        State = state;
        Values = Array.AsReadOnly(normalized);
    }

    public string Name { get; }
    public CruiseDiscoveryCriterionState State { get; }
    public IReadOnlyList<string> Values { get; }
    internal string Canonical => $"{CruiseHistoryText.Component(Name)}:{(int)State}:{string.Join(',', Values.Select(CruiseHistoryText.Component))}";
}

public sealed record CruiseDiscoveryScope
{
    public const int MaximumCaptureContractVersion = 1000;

    public CruiseDiscoveryScope(CruiseSource source, string operatorId, CruiseDiscoverySurface surface,
        int captureContractVersion, IEnumerable<CruiseDiscoveryCriterion>? criteria = null)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        OperatorId = CruiseHistoryText.NormalizeRequired(operatorId, nameof(operatorId));
        if (OperatorId.Length > 200) throw new ArgumentException("Operator id is too long.", nameof(operatorId));
        if (!Enum.IsDefined(surface)) throw new ArgumentOutOfRangeException(nameof(surface));
        if (captureContractVersion is < 1 or > MaximumCaptureContractVersion) throw new ArgumentOutOfRangeException(nameof(captureContractVersion));
        var ordered = (criteria ?? []).OrderBy(value => value.Name, StringComparer.Ordinal).ToArray();
        if (ordered.Any(value => value is null)) throw new ArgumentException("Criteria cannot contain null.", nameof(criteria));
        if (ordered.Select(value => value.Name).Distinct(StringComparer.Ordinal).Count() != ordered.Length)
            throw new ArgumentException("Criterion names must be unique.", nameof(criteria));
        Surface = surface;
        CaptureContractVersion = captureContractVersion;
        Criteria = Array.AsReadOnly(ordered);
        var canonical = string.Join('|', "discovery-scope:v1", CruiseHistoryText.Component(CruiseHistoryText.NormalizeRequired(source.Id, nameof(source))),
            CruiseHistoryText.Component(OperatorId), (int)surface, captureContractVersion.ToString(CultureInfo.InvariantCulture),
            string.Join(';', ordered.Select(value => value.Canonical)));
        Fingerprint = CruiseDiscoveryIdentity.Hash(canonical);
    }

    public CruiseSource Source { get; }
    public string OperatorId { get; }
    public CruiseDiscoverySurface Surface { get; }
    public int CaptureContractVersion { get; }
    public IReadOnlyList<CruiseDiscoveryCriterion> Criteria { get; }
    public string Fingerprint { get; }
}

public sealed record CruiseItineraryOccurrence
{
    public const int MaximumDisplayLength = 1000;
    public const int MaximumSummaryLength = 4000;
    public const int MaximumSourceReferenceLength = 4000;
    public const int MaximumDurationNights = 365;

    public CruiseItineraryOccurrence(CruiseItineraryKey itineraryKey, CruiseSource source, DateTimeOffset observedAt,
        string evidenceKey, string? title = null, string? shipName = null, DateOnly? departureDate = null,
        int? durationNights = null, string? departurePort = null, string? itinerarySummary = null,
        string? providerOfferId = null, string? sourceReference = null)
    {
        ItineraryKey = itineraryKey ?? throw new ArgumentNullException(nameof(itineraryKey));
        Source = source ?? throw new ArgumentNullException(nameof(source));
        EvidenceKey = Required(evidenceKey, MaximumSummaryLength, nameof(evidenceKey), normalize: false);
        Title = Optional(title, MaximumDisplayLength, nameof(title));
        ShipName = Optional(shipName, MaximumDisplayLength, nameof(shipName));
        if (durationNights is < 1 or > MaximumDurationNights) throw new ArgumentOutOfRangeException(nameof(durationNights));
        DepartureDate = departureDate;
        DurationNights = durationNights;
        DeparturePort = Optional(departurePort, MaximumDisplayLength, nameof(departurePort));
        ItinerarySummary = Optional(itinerarySummary, MaximumSummaryLength, nameof(itinerarySummary));
        ProviderOfferId = Optional(providerOfferId, MaximumDisplayLength, nameof(providerOfferId));
        SourceReference = Optional(sourceReference, MaximumSourceReferenceLength, nameof(sourceReference), normalize: false);
        ObservedAt = observedAt;
        CatalogueKey = new(source, itineraryKey);
        Fingerprint = CruiseDiscoveryIdentity.Hash(string.Join('|', "occurrence:v1", CatalogueKey.PersistenceKey,
            CruiseHistoryText.Component(CruiseHistoryText.NormalizeOptional(Title)), CruiseHistoryText.Component(CruiseHistoryText.NormalizeOptional(ShipName)),
            DepartureDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-", DurationNights?.ToString(CultureInfo.InvariantCulture) ?? "-",
            CruiseHistoryText.Component(CruiseHistoryText.NormalizeOptional(DeparturePort)), CruiseHistoryText.Component(CruiseHistoryText.NormalizeOptional(ItinerarySummary)),
            CruiseHistoryText.Component(CruiseHistoryText.NormalizeOptional(ProviderOfferId)), CruiseHistoryText.Component(EvidenceKey.Trim()),
            CruiseHistoryText.Component(SourceReference?.Trim())));
    }

    public CruiseItineraryKey ItineraryKey { get; }
    public CruiseItineraryCatalogueKey CatalogueKey { get; }
    public CruiseSource Source { get; }
    public string? Title { get; }
    public string? ShipName { get; }
    public DateOnly? DepartureDate { get; }
    public int? DurationNights { get; }
    public string? DeparturePort { get; }
    public string? ItinerarySummary { get; }
    public string? ProviderOfferId { get; }
    public DateTimeOffset ObservedAt { get; }
    public string EvidenceKey { get; }
    public string? SourceReference { get; }
    public string Fingerprint { get; }

    private static string Required(string value, int maximum, string name, bool normalize = true) =>
        Optional(value, maximum, name, normalize) ?? throw new ArgumentException($"{name} is required.", name);
    private static string? Optional(string? value, int maximum, string name, bool normalize = true)
    {
        if (value is null) return null;
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{name} cannot be empty.", name);
        var result = normalize ? string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)) : value.Trim();
        if (result.Length > maximum) throw new ArgumentException($"{name} is too long.", name);
        return result;
    }
}

public sealed record CruiseDiscoveryRejection
{
    public CruiseDiscoveryRejection(string candidateKey, string reason)
    {
        CandidateKey = Required(candidateKey, nameof(candidateKey));
        Reason = Required(reason, nameof(reason));
    }
    public string CandidateKey { get; }
    public string Reason { get; }
    private static string Required(string value, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var result = value.Trim();
        if (result.Length > 1000) throw new ArgumentException($"{name} is too long.", name);
        return result;
    }
}

public sealed record CruiseDiscoveryCheck
{
    public const int MaximumOccurrenceCount = 10;
    public const int MaximumRejectionCount = 10;

    public CruiseDiscoveryCheck(CruiseDiscoveryScope scope, DateTimeOffset observedAt,
        IEnumerable<CruiseItineraryOccurrence> occurrences, IEnumerable<CruiseDiscoveryRejection>? rejections = null,
        bool wasTruncated = false)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        var accepted = occurrences?.ToArray() ?? throw new ArgumentNullException(nameof(occurrences));
        if (accepted.Length is < 1 or > MaximumOccurrenceCount) throw new ArgumentException("Accepted occurrence count is invalid.", nameof(occurrences));
        if (accepted.Any(value => value is null || value.ObservedAt != observedAt ||
            !string.Equals(value.ItineraryKey.OperatorId, scope.OperatorId, StringComparison.Ordinal) ||
            !string.Equals(value.CatalogueKey.RetailSourceId, CruiseHistoryText.NormalizeRequired(scope.Source.Id, nameof(scope)), StringComparison.Ordinal)))
            throw new ArgumentException("Occurrences must match the check scope and time.", nameof(occurrences));
        var ordered = accepted.OrderBy(value => value.CatalogueKey.PersistenceKey, StringComparer.Ordinal).ThenBy(value => value.Fingerprint, StringComparer.Ordinal).ToArray();
        if (ordered.Select(value => value.CatalogueKey.PersistenceKey).Distinct(StringComparer.Ordinal).Count() != ordered.Length)
            throw new ArgumentException("A check cannot contain duplicate itinerary identities.", nameof(occurrences));
        var rejected = (rejections ?? []).OrderBy(value => value.CandidateKey, StringComparer.Ordinal).ThenBy(value => value.Reason, StringComparer.Ordinal).ToArray();
        if (rejected.Length > MaximumRejectionCount) throw new ArgumentException("Rejected candidate count is invalid.", nameof(rejections));
        ObservedAt = observedAt;
        Occurrences = Array.AsReadOnly(ordered);
        Rejections = Array.AsReadOnly(rejected);
        WasTruncated = wasTruncated;
        EvidenceKey = CruiseDiscoveryIdentity.Hash(string.Join('|', "discovery-check:v1", scope.Fingerprint,
            observedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture), wasTruncated,
            string.Join(';', ordered.Select(value => value.Fingerprint)),
            string.Join(';', rejected.Select(value => $"{CruiseHistoryText.Component(value.CandidateKey)}:{CruiseHistoryText.Component(value.Reason)}"))));
    }

    public CruiseDiscoveryScope Scope { get; }
    public DateTimeOffset ObservedAt { get; }
    public IReadOnlyList<CruiseItineraryOccurrence> Occurrences { get; }
    public IReadOnlyList<CruiseDiscoveryRejection> Rejections { get; }
    public bool WasTruncated { get; }
    public string EvidenceKey { get; }
}

public enum CruiseItineraryDetectionStatus { BaselineSeeded, NoNewItineraries, FirstObserved }

public sealed record CruiseItineraryFirstObservedEvent
{
    public CruiseItineraryFirstObservedEvent(CruiseItineraryOccurrence occurrence, string scopeFingerprint, string checkEvidenceKey)
    {
        Occurrence = occurrence ?? throw new ArgumentNullException(nameof(occurrence));
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(checkEvidenceKey);
        ScopeFingerprint = scopeFingerprint.Trim();
        CheckEvidenceKey = checkEvidenceKey.Trim();
        EventKey = CruiseDiscoveryIdentity.Hash($"itinerary-first-observed:v1|{occurrence.CatalogueKey.PersistenceKey}|{ScopeFingerprint}|{CheckEvidenceKey}");
    }
    public CruiseItineraryOccurrence Occurrence { get; }
    public string ScopeFingerprint { get; }
    public string CheckEvidenceKey { get; }
    public DateTimeOffset ObservedAt => Occurrence.ObservedAt;
    public string EventKey { get; }
}

public sealed record CruiseItineraryDetectionResult(CruiseItineraryDetectionStatus Status, IReadOnlyList<CruiseItineraryFirstObservedEvent> Events);

public sealed class CruiseNewItineraryDetector
{
    public CruiseItineraryDetectionResult Detect(bool hasScopeBaseline,
        IEnumerable<CruiseItineraryCatalogueKey> knownCatalogue, CruiseDiscoveryCheck check)
    {
        ArgumentNullException.ThrowIfNull(knownCatalogue);
        ArgumentNullException.ThrowIfNull(check);
        if (!hasScopeBaseline) return new(CruiseItineraryDetectionStatus.BaselineSeeded, []);
        var sourceId = CruiseHistoryText.NormalizeRequired(check.Scope.Source.Id, nameof(check));
        var known = knownCatalogue.ToArray();
        if (known.Any(value => !string.Equals(value.RetailSourceId, sourceId, StringComparison.Ordinal)))
            throw new ArgumentException("Known catalogue entries must match the check source.", nameof(knownCatalogue));
        var keys = known.Select(value => value.PersistenceKey).ToHashSet(StringComparer.Ordinal);
        var events = check.Occurrences.Where(value => !keys.Contains(value.CatalogueKey.PersistenceKey))
            .OrderBy(value => value.CatalogueKey.PersistenceKey, StringComparer.Ordinal)
            .Select(value => new CruiseItineraryFirstObservedEvent(value, check.Scope.Fingerprint, check.EvidenceKey)).ToArray();
        return new(events.Length == 0 ? CruiseItineraryDetectionStatus.NoNewItineraries : CruiseItineraryDetectionStatus.FirstObserved,
            Array.AsReadOnly(events));
    }
}

internal static class CruiseDiscoveryIdentity
{
    internal static string Hash(string canonical) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
}
