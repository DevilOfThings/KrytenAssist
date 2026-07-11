using System.Threading;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Models;

namespace KrytenAssist.Avalonia.Services;

public interface ITool
{
    ToolDefinition Definition { get; }

    Task<ToolResult> ExecuteAsync(
        ToolInvocation invocation,
        CancellationToken cancellationToken);
}