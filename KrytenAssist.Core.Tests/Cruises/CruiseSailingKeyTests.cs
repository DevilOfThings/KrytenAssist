using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class CruiseSailingKeyTests
{
    [Fact]
    public void Constructor_NormalizesCanonicalTextAndRetainsSailingValues()
    {
        var key = new CruiseSailingKey(
            "  MARELLA\t ",
            " Marella   Discovery\n2 ",
            new DateOnly(2026, 12, 18),
            7);

        key.OperatorId.Should().Be("marella");
        key.ShipName.Should().Be("marella discovery 2");
        key.DepartureDate.Should().Be(new DateOnly(2026, 12, 18));
        key.DurationNights.Should().Be(7);
    }

    [Fact]
    public void From_UsesOnlyStablePhysicalSailingValues()
    {
        var observation = CruiseHistoryTestData.Observation();

        var key = CruiseSailingKey.From(observation);

        key.Should().Be(new CruiseSailingKey(
            "marella",
            "Marella Example",
            new DateOnly(2026, 12, 18),
            7));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsInvalidOperatorId(string? value)
    {
        var action = () => new CruiseSailingKey(
            value!, "Ship", new DateOnly(2026, 1, 1), 7);

        action.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("operatorId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsInvalidShipName(string? value)
    {
        var action = () => new CruiseSailingKey(
            "operator", value!, new DateOnly(2026, 1, 1), 7);

        action.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("shipName");
    }

    [Fact]
    public void Constructor_RejectsNonPositiveDuration()
    {
        var action = () => new CruiseSailingKey(
            "operator", "Ship", new DateOnly(2026, 1, 1), 0);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("durationNights");
    }

    [Fact]
    public void Equality_IgnoresOnlyCaseAndWhitespaceDifferences()
    {
        var first = new CruiseSailingKey(
            "MARELLA", "Marella  Example", new DateOnly(2026, 12, 18), 7);
        var second = new CruiseSailingKey(
            " marella ", "marella\texample", new DateOnly(2026, 12, 18), 7);

        first.Should().Be(second);
    }

    [Fact]
    public void Equality_ChangesForEveryCanonicalComponent()
    {
        var key = new CruiseSailingKey(
            "marella", "Marella Example", new DateOnly(2026, 12, 18), 7);

        key.Should().NotBe(new CruiseSailingKey("other", "Marella Example", new DateOnly(2026, 12, 18), 7));
        key.Should().NotBe(new CruiseSailingKey("marella", "Other Ship", new DateOnly(2026, 12, 18), 7));
        key.Should().NotBe(new CruiseSailingKey("marella", "Marella Example", new DateOnly(2026, 12, 19), 7));
        key.Should().NotBe(new CruiseSailingKey("marella", "Marella Example", new DateOnly(2026, 12, 18), 14));
    }
}
