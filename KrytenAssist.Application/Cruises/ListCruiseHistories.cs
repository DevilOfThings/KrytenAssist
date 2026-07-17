using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class ListCruiseHistories
{
    private readonly ICruiseObservationRepository _repository;
    private readonly CruisePriceHistoryAnalyzer _analyzer;

    public ListCruiseHistories(
        ICruiseObservationRepository repository,
        CruisePriceHistoryAnalyzer analyzer)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(analyzer);
        _repository = repository;
        _analyzer = analyzer;
    }

    public async Task<CruiseHistoryListResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return CruiseHistoryListResult.Cancelled();
        }

        try
        {
            var histories = await _repository.ListAsync(cancellationToken);
            var details = histories
                .Select(history => new CruiseHistoryDetails(history, history.Analyze(_analyzer)))
                .OrderBy(detail => detail.Summary.SailingKey.DepartureDate)
                .ThenByDescending(detail => detail.Summary.LastObservedAt)
                .ThenBy(detail => detail.Summary.SailingKey.OperatorId, StringComparer.Ordinal)
                .ThenBy(detail => detail.Summary.SailingKey.ShipName, StringComparer.Ordinal)
                .ThenBy(detail => detail.Summary.SailingKey.DurationNights)
                .ThenBy(detail => detail.History.Source?.Id ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return CruiseHistoryListResult.Success(details);
        }
        catch (OperationCanceledException)
        {
            return CruiseHistoryListResult.Cancelled();
        }
        catch (Exception)
        {
            return CruiseHistoryListResult.Failed();
        }
    }
}
