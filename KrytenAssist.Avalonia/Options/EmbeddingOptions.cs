namespace KrytenAssist.Avalonia.Options;

public sealed class EmbeddingOptions
{
    public string Provider { get; init; } = "Deterministic";

    public string Model { get; init; } = "text-embedding-3-small";
}