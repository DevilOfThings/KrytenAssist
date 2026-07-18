using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class UpdateCruiseEvaluation(ISavedCruiseRepository repository)
{
    public async Task<SavedCruiseMutationResult> ExecuteAsync(CruiseSailingKey key, CruiseEvaluation evaluation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key); ArgumentNullException.ThrowIfNull(evaluation);
        if (cancellationToken.IsCancellationRequested) return SavedCruiseMutationResult.Cancelled();
        try
        {
            var existing = await repository.GetAsync(key, cancellationToken);
            if (existing is null) return SavedCruiseMutationResult.NotFound();
            var updated = existing.WithEvaluation(evaluation);
            if (updated == existing) return SavedCruiseMutationResult.Success(SavedCruiseMutationStatus.Unchanged, existing);
            await repository.UpsertAsync(updated, cancellationToken);
            return SavedCruiseMutationResult.Success(SavedCruiseMutationStatus.Updated, updated);
        }
        catch (OperationCanceledException) { return SavedCruiseMutationResult.Cancelled(); }
        catch (Exception) { return SavedCruiseMutationResult.Failed(); }
    }
}
