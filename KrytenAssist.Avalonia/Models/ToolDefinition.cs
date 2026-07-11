namespace KrytenAssist.Avalonia.Models;

public sealed class ToolDefinition
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public required string ParametersJsonSchema { get; init; }
}