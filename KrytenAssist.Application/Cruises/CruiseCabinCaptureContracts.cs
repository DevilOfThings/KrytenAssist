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
