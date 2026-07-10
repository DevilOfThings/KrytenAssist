
using System.Collections.Generic;
using KrytenAssist.Avalonia.Models;

namespace KrytenAssist.Avalonia.Services;

public interface IConversationMemory
{
    IReadOnlyList<ConversationMessage> GetRecentMessages();

    void AddTurn(
        ConversationMessage userMessage,
        ConversationMessage assistantMessage);

    void Clear();
}