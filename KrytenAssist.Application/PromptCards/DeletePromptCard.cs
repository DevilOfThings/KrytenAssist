using KrytenAssist.Application.Abstractions.Persistence;

namespace KrytenAssist.Application.PromptCards;

public sealed class DeletePromptCard
{
    private readonly IPromptCardRepository _promptCardRepository;

    public DeletePromptCard(IPromptCardRepository promptCardRepository)
    {
        _promptCardRepository = promptCardRepository;
    }

    public async Task<DeletePromptCardResponse> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _promptCardRepository.DeleteAsync(id, cancellationToken);

        return new DeletePromptCardResponse
        {
            Deleted = deleted
        };
    }
}