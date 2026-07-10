using System;
using System.Collections.Generic;
using KrytenAssist.Avalonia.Models;
using KrytenAssist.Avalonia.Options;
using Microsoft.Extensions.Options;

namespace KrytenAssist.Avalonia.Services;

public sealed class InMemoryConversationMemory : IConversationMemory
{
    private readonly List<ConversationMessage> _messages = [];
    private readonly int _maxContextMessages;

    public InMemoryConversationMemory(IOptions<ConversationOptions> options)
    {
        _maxContextMessages = Math.Max(2, options.Value.MaxContextMessages);
    }

    public IReadOnlyList<ConversationMessage> GetRecentMessages()
    {
        return _messages.AsReadOnly();
    }

    public void AddTurn(
        ConversationMessage userMessage,
        ConversationMessage assistantMessage)
    {
        _messages.Add(userMessage);
        _messages.Add(assistantMessage);

        while (_messages.Count > _maxContextMessages)
        {
            _messages.RemoveAt(0);
        }
    }

    public void Clear()
    {
        _messages.Clear();
    }
}