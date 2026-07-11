namespace KrytenAssist.Avalonia.Models;

public sealed class ToolInvocation
{
    public required string CallId { get; init; }

    public required string ToolName { get; init; }

    public required string ArgumentsJson { get; init; }
}