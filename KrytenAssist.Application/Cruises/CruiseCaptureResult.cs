using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed record CruiseCaptureResult
{
    public const int MaximumMissingFieldCount = 16;

    private CruiseCaptureResult(
        CruiseCaptureStatus status,
        CruiseObservation? observation,
        string? message,
        IReadOnlyList<string> missingFields)
    {
        Status = status;
        Observation = observation;
        Message = message;
        MissingFields = missingFields;
    }

    public CruiseCaptureStatus Status { get; }

    public CruiseObservation? Observation { get; }

    public string? Message { get; }

    public IReadOnlyList<string> MissingFields { get; }

    public bool IsSuccess => Status == CruiseCaptureStatus.Success;

    public static CruiseCaptureResult Succeeded(CruiseObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        return new CruiseCaptureResult(
            CruiseCaptureStatus.Success,
            observation,
            null,
            Array.Empty<string>());
    }

    public static CruiseCaptureResult Incomplete(
        string message,
        IEnumerable<string> missingFields)
    {
        var validatedMessage = ValidateMessage(message);
        ArgumentNullException.ThrowIfNull(missingFields);

        var fields = missingFields.ToList();
        if (fields.Count == 0 || fields.Count > MaximumMissingFieldCount)
        {
            throw new ArgumentException(
                $"Missing fields must contain between 1 and {MaximumMissingFieldCount} values.",
                nameof(missingFields));
        }

        if (fields.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Missing field names cannot be empty or whitespace.",
                nameof(missingFields));
        }

        if (fields.Distinct(StringComparer.OrdinalIgnoreCase).Count() != fields.Count)
        {
            throw new ArgumentException(
                "Missing field names must be distinct.",
                nameof(missingFields));
        }

        return new CruiseCaptureResult(
            CruiseCaptureStatus.Incomplete,
            null,
            validatedMessage,
            fields.AsReadOnly());
    }

    public static CruiseCaptureResult Ambiguous(string message) =>
        Failure(CruiseCaptureStatus.Ambiguous, message);

    public static CruiseCaptureResult Unsupported(string message) =>
        Failure(CruiseCaptureStatus.Unsupported, message);

    public static CruiseCaptureResult Failed(string message) =>
        Failure(CruiseCaptureStatus.Failed, message);

    public static CruiseCaptureResult Cancelled(string message) =>
        Failure(CruiseCaptureStatus.Cancelled, message);

    private static CruiseCaptureResult Failure(CruiseCaptureStatus status, string message) =>
        new(status, null, ValidateMessage(message), Array.Empty<string>());

    private static string ValidateMessage(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return message;
    }
}
