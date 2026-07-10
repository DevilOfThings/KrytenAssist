namespace KrytenAssist.Avalonia.Models;

public sealed class ConversationMessage
{
    public required ConversationRole Role { get; init; }

    public required string Content { get; init; }
}