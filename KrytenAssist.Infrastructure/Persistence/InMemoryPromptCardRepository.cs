

using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Entities;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class InMemoryPromptCardRepository : IPromptCardRepository
{
    private readonly List<PromptCard> _promptCards = [];
    private readonly object _syncRoot = new();

    public Task AddAsync(PromptCard promptCard, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(promptCard);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            _promptCards.Add(promptCard);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<PromptCard>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            return Task.FromResult<IReadOnlyCollection<PromptCard>>(_promptCards.ToArray());
        }
    }

    public Task<PromptCard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            var promptCard = _promptCards.FirstOrDefault(card => card.Id == id);
            return Task.FromResult(promptCard);
        }
    }

    public Task<bool> UpdateAsync(PromptCard promptCard, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(promptCard);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            var index = _promptCards.FindIndex(card => card.Id == promptCard.Id);

            if (index < 0)
            {
                return Task.FromResult(false);
            }

            _promptCards[index] = promptCard;
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            var promptCard = _promptCards.FirstOrDefault(card => card.Id == id);

            if (promptCard is null)
            {
                return Task.FromResult(false);
            }

            _promptCards.Remove(promptCard);
            return Task.FromResult(true);
        }
    }
}