using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class CruiseObservationTests
{
    private static readonly DateTimeOffset ObservedAt =
        new(2026, 7, 14, 10, 30, 0, TimeSpan.FromHours(1));

    [Fact]
    public void Constructor_ShouldRetainSnapshotTimestampOffsetAndSourceReference()
    {
        var snapshot = CreateSnapshot();

        var observation = new CruiseObservation(
            snapshot,
            ObservedAt,
            "sample://cruises/offer-001");

        observation.Snapshot.Should().BeSameAs(snapshot);
        observation.ObservedAt.Should().Be(ObservedAt);
        observation.ObservedAt.Offset.Should().Be(TimeSpan.FromHours(1));
        observation.SourceReference.Should().Be("sample://cruises/offer-001");
    }

    [Fact]
    public void Constructor_ShouldAcceptNullSourceReference()
    {
        var observation = new CruiseObservation(CreateSnapshot(), ObservedAt);

        observation.SourceReference.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldAcceptSourceReferenceThatIsNotAUri()
    {
        var observation = new CruiseObservation(CreateSnapshot(), ObservedAt, "provider reference 1");

        observation.SourceReference.Should().Be("provider reference 1");
    }

    [Fact]
    public void Constructor_ShouldRejectNullSnapshot()
    {
        Action act = () => new CruiseObservation(null!, ObservedAt);

        act.Should().Throw<ArgumentNullException>().WithParameterName("snapshot");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldRejectBlankSourceReference(string sourceReference)
    {
        Action act = () => new CruiseObservation(CreateSnapshot(), ObservedAt, sourceReference);

        act.Should().Throw<ArgumentException>().WithParameterName("sourceReference");
    }

    [Fact]
    public void Equality_ShouldUseSnapshotTimestampAndSourceReference()
    {
        var snapshot = CreateSnapshot();
        var first = new CruiseObservation(snapshot, ObservedAt, "sample-reference");
        var second = new CruiseObservation(snapshot, ObservedAt, "sample-reference");

        first.Should().Be(second);
        first.Should().NotBe(new CruiseObservation(snapshot, ObservedAt.AddMinutes(1), "sample-reference"));
        first.Should().NotBe(new CruiseObservation(snapshot, ObservedAt, "other-reference"));
    }

    private static CruiseSnapshot CreateSnapshot()
    {
        var offer = new CruiseOffer(
            new CruiseProvider("sample.cruises", "Sample Cruises"),
            "sample-offer-001",
            "Mediterranean Escape",
            "Sample Voyager",
            new DateOnly(2027, 7, 14),
            7);

        return new CruiseSnapshot(offer, [new CruisePrice(799.50m, "GBP", "per person")]);
    }
}
