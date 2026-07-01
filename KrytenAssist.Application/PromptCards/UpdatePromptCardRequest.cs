namespace KrytenAssist.Application.PromptCards;

public sealed class UpdatePromptCardRequest
{
    public required string Title { get; init; }

    public required string Category { get; init; }

    public string? Description { get; init; }

    public required string PromptText { get; init; }

    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
}