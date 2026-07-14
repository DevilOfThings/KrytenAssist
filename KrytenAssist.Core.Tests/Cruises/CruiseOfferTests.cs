using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class CruiseOfferTests
{
    private static readonly DateOnly DepartureDate = new(2027, 7, 14);

    [Fact]
    public void Constructor_ShouldRetainRequiredAndOptionalValues()
    {
        var provider = CreateProvider();

        var offer = new CruiseOffer(
            provider,
            "sample-offer-001",
            "Mediterranean Escape",
            "Sample Voyager",
            DepartureDate,
            7,
            "Palma",
            "Western Mediterranean");

        offer.Provider.Should().BeSameAs(provider);
        offer.ProviderOfferId.Should().Be("sample-offer-001");
        offer.Title.Should().Be("Mediterranean Escape");
        offer.ShipName.Should().Be("Sample Voyager");
        offer.DepartureDate.Should().Be(DepartureDate);
        offer.DurationNights.Should().Be(7);
        offer.DeparturePort.Should().Be("Palma");
        offer.ItinerarySummary.Should().Be("Western Mediterranean");
    }

    [Fact]
    public void Constructor_ShouldAcceptNullOptionalValues()
    {
        var offer = CreateOffer();

        offer.DeparturePort.Should().BeNull();
        offer.ItinerarySummary.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldRejectNullProvider()
    {
        Action act = () => new CruiseOffer(
            null!,
            "sample-offer-001",
            "Mediterranean Escape",
            "Sample Voyager",
            DepartureDate,
            7);

        act.Should().Throw<ArgumentNullException>().WithParameterName("provider");
    }

    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData("   ", typeof(ArgumentException))]
    public void Constructor_ShouldRejectInvalidProviderOfferId(string? value, Type exceptionType)
    {
        Action act = () => CreateOffer(providerOfferId: value!);

        act.Should().Throw<ArgumentException>().Which.GetType().Should().Be(exceptionType);
    }

    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData("   ", typeof(ArgumentException))]
    public void Constructor_ShouldRejectInvalidTitle(string? value, Type exceptionType)
    {
        Action act = () => CreateOffer(title: value!);

        act.Should().Throw<ArgumentException>().Which.GetType().Should().Be(exceptionType);
    }

    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData("   ", typeof(ArgumentException))]
    public void Constructor_ShouldRejectInvalidShipName(string? value, Type exceptionType)
    {
        Action act = () => CreateOffer(shipName: value!);

        act.Should().Throw<ArgumentException>().Which.GetType().Should().Be(exceptionType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldRejectNonPositiveDuration(int durationNights)
    {
        Action act = () => CreateOffer(durationNights: durationNights);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("durationNights");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldRejectBlankDeparturePort(string departurePort)
    {
        Action act = () => CreateOffer(departurePort: departurePort);

        act.Should().Throw<ArgumentException>().WithParameterName("departurePort");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldRejectBlankItinerarySummary(string itinerarySummary)
    {
        Action act = () => CreateOffer(itinerarySummary: itinerarySummary);

        act.Should().Throw<ArgumentException>().WithParameterName("itinerarySummary");
    }

    [Fact]
    public void Equality_ShouldUseAllPublicValues()
    {
        var first = CreateOffer(departurePort: "Palma", itinerarySummary: "Western Mediterranean");
        var second = CreateOffer(departurePort: "Palma", itinerarySummary: "Western Mediterranean");

        first.Should().Be(second);
        first.Should().NotBe(CreateOffer(title: "Changed title"));
    }

    private static CruiseProvider CreateProvider() =>
        new("sample.cruises", "Sample Cruises");

    private static CruiseOffer CreateOffer(
        CruiseProvider? provider = null,
        string providerOfferId = "sample-offer-001",
        string title = "Mediterranean Escape",
        string shipName = "Sample Voyager",
        int durationNights = 7,
        string? departurePort = null,
        string? itinerarySummary = null)
    {
        return new CruiseOffer(
            provider ?? CreateProvider(),
            providerOfferId,
            title,
            shipName,
            DepartureDate,
            durationNights,
            departurePort,
            itinerarySummary);
    }
}
