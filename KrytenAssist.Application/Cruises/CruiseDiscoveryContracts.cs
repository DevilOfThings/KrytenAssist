using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public enum CruiseDiscoveryRecordState { BaselineSeeded, RecordedNoNewItineraries, RecordedWithFirstObserved, AlreadyRecorded }
public enum CruiseDiscoveryOperationStatus { Success, Found, NotFound, BaselineSeeded, RecordedNoNewItineraries, RecordedWithFirstObserved, AlreadyRecorded, Cancelled, Failed }

public sealed record CruiseDiscoveryRepositoryRecordResult(
    CruiseDiscoveryRecordState State,
    CruiseDiscoveryCheck Check,
    IReadOnlyList<CruiseItineraryFirstObservedEvent> FirstObservedEvents);

public sealed record CruiseItineraryCatalogueEntry(
    CruiseItineraryCatalogueKey CatalogueKey,
    CruiseItineraryOccurrence FirstOccurrence,
    CruiseItineraryOccurrence LatestOccurrence,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    string? FirstObservedEventKey);

public sealed record CruiseDiscoveryRecordResult(
    CruiseDiscoveryOperationStatus Status,
    CruiseDiscoveryCheck? Check,
    IReadOnlyList<CruiseItineraryFirstObservedEvent> FirstObservedEvents,
    string? Message)
{
    public static CruiseDiscoveryRecordResult Cancelled() => new(CruiseDiscoveryOperationStatus.Cancelled, null, [], "Discovery recording was cancelled.");
    public static CruiseDiscoveryRecordResult Failed() => new(CruiseDiscoveryOperationStatus.Failed, null, [], "Discovery evidence could not be recorded locally.");
}

public enum CruiseDiscoveryAlertEvaluationStatus { NotRequired, Disabled, Success, Cancelled, Failed }
public sealed record CruiseDiscoveryRecordingAndAlertResult(
    CruiseDiscoveryRecordResult Recording,
    CruiseDiscoveryAlertEvaluationStatus AlertEvaluation,
    CruiseAlertEvaluationResult? Alerts);

public sealed record CruiseItineraryListResult(CruiseDiscoveryOperationStatus Status, IReadOnlyList<CruiseItineraryCatalogueEntry> Entries, string? Message);
public sealed record CruiseItineraryQueryResult(CruiseDiscoveryOperationStatus Status, CruiseItineraryCatalogueEntry? Entry, string? Message);
public sealed record CruiseDiscoveryCheckListResult(CruiseDiscoveryOperationStatus Status, IReadOnlyList<CruiseDiscoveryCheck> Checks, string? Message);

public sealed record CruiseItineraryPageCaptureRequest
{
    public const int MaximumPagePayloadLength = 65_536;
    public CruiseItineraryPageCaptureRequest(string sourceIdentifier, CruiseSource source, string sourceReference,
        DateTimeOffset observedAt, string pagePayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceIdentifier);
        Source = source ?? throw new ArgumentNullException(nameof(source));
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(pagePayload);
        if (!Uri.TryCreate(sourceReference, UriKind.Absolute, out var address) || address.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("The source reference must be an absolute HTTPS address.", nameof(sourceReference));
        if (sourceReference.Length > CruiseItineraryOccurrence.MaximumSourceReferenceLength) throw new ArgumentException("Source reference is too long.", nameof(sourceReference));
        if (pagePayload.Length > MaximumPagePayloadLength) throw new ArgumentException("Page payload is too long.", nameof(pagePayload));
        SourceIdentifier = sourceIdentifier.Trim(); SourceReference = sourceReference; ObservedAt = observedAt; PagePayload = pagePayload;
    }
    public string SourceIdentifier { get; }
    public CruiseSource Source { get; }
    public string SourceReference { get; }
    public DateTimeOffset ObservedAt { get; }
    public string PagePayload { get; }
}

public enum CruiseItineraryCaptureCandidateStatus { Ready, Ineligible, Incomplete, Unsupported, Failed }
public sealed record CruiseItineraryCaptureCandidateResult
{
    private CruiseItineraryCaptureCandidateResult(CruiseItineraryCaptureCandidateStatus status, string displayLabel,
        CruiseItineraryOccurrence? occurrence, IReadOnlyList<string> missingFields, string? message)
    { Status = status; DisplayLabel = displayLabel; Occurrence = occurrence; MissingFields = missingFields; Message = message; }
    public CruiseItineraryCaptureCandidateStatus Status { get; }
    public string DisplayLabel { get; }
    public CruiseItineraryOccurrence? Occurrence { get; }
    public IReadOnlyList<string> MissingFields { get; }
    public string? Message { get; }
    public static CruiseItineraryCaptureCandidateResult Ready(string label, CruiseItineraryOccurrence occurrence) =>
        Create(CruiseItineraryCaptureCandidateStatus.Ready, label, occurrence, [], null);
    public static CruiseItineraryCaptureCandidateResult Ineligible(string label, IEnumerable<string> fields, string message) =>
        Create(CruiseItineraryCaptureCandidateStatus.Ineligible, label, null, fields, message);
    public static CruiseItineraryCaptureCandidateResult Incomplete(string label, IEnumerable<string> fields, string message) =>
        Create(CruiseItineraryCaptureCandidateStatus.Incomplete, label, null, fields, message);
    public static CruiseItineraryCaptureCandidateResult Unsupported(string label, string message) =>
        Create(CruiseItineraryCaptureCandidateStatus.Unsupported, label, null, [], message);
    public static CruiseItineraryCaptureCandidateResult Failed(string label, string message) =>
        Create(CruiseItineraryCaptureCandidateStatus.Failed, label, null, [], message);
    private static CruiseItineraryCaptureCandidateResult Create(CruiseItineraryCaptureCandidateStatus status, string label,
        CruiseItineraryOccurrence? occurrence, IEnumerable<string> fields, string? message)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        if (label.Trim().Length > CruiseItineraryOccurrence.MaximumDisplayLength) throw new ArgumentException("Display label is too long.", nameof(label));
        var missing = fields.Select(value =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            var field = value.Trim();
            if (field.Length > 100) throw new ArgumentException("Missing field name is too long.", nameof(fields));
            return field;
        }).ToArray();
        if (missing.Length > 16 || missing.Distinct(StringComparer.OrdinalIgnoreCase).Count() != missing.Length)
            throw new ArgumentException("Missing fields must be bounded and distinct.", nameof(fields));
        if (status == CruiseItineraryCaptureCandidateStatus.Ready && occurrence is null)
            throw new ArgumentException("Ready candidates require occurrence evidence.", nameof(occurrence));
        if (status != CruiseItineraryCaptureCandidateStatus.Ready && occurrence is not null)
            throw new ArgumentException("Non-ready candidates cannot contain occurrence evidence.", nameof(occurrence));
        if (status is CruiseItineraryCaptureCandidateStatus.Ineligible or CruiseItineraryCaptureCandidateStatus.Incomplete && missing.Length == 0)
            throw new ArgumentException("Ineligible and incomplete candidates require missing fields.", nameof(fields));
        if (status != CruiseItineraryCaptureCandidateStatus.Ready && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Non-ready candidates require a message.", nameof(message));
        return new(status, label.Trim(), occurrence, Array.AsReadOnly(missing), message?.Trim());
    }
}

public sealed record CruiseItineraryCaptureBatchResult
{
    private CruiseItineraryCaptureBatchResult(CruiseCaptureBatchStatus status, CruiseDiscoveryScope? scope,
        IReadOnlyList<CruiseItineraryCaptureCandidateResult> candidates, bool truncated, string? message)
    { Status = status; Scope = scope; Candidates = candidates; WasTruncated = truncated; Message = message; }
    public CruiseCaptureBatchStatus Status { get; }
    public CruiseDiscoveryScope? Scope { get; }
    public IReadOnlyList<CruiseItineraryCaptureCandidateResult> Candidates { get; }
    public bool WasTruncated { get; }
    public string? Message { get; }
    public static CruiseItineraryCaptureBatchResult Completed(CruiseDiscoveryScope scope, IEnumerable<CruiseItineraryCaptureCandidateResult> candidates, bool truncated)
    { var values = candidates.ToArray(); if (values.Length is < 1 or > CruiseDiscoveryCheck.MaximumOccurrenceCount) throw new ArgumentException("Candidate count is invalid."); return new(CruiseCaptureBatchStatus.Completed, scope, Array.AsReadOnly(values), truncated, null); }
    public static CruiseItineraryCaptureBatchResult Incomplete(string message) => Failure(CruiseCaptureBatchStatus.Incomplete, message);
    public static CruiseItineraryCaptureBatchResult Unsupported(string message) => Failure(CruiseCaptureBatchStatus.Unsupported, message);
    public static CruiseItineraryCaptureBatchResult Cancelled(string message) => Failure(CruiseCaptureBatchStatus.Cancelled, message);
    public static CruiseItineraryCaptureBatchResult Failed(string message) => Failure(CruiseCaptureBatchStatus.Failed, message);
    private static CruiseItineraryCaptureBatchResult Failure(CruiseCaptureBatchStatus status, string message)
    { ArgumentException.ThrowIfNullOrWhiteSpace(message); return new(status, null, [], false, message.Trim()); }
}

public interface ICruiseItineraryPageCaptureService
{
    Task<CruiseItineraryCaptureBatchResult> CaptureAsync(CruiseItineraryPageCaptureRequest request, CancellationToken cancellationToken = default);
}
