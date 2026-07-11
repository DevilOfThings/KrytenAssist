using FluentAssertions;
using KrytenAssist.Avalonia.Services;
using System.Text.Json;
using KrytenAssist.Avalonia.Models;
using KrytenAssist.Avalonia.Tools;
using Xunit;

namespace KrytenAssist.Avalonia.Tests.Services;

public sealed class CurrentDateTimeToolTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsCurrentDateTimeFromClock()
    {
        // Arrange
        var now = new DateTimeOffset(
            2026,
            7,
            11,
            14,
            30,
            45,
            TimeSpan.Zero);

        var tool = new CurrentDateTimeTool(new FakeClock(now));

        var invocation = new ToolInvocation
        {
            CallId = "call-1",
            ToolName = "get_current_date_time",
            ArgumentsJson = "{}"
        };

        // Act
        var result = await tool.ExecuteAsync(
            invocation,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        using var document = JsonDocument.Parse(result.Content);

        document.RootElement
            .GetProperty("localDateTime")
            .GetString()
            .Should()
            .Be(now.ToString("O"));
    }

    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset Now { get; } = now;
    }
}