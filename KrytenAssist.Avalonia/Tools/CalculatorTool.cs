using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Models;
using KrytenAssist.Avalonia.Services;

namespace KrytenAssist.Avalonia.Tools;

public sealed class CalculatorTool : ITool
{
    public ToolDefinition Definition { get; } = new()
    {
        Name = "calculate",
        Description = "Performs basic arithmetic using add, subtract, multiply, or divide.",
        ParametersJsonSchema = """
                               {
                                 "type": "object",
                                 "properties": {
                                   "left": {
                                     "type": "number"
                                   },
                                   "right": {
                                     "type": "number"
                                   },
                                   "operation": {
                                     "type": "string",
                                     "enum": [
                                       "add",
                                       "subtract",
                                       "multiply",
                                       "divide"
                                     ]
                                   }
                                 },
                                 "required": [
                                   "left",
                                   "right",
                                   "operation"
                                 ],
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

        CalculatorArguments? arguments;

        try
        {
            arguments = JsonSerializer.Deserialize<CalculatorArguments>(
                invocation.ArgumentsJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (JsonException)
        {
            return Task.FromResult(CreateFailure(
                invocation,
                "The calculator arguments were not valid JSON."));
        }

        if (arguments is null)
        {
            return Task.FromResult(CreateFailure(
                invocation,
                "Calculator arguments are required."));
        }

        if (!arguments.Left.HasValue)
        {
            return Task.FromResult(CreateFailure(
                invocation,
                "The 'left' value is required."));
        }

        if (!arguments.Right.HasValue)
        {
            return Task.FromResult(CreateFailure(
                invocation,
                "The 'right' value is required."));
        }

        if (string.IsNullOrWhiteSpace(arguments.Operation))
        {
            return Task.FromResult(CreateFailure(
                invocation,
                "The 'operation' value is required."));
        }

        decimal result;

        switch (arguments.Operation)
        {
            case "add":
                result = arguments.Left.Value + arguments.Right.Value;
                break;

            case "subtract":
                result = arguments.Left.Value - arguments.Right.Value;
                break;

            case "multiply":
                result = arguments.Left.Value * arguments.Right.Value;
                break;

            case "divide":
                if (arguments.Right.Value == 0)
                {
                    return Task.FromResult(CreateFailure(
                        invocation,
                        "Division by zero is not allowed."));
                }

                result = arguments.Left.Value / arguments.Right.Value;
                break;

            default:
                return Task.FromResult(CreateFailure(
                    invocation,
                    $"Unsupported calculator operation '{arguments.Operation}'."));
        }

        var content = JsonSerializer.Serialize(new
        {
            result
        });

        return Task.FromResult(new ToolResult
        {
            CallId = invocation.CallId,
            ToolName = invocation.ToolName,
            Content = content,
            IsSuccess = true
        });
    }

    private static ToolResult CreateFailure(
        ToolInvocation invocation,
        string message)
    {
        return new ToolResult
        {
            CallId = invocation.CallId,
            ToolName = invocation.ToolName,
            Content = message,
            IsSuccess = false
        };
    }

    private sealed class CalculatorArguments
    {
        public decimal? Left { get; init; }

        public decimal? Right { get; init; }

        public string? Operation { get; init; }
    }
}