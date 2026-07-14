using System.ComponentModel;
using KrytenAssist.Avalonia.Navigation.Models;
using KrytenAssist.Avalonia.Skills.Services;
using KrytenAssist.Avalonia.ViewModels;
using static KrytenAssist.Avalonia.Tests.ViewModels.ShellTestFactory;

namespace KrytenAssist.Avalonia.Tests.ViewModels;

public sealed class ShellViewModelTests
{
    [Fact]
    public void Constructor_RejectsNullAssistantWorkspace()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ShellViewModel(null!, new SkillRegistry()));
    }

    [Fact]
    public void Constructor_RejectsNullSkillRegistry()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ShellViewModel(CreateAssistantWorkspace(), null!));
    }

    [Fact]
    public void Constructor_SelectsDashboardAndRetainsAssistantWorkspace()
    {
        var assistant = CreateAssistantWorkspace();
        var viewModel = new ShellViewModel(assistant, new SkillRegistry());

        Assert.Same(assistant, viewModel.AssistantWorkspace);
        Assert.Same(viewModel.NavigationItems[0], viewModel.SelectedNavigationItem);
        Assert.Equal("navigation.dashboard", viewModel.SelectedNavigationItem.Id);
        Assert.True(viewModel.IsDashboardSelected);
        Assert.False(viewModel.IsAssistantSelected);
        Assert.False(viewModel.IsSkillSelected);
        Assert.Null(viewModel.SelectedSkillManifest);
        Assert.NotNull(viewModel.NavigateCommand);
    }

    [Fact]
    public void Constructor_CreatesExactBuiltInsForEmptyRegistry()
    {
        var viewModel = new ShellViewModel(
            CreateAssistantWorkspace(),
            new SkillRegistry());

        Assert.Collection(
            viewModel.NavigationItems,
            dashboard => AssertNavigationItem(
                dashboard,
                "navigation.dashboard",
                "Dashboard",
                NavigationDestinationKind.Dashboard,
                null),
            assistant => AssertNavigationItem(
                assistant,
                "navigation.assistant",
                "Assistant",
                NavigationDestinationKind.Assistant,
                null));
        Assert.Empty(viewModel.DashboardCards);
        Assert.False(viewModel.HasDashboardCards);
        Assert.Same(viewModel.NavigationItems, viewModel.NavigationItems);
        Assert.Same(viewModel.DashboardCards, viewModel.DashboardCards);
    }

    [Fact]
    public void Constructor_MapsSkillsAndPreservesRegistryOrder()
    {
        var first = new CountingSkill(FirstManifest);
        var second = new CountingSkill(SecondManifest);

        var viewModel = new ShellViewModel(
            CreateAssistantWorkspace(),
            CreateRegistry(first, second));

        Assert.Collection(
            viewModel.NavigationItems,
            item => Assert.Equal("navigation.dashboard", item.Id),
            item => Assert.Equal("navigation.assistant", item.Id),
            item => AssertNavigationItem(
                item,
                "navigation.skill:test.first",
                "First Skill",
                NavigationDestinationKind.Skill,
                "test.first"),
            item => AssertNavigationItem(
                item,
                "navigation.skill:test.second",
                "Second Skill",
                NavigationDestinationKind.Skill,
                "test.second"));
        Assert.Collection(
            viewModel.DashboardCards,
            card => AssertCard(card, FirstManifest.Id, FirstManifest.Name,
                FirstManifest.Description, FirstManifest.Version),
            card => AssertCard(card, SecondManifest.Id, SecondManifest.Name,
                SecondManifest.Description, SecondManifest.Version));
        Assert.True(viewModel.HasDashboardCards);
        Assert.Equal(0, first.ExecutionCount);
        Assert.Equal(0, second.ExecutionCount);
    }

    [Fact]
    public void Collections_AreExternallyReadOnly()
    {
        var viewModel = new ShellViewModel(
            CreateAssistantWorkspace(),
            CreateRegistry(new CountingSkill(FirstManifest)));

        var navigationItems = Assert.IsAssignableFrom<IList<NavigationItem>>(
            viewModel.NavigationItems);
        var cards = Assert.IsAssignableFrom<IList<DashboardSkillCard>>(
            viewModel.DashboardCards);

        Assert.Throws<NotSupportedException>(() => navigationItems.Add(
            new NavigationItem("navigation.extra", "Extra", NavigationDestinationKind.Dashboard)));
        Assert.Throws<NotSupportedException>(() => cards.Add(
            new DashboardSkillCard("test.extra", "Extra", "Description", "1.0.0")));
    }

    [Fact]
    public void NavigateCommand_SelectsBuiltInDestinations()
    {
        var viewModel = CreatePopulatedViewModel();
        var assistant = viewModel.NavigationItems[1];
        var dashboard = viewModel.NavigationItems[0];

        viewModel.NavigateCommand.Execute(assistant);

        Assert.Same(assistant, viewModel.SelectedNavigationItem);
        Assert.False(viewModel.IsDashboardSelected);
        Assert.True(viewModel.IsAssistantSelected);
        Assert.False(viewModel.IsSkillSelected);
        Assert.Null(viewModel.SelectedSkillManifest);

        viewModel.NavigateCommand.Execute(dashboard);

        Assert.Same(dashboard, viewModel.SelectedNavigationItem);
        Assert.True(viewModel.IsDashboardSelected);
        Assert.False(viewModel.IsAssistantSelected);
        Assert.False(viewModel.IsSkillSelected);
        Assert.Null(viewModel.SelectedSkillManifest);
    }

    [Fact]
    public void NavigateCommand_SelectsCanonicalSkillItemAndManifest()
    {
        var viewModel = CreatePopulatedViewModel();
        var canonicalFirst = viewModel.NavigationItems[2];
        var equivalentItem = new NavigationItem(
            canonicalFirst.Id,
            "Caller-owned title",
            NavigationDestinationKind.Skill,
            "caller-owned-id");

        viewModel.NavigateCommand.Execute(equivalentItem);

        Assert.Same(canonicalFirst, viewModel.SelectedNavigationItem);
        Assert.Same(FirstManifest, viewModel.SelectedSkillManifest);
        Assert.False(viewModel.IsDashboardSelected);
        Assert.False(viewModel.IsAssistantSelected);
        Assert.True(viewModel.IsSkillSelected);

        viewModel.NavigateCommand.Execute(viewModel.NavigationItems[3]);

        Assert.Same(viewModel.NavigationItems[3], viewModel.SelectedNavigationItem);
        Assert.Same(SecondManifest, viewModel.SelectedSkillManifest);
    }

    [Fact]
    public void NavigateCommand_DashboardCardAndNavigationItemReachSameDestination()
    {
        var viewModel = CreatePopulatedViewModel();
        var firstSkillItem = viewModel.NavigationItems[2];

        viewModel.NavigateCommand.Execute(viewModel.DashboardCards[0]);

        Assert.Same(firstSkillItem, viewModel.SelectedNavigationItem);
        Assert.Same(FirstManifest, viewModel.SelectedSkillManifest);

        viewModel.NavigateCommand.Execute(viewModel.NavigationItems[0]);
        viewModel.NavigateCommand.Execute(firstSkillItem);

        Assert.Same(firstSkillItem, viewModel.SelectedNavigationItem);
        Assert.Same(FirstManifest, viewModel.SelectedSkillManifest);
    }

    [Fact]
    public void NavigateCommand_IgnoresUnsupportedUnknownAndStaleInputs()
    {
        var viewModel = CreatePopulatedViewModel();
        var initial = viewModel.SelectedNavigationItem;
        var inputs = new object?[]
        {
            null,
            42,
            new NavigationItem("navigation.unknown", "Unknown", NavigationDestinationKind.Dashboard),
            new NavigationItem(
                "navigation.skill:stale",
                "Stale",
                NavigationDestinationKind.Skill,
                "test.stale"),
            new DashboardSkillCard("test.unknown", "Unknown", "Unknown Skill", "1.0.0")
        };

        foreach (var input in inputs)
        {
            viewModel.NavigateCommand.Execute(input);
            Assert.Same(initial, viewModel.SelectedNavigationItem);
            Assert.Null(viewModel.SelectedSkillManifest);
        }
    }

    [Fact]
    public void NavigateCommand_RaisesRequiredNotificationsForTransitions()
    {
        var viewModel = CreatePopulatedViewModel();

        AssertNotifications(
            viewModel,
            viewModel.NavigationItems[1],
            expectManifestNotification: false);
        AssertNotifications(
            viewModel,
            viewModel.NavigationItems[2],
            expectManifestNotification: true);
        AssertNotifications(
            viewModel,
            viewModel.NavigationItems[3],
            expectManifestNotification: true);
        AssertNotifications(
            viewModel,
            viewModel.NavigationItems[0],
            expectManifestNotification: true);
    }

    [Fact]
    public void NavigateCommand_CurrentAndUnknownInputsRaiseNoNotifications()
    {
        var viewModel = CreatePopulatedViewModel();
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        viewModel.NavigateCommand.Execute(viewModel.SelectedNavigationItem);
        viewModel.NavigateCommand.Execute(new NavigationItem(
            "navigation.unknown",
            "Unknown",
            NavigationDestinationKind.Dashboard));

        Assert.Empty(notifications);
    }

    [Fact]
    public void DiscoveryAndAllNavigationPaths_DoNotExecuteSkills()
    {
        var first = new CountingSkill(FirstManifest);
        var second = new CountingSkill(SecondManifest);
        var registry = CreateRegistry(first, second);
        var viewModel = new ShellViewModel(CreateAssistantWorkspace(), registry);

        _ = viewModel.NavigationItems;
        _ = viewModel.DashboardCards;
        foreach (var item in viewModel.NavigationItems)
        {
            viewModel.NavigateCommand.Execute(item);
            _ = viewModel.SelectedSkillManifest;
        }
        foreach (var card in viewModel.DashboardCards)
        {
            viewModel.NavigateCommand.Execute(card);
            _ = viewModel.SelectedSkillManifest;
        }

        Assert.Equal(0, first.ExecutionCount);
        Assert.Equal(0, second.ExecutionCount);
    }

    private static ShellViewModel CreatePopulatedViewModel()
    {
        return new ShellViewModel(
            CreateAssistantWorkspace(),
            CreateRegistry(
                new CountingSkill(FirstManifest),
                new CountingSkill(SecondManifest)));
    }

    private static void AssertNotifications(
        ShellViewModel viewModel,
        object destination,
        bool expectManifestNotification)
    {
        var notifications = new List<string?>();
        PropertyChangedEventHandler handler = (_, e) => notifications.Add(e.PropertyName);
        viewModel.PropertyChanged += handler;

        viewModel.NavigateCommand.Execute(destination);

        viewModel.PropertyChanged -= handler;
        Assert.Contains(nameof(ShellViewModel.SelectedNavigationItem), notifications);
        Assert.Contains(nameof(ShellViewModel.IsDashboardSelected), notifications);
        Assert.Contains(nameof(ShellViewModel.IsAssistantSelected), notifications);
        Assert.Contains(nameof(ShellViewModel.IsSkillSelected), notifications);
        Assert.Equal(
            expectManifestNotification,
            notifications.Contains(nameof(ShellViewModel.SelectedSkillManifest)));
    }

    private static void AssertNavigationItem(
        NavigationItem item,
        string id,
        string title,
        NavigationDestinationKind kind,
        string? skillId)
    {
        Assert.Equal(id, item.Id);
        Assert.Equal(title, item.Title);
        Assert.Equal(kind, item.Kind);
        Assert.Equal(skillId, item.SkillId);
    }

    private static void AssertCard(
        DashboardSkillCard card,
        string skillId,
        string name,
        string description,
        string version)
    {
        Assert.Equal(skillId, card.SkillId);
        Assert.Equal(name, card.Name);
        Assert.Equal(description, card.Description);
        Assert.Equal(version, card.Version);
    }
}
