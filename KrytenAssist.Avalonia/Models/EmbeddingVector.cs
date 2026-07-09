using System.Collections.Generic;

namespace KrytenAssist.Avalonia.Models;

public sealed class EmbeddingVector
{
    public EmbeddingVector(IReadOnlyList<double> values)
    {
        Values = values;
    }

    public IReadOnlyList<double> Values { get; }

    public int Dimension => Values.Count;
}