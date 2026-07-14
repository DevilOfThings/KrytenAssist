extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using FluentAssertions;
using KrytenAssist.Avalonia.Tests.Cruises;
using CruiseOfTheWeekException =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseOfTheWeekException;
using MarellaCruiseOfTheWeekParser =
    KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Marella.MarellaCruiseOfTheWeekParser;

namespace KrytenAssist.Avalonia.Tests.Cruises.Marella;

public sealed class MarellaCruiseOfTheWeekParserTests
{
    private readonly MarellaCruiseOfTheWeekParser _parser = new();

    [Fact]
    public void Parse_ShouldMapCompleteResult()
    {
        // Arrange
        var html = CruiseTestData.CreateHtml();

        // Act
        var observation = _parser.Parse(
            html,
            CruiseTestData.ObservedAt,
            CruiseTestData.SourceUrl);

        // Assert
        observation.ObservedAt.Should().Be(CruiseTestData.ObservedAt);
        observation.ObservedAt.Offset.Should().Be(TimeSpan.FromHours(1));
        observation.SourceReference.Should().Be(CruiseTestData.SourceUrl);
        observation.Snapshot.PromotionSummary.Should().Be(
            "This week's deal: Save £300 on “Mediterranean Medley” with code WEEK300");

        var offer = observation.Snapshot.Offer;
        offer.Provider.Id.Should().Be("marella");
        offer.Provider.Name.Should().Be("Marella Cruises");
        offer.ProviderOfferId.Should().Be("offer-123");
        offer.Title.Should().Be("Mediterranean Medley");
        offer.ShipName.Should().Be("Marella Explorer");
        offer.DepartureDate.Should().Be(new DateOnly(2026, 10, 27));
        offer.DurationNights.Should().Be(7);
        offer.DeparturePort.Should().Be("Palma");
        offer.ItinerarySummary.Should().Be("Western Mediterranean");

        observation.Snapshot.Prices.Should().HaveCount(2);
        observation.Snapshot.Prices[0].Amount.Should().Be(903m);
        observation.Snapshot.Prices[0].Currency.Should().Be("GBP");
        observation.Snapshot.Prices[0].Basis.Should().Be("per person");
        observation.Snapshot.Prices[1].Amount.Should().Be(1806m);
        observation.Snapshot.Prices[1].Basis.Should().Be("total based on 2 sharing");
    }

    [Fact]
    public void Parse_ShouldAllowOptionalValuesToBeAbsent()
    {
        // Arrange
        var html = CruiseTestData.CreateHtml(
            departurePort: null,
            totalPrice: null,
            itinerarySummary: null);

        // Act
        var observation = _parser.Parse(
            html,
            CruiseTestData.ObservedAt,
            CruiseTestData.SourceUrl);

        // Assert
        observation.Snapshot.Offer.DeparturePort.Should().BeNull();
        observation.Snapshot.Offer.ItinerarySummary.Should().BeNull();
        observation.Snapshot.Prices.Should().ContainSingle();
    }

    [Fact]
    public void Parse_ShouldDeriveStableIdentifierWhenProviderIdIsAbsent()
    {
        // Arrange
        var html = CruiseTestData.CreateHtml(providerId: null);

        // Act
        var first = _parser.Parse(html, CruiseTestData.ObservedAt, CruiseTestData.SourceUrl);
        var second = _parser.Parse(
            html,
            CruiseTestData.ObservedAt.AddYears(10),
            CruiseTestData.SourceUrl);

        // Assert
        first.Snapshot.Offer.ProviderOfferId.Should().Be(
            "marella:mediterranean-medley:2026-10-27");
        second.Snapshot.Offer.ProviderOfferId.Should().Be(
            first.Snapshot.Offer.ProviderOfferId);
    }

    [Fact]
    public void Parse_ShouldChangeDerivedIdentifierWhenDepartureChanges()
    {
        // Arrange
        var firstHtml = CruiseTestData.CreateHtml(providerId: null);
        var secondHtml = CruiseTestData.CreateHtml(
            departure: "3 Nov 2026 - 7 nights",
            departurePort: "From Palma on 3 Nov 2026",
            providerId: null);

        // Act
        var first = _parser.Parse(firstHtml, CruiseTestData.ObservedAt, CruiseTestData.SourceUrl);
        var second = _parser.Parse(secondHtml, CruiseTestData.ObservedAt, CruiseTestData.SourceUrl);

        // Assert
        second.Snapshot.Offer.ProviderOfferId.Should().NotBe(
            first.Snapshot.Offer.ProviderOfferId);
    }

    [Theory]
    [InlineData("", "Marella Explorer", "27 Oct 2026 - 7 nights", "£903 pp")]
    [InlineData("Mediterranean Medley", "", "27 Oct 2026 - 7 nights", "£903 pp")]
    [InlineData("Mediterranean Medley", "Marella Explorer", "32 Oct 2026 - 7 nights", "£903 pp")]
    [InlineData("Mediterranean Medley", "Marella Explorer", "27 Oct 2026 - 0 nights", "£903 pp")]
    [InlineData("Mediterranean Medley", "Marella Explorer", "27 Oct 2026 - 7 nights", "No price")]
    public void Parse_ShouldFailWhenRequiredResultDataIsInvalid(
        string title,
        string ship,
        string departure,
        string price)
    {
        // Arrange
        var html = CruiseTestData.CreateHtml(
            title: title,
            ship: ship,
            departure: departure,
            perPersonPrice: price,
            promotion: $"This week's deal: Save now on “{title}”");

        // Act
        Action act = () => _parser.Parse(
            html,
            CruiseTestData.ObservedAt,
            CruiseTestData.SourceUrl);

        // Assert
        act.Should().Throw<CruiseOfTheWeekException>();
    }

    [Fact]
    public void Parse_ShouldFailForAmbiguousPerPersonPrices()
    {
        // Arrange
        var html = CruiseTestData.CreateHtml()
            .Replace(
                "<p>£903 pp</p>",
                "<p>£903 pp</p><p>£999 pp</p>",
                StringComparison.Ordinal);

        // Act
        Action act = () => _parser.Parse(
            html,
            CruiseTestData.ObservedAt,
            CruiseTestData.SourceUrl);

        // Assert
        act.Should().Throw<CruiseOfTheWeekException>()
            .WithMessage("*ambiguous*");
    }

    [Fact]
    public void Parse_ShouldIgnorePricesOutsideMatchingResult()
    {
        // Arrange
        var html = CruiseTestData.CreateHtml()
            .Replace("<h2>", "<aside><p>£99 pp</p></aside><h2>", StringComparison.Ordinal);

        // Act
        var observation = _parser.Parse(
            html,
            CruiseTestData.ObservedAt,
            CruiseTestData.SourceUrl);

        // Assert
        observation.Snapshot.Prices[0].Amount.Should().Be(903m);
    }

    [Fact]
    public void Parse_ShouldFailWhenWeeklyPromotionIsAmbiguous()
    {
        // Arrange
        var html = CruiseTestData.CreateHtml()
            .Replace(
                "<h2>This week's deal:",
                "<h2>This week's deal: Duplicate</h2><h2>This week's deal:",
                StringComparison.Ordinal);

        // Act
        Action act = () => _parser.Parse(
            html,
            CruiseTestData.ObservedAt,
            CruiseTestData.SourceUrl);

        // Assert
        act.Should().Throw<CruiseOfTheWeekException>()
            .WithMessage("*ambiguous*");
    }

    [Fact]
    public void Parse_ShouldFailWhenMatchingCruiseCardsAreAmbiguous()
    {
        // Arrange
        var result = CruiseTestData.CreateHtml()
            .Replace("<html><body>", string.Empty, StringComparison.Ordinal)
            .Replace("</body></html>", string.Empty, StringComparison.Ordinal);
        var duplicateArticle = result[(result.IndexOf("<article>", StringComparison.Ordinal))..];
        var html = $"<html><body>{result}{duplicateArticle}</body></html>";

        // Act
        Action act = () => _parser.Parse(
            html,
            CruiseTestData.ObservedAt,
            CruiseTestData.SourceUrl);

        // Assert
        act.Should().Throw<CruiseOfTheWeekException>()
            .WithMessage("*ambiguous*");
    }
}
