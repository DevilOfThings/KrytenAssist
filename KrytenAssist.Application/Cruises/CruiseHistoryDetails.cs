using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed record CruiseHistoryDetails
{
    public CruiseHistoryDetails(
        CruiseRecordedHistory history,
        CruisePriceHistorySummary summary)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(summary);
        if (history.SailingKey != summary.SailingKey)
        {
            throw new ArgumentException("History and summary must describe the same sailing.", nameof(summary));
        }

        History = history;
        Summary = summary;
    }

    public CruiseRecordedHistory History { get; }
    public CruisePriceHistorySummary Summary { get; }
    public DateTimeOffset LastSeenAt => History.LastSeenAt;
}
