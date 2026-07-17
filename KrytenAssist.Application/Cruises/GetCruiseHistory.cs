using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class GetCruiseHistory
{
    private readonly ICruiseObservationRepository _repository;
    private readonly CruisePriceHistoryAnalyzer _analyzer;

    public GetCruiseHistory(
        ICruiseObservationRepository repository,
        CruisePriceHistoryAnalyzer analyzer)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(analyzer);
        _repository = repository;
        _analyzer = analyzer;
    }

    public async Task<CruiseHistoryQueryResult> ExecuteAsync(
        CruiseSailingKey sailingKey,
        CruiseSource? source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sailingKey);
        if (cancellationToken.IsCancellationRequested)
        {
            return CruiseHistoryQueryResult.Cancelled();
        }

        try
        {
            var history = await _repository.GetAsync(sailingKey, source, cancellationToken);
            return history is null
                ? CruiseHistoryQueryResult.NotFound()
                : CruiseHistoryQueryResult.Found(
                    new CruiseHistoryDetails(history, history.Analyze(_analyzer)));
        }
        catch (OperationCanceledException)
        {
            return CruiseHistoryQueryResult.Cancelled();
        }
        catch (Exception)
        {
            return CruiseHistoryQueryResult.Failed();
        }
    }
}
