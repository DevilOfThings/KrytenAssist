using FluentAssertions;
using KrytenAssist.Avalonia.Models;
using KrytenAssist.Avalonia.Services;

namespace KrytenAssist.Avalonia.Tests.Services;

public sealed class ToolRegistryTests
{
    [Fact]
    public void GetDefinitions_ReturnsRegisteredDefinitions()
    {
        // Arrange

        var tool = new FakeTool("test_tool");

        var registry = new ToolRegistry(new[] { tool });

        // Act

        var definitions = registry.GetDefinitions();

        // Assert

        definitions.Should().ContainSingle();

        definitions.Single().Name.Should().Be("test_tool");
    }
    
    [Fact]
    public async Task ExecuteAsync_ExecutesKnownTool()
    {
        // Arrange
        var tool = new FakeTool("test_tool");

        var registry = new ToolRegistry(new[] { tool });

        var invocation = new ToolInvocation
        {
            CallId = "call-1",
            ToolName = "test_tool",
            ArgumentsJson = "{}"
        };

        // Act
        var result = await registry.ExecuteAsync(
            invocation,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CallId.Should().Be("call-1");
        result.ToolName.Should().Be("test_tool");
        result.Content.Should().Be("Tool executed.");
    }
    
    [Fact]
    public async Task ExecuteAsync_WithUnknownTool_ReturnsControlledFailure()
    {
        // Arrange
        var registry = new ToolRegistry(Array.Empty<ITool>());

        var invocation = new ToolInvocation
        {
            CallId = "call-1",
            ToolName = "unknown_tool",
            ArgumentsJson = "{}"
        };

        // Act
        var result = await registry.ExecuteAsync(
            invocation,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.CallId.Should().Be("call-1");
        result.ToolName.Should().Be("unknown_tool");
        result.Content.Should().Contain("Unknown tool");
    }
    
    [Fact]
    public void Constructor_WithDuplicateToolNames_Throws()
    {
        // Arrange
        var firstTool = new FakeTool("duplicate_tool");
        var secondTool = new FakeTool("duplicate_tool");

        // Act
        Action action = () => new ToolRegistry(
            new ITool[]
            {
                firstTool,
                secondTool
            });

        // Assert
        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*duplicate_tool*");
    }
    
    [Fact]
    public async Task ExecuteAsync_PassesCancellationTokenToTool()
    {
        // Arrange
        var tool = new FakeTool("test_tool");
        var registry = new ToolRegistry(new[] { tool });

        using var cancellationTokenSource = new CancellationTokenSource();

        var invocation = new ToolInvocation
        {
            CallId = "call-1",
            ToolName = "test_tool",
            ArgumentsJson = "{}"
        };

        // Act
        await registry.ExecuteAsync(
            invocation,
            cancellationTokenSource.Token);

        // Assert
        tool.ReceivedCancellationToken
            .Should()
            .Be(cancellationTokenSource.Token);
    }

    private sealed class FakeTool : ITool
    {
        public FakeTool(string name)
        {
            Definition = new ToolDefinition
            {
                Name = name,
                Description = "Test tool",
                ParametersJsonSchema = "{}"
            };
        }

        public ToolDefinition Definition { get; }
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<ToolResult> ExecuteAsync(
            ToolInvocation invocation,
            CancellationToken cancellationToken)
        {
            ReceivedCancellationToken = cancellationToken;

            return Task.FromResult(new ToolResult
            {
                CallId = invocation.CallId,
                ToolName = invocation.ToolName,
                Content = "Tool executed.",
                IsSuccess = true
            });
        }
    }
}