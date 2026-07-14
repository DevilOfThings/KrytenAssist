using FluentAssertions;
using KrytenAssist.Avalonia.Skills.Models;
using KrytenAssist.Avalonia.Skills.Samples;

namespace KrytenAssist.Avalonia.Tests.Skills;

public sealed class EchoSkillTests
{
    [Fact]
    public void Manifest_ShouldContainExpectedMetadata()
    {
        // Arrange
        var skill = new EchoSkill();

        // Act
        var manifest = skill.Manifest;

        // Assert
        manifest.Should().Be(new SkillManifest(
            "sample.echo",
            "Echo",
            "Returns a supplied message to validate the Skills framework.",
            "1.0.0"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccessfulResult_ForValidEchoRequest()
    {
        // Arrange
        var skill = new EchoSkill();
        var request = new SkillRequest(
            "echo",
            new Dictionary<string, object?>
            {
                ["message"] = "Hello, Kryten."
            });

        // Act
        var result = await skill.ExecuteAsync(request, CreateContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("Hello, Kryten.");
        result.Message.Should().Be("Echo completed successfully.");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMatchOperationCaseInsensitively()
    {
        // Arrange
        var skill = new EchoSkill();
        var request = CreateEchoRequest("Hello, Kryten.", operation: "ECHO");

        // Act
        var result = await skill.ExecuteAsync(request, CreateContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnOriginalMessageWithoutTransformation()
    {
        // Arrange
        const string message = "  Hello, Kryten.  ";
        var skill = new EchoSkill();
        var request = CreateEchoRequest(message);

        // Act
        var result = await skill.ExecuteAsync(request, CreateContext());

        // Assert
        result.Data.Should().Be(message);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenMessageIsMissing()
    {
        // Arrange
        var skill = new EchoSkill();
        var request = new SkillRequest("echo");

        // Act
        var result = await skill.ExecuteAsync(request, CreateContext());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("A non-empty 'message' parameter is required.");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenMessageIsNull()
    {
        // Arrange
        var skill = new EchoSkill();
        var request = CreateEchoRequest(null);

        // Act
        var result = await skill.ExecuteAsync(request, CreateContext());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("A non-empty 'message' parameter is required.");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenMessageIsNotAString()
    {
        // Arrange
        var skill = new EchoSkill();
        var request = CreateEchoRequest(42);

        // Act
        var result = await skill.ExecuteAsync(request, CreateContext());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("A non-empty 'message' parameter is required.");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenMessageIsEmpty()
    {
        // Arrange
        var skill = new EchoSkill();
        var request = CreateEchoRequest(string.Empty);

        // Act
        var result = await skill.ExecuteAsync(request, CreateContext());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("A non-empty 'message' parameter is required.");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenMessageContainsOnlyWhitespace()
    {
        // Arrange
        var skill = new EchoSkill();
        var request = CreateEchoRequest("   ");

        // Act
        var result = await skill.ExecuteAsync(request, CreateContext());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("A non-empty 'message' parameter is required.");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenOperationIsUnsupported()
    {
        // Arrange
        var skill = new EchoSkill();
        var request = CreateEchoRequest("Hello, Kryten.", operation: "unknown");

        // Act
        var result = await skill.ExecuteAsync(request, CreateContext());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("unknown");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenRequestIsNull()
    {
        // Arrange
        var skill = new EchoSkill();

        // Act
        Func<Task> act = () => skill.ExecuteAsync(null!, CreateContext());

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenContextIsNull()
    {
        // Arrange
        var skill = new EchoSkill();
        var request = CreateEchoRequest("Hello, Kryten.");

        // Act
        Func<Task> act = () => skill.ExecuteAsync(request, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenCancellationIsRequested()
    {
        // Arrange
        var skill = new EchoSkill();
        var request = CreateEchoRequest("Hello, Kryten.");
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        Func<Task> act = () => skill.ExecuteAsync(
            request,
            CreateContext(),
            cancellationTokenSource.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static SkillRequest CreateEchoRequest(
        object? message,
        string operation = "echo")
    {
        return new SkillRequest(
            operation,
            new Dictionary<string, object?>
            {
                ["message"] = message
            });
    }

    private static SkillContext CreateContext()
    {
        return new SkillContext(
            new DateTimeOffset(2026, 7, 14, 10, 0, 0, TimeSpan.Zero));
    }
}
