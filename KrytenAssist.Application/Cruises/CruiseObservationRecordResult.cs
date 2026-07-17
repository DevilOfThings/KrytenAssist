using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed record CruiseObservationRecordResult
{
    private CruiseObservationRecordResult(
        CruiseObservationRecordStatus status,
        bool snapshotInserted,
        CruisePriceHistorySummary? summary,
        DateTimeOffset? lastSeenAt,
        string? message)
    {
        Status = status;
        SnapshotInserted = snapshotInserted;
        Summary = summary;
        LastSeenAt = lastSeenAt;
        Message = message;
    }

    public CruiseObservationRecordStatus Status { get; }
    public bool SnapshotInserted { get; }
    public CruisePriceHistorySummary? Summary { get; }
    public DateTimeOffset? LastSeenAt { get; }
    public string? Message { get; }

    public static CruiseObservationRecordResult Recorded(
        CruiseObservationRecordStatus status,
        CruisePriceHistorySummary summary,
        DateTimeOffset lastSeenAt)
    {
        if (status is not CruiseObservationRecordStatus.FirstObservationRecorded and
            not CruiseObservationRecordStatus.ChangedObservationRecorded and
            not CruiseObservationRecordStatus.AlreadyCurrent)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        ArgumentNullException.ThrowIfNull(summary);
        return new CruiseObservationRecordResult(
            status,
            status != CruiseObservationRecordStatus.AlreadyCurrent,
            summary,
            lastSeenAt,
            null);
    }

    public static CruiseObservationRecordResult Cancelled() =>
        new(
            CruiseObservationRecordStatus.Cancelled,
            false,
            null,
            null,
            "Recording the cruise observation was cancelled.");

    public static CruiseObservationRecordResult Failed() =>
        new(
            CruiseObservationRecordStatus.Failed,
            false,
            null,
            null,
            "The cruise observation could not be recorded locally.");
}
