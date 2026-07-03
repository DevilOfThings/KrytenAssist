using System.Text.Json;
using KrytenAssist.Application.Abstractions.Persistence; 
using KrytenAssist.Core.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class SqlitePromptCardRepository : IPromptCardRepository
{
    private readonly KrytenAssistDbContext _dbContext;

    public SqlitePromptCardRepository(KrytenAssistDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        PromptCard promptCard,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.PromptCards.AddAsync(promptCard, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PromptCard>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PromptCards
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PromptCard?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PromptCards
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    } 

    public async Task<bool> UpdateAsync(
        PromptCard promptCard,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.PromptCards
            .FirstOrDefaultAsync(x => x.Id == promptCard.Id, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        _dbContext.Entry(existing).CurrentValues.SetValues(promptCard);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var promptCard = await _dbContext.PromptCards
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (promptCard is null)
        {
            return false;
        }

        _dbContext.PromptCards.Remove(promptCard);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}