using KrytenAssist.Core.Entities;

namespace KrytenAssist.Application.Abstractions.Persistence;

public interface IPromptCardRepository
{
    Task AddAsync(PromptCard promptCard, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PromptCard>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PromptCard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(PromptCard promptCard, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}