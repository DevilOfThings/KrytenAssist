using KrytenAssist.Avalonia.Navigation.Models;

namespace KrytenAssist.Avalonia.Tests.Navigation;

public sealed class NavigationItemTests
{
    [Theory]
    [InlineData("navigation.dashboard", "Dashboard", NavigationDestinationKind.Dashboard, null)]
    [InlineData("navigation.assistant", "Assistant", NavigationDestinationKind.Assistant, null)]
    [InlineData("navigation.skill:test.first", "First Skill", NavigationDestinationKind.Skill, "test.first")]
    public void Constructor_PreservesValidValues(
        string id,
        string title,
        NavigationDestinationKind kind,
        string? skillId)
    {
        var item = new NavigationItem(id, title, kind, skillId);

        Assert.Equal(id, item.Id);
        Assert.Equal(title, item.Title);
        Assert.Equal(kind, item.Kind);
        Assert.Equal(skillId, item.SkillId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsInvalidId(string? id)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            new NavigationItem(id!, "Dashboard", NavigationDestinationKind.Dashboard));

        Assert.Equal("id", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsInvalidTitle(string? title)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            new NavigationItem("navigation.dashboard", title!, NavigationDestinationKind.Dashboard));

        Assert.Equal("title", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsSkillDestinationWithoutValidSkillId(string? skillId)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            new NavigationItem(
                "navigation.skill:test",
                "Test Skill",
                NavigationDestinationKind.Skill,
                skillId));

        Assert.Equal("skillId", exception.ParamName);
    }

    [Theory]
    [InlineData(NavigationDestinationKind.Dashboard)]
    [InlineData(NavigationDestinationKind.Assistant)]
    public void Constructor_RejectsSkillIdForBuiltInDestination(
        NavigationDestinationKind kind)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new NavigationItem("navigation.built-in", "Built In", kind, "test.skill"));

        Assert.Equal("skillId", exception.ParamName);
    }

    [Fact]
    public void Constructor_RejectsUnknownDestinationKind()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new NavigationItem("navigation.unknown", "Unknown", (NavigationDestinationKind)999));

        Assert.Equal("kind", exception.ParamName);
    }
}
