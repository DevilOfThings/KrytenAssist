using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Entities;

namespace KrytenAssist.Application.PromptCards;

public sealed class CreatePromptCard
{
    private readonly IPromptCardRepository _promptCardRepository;

    public CreatePromptCard(IPromptCardRepository promptCardRepository)
    {
        _promptCardRepository = promptCardRepository;
    }

    public async Task<CreatePromptCardResponse> ExecuteAsync(
        CreatePromptCardRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Title is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            throw new ArgumentException("Category is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.PromptText))
        {
            throw new ArgumentException("Prompt text is required.", nameof(request));
        }

        var now = DateTimeOffset.UtcNow;

        var promptCard = new PromptCard
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Category = request.Category.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            PromptText = request.PromptText.Trim(),
            Tags = request.Tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _promptCardRepository.AddAsync(promptCard, cancellationToken);

        return new CreatePromptCardResponse(promptCard.Id);
    }
}