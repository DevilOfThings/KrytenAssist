using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Entities;

namespace KrytenAssist.Application.PromptCards;

public sealed class UpdatePromptCard
{
    private readonly IPromptCardRepository _promptCardRepository;

    public UpdatePromptCard(IPromptCardRepository promptCardRepository)
    {
        _promptCardRepository = promptCardRepository;
    }

    public async Task<UpdatePromptCardResponse?> ExecuteAsync(
        Guid id,
        UpdatePromptCardRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingPromptCard = await _promptCardRepository.GetByIdAsync(id, cancellationToken);

        if (existingPromptCard is null)
        {
            return null;
        }

        var updatedPromptCard = new PromptCard
        {
            Id = existingPromptCard.Id,
            Title = request.Title,
            Category = request.Category,
            Description = request.Description,
            PromptText = request.PromptText,
            Tags = request.Tags,
            CreatedAt = existingPromptCard.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _promptCardRepository.UpdateAsync(updatedPromptCard, cancellationToken);

        return new UpdatePromptCardResponse
        {
            Id = updatedPromptCard.Id,
            Title = updatedPromptCard.Title,
            Category = updatedPromptCard.Category,
            Description = updatedPromptCard.Description,
            PromptText = updatedPromptCard.PromptText,
            Tags = updatedPromptCard.Tags,
            CreatedAt = updatedPromptCard.CreatedAt,
            UpdatedAt = updatedPromptCard.UpdatedAt
        };
    }
}