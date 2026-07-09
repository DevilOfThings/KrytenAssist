using System;
using KrytenAssist.Avalonia.Models;

namespace KrytenAssist.Avalonia.Services;

public sealed class CosineSimilarityService
{
    public double Calculate(EmbeddingVector first, EmbeddingVector second)
    {
        if (first.Dimension != second.Dimension)
        {
            throw new InvalidOperationException("Embedding vectors must have the same dimension.");
        }

        var dotProduct = 0.0;
        var firstMagnitude = 0.0;
        var secondMagnitude = 0.0;

        for (var index = 0; index < first.Dimension; index++)
        {
            var firstValue = first.Values[index];
            var secondValue = second.Values[index];

            dotProduct += firstValue * secondValue;
            firstMagnitude += firstValue * firstValue;
            secondMagnitude += secondValue * secondValue;
        }

        if (firstMagnitude == 0 || secondMagnitude == 0)
        {
            return 0;
        }

        return dotProduct / (Math.Sqrt(firstMagnitude) * Math.Sqrt(secondMagnitude));
    }
}