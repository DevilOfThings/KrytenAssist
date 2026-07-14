using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class CruiseProviderTests
{
    [Fact]
    public void Constructor_ShouldRetainSuppliedValuesExactly()
    {
        var provider = new CruiseProvider("  Sample.Cruises  ", "  Sample Cruises  ");

        provider.Id.Should().Be("  Sample.Cruises  ");
        provider.Name.Should().Be("  Sample Cruises  ");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldRejectBlankId(string id)
    {
        Action act = () => new CruiseProvider(id, "Sample Cruises");

        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    [Fact]
    public void Constructor_ShouldRejectNullId()
    {
        Action act = () => new CruiseProvider(null!, "Sample Cruises");

        act.Should().Throw<ArgumentNullException>().WithParameterName("id");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldRejectBlankName(string name)
    {
        Action act = () => new CruiseProvider("sample.cruises", name);

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Constructor_ShouldRejectNullName()
    {
        Action act = () => new CruiseProvider("sample.cruises", null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("name");
    }

    [Fact]
    public void Equality_ShouldUseIdAndNameValues()
    {
        var first = new CruiseProvider("sample.cruises", "Sample Cruises");
        var second = new CruiseProvider("sample.cruises", "Sample Cruises");

        first.Should().Be(second);
    }

    [Fact]
    public void Equality_ShouldDetectChangedIdOrName()
    {
        var provider = new CruiseProvider("sample.cruises", "Sample Cruises");

        provider.Should().NotBe(new CruiseProvider("other.cruises", "Sample Cruises"));
        provider.Should().NotBe(new CruiseProvider("sample.cruises", "Other Cruises"));
    }
}
