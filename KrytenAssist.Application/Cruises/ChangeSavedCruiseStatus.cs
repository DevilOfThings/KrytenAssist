using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public abstract class ChangeSavedCruiseStatus(ISavedCruiseRepository repository, SavedCruiseStatus target, SavedCruiseMutationStatus success)
{
    public async Task<SavedCruiseMutationResult> ExecuteAsync(CruiseSailingKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (cancellationToken.IsCancellationRequested) return SavedCruiseMutationResult.Cancelled();
        try
        {
            var existing = await repository.GetAsync(key, cancellationToken);
            if (existing is null) return SavedCruiseMutationResult.NotFound();
            if (existing.Status == target) return SavedCruiseMutationResult.Success(SavedCruiseMutationStatus.Unchanged, existing);
            var updated = existing.WithStatus(target);
            await repository.UpsertAsync(updated, cancellationToken);
            return SavedCruiseMutationResult.Success(success, updated);
        }
        catch (OperationCanceledException) { return SavedCruiseMutationResult.Cancelled(); }
        catch (Exception) { return SavedCruiseMutationResult.Failed(); }
    }
}

public sealed class DismissCruise(ISavedCruiseRepository repository)
    : ChangeSavedCruiseStatus(repository, SavedCruiseStatus.Dismissed, SavedCruiseMutationStatus.Dismissed);

public sealed class RestoreCruise(ISavedCruiseRepository repository)
    : ChangeSavedCruiseStatus(repository, SavedCruiseStatus.Shortlisted, SavedCruiseMutationStatus.Restored);
