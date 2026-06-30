namespace KrytenAssist.Application.PromptCards;

public sealed record CreatePromptCardRequest(
    string Title,
    string Category,
    string? Description,
    string PromptText,
    IReadOnlyCollection<string> Tags);
    
    