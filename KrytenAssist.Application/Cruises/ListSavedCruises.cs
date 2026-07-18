using KrytenAssist.Application.Abstractions.Persistence;

namespace KrytenAssist.Application.Cruises;

public sealed class ListSavedCruises(ISavedCruiseRepository repository)
{
    public async Task<SavedCruiseListResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return SavedCruiseListResult.Cancelled();
        try { return SavedCruiseListResult.Success((await repository.ListAsync(cancellationToken)).OrderBy(x => x.SailingKey.DepartureDate).ThenBy(x => x.SailingKey.OperatorId, StringComparer.Ordinal).ThenBy(x => x.SailingKey.ShipName, StringComparer.Ordinal)); }
        catch (OperationCanceledException) { return SavedCruiseListResult.Cancelled(); }
        catch (Exception) { return SavedCruiseListResult.Failed(); }
    }
}
