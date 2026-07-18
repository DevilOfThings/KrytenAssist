using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class RemoveSavedCruise(ISavedCruiseRepository repository)
{
    public async Task<SavedCruiseMutationResult> ExecuteAsync(CruiseSailingKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (cancellationToken.IsCancellationRequested) return SavedCruiseMutationResult.Cancelled();
        try { return await repository.RemoveAsync(key, cancellationToken) ? SavedCruiseMutationResult.Success(SavedCruiseMutationStatus.Removed) : SavedCruiseMutationResult.NotFound(); }
        catch (OperationCanceledException) { return SavedCruiseMutationResult.Cancelled(); }
        catch (Exception) { return SavedCruiseMutationResult.Failed(); }
    }
}
