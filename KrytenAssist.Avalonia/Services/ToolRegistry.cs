using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Models;

namespace KrytenAssist.Avalonia.Services;

public sealed class ToolRegistry : IToolRegistry
{
    private readonly IReadOnlyDictionary<string, ITool> _tools;

    public ToolRegistry(IEnumerable<ITool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        var dictionary = new Dictionary<string, ITool>(StringComparer.Ordinal);

        foreach (var tool in tools)
        {
            if (!dictionary.TryAdd(tool.Definition.Name, tool))
            {
                throw new InvalidOperationException(
                    $"Duplicate tool registration detected for '{tool.Definition.Name}'.");
            }
        }

        _tools = dictionary;
    }

    public IReadOnlyCollection<ToolDefinition> GetDefinitions() =>
        _tools.Values
            .Select(tool => tool.Definition)
            .ToArray();

    public Task<ToolResult> ExecuteAsync(
        ToolInvocation invocation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        cancellationToken.ThrowIfCancellationRequested();

        if (!_tools.TryGetValue(invocation.ToolName, out var tool))
        {
            return Task.FromResult(new ToolResult
            {
                CallId = invocation.CallId,
                ToolName = invocation.ToolName,
                Content = $"Unknown tool '{invocation.ToolName}'.",
                IsSuccess = false
            });
        }

        return tool.ExecuteAsync(invocation, cancellationToken);
    }
}