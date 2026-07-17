namespace KrytenAssist.Application.Cruises;

public sealed record CruiseObservationRepositoryRecordResult
{
    public CruiseObservationRepositoryRecordResult(
        CruiseObservationRepositoryRecordState state,
        CruiseRecordedHistory history)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        ArgumentNullException.ThrowIfNull(history);
        State = state;
        History = history;
    }

    public CruiseObservationRepositoryRecordState State { get; }
    public CruiseRecordedHistory History { get; }
}
