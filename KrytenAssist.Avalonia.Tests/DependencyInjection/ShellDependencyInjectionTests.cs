using KrytenAssist.Avalonia.DependencyInjection;
using KrytenAssist.Avalonia.Skills.Services;
using KrytenAssist.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using static KrytenAssist.Avalonia.Tests.ViewModels.ShellTestFactory;

namespace KrytenAssist.Avalonia.Tests.DependencyInjection;

public sealed class ShellDependencyInjectionTests
{
    [Fact]
    public void AddShell_RejectsNullServiceCollection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ShellServiceCollectionExtensions.AddShell(null!));
    }

    [Fact]
    public void AddShell_ReturnsOriginalCollectionAndRegistersTransientViewModels()
    {
        var services = new ServiceCollection();

        var returned = services.AddShell();

        Assert.Same(services, returned);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(MainWindowViewModel) &&
            descriptor.ImplementationType == typeof(MainWindowViewModel) &&
            descriptor.Lifetime == ServiceLifetime.Transient);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(CruiseOfTheWeekViewModel) &&
            descriptor.ImplementationType == typeof(CruiseOfTheWeekViewModel) &&
            descriptor.Lifetime == ServiceLifetime.Transient);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(ShellViewModel) &&
            descriptor.ImplementationType == typeof(ShellViewModel) &&
            descriptor.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddShell_ResolvesTransientShellsWithAssistantAndRegistrySkills()
    {
        var skill = new CountingSkill(FirstManifest);
        var registry = CreateRegistry(skill);
        var services = new ServiceCollection();
        AddAssistantDependencies(services);
        services.AddSingleton<ISkillRegistry>(registry);
        services.AddShell();
        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<ShellViewModel>();
        var second = provider.GetRequiredService<ShellViewModel>();

        Assert.NotSame(first, second);
        Assert.NotSame(first.AssistantWorkspace, second.AssistantWorkspace);
        Assert.NotSame(first.CruiseOfTheWeek, second.CruiseOfTheWeek);
        Assert.NotSame(
            first.CruiseOfTheWeek.BrowserFeasibility,
            second.CruiseOfTheWeek.BrowserFeasibility);
        Assert.False(first.CruiseOfTheWeek.BrowserFeasibility.HasStarted);
        Assert.False(second.CruiseOfTheWeek.BrowserFeasibility.HasStarted);
        Assert.True(first.IsDashboardSelected);
        Assert.True(second.IsDashboardSelected);
        Assert.Contains(first.NavigationItems, item => item.SkillId == FirstManifest.Id);
        Assert.Contains(first.DashboardCards, card => card.SkillId == FirstManifest.Id);
        Assert.Equal(0, skill.ExecutionCount);
    }

    [Fact]
    public void ResolvingAndNavigatingShell_DoesNotExecuteRegisteredSkill()
    {
        var skill = new CountingSkill(FirstManifest);
        var services = new ServiceCollection();
        AddAssistantDependencies(services);
        services.AddSingleton<ISkillRegistry>(CreateRegistry(skill));
        services.AddShell();
        using var provider = services.BuildServiceProvider();

        var shell = provider.GetRequiredService<ShellViewModel>();
        var clock = provider.GetRequiredService<FixedClock>();
        shell.NavigateCommand.Execute(shell.NavigationItems[2]);
        shell.NavigateCommand.Execute(shell.DashboardCards[0]);

        Assert.Equal(0, skill.ExecutionCount);
        Assert.Equal(0, clock.ReadCount);
    }
}
