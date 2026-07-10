
using System.Collections.Generic;

namespace KrytenAssist.Avalonia.Models;

public sealed class ConversationRequest
{
    public required string SystemPrompt { get; init; }

    public required IReadOnlyList<ConversationMessage> Messages { get; init; }
}