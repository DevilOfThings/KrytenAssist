using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed record CruiseCaptureCandidateResult
{
    public const int MaximumDisplayLabelLength = 512;
    public const int MaximumSourceReferenceLength = 4_000;
    public const int MaximumMessageLength = 1_000;
    public const int MaximumMissingFieldCount = 16;
    public const int MaximumMissingFieldLength = 200;

    private CruiseCaptureCandidateResult(
        CruiseCaptureCandidateStatus status,
        string displayLabel,
        string sourceReference,
        CruiseObservation? observation,
        string? message,
        IReadOnlyList<string> missingFields)
    {
        Status = status;
        DisplayLabel = displayLabel;
        SourceReference = sourceReference;
        Observation = observation;
        Message = message;
        MissingFields = missingFields;
    }

    public CruiseCaptureCandidateStatus Status { get; }

    public string DisplayLabel { get; }

    public string SourceReference { get; }

    public CruiseObservation? Observation { get; }

    public string? Message { get; }

    public IReadOnlyList<string> MissingFields { get; }

    public bool IsReady => Status == CruiseCaptureCandidateStatus.Ready;

    public static CruiseCaptureCandidateResult Ready(
        string displayLabel,
        string sourceReference,
        CruiseObservation observation)
    {
        var validatedLabel = ValidateDisplayLabel(displayLabel);
        var validatedReference = ValidateSourceReference(sourceReference);
        ArgumentNullException.ThrowIfNull(observation);
        if (!string.Equals(
                observation.SourceReference,
                validatedReference,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The observation source reference must match the candidate source reference.",
                nameof(observation));
        }

        return new CruiseCaptureCandidateResult(
            CruiseCaptureCandidateStatus.Ready,
            validatedLabel,
            validatedReference,
            observation,
            null,
            Array.Empty<string>());
    }

    public static CruiseCaptureCandidateResult Incomplete(
        string displayLabel,
        string sourceReference,
        string message,
        IEnumerable<string> missingFields)
    {
        var validatedLabel = ValidateDisplayLabel(displayLabel);
        var validatedReference = ValidateSourceReference(sourceReference);
        var validatedMessage = ValidateMessage(message);
        var fields = ValidateMissingFields(missingFields);
        return new CruiseCaptureCandidateResult(
            CruiseCaptureCandidateStatus.Incomplete,
            validatedLabel,
            validatedReference,
            null,
            validatedMessage,
            fields);
    }

    public static CruiseCaptureCandidateResult Failed(
        string displayLabel,
        string sourceReference,
        string message) =>
        new(
            CruiseCaptureCandidateStatus.Failed,
            ValidateDisplayLabel(displayLabel),
            ValidateSourceReference(sourceReference),
            null,
            ValidateMessage(message),
            Array.Empty<string>());

    private static string ValidateDisplayLabel(string displayLabel)
    {
        ArgumentNullException.ThrowIfNull(displayLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayLabel);
        if (displayLabel.Length > MaximumDisplayLabelLength)
        {
            throw new ArgumentException(
                $"The display label cannot exceed {MaximumDisplayLabelLength} characters.",
                nameof(displayLabel));
        }

        return displayLabel;
    }

    private static string ValidateSourceReference(string sourceReference)
    {
        ArgumentNullException.ThrowIfNull(sourceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceReference);
        if (sourceReference.Length > MaximumSourceReferenceLength)
        {
            throw new ArgumentException(
                $"The source reference cannot exceed {MaximumSourceReferenceLength} characters.",
                nameof(sourceReference));
        }

        if (!Uri.TryCreate(sourceReference, UriKind.Absolute, out var address) ||
            address.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(address.Host))
        {
            throw new ArgumentException(
                "The source reference must be an absolute HTTPS address.",
                nameof(sourceReference));
        }

        return sourceReference;
    }

    private static string ValidateMessage(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (message.Length > MaximumMessageLength)
        {
            throw new ArgumentException(
                $"The message cannot exceed {MaximumMessageLength} characters.",
                nameof(message));
        }

        return message;
    }

    private static IReadOnlyList<string> ValidateMissingFields(
        IEnumerable<string> missingFields)
    {
        ArgumentNullException.ThrowIfNull(missingFields);
        var fields = missingFields.ToList();
        if (fields.Count == 0 || fields.Count > MaximumMissingFieldCount)
        {
            throw new ArgumentException(
                $"Missing fields must contain between 1 and {MaximumMissingFieldCount} values.",
                nameof(missingFields));
        }

        if (fields.Any(field =>
                string.IsNullOrWhiteSpace(field) ||
                field.Length > MaximumMissingFieldLength))
        {
            throw new ArgumentException(
                $"Missing field names must contain between 1 and {MaximumMissingFieldLength} non-whitespace characters.",
                nameof(missingFields));
        }

        if (fields.Distinct(StringComparer.OrdinalIgnoreCase).Count() != fields.Count)
        {
            throw new ArgumentException(
                "Missing field names must be distinct.",
                nameof(missingFields));
        }

        return fields.AsReadOnly();
    }
}
