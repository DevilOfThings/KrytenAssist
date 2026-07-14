extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Avalonia.Skills.Cruises;
using KrytenAssist.Avalonia.Skills.Models;
using KrytenAssist.Avalonia.Tests.Cruises;
using CruiseOfTheWeekException =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseOfTheWeekException;

namespace KrytenAssist.Avalonia.Tests.Skills;

public sealed class CruiseOfTheWeekSkillTests
{
    [Fact]
    public void Manifest_ShouldDescribeCruiseOfTheWeekCapability()
    {
        // Arrange
        var skill = CreateSkill();

        // Act
        var manifest = skill.Manifest;

        // Assert
        manifest.Id.Should().Be("cruise.of-the-week");
        manifest.Name.Should().Be("Cruise of the Week");
        manifest.Description.Should().Be(
            "Retrieves Marella Cruises' current Cruise of the Week.");
        manifest.Version.Should().Be("1.0.0");
    }

    [Theory]
    [InlineData("get-current")]
    [InlineData("GET-CURRENT")]
    public async Task ExecuteAsync_ShouldReturnProviderObservation(string operation)
    {
        // Arrange
        var observation = CruiseTestData.CreateObservation();
        var provider = new FakeCruiseOfTheWeekProvider { Observation = observation };
        var skill = new CruiseOfTheWeekSkill(provider);
        var context = new SkillContext(CruiseTestData.ObservedAt);

        // Act
        var result = await skill.ExecuteAsync(new SkillRequest(operation), context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeSameAs(observation);
        result.Message.Should().Be("Cruise of the Week retrieved successfully.");
        provider.InvocationCount.Should().Be(1);
        provider.ReceivedObservedAt.Should().Be(CruiseTestData.ObservedAt);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectParametersWithoutCallingProvider()
    {
        // Arrange
        var provider = CreateProvider();
        var skill = new CruiseOfTheWeekSkill(provider);
        var request = new SkillRequest(
            "get-current",
            new Dictionary<string, object?> { ["cabin"] = "balcony" });

        // Act
        var result = await skill.ExecuteAsync(
            request,
            new SkillContext(CruiseTestData.ObservedAt));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("The Cruise of the Week Skill does not accept parameters.");
        provider.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectUnsupportedOperation()
    {
        // Arrange
        var provider = CreateProvider();
        var skill = new CruiseOfTheWeekSkill(provider);

        // Act
        var result = await skill.ExecuteAsync(
            new SkillRequest("history"),
            new SkillContext(CruiseTestData.ObservedAt));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("not supported");
        provider.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTranslateExpectedProviderFailure()
    {
        // Arrange
        var provider = new FakeCruiseOfTheWeekProvider
        {
            Exception = new CruiseOfTheWeekException("Marella is unavailable.")
        };
        var skill = new CruiseOfTheWeekSkill(provider);

        // Act
        var result = await skill.ExecuteAsync(
            new SkillRequest("get-current"),
            new SkillContext(CruiseTestData.ObservedAt));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Marella is unavailable.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPropagateCancellation()
    {
        // Arrange
        var provider = CreateProvider();
        var skill = new CruiseOfTheWeekSkill(provider);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        // Act
        Func<Task> act = () => skill.ExecuteAsync(
            new SkillRequest("get-current"),
            new SkillContext(CruiseTestData.ObservedAt),
            cancellationSource.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        provider.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotSwallowUnexpectedExceptions()
    {
        // Arrange
        var provider = new FakeCruiseOfTheWeekProvider
        {
            Exception = new InvalidOperationException("Programming error")
        };
        var skill = new CruiseOfTheWeekSkill(provider);

        // Act
        Func<Task> act = () => skill.ExecuteAsync(
            new SkillRequest("get-current"),
            new SkillContext(CruiseTestData.ObservedAt));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ConstructorAndExecution_ShouldGuardNullArguments()
    {
        // Arrange
        var skill = CreateSkill();

        // Act
        Action constructor = () => new CruiseOfTheWeekSkill(null!);
        Func<Task> nullRequest = () => skill.ExecuteAsync(
            null!,
            new SkillContext(CruiseTestData.ObservedAt));
        Func<Task> nullContext = () => skill.ExecuteAsync(
            new SkillRequest("get-current"),
            null!);

        // Assert
        constructor.Should().Throw<ArgumentNullException>();
        await nullRequest.Should().ThrowAsync<ArgumentNullException>();
        await nullContext.Should().ThrowAsync<ArgumentNullException>();
    }

    private static CruiseOfTheWeekSkill CreateSkill()
    {
        return new CruiseOfTheWeekSkill(CreateProvider());
    }

    private static FakeCruiseOfTheWeekProvider CreateProvider()
    {
        return new FakeCruiseOfTheWeekProvider
        {
            Observation = CruiseTestData.CreateObservation()
        };
    }
}
