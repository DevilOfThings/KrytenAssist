using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Abstractions.Persistence;

public interface ISavedCruiseRepository
{
    Task<SavedCruise?> GetAsync(CruiseSailingKey sailingKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavedCruise>> ListAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(SavedCruise savedCruise, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(CruiseSailingKey sailingKey, CancellationToken cancellationToken = default);
}
