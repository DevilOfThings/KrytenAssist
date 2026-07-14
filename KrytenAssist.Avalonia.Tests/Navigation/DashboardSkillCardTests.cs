using KrytenAssist.Avalonia.Navigation.Models;

namespace KrytenAssist.Avalonia.Tests.Navigation;

public sealed class DashboardSkillCardTests
{
    [Fact]
    public void Constructor_PreservesValidValues()
    {
        var card = new DashboardSkillCard(
            "test.first",
            "First Skill",
            "First deterministic test Skill.",
            "1.0.0");

        Assert.Equal("test.first", card.SkillId);
        Assert.Equal("First Skill", card.Name);
        Assert.Equal("First deterministic test Skill.", card.Description);
        Assert.Equal("1.0.0", card.Version);
    }

    [Theory]
    [MemberData(nameof(InvalidRequiredValues))]
    public void Constructor_RejectsInvalidRequiredValue(
        string parameterName,
        string? skillId,
        string? name,
        string? description,
        string? version)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            new DashboardSkillCard(skillId!, name!, description!, version!));

        Assert.Equal(parameterName, exception.ParamName);
    }

    public static TheoryData<string, string?, string?, string?, string?> InvalidRequiredValues =>
        new()
        {
            { "skillId", null, "Name", "Description", "1.0.0" },
            { "skillId", "", "Name", "Description", "1.0.0" },
            { "skillId", "  ", "Name", "Description", "1.0.0" },
            { "name", "test.skill", null, "Description", "1.0.0" },
            { "name", "test.skill", "", "Description", "1.0.0" },
            { "name", "test.skill", "  ", "Description", "1.0.0" },
            { "description", "test.skill", "Name", null, "1.0.0" },
            { "description", "test.skill", "Name", "", "1.0.0" },
            { "description", "test.skill", "Name", "  ", "1.0.0" },
            { "version", "test.skill", "Name", "Description", null },
            { "version", "test.skill", "Name", "Description", "" },
            { "version", "test.skill", "Name", "Description", "  " }
        };
}
