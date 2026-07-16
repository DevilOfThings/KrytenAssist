using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class CruiseSourceTests
{
    [Fact]
    public void Constructor_ShouldRetainIdentity()
    {
        var source = new CruiseSource("tui", "TUI");

        source.Id.Should().Be("tui");
        source.Name.Should().Be("TUI");
    }

    [Theory]
    [InlineData(null, "TUI", "id")]
    [InlineData("", "TUI", "id")]
    [InlineData("   ", "TUI", "id")]
    [InlineData("tui", null, "name")]
    [InlineData("tui", "", "name")]
    [InlineData("tui", "   ", "name")]
    public void Constructor_ShouldRejectMissingValues(string? id, string? name, string parameter)
    {
        Action act = () => new CruiseSource(id!, name!);

        act.Should().Throw<ArgumentException>().WithParameterName(parameter);
    }

    [Fact]
    public void Equality_ShouldUseIdAndName()
    {
        new CruiseSource("tui", "TUI").Should().Be(new CruiseSource("tui", "TUI"));
        new CruiseSource("tui", "TUI").Should().NotBe(new CruiseSource("iglu", "Iglu"));
    }
}
