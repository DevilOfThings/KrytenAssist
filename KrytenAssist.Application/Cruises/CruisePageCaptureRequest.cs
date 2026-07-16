using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed record CruisePageCaptureRequest
{
    public const int MaximumPagePayloadLength = 65_536;

    public CruisePageCaptureRequest(
        string sourceIdentifier,
        CruiseSource source,
        string sourceReference,
        DateTimeOffset observedAt,
        string pagePayload)
    {
        ArgumentNullException.ThrowIfNull(sourceIdentifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceIdentifier);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sourceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceReference);
        ArgumentNullException.ThrowIfNull(pagePayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(pagePayload);

        if (!Uri.TryCreate(sourceReference, UriKind.Absolute, out var address) ||
            address.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(address.Host))
        {
            throw new ArgumentException(
                "The source reference must be an absolute HTTPS address.",
                nameof(sourceReference));
        }

        if (pagePayload.Length > MaximumPagePayloadLength)
        {
            throw new ArgumentException(
                $"The page payload cannot exceed {MaximumPagePayloadLength} characters.",
                nameof(pagePayload));
        }

        SourceIdentifier = sourceIdentifier;
        Source = source;
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
