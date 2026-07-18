using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public enum SavedCruiseDetailsListStatus
{
    Success,
    Cancelled,
    Failed
}

public sealed record SavedCruiseDetailsListResult(
    SavedCruiseDetailsListStatus Status,
    IReadOnlyList<SavedCruiseDetails> Details,
    string? Message)
{
    public static SavedCruiseDetailsListResult Success(IEnumerable<SavedCruiseDetails> details) =>
        new(SavedCruiseDetailsListStatus.Success, Array.AsReadOnly(details.ToArray()), null);

    public static SavedCruiseDetailsListResult Cancelled() =>
        new(SavedCruiseDetailsListStatus.Cancelled, [], "Loading saved cruises was cancelled.");

    public static SavedCruiseDetailsListResult Failed() =>
        new(SavedCruiseDetailsListStatus.Failed, [], "Saved cruises could not be loaded locally.");
}

public sealed class ListSavedCruiseDetails(
    ISavedCruiseRepository savedCruises,
    IFavouriteCruiseShipRepository favouriteShips,
    ICruiseObservationRepository observations,
    CruisePriceHistoryAnalyzer analyzer)
{
    public async Task<SavedCruiseDetailsListResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return SavedCruiseDetailsListResult.Cancelled();
        }

        try
        {
            // These repositories share one scoped DbContext in desktop composition,
            // so intentionally load them sequentially.
            var saved = await savedCruises.ListAsync(cancellationToken);
            var ships = (await favouriteShips.ListAsync(cancellationToken)).ToHashSet();
            var histories = await observations.ListAsync(cancellationToken);
            var historyDetails = histories
                .Select(history => new CruiseHistoryDetails(history, history.Analyze(analyzer)))
                .GroupBy(history => history.History.SailingKey)
                .ToDictionary(group => group.Key, group => group.AsEnumerable());

            var details = saved
                .OrderBy(cruise => cruise.SailingKey.DepartureDate)
                .ThenBy(cruise => cruise.SailingKey.OperatorId, StringComparer.Ordinal)
                .ThenBy(cruise => cruise.SailingKey.ShipName, StringComparer.Ordinal)
                .ThenBy(cruise => cruise.SailingKey.DurationNights)
                .ThenBy(cruise => cruise.Snapshot.Title, StringComparer.Ordinal)
                .Select(cruise => new SavedCruiseDetails(
                    cruise,
                    ships.Contains(CruiseShipKey.From(cruise.SailingKey)),
                    historyDetails.GetValueOrDefault(cruise.SailingKey)))
                .ToArray();

            return SavedCruiseDetailsListResult.Success(details);
        }
        catch (OperationCanceledException)
        {
            return SavedCruiseDetailsListResult.Cancelled();
        }
        catch (Exception)
        {
            return SavedCruiseDetailsListResult.Failed();
        }
    }
}
