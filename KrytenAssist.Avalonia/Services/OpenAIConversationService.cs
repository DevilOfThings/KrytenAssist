using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    private readonly IToolRegistry _toolRegistry;
    
    private readonly ConversationOptions _conversationOptions;

    public OpenAIConversationService(
        IOptions<ConversationOptions> options,
        IToolRegistry toolRegistry)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(toolRegistry);

        _conversationOptions = options.Value;
        
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "AI conversations are enabled, but the OPENAI_API_KEY environment variable is not set.");
        }

        _chatClient = new ChatClient(
            _conversationOptions.Model,
            apiKey);

        _toolRegistry = toolRegistry;
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
                ConversationRole.System =>
                    new SystemChatMessage(message.Content),

                ConversationRole.User =>
                    new UserChatMessage(message.Content),

                ConversationRole.Assistant =>
                    new AssistantChatMessage(message.Content),

                _ => throw new InvalidOperationException(
                    $"Unsupported conversation role: {message.Role}")
            });
        }

        var completionOptions = new ChatCompletionOptions();

        foreach (var definition in _toolRegistry.GetDefinitions())
        {
            completionOptions.Tools.Add(
                ChatTool.CreateFunctionTool(
                    functionName: definition.Name,
                    functionDescription: definition.Description,
                    functionParameters: BinaryData.FromBytes(
                        Encoding.UTF8.GetBytes(
                            definition.ParametersJsonSchema))));
        }

        for (var iteration = 0;
             iteration < _conversationOptions.MaxToolIterations;
             iteration++)
        {
            var response = await _chatClient.CompleteChatAsync(
                messages,
                completionOptions,
                cancellationToken);


            if (response.Value.FinishReason == ChatFinishReason.ToolCalls)
            {
                messages.Add(new AssistantChatMessage(response.Value));

                var invocations = response.Value.ToolCalls
                    .Select(toolCall => new ToolInvocation
                    {
                        CallId = toolCall.Id,
                        ToolName = toolCall.FunctionName,
                        ArgumentsJson = toolCall.FunctionArguments.ToString()
                    })
                    .ToArray();

                foreach (var invocation in invocations)
                {
                    var result = await _toolRegistry.ExecuteAsync(
                        invocation,
                        cancellationToken);

                    messages.Add(
                        new ToolChatMessage(
                            result.CallId,
                            result.Content));
                }

                continue;
            }

            var content = response.Value.Content.Count > 0
                ? response.Value.Content[0].Text
                : string.Empty;

            return new ConversationResponse
            {
                Content = content
            };
        }

        throw new InvalidOperationException(
            "Maximum tool iterations exceeded.");
    }
}