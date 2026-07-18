using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class SetSavedCruiseFavourite(ISavedCruiseRepository repository)
{
    public async Task<SavedCruiseMutationResult> ExecuteAsync(CruiseSailingKey key, bool isFavourite, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (cancellationToken.IsCancellationRequested) return SavedCruiseMutationResult.Cancelled();
        try
        {
            var existing = await repository.GetAsync(key, cancellationToken);
            if (existing is null) return SavedCruiseMutationResult.NotFound();
            if (existing.IsFavourite == isFavourite) return SavedCruiseMutationResult.Success(SavedCruiseMutationStatus.Unchanged, existing);
            var updated = existing.WithFavourite(isFavourite);
            await repository.UpsertAsync(updated, cancellationToken);
            return SavedCruiseMutationResult.Success(SavedCruiseMutationStatus.Updated, updated);
        }
        catch (OperationCanceledException) { return SavedCruiseMutationResult.Cancelled(); }
        catch (Exception) { return SavedCruiseMutationResult.Failed(); }
    }
}
