using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class GetSavedCruise(ISavedCruiseRepository repository)
{
    public async Task<SavedCruiseQueryResult> ExecuteAsync(CruiseSailingKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (cancellationToken.IsCancellationRequested) return SavedCruiseQueryResult.Cancelled();
        try { var value = await repository.GetAsync(key, cancellationToken); return value is null ? SavedCruiseQueryResult.NotFound() : SavedCruiseQueryResult.Found(value); }
        catch (OperationCanceledException) { return SavedCruiseQueryResult.Cancelled(); }
        catch (Exception) { return SavedCruiseQueryResult.Failed(); }
    }
}
