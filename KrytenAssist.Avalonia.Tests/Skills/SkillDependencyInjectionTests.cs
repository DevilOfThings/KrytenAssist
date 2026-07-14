using FluentAssertions;
using KrytenAssist.Avalonia.DependencyInjection;
using KrytenAssist.Avalonia.Skills.Models;
using KrytenAssist.Avalonia.Skills.Samples;
using KrytenAssist.Avalonia.Skills.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KrytenAssist.Avalonia.Tests.Skills;

public sealed class SkillDependencyInjectionTests
{
    [Fact]
    public void AddSkills_ShouldRegisterSkillRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSkills();

        // Act
        using var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetService<ISkillRegistry>();

        // Assert
        registry.Should().NotBeNull();
    }

    [Fact]
    public void AddSkills_ShouldRegisterEchoSkill()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSkills();

        // Act
        using var serviceProvider = services.BuildServiceProvider();
        var skills = serviceProvider.GetServices<ISkill>().ToArray();

        // Assert
        skills.Should().ContainSingle();
        skills.Single().Should().BeOfType<EchoSkill>();
    }

    [Fact]
    public void AddSkills_ShouldPopulateRegistryWithEchoSkill()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSkills();

        // Act
        using var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<ISkillRegistry>();
        var skill = registry.Find("sample.echo");

        // Assert
        skill.Should().BeOfType<EchoSkill>();
    }

    [Fact]
    public void AddSkills_ShouldRegisterRegistryAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSkills();

        // Act
        using var serviceProvider = services.BuildServiceProvider();
        var firstRegistry = serviceProvider.GetRequiredService<ISkillRegistry>();
        var secondRegistry = serviceProvider.GetRequiredService<ISkillRegistry>();

        // Assert
        secondRegistry.Should().BeSameAs(firstRegistry);
    }

    [Fact]
    public void AddSkills_ShouldRegisterEchoSkillAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSkills();

        // Act
        using var serviceProvider = services.BuildServiceProvider();
        var firstSkill = serviceProvider.GetServices<ISkill>().Single();
        var secondSkill = serviceProvider.GetServices<ISkill>().Single();

        // Assert
        secondSkill.Should().BeSameAs(firstSkill);
    }

    [Fact]
    public void AddSkills_ShouldPopulateRegistryWithTheDependencyInjectionSkillInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSkills();

        // Act
        using var serviceProvider = services.BuildServiceProvider();
        var dependencyInjectionSkill = serviceProvider.GetServices<ISkill>().Single();
        var registrySkill = serviceProvider
            .GetRequiredService<ISkillRegistry>()
            .Find("sample.echo");

        // Assert
        registrySkill.Should().BeSameAs(dependencyInjectionSkill);
    }

    [Fact]
    public async Task RegisteredEchoSkill_ShouldExecuteSuccessfullyThroughRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSkills();
        using var serviceProvider = services.BuildServiceProvider();
        var skill = serviceProvider
            .GetRequiredService<ISkillRegistry>()
            .Find("sample.echo");
        var request = new SkillRequest(
            "echo",
            new Dictionary<string, object?>
            {
                ["message"] = "Hello, Kryten."
            });
        var context = new SkillContext(
            new DateTimeOffset(2026, 7, 14, 10, 0, 0, TimeSpan.Zero));

        // Act
        var result = await skill!.ExecuteAsync(request, context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("Hello, Kryten.");
    }
}
