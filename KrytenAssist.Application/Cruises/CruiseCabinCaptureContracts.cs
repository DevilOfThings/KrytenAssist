using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed record CruiseCabinCaptureRequest
{
    public CruiseCabinCaptureRequest(CruiseSailingKey sailingKey, CruiseSource source,
        CruiseCabinSearchContext searchContext, string sourceReference, DateTimeOffset observedAt)
    {
        SailingKey = sailingKey ?? throw new ArgumentNullException(nameof(sailingKey));
        Source = source ?? throw new ArgumentNullException(nameof(source));
        SearchContext = searchContext ?? throw new ArgumentNullException(nameof(searchContext));
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceReference);
        if (sourceReference.Length > CruiseCabinObservation.MaximumSourceReferenceLength)
            throw new ArgumentException("Source reference is too long.", nameof(sourceReference));
        SourceReference = sourceReference.Trim();
        ObservedAt = observedAt;
    }
    public CruiseSailingKey SailingKey { get; }
    public CruiseSource Source { get; }
    public CruiseCabinSearchContext SearchContext { get; }
    public string SourceReference { get; }
    public DateTimeOffset ObservedAt { get; }
}

public enum CruiseCabinCaptureStatus { Ready, Incomplete, Unsupported, Cancelled, Failed }

public sealed record CruiseCabinCaptureResult
{
    private CruiseCabinCaptureResult(CruiseCabinCaptureStatus status, CruiseCabinObservation? observation,
        IReadOnlyList<string> missingFields, string? message)
    { Status = status; Observation = observation; MissingFields = missingFields; Message = message; }
    public CruiseCabinCaptureStatus Status { get; }
    public CruiseCabinObservation? Observation { get; }
    public IReadOnlyList<string> MissingFields { get; }
    public string? Message { get; }
    public static CruiseCabinCaptureResult Ready(CruiseCabinObservation value) => new(CruiseCabinCaptureStatus.Ready, value ?? throw new ArgumentNullException(nameof(value)), [], null);
    public static CruiseCabinCaptureResult Incomplete(IEnumerable<string> missingFields, string message) =>
        Failure(CruiseCabinCaptureStatus.Incomplete, missingFields, message);
    public static CruiseCabinCaptureResult Unsupported(string message) => Failure(CruiseCabinCaptureStatus.Unsupported, [], message);
    public static CruiseCabinCaptureResult Cancelled(string message) => Failure(CruiseCabinCaptureStatus.Cancelled, [], message);
    public static CruiseCabinCaptureResult Failed(string message) => Failure(CruiseCabinCaptureStatus.Failed, [], message);
    private static CruiseCabinCaptureResult Failure(CruiseCabinCaptureStatus status, IEnumerable<string> missingFields, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message); ArgumentNullException.ThrowIfNull(missingFields);
        var suppliedFields = missingFields.ToArray();
        if (suppliedFields.Length > 16 || suppliedFields.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Missing fields must be bounded, distinct names.", nameof(missingFields));
        var fields = suppliedFields.Select(value => value.Trim()).ToArray();
        if (fields.Distinct(StringComparer.OrdinalIgnoreCase).Count() != fields.Length)
            throw new ArgumentException("Missing fields must be bounded, distinct names.", nameof(missingFields));
        if (status == CruiseCabinCaptureStatus.Incomplete && fields.Length == 0)
            throw new ArgumentException("Incomplete capture requires missing fields.", nameof(missingFields));
        return new(status, null, Array.AsReadOnly(fields), message.Trim());
    }
}

public interface ICruiseCabinCaptureService
{
    Task<CruiseCabinCaptureResult> CaptureAsync(CruiseCabinCaptureRequest request, CancellationToken cancellationToken = default);
}

public sealed record CruiseCabinPageCaptureRequest
{
    public const int MaximumPagePayloadLength = 65_536;

    public CruiseCabinPageCaptureRequest(string sourceIdentifier, CruiseSource source,
        string sourceReference, DateTimeOffset observedAt, string pagePayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceIdentifier);
        Source = source ?? throw new ArgumentNullException(nameof(source));
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(pagePayload);
        if (!Uri.TryCreate(sourceReference, UriKind.Absolute, out var address) ||
            address.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(address.Host))
            throw new ArgumentException("The source reference must be an absolute HTTPS address.", nameof(sourceReference));
        if (sourceReference.Length > CruiseCabinObservation.MaximumSourceReferenceLength)
            throw new ArgumentException("Source reference is too long.", nameof(sourceReference));
        if (pagePayload.Length > MaximumPagePayloadLength)
            throw new ArgumentException("Page payload is too long.", nameof(pagePayload));
        SourceIdentifier = sourceIdentifier.Trim();
        SourceReference = sourceReference;
        ObservedAt = observedAt;
        PagePayload = pagePayload;
    }

    public string SourceIdentifier { get; }
    public CruiseSource Source { get; }
    public string SourceReference { get; }
    public DateTimeOffset ObservedAt { get; }
    public string PagePayload { get; }
}

public sealed record CruiseCabinCaptureCandidateResult
{
    private CruiseCabinCaptureCandidateResult(CruiseCabinCaptureStatus status, string displayLabel,
        string sourceReference, CruiseCabinObservation? observation, IReadOnlyList<string> missingFields,
        string? message)
    { Status = status; DisplayLabel = displayLabel; SourceReference = sourceReference; Observation = observation; MissingFields = missingFields; Message = message; }

    public CruiseCabinCaptureStatus Status { get; }
    public string DisplayLabel { get; }
    public string SourceReference { get; }
    public CruiseCabinObservation? Observation { get; }
    public IReadOnlyList<string> MissingFields { get; }
    public string? Message { get; }

    public static CruiseCabinCaptureCandidateResult Ready(string label, string reference, CruiseCabinObservation observation) =>
        Create(CruiseCabinCaptureStatus.Ready, label, reference, observation, [], null);
    public static CruiseCabinCaptureCandidateResult Incomplete(string label, string reference, IEnumerable<string> fields, string message) =>
        Create(CruiseCabinCaptureStatus.Incomplete, label, reference, null, fields, message);
    public static CruiseCabinCaptureCandidateResult Unsupported(string label, string reference, string message) =>
        Create(CruiseCabinCaptureStatus.Unsupported, label, reference, null, [], message);
    public static CruiseCabinCaptureCandidateResult Failed(string label, string reference, string message) =>
        Create(CruiseCabinCaptureStatus.Failed, label, reference, null, [], message);

    private static CruiseCabinCaptureCandidateResult Create(CruiseCabinCaptureStatus status, string label,
        string reference, CruiseCabinObservation? observation, IEnumerable<string> fields, string? message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label); ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        if (label.Length > 512 || reference.Length > 4_000) throw new ArgumentException("Candidate value is too long.");
        if (!Uri.TryCreate(reference, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Candidate reference must be HTTPS.", nameof(reference));
        var missing = fields.ToArray();
        if (missing.Length > 16 || missing.Any(string.IsNullOrWhiteSpace) || missing.Distinct(StringComparer.OrdinalIgnoreCase).Count() != missing.Length)
            throw new ArgumentException("Missing fields must be bounded and distinct.", nameof(fields));
        if (status == CruiseCabinCaptureStatus.Incomplete && missing.Length == 0) throw new ArgumentException("Incomplete candidates require missing fields.");
        if (status == CruiseCabinCaptureStatus.Ready && (observation is null || observation.SourceReference != reference)) throw new ArgumentException("Ready candidate observation must match its reference.");
        if (status != CruiseCabinCaptureStatus.Ready && string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Non-ready candidates require a message.");
        return new(status, label, reference, observation, Array.AsReadOnly(missing), message);
    }
}

public sealed record CruiseCabinCaptureBatchResult
{
    public const int MaximumCandidateCount = 10;
    private CruiseCabinCaptureBatchResult(CruiseCaptureBatchStatus status, IReadOnlyList<CruiseCabinCaptureCandidateResult> candidates, bool wasTruncated, string? message)
    { Status = status; Candidates = candidates; WasTruncated = wasTruncated; Message = message; }
    public CruiseCaptureBatchStatus Status { get; }
    public IReadOnlyList<CruiseCabinCaptureCandidateResult> Candidates { get; }
    public bool WasTruncated { get; }
    public string? Message { get; }
    public int ReadyCount => Candidates.Count(x => x.Status == CruiseCabinCaptureStatus.Ready);
    public int IncompleteCount => Candidates.Count(x => x.Status == CruiseCabinCaptureStatus.Incomplete);
    public int UnsupportedCount => Candidates.Count(x => x.Status == CruiseCabinCaptureStatus.Unsupported);
    public int FailedCount => Candidates.Count(x => x.Status == CruiseCabinCaptureStatus.Failed);
    public static CruiseCabinCaptureBatchResult Completed(IEnumerable<CruiseCabinCaptureCandidateResult> candidates, bool truncated)
    { var values = candidates.ToArray(); if (values.Length is < 1 or > MaximumCandidateCount) throw new ArgumentException("Candidate count is invalid."); return new(CruiseCaptureBatchStatus.Completed, Array.AsReadOnly(values), truncated, null); }
    public static CruiseCabinCaptureBatchResult Incomplete(string message) => Failure(CruiseCaptureBatchStatus.Incomplete, message);
    public static CruiseCabinCaptureBatchResult Unsupported(string message) => Failure(CruiseCaptureBatchStatus.Unsupported, message);
    public static CruiseCabinCaptureBatchResult Failed(string message) => Failure(CruiseCaptureBatchStatus.Failed, message);
    public static CruiseCabinCaptureBatchResult Cancelled(string message) => Failure(CruiseCaptureBatchStatus.Cancelled, message);
    private static CruiseCabinCaptureBatchResult Failure(CruiseCaptureBatchStatus status, string message)
    { ArgumentException.ThrowIfNullOrWhiteSpace(message); return new(status, [], false, message); }
}

public interface ICruiseCabinPageCaptureService
{
    Task<CruiseCabinCaptureBatchResult> CaptureAsync(CruiseCabinPageCaptureRequest request, CancellationToken cancellationToken = default);
}
