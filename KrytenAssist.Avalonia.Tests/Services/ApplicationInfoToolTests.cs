using FluentAssertions;
using KrytenAssist.Avalonia.Tools;
using KrytenAssist.Avalonia.Services;
using System.Text.Json;
using KrytenAssist.Avalonia.Models;
using Xunit;

namespace KrytenAssist.Avalonia.Tests.Services;

public sealed class ApplicationInfoToolTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsApplicationInformation()
    {
        // Arrange
        var tool = new ApplicationInfoTool();

        var invocation = new ToolInvocation
        {
            CallId = "call-1",
            ToolName = "get_application_info",
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
            .GetProperty("name")
            .GetString()
            .Should()
            .Be("Kryten Assist");

        document.RootElement
            .GetProperty("client")
            .GetString()
            .Should()
            .Be("Avalonia");

        document.RootElement
            .GetProperty("mode")
            .GetString()
            .Should()
            .Be("Desktop");
    }
}