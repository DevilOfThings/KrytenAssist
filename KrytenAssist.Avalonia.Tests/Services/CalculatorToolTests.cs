using System.Text.Json;
using FluentAssertions;
using KrytenAssist.Avalonia.Models;
using KrytenAssist.Avalonia.Services;
using KrytenAssist.Avalonia.Tools;
using Xunit;

namespace KrytenAssist.Avalonia.Tests.Services;

public sealed class CalculatorToolTests
{
    private static ToolInvocation CreateInvocation(string argumentsJson)
    {
        return new ToolInvocation
        {
            CallId = "call-1",
            ToolName = "calculate",
            ArgumentsJson = argumentsJson
        };
    }
    
    private static decimal GetResultValue(string content)
    {
        using var document = JsonDocument.Parse(content);

        return document.RootElement
            .GetProperty("result")
            .GetDecimal();
    }
    
    [Fact]
    public async Task ExecuteAsync_WithAddOperation_ReturnsSum()
    {
        // Arrange
        var tool = new CalculatorTool();

        var invocation = CreateInvocation(
            """
            {
              "operation":"add",
              "left":2,
              "right":3
            }
            """);

        // Act
        var result = await tool.ExecuteAsync(
            invocation,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        GetResultValue(result.Content).Should().Be(5m);
    }
    
    [Fact]
    public async Task ExecuteAsync_WithSubtractOperation_ReturnsDifference()
    {
        // Arrange
        var tool = new CalculatorTool();

        var invocation = CreateInvocation(
            """
            {
              "operation":"subtract",
              "left":10,
              "right":3
            }
            """);

        // Act
        var result = await tool.ExecuteAsync(
            invocation,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        GetResultValue(result.Content).Should().Be(7m);
    }
    
    [Fact]
    public async Task ExecuteAsync_WithMultiplyOperation_ReturnsProduct()
    {
        // Arrange
        var tool = new CalculatorTool();

        var invocation = CreateInvocation(
            """
            {
              "operation":"multiply",
              "left":4,
              "right":5
            }
            """);

        // Act
        var result = await tool.ExecuteAsync(
            invocation,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        GetResultValue(result.Content).Should().Be(20m);
    }
    
    [Fact]
    public async Task ExecuteAsync_WithDivideOperation_ReturnsQuotient()
    {
        // Arrange
        var tool = new CalculatorTool();

        var invocation = CreateInvocation(
            """
            {
              "operation":"divide",
              "left":20,
              "right":4
            }
            """);

        // Act
        var result = await tool.ExecuteAsync(
            invocation,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        GetResultValue(result.Content).Should().Be(5m);
    }
    
    [Fact]
    public async Task ExecuteAsync_WithDivideByZero_ReturnsFailure()
    {
        // Arrange
        var tool = new CalculatorTool();

        var invocation = CreateInvocation(
            """
            {
              "operation":"divide",
              "left":20,
              "right":0
            }
            """);

        // Act
        var result = await tool.ExecuteAsync(
            invocation,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Content.Should().Contain("zero");
    }
    
    [Fact]
    public async Task ExecuteAsync_WithUnsupportedOperation_ReturnsFailure()
    {
        // Arrange
        var tool = new CalculatorTool();

        var invocation = CreateInvocation(
            """
            {
              "operation":"power",
              "left":2,
              "right":8
            }
            """);

        // Act
        var result = await tool.ExecuteAsync(
            invocation,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Content.Should().Contain("Unsupported");
    }
    
    [Fact]
    public async Task ExecuteAsync_WithMissingOperation_ReturnsFailure()
    {
        // Arrange
        var tool = new CalculatorTool();

        var invocation = CreateInvocation(
            """
            {
              "left":2,
              "right":3
            }
            """);

        // Act
        var result = await tool.ExecuteAsync(
            invocation,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Content.Should().Contain("operation");
    }
    
    [Fact]
    public async Task ExecuteAsync_WithMissingLeftOperand_ReturnsFailure()
    {
        // Arrange
        var tool = new CalculatorTool();

        var invocation = CreateInvocation(
            """
            {
              "operation":"add",
              "right":3
            }
            """);

        // Act
        var result = await tool.ExecuteAsync(
            invocation,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Content.Should().Contain("left");
    }
    
    [Fact]
    public async Task ExecuteAsync_WithMissingRightOperand_ReturnsFailure()
    {
        // Arrange
        var tool = new CalculatorTool();

        var invocation = CreateInvocation(
            """
            {
              "operation":"add",
              "left":2
            }
            """);

        // Act
        var result = await tool.ExecuteAsync(
            invocation,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Content.Should().Contain("right");
    }
    
    [Fact]
    public async Task ExecuteAsync_WithInvalidJson_ReturnsFailure()
    {
        // Arrange
        var tool = new CalculatorTool();

        var invocation = CreateInvocation(
            "{ invalid json }");

        // Act
        var result = await tool.ExecuteAsync(
            invocation,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Content.Should().Contain("JSON");
    }
}