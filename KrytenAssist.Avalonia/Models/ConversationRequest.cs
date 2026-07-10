
namespace KrytenAssist.Avalonia.Models;

public sealed class ConversationRequest
{
    public required string SystemPrompt { get; init; }

    public required string UserMessage { get; init; }
}