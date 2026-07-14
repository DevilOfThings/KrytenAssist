namespace KrytenAssist.Core.Cruises;

public sealed record CruiseObservation
{
    public CruiseObservation(
        CruiseSnapshot snapshot,
        DateTimeOffset observedAt,
        string? sourceReference = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (sourceReference is not null && string.IsNullOrWhiteSpace(sourceReference))
        {
            throw new ArgumentException(
                "Source reference cannot be empty or whitespace.",
                nameof(sourceReference));
        }

        Snapshot = snapshot;
        ObservedAt = observedAt;
        SourceReference = sourceReference;
    }

    public CruiseSnapshot Snapshot { get; }

    public DateTimeOffset ObservedAt { get; }

    public string? SourceReference { get; }
}
