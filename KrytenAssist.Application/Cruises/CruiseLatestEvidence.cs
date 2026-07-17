namespace KrytenAssist.Application.Cruises;

public sealed record CruiseLatestEvidence
{
    public CruiseLatestEvidence(
        string providerOfferId,
        string? sourceReference,
        DateTimeOffset observedAt)
    {
        ArgumentNullException.ThrowIfNull(providerOfferId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerOfferId);
        if (sourceReference is not null && string.IsNullOrWhiteSpace(sourceReference))
        {
            throw new ArgumentException(
                "Source reference cannot be empty or whitespace.",
                nameof(sourceReference));
        }

        ProviderOfferId = providerOfferId;
        SourceReference = sourceReference;
        ObservedAt = observedAt;
    }

    public string ProviderOfferId { get; }
    public string? SourceReference { get; }
    public DateTimeOffset ObservedAt { get; }
}
