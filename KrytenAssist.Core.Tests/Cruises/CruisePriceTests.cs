using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class CruisePriceTests
{
    [Fact]
    public void Constructor_ShouldRetainValidValuesWithoutRounding()
    {
        var price = new CruisePrice(799.501m, "GBP", "per person");

        price.Amount.Should().Be(799.501m);
        price.Currency.Should().Be("GBP");
        price.Basis.Should().Be("per person");
    }

    [Fact]
    public void Constructor_ShouldAcceptZeroAmountAndNullBasis()
    {
        var price = new CruisePrice(0m, "GBP");

        price.Amount.Should().Be(0m);
        price.Basis.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldNormalizeCurrencyUsingInvariantUppercase()
    {
        var price = new CruisePrice(799.50m, "gbp");

        price.Currency.Should().Be("GBP");
    }

    [Fact]
    public void Constructor_ShouldRejectNegativeAmount()
    {
        Action act = () => new CruisePrice(-0.01m, "GBP");

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("amount");
    }

    [Fact]
    public void Constructor_ShouldRejectNullCurrency()
    {
        Action act = () => new CruisePrice(10m, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("currency");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("GB")]
    [InlineData("GBPP")]
    [InlineData("G1P")]
    [InlineData("G-P")]
    [InlineData("GéP")]
    public void Constructor_ShouldRejectStructurallyInvalidCurrency(string currency)
    {
        Action act = () => new CruisePrice(10m, currency);

        act.Should().Throw<ArgumentException>().WithParameterName("currency");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldRejectBlankBasis(string basis)
    {
        Action act = () => new CruisePrice(10m, "GBP", basis);

        act.Should().Throw<ArgumentException>().WithParameterName("basis");
    }

    [Fact]
    public void Equality_ShouldUseNormalizedValues()
    {
        var first = new CruisePrice(799.50m, "gbp", "per person");
        var second = new CruisePrice(799.50m, "GBP", "per person");

        first.Should().Be(second);
    }

    [Fact]
    public void Equality_ShouldDetectChangedAmountCurrencyOrBasis()
    {
        var price = new CruisePrice(799.50m, "GBP", "per person");

        price.Should().NotBe(new CruisePrice(899.50m, "GBP", "per person"));
        price.Should().NotBe(new CruisePrice(799.50m, "USD", "per person"));
        price.Should().NotBe(new CruisePrice(799.50m, "GBP", "total"));
    }
}
