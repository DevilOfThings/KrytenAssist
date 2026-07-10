using System;
using System.Threading;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Models;

namespace KrytenAssist.Avalonia.Services;

public class ResilientEmbeddingService 
(
    OpenAIEmbeddingService openAIEmbeddingService,
    DeterministicEmbeddingService deterministicEmbeddingService)  : IEmbeddingService, IEmbeddingServiceStatus
{
    public async Task<EmbeddingVector> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var embedding = await openAIEmbeddingService.GenerateEmbeddingAsync(
                text,
                cancellationToken);

            UpdateStatus(null);

            return embedding;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            UpdateStatus(
                "OpenAI embeddings are unavailable. Deterministic embeddings are being used.");

            return await deterministicEmbeddingService.GenerateEmbeddingAsync(
                text,
                cancellationToken);
        }
    }
    
    private void UpdateStatus(string? message)
    {
        if (StatusMessage == message)
        {
            return;
        }

        StatusMessage = message;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    public string? StatusMessage { get; private set; }
    public event EventHandler? StatusChanged;
}