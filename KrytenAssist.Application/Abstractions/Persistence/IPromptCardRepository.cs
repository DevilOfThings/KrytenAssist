using KrytenAssist.Core.Entities;

namespace KrytenAssist.Application.Abstractions.Persistence;

public interface IPromptCardRepository
{
    Task AddAsync(PromptCard promptCard, CancellationToken cancellationToken = default);
}

