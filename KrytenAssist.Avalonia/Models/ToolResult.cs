namespace KrytenAssist.Avalonia.Models;

public sealed class ToolResult
{
    public required string CallId { get; init; }

    public required string ToolName { get; init; }

    public required string Content { get; init; }

    public bool IsSuccess { get; init; }
}