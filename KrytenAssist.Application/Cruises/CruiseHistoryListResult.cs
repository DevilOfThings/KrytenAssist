namespace KrytenAssist.Application.Cruises;

public sealed record CruiseHistoryListResult
{
    private CruiseHistoryListResult(
        CruiseHistoryListStatus status,
        IReadOnlyList<CruiseHistoryDetails> histories,
        string? message)
    {
        Status = status;
        Histories = histories;
        Message = message;
    }

    public CruiseHistoryListStatus Status { get; }
    public IReadOnlyList<CruiseHistoryDetails> Histories { get; }
    public string? Message { get; }

    public static CruiseHistoryListResult Success(IEnumerable<CruiseHistoryDetails> histories)
    {
        ArgumentNullException.ThrowIfNull(histories);
        var copy = histories.ToArray();
        if (copy.Any(history => history is null))
        {
            throw new ArgumentException("History list cannot contain null values.", nameof(histories));
        }

        return new(
            CruiseHistoryListStatus.Success,
            Array.AsReadOnly(copy),
            null);
    }

    public static CruiseHistoryListResult Cancelled() =>
        new(
            CruiseHistoryListStatus.Cancelled,
            Array.Empty<CruiseHistoryDetails>(),
            "Loading cruise histories was cancelled.");

    public static CruiseHistoryListResult Failed() =>
        new(
            CruiseHistoryListStatus.Failed,
            Array.Empty<CruiseHistoryDetails>(),
            "Cruise histories could not be loaded locally.");
}
