namespace KrytenAssist.Core.Entities;

public sealed class PromptCard
{
    public Guid Id { get; init; }

    public required string Title { get; init; }

    public required string Category { get; init; }

    public string? Description { get; init; }

    public required string PromptText { get; init; }

    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
