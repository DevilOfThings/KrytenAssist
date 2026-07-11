namespace KrytenAssist.Avalonia.Options;

public sealed class ConversationOptions
{
    public string Model { get; init; } = "gpt-4.1-mini";

    public string SystemPrompt { get; init; } =
        "You are Kryten Assist. Provide concise, accurate and practical help.";

    public int MaxContextMessages { get; init; } = 12;

    public int MaxToolIterations { get; init; } = 5;
}