using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Models;
using KrytenAssist.Avalonia.Options;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace KrytenAssist.Avalonia.Services;

public sealed class OpenAIConversationService : IConversationService
{
    private readonly ChatClient _chatClient;

    public OpenAIConversationService(IOptions<ConversationOptions> options)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "AI conversations are enabled, but the OPENAI_API_KEY environment variable is not set.");
        }

        _chatClient = new ChatClient(
            options.Value.Model,
            apiKey);
    }

    public async Task<ConversationResponse> SendAsync(
        ConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        List<ChatMessage> messages =
        [
            new SystemChatMessage(request.SystemPrompt)
        ];

        foreach (var message in request.Messages)
        {
            messages.Add(message.Role switch
            {
                ConversationRole.System => new SystemChatMessage(message.Content),
                ConversationRole.User => new UserChatMessage(message.Content),
                ConversationRole.Assistant => new AssistantChatMessage(message.Content),
                _ => throw new InvalidOperationException($"Unsupported conversation role: {message.Role}")
            });
        }

        var response = await _chatClient.CompleteChatAsync(
            messages,
            cancellationToken: cancellationToken);

        var content = response.Value.Content.Count > 0
            ? response.Value.Content[0].Text
            : string.Empty;

        return new ConversationResponse
        {
            Content = content
        };
    }
}