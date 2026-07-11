using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Models;

namespace KrytenAssist.Avalonia.Services;

public interface IToolRegistry
{
    IReadOnlyCollection<ToolDefinition> GetDefinitions();

    Task<ToolResult> ExecuteAsync(
        ToolInvocation invocation,
        CancellationToken cancellationToken);
}