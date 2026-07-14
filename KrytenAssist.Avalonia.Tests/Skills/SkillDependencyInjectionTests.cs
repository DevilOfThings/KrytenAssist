extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Avalonia.DependencyInjection;
using KrytenAssist.Avalonia.Skills.Cruises;
using KrytenAssist.Avalonia.Skills.Models;
using KrytenAssist.Avalonia.Skills.Samples;
using KrytenAssist.Avalonia.Skills.Services;
using KrytenAssist.Avalonia.Tests.Cruises;
using Microsoft.Extensions.DependencyInjection;
using ICruiseOfTheWeekProvider =
    KrytenApplication::KrytenAssist.Application.Cruises.ICruiseOfTheWeekProvider;

namespace KrytenAssist.Avalonia.Tests.Skills;

public sealed class SkillDependencyInjectionTests
{
    [Fact]
    public void AddSkills_ShouldRegisterSkillRegistry()
    {
        // Arrange
        var services = CreateServices();

        // Act
        using var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetService<ISkillRegistry>();

        // Assert
        registry.Should().NotBeNull();
    }

    [Fact]
    public void AddSkills_ShouldRegisterSkillsInExpectedOrder()
    {
        // Arrange
        var services = CreateServices();

        // Act
        using var serviceProvider = services.BuildServiceProvider();
        var skills = serviceProvider.GetServices<ISkill>().ToArray();

        // Assert
        skills.Should().HaveCount(2);
        skills[0].Should().BeOfType<EchoSkill>();
        skills[1].Should().BeOfType<CruiseOfTheWeekSkill>();
    }

    [Theory]
    [InlineData("sample.echo", typeof(EchoSkill))]
    [InlineData("cruise.of-the-week", typeof(CruiseOfTheWeekSkill))]
    public void AddSkills_ShouldPopulateRegistryWithRegisteredSkills(
        string skillId,
        Type expectedType)
    {
        // Arrange
        var services = CreateServices();

        // Act
        using var serviceProvider = services.BuildServiceProvider();
        var skill = serviceProvider
            .GetRequiredService<ISkillRegistry>()
            .Find(skillId);

        // Assert
        skill.Should().BeOfType(expectedType);
    }

    [Fact]
    public void AddSkills_ShouldRegisterRegistryAsSingleton()
    {
        // Arrange
        var services = CreateServices();

        // Act
        using var serviceProvider = services.BuildServiceProvider();
        var first = serviceProvider.GetRequiredService<ISkillRegistry>();
        var second = serviceProvider.GetRequiredService<ISkillRegistry>();

        // Assert
        second.Should().BeSameAs(first);
    }

    [Fact]
    public void AddSkills_ShouldRegisterBothSkillsAsSingletons()
    {
        // Arrange
        var services = CreateServices();

        // Act
        using var serviceProvider = services.BuildServiceProvider();
        var first = serviceProvider.GetServices<ISkill>().ToArray();
        var second = serviceProvider.GetServices<ISkill>().ToArray();

        // Assert
        second.Should().HaveCount(first.Length);
        second[0].Should().BeSameAs(first[0]);
        second[1].Should().BeSameAs(first[1]);
    }

    [Fact]
    public void AddSkills_ShouldPopulateRegistryWithDependencyInjectionInstances()
    {
        // Arrange
        var services = CreateServices();

        // Act
        using var serviceProvider = services.BuildServiceProvider();
        var skills = serviceProvider.GetServices<ISkill>().ToArray();
        var registry = serviceProvider.GetRequiredService<ISkillRegistry>();

        // Assert
        registry.Find("sample.echo").Should().BeSameAs(skills[0]);
        registry.Find("cruise.of-the-week").Should().BeSameAs(skills[1]);
    }

    [Fact]
    public async Task RegisteredEchoSkill_ShouldExecuteSuccessfullyThroughRegistry()
    {
        // Arrange
        var services = CreateServices();
        using var serviceProvider = services.BuildServiceProvider();
        var skill = serviceProvider
            .GetRequiredService<ISkillRegistry>()
            .Find("sample.echo");
        var request = new SkillRequest(
            "echo",
            new Dictionary<string, object?> { ["message"] = "Hello, Kryten." });
        var context = new SkillContext(CruiseTestData.ObservedAt);

        // Act
        var result = await skill!.ExecuteAsync(request, context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("Hello, Kryten.");
    }

    [Fact]
    public async Task RegisteredCruiseSkill_ShouldExecuteSuccessfullyThroughRegistry()
    {
        // Arrange
        var services = CreateServices();
        using var serviceProvider = services.BuildServiceProvider();
        var skill = serviceProvider
            .GetRequiredService<ISkillRegistry>()
            .Find("cruise.of-the-week");

        // Act
        var result = await skill!.ExecuteAsync(
            new SkillRequest("get-current"),
            new SkillContext(CruiseTestData.ObservedAt));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeOfType<KrytenAssist.Core.Cruises.CruiseObservation>();
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICruiseOfTheWeekProvider>(
            new FakeCruiseOfTheWeekProvider
            {
                Observation = CruiseTestData.CreateObservation()
            });
        services.AddSkills();
        return services;
    }
}
