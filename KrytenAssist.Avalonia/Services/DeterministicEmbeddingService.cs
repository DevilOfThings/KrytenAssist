using System;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Models;

namespace KrytenAssist.Avalonia.Services;

public sealed class DeterministicEmbeddingService : IEmbeddingService
{
    private const int VectorSize = 32;

    public Task<EmbeddingVector> GenerateEmbeddingAsync(string text)
    {
        var values = new double[VectorSize];

        if (!string.IsNullOrWhiteSpace(text))
        {
            var tokens = text
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var token in tokens)
            {
                var bucket = GetStableBucket(token);
                values[bucket]++;
            }
        }

        return Task.FromResult(new EmbeddingVector(values));
    }
    
    private static int GetStableBucket(string token)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;

        foreach (var character in token)
        {
            hash ^= character;
            hash *= prime;
        }

        return (int)(hash % VectorSize);
    }
}