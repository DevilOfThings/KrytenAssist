namespace KrytenAssist.Application.Cruises;

public sealed record CruiseHistoryQueryResult
{
    private CruiseHistoryQueryResult(
        CruiseHistoryQueryStatus status,
        CruiseHistoryDetails? details,
        string? message)
    {
        Status = status;
        Details = details;
        Message = message;
    }

    public CruiseHistoryQueryStatus Status { get; }
    public CruiseHistoryDetails? Details { get; }
    public string? Message { get; }

    public static CruiseHistoryQueryResult Found(CruiseHistoryDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        return new(CruiseHistoryQueryStatus.Found, details, null);
    }

    public static CruiseHistoryQueryResult NotFound() =>
        new(CruiseHistoryQueryStatus.NotFound, null, null);

    public static CruiseHistoryQueryResult Cancelled() =>
        new(CruiseHistoryQueryStatus.Cancelled, null, "Loading cruise history was cancelled.");

    public static CruiseHistoryQueryResult Failed() =>
        new(CruiseHistoryQueryStatus.Failed, null, "Cruise history could not be loaded locally.");
}
