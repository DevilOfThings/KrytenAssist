using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class CruisePreferencesTests
{
    [Fact]
    public void Collections_are_deduplicated_ordered_and_value_equal()
    {
        var value = new CruisePreferences([9, 5, 9], [CruiseCabinType.Suite, CruiseCabinType.Balcony, CruiseCabinType.Suite], new CruiseBudget(3000, "gbp", CruiseBudgetBasis.TotalBooking));
        value.DepartureMonths.Should().Equal(5, 9);
        value.PreferredCabins.Should().Equal(CruiseCabinType.Balcony, CruiseCabinType.Suite);
        value.Should().Be(new CruisePreferences([5, 9], [CruiseCabinType.Balcony, CruiseCabinType.Suite], new CruiseBudget(3000, "GBP", CruiseBudgetBasis.TotalBooking)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Invalid_month_is_rejected(int month) =>
        FluentActions.Invoking(() => new CruisePreferences([month])).Should().Throw<ArgumentOutOfRangeException>();

    [Fact]
    public void Invalid_cabin_is_rejected() =>
        FluentActions.Invoking(() => new CruisePreferences(preferredCabins: [(CruiseCabinType)99])).Should().Throw<ArgumentOutOfRangeException>();

    [Fact]
    public void Budget_validates_and_normalizes_value()
    {
        new CruiseBudget(1200, "gbp", CruiseBudgetBasis.PerPerson).Currency.Should().Be("GBP");
        FluentActions.Invoking(() => new CruiseBudget(-1, "GBP", CruiseBudgetBasis.PerPerson)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new CruiseBudget(1, "£", CruiseBudgetBasis.PerPerson)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new CruiseBudget(1, "GBP", (CruiseBudgetBasis)99)).Should().Throw<ArgumentOutOfRangeException>();
    }
}
