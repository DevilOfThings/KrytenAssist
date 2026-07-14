using System.Collections;
using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class CruiseSnapshotTests
{
    [Fact]
    public void Constructor_ShouldRetainOfferPricesAndPromotion()
    {
        var offer = CreateOffer();
        var first = new CruisePrice(799.50m, "GBP", "per person");
        var second = new CruisePrice(999.50m, "GBP", "total");

        var snapshot = new CruiseSnapshot(offer, [first, second], "Summer saving");

        snapshot.Offer.Should().BeSameAs(offer);
        snapshot.Prices.Should().ContainInOrder(first, second);
        snapshot.PromotionSummary.Should().Be("Summer saving");
    }

    [Fact]
    public void Constructor_ShouldAcceptOnePriceAndNullPromotion()
    {
        var snapshot = new CruiseSnapshot(CreateOffer(), [CreatePrice()]);

        snapshot.Prices.Should().ContainSingle();
        snapshot.PromotionSummary.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldRejectNullOffer()
    {
        Action act = () => new CruiseSnapshot(null!, [CreatePrice()]);

        act.Should().Throw<ArgumentNullException>().WithParameterName("offer");
    }

    [Fact]
    public void Constructor_ShouldRejectNullPrices()
    {
        Action act = () => new CruiseSnapshot(CreateOffer(), null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("prices");
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyPrices()
    {
        Action act = () => new CruiseSnapshot(CreateOffer(), []);

        act.Should().Throw<ArgumentException>().WithParameterName("prices");
    }

    [Fact]
    public void Constructor_ShouldRejectNullPriceElement()
    {
        Action act = () => new CruiseSnapshot(CreateOffer(), [CreatePrice(), null!]);

        act.Should().Throw<ArgumentException>().WithParameterName("prices");
    }

    [Fact]
    public void Constructor_ShouldDefensivelyCopySourcePrices()
    {
        var source = new List<CruisePrice> { CreatePrice() };
        var snapshot = new CruiseSnapshot(CreateOffer(), source);

        source.Add(new CruisePrice(999m, "GBP"));

        snapshot.Prices.Should().ContainSingle();
    }

    [Fact]
    public void Prices_ShouldRejectMutationThroughMutableInterface()
    {
        var snapshot = new CruiseSnapshot(CreateOffer(), [CreatePrice()]);
        var exposed = (IList<CruisePrice>)snapshot.Prices;

        Action act = () => exposed.Add(new CruisePrice(999m, "GBP"));

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Constructor_ShouldEnumerateSourceExactlyOnce()
    {
        var source = new SingleUseEnumerable<CruisePrice>([CreatePrice()]);

        var snapshot = new CruiseSnapshot(CreateOffer(), source);

        snapshot.Prices.Should().ContainSingle();
        source.EnumerationCount.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldRejectBlankPromotionSummary(string promotionSummary)
    {
        Action act = () => new CruiseSnapshot(
            CreateOffer(),
            [CreatePrice()],
            promotionSummary);

        act.Should().Throw<ArgumentException>().WithParameterName("promotionSummary");
    }

    private static CruiseOffer CreateOffer() => new(
        new CruiseProvider("sample.cruises", "Sample Cruises"),
        "sample-offer-001",
        "Mediterranean Escape",
        "Sample Voyager",
        new DateOnly(2027, 7, 14),
        7);

    private static CruisePrice CreatePrice() => new(799.50m, "GBP", "per person");

    private sealed class SingleUseEnumerable<T>(IEnumerable<T> values) : IEnumerable<T>
    {
        public int EnumerationCount { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            if (EnumerationCount > 1)
            {
                throw new InvalidOperationException("The sequence was enumerated more than once.");
            }

            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
