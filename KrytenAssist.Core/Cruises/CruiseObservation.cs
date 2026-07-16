namespace KrytenAssist.Core.Cruises;

public sealed record CruiseObservation
{
    public CruiseObservation(
        CruiseSnapshot snapshot,
        DateTimeOffset observedAt,
        string? sourceReference = null,
        CruiseSource? source = null)
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
        Source = source;
    }

    public CruiseSnapshot Snapshot { get; }

    public DateTimeOffset ObservedAt { get; }

    public string? SourceReference { get; }

    public CruiseSource? Source { get; }
}
