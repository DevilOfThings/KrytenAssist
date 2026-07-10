using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Models;
using KrytenAssist.Avalonia.Options;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace KrytenAssist.Avalonia.Services;

public sealed class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;

    public OpenAIEmbeddingService(IOptions<EmbeddingOptions> options)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI embeddings are enabled, but the OPENAI_API_KEY environment variable is not set.");
        }

        _embeddingClient = new EmbeddingClient(
            options.Value.Model,
            apiKey);
    }

    public async Task<EmbeddingVector> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var response = await _embeddingClient.GenerateEmbeddingAsync(
            text,
            cancellationToken: cancellationToken);

        var values = response.Value
            .ToFloats()
            .ToArray()
            .Select(value => (double)value)
            .ToArray();

        return new EmbeddingVector(values);
    }
}