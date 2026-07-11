using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Models;
using KrytenAssist.Avalonia.Services;

namespace KrytenAssist.Avalonia.Tools;

public sealed class ApplicationInfoTool : ITool
{
    public ToolDefinition Definition { get; } = new()
    {
        Name = "get_application_info",
        Description = "Returns stable information about the running Kryten Assist desktop application.",
        ParametersJsonSchema = """
                               {
                                 "type": "object",
                                 "properties": {},
                                 "additionalProperties": false
                               }
                               """
    };

    public Task<ToolResult> ExecuteAsync(
        ToolInvocation invocation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        cancellationToken.ThrowIfCancellationRequested();

        var content = JsonSerializer.Serialize(new
        {
            name = "Kryten Assist",
            client = "Avalonia",
            mode = "Desktop"
        });

        return Task.FromResult(new ToolResult
        {
            CallId = invocation.CallId,
            ToolName = invocation.ToolName,
            Content = content,
            IsSuccess = true
        });
    }
}