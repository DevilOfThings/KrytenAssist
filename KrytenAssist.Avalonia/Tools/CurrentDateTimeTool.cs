using System;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Models;
using KrytenAssist.Avalonia.Services;

namespace KrytenAssist.Avalonia.Tools;

public interface IClock
{
    DateTimeOffset Now { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

public sealed class CurrentDateTimeTool : ITool
{
    private readonly IClock _clock;

    public CurrentDateTimeTool(IClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public ToolDefinition Definition { get; } = new()
    {
        Name = "get_current_date_time",
        Description = "Returns the current local date and time together with the current UTC offset.",
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

        var now = _clock.Now;

        var content = JsonSerializer.Serialize(new
        {
            localDateTime = now.ToString("O", CultureInfo.InvariantCulture),
            utcOffset = now.ToString("zzz", CultureInfo.InvariantCulture)
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