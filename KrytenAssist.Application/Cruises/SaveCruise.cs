using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class SaveCruise(ISavedCruiseRepository repository)
{
    public async Task<SavedCruiseMutationResult> ExecuteAsync(SavedCruiseSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (cancellationToken.IsCancellationRequested) return SavedCruiseMutationResult.Cancelled();
        try
        {
            var existing = await repository.GetAsync(snapshot.SailingKey, cancellationToken);
            var saved = existing is null ? new SavedCruise(snapshot) : existing.RefreshSnapshot(snapshot);
            if (existing == saved) return SavedCruiseMutationResult.Success(SavedCruiseMutationStatus.Unchanged, existing);
            await repository.UpsertAsync(saved, cancellationToken);
            return SavedCruiseMutationResult.Success(existing is null ? SavedCruiseMutationStatus.Created : SavedCruiseMutationStatus.Updated, saved);
        }
        catch (OperationCanceledException) { return SavedCruiseMutationResult.Cancelled(); }
        catch (Exception) { return SavedCruiseMutationResult.Failed(); }
    }
}
