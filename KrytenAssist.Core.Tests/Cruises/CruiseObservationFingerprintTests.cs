using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class CruiseObservationFingerprintTests
{
    [Fact]
    public void From_NormalizesTextAndDeduplicatesEquivalentPrices()
    {
        var observation = CruiseHistoryTestData.Observation(
            title: " ATLANTIC   Discovery ",
            shipName: " Marella\tExample ",
            prices:
            [
                new CruisePrice(988m, "gbp", " Per   Person "),
                new CruisePrice(988m, "GBP", "per person"),
                new CruisePrice(1975m, "GBP", " Total based on 2 sharing ")
            ]);

        var fingerprint = CruiseObservationFingerprint.From(observation);

        fingerprint.Title.Should().Be("atlantic discovery");
        fingerprint.ShipName.Should().Be("marella example");
        fingerprint.Prices.Should().HaveCount(2);
        fingerprint.Prices.Should().ContainEquivalentOf(new CruisePrice(988m, "GBP", "per person"));
    }

    [Fact]
    public void Equality_IgnoresNonMeaningfulEvidenceChanges()
    {
        var first = CruiseObservationFingerprint.From(CruiseHistoryTestData.Observation());
        var changedEvidence = CruiseObservationFingerprint.From(CruiseHistoryTestData.Observation(
            observedAt: CruiseHistoryTestData.FirstObserved.AddDays(7),
            providerOfferId: "different-package",
            sourceReference: "https://www.tui.co.uk/cruise/bookitineraries/fictional-101?tracking=changed"));

        first.Should().Be(changedEvidence);
        first.GetHashCode().Should().Be(changedEvidence.GetHashCode());
    }

    [Fact]
    public void Equality_IgnoresCaseWhitespacePriceOrderAndEquivalentDuplicates()
    {
        var first = CruiseObservationFingerprint.From(CruiseHistoryTestData.Observation());
        var reformatted = CruiseObservationFingerprint.From(CruiseHistoryTestData.Observation(
            title: " ATLANTIC   DISCOVERY ",
            operatorName: "marella   cruises",
            shipName: "MARELLA\tEXAMPLE",
            departurePort: " santa cruz,   tenerife ",
            itinerary: "TENERIFE, GRAN CANARIA AND LANZAROTE",
            promotion: "gbp 380   PER PERSON discount",
            source: new CruiseSource(" TUI ", " tui "),
            prices:
            [
                new CruisePrice(1975m, "GBP", "TOTAL BASED ON 2 SHARING"),
                new CruisePrice(988m, "GBP", "PER PERSON"),
                new CruisePrice(988m, "GBP", " per   person ")
            ]));

        first.Should().Be(reformatted);
    }

    [Fact]
    public void Equality_DetectsEveryMeaningfulAdvertisedChange()
    {
        var original = CruiseObservationFingerprint.From(CruiseHistoryTestData.Observation());

        original.Should().NotBe(Fingerprint(CruiseHistoryTestData.Observation(title: "Island Discovery")));
        original.Should().NotBe(Fingerprint(CruiseHistoryTestData.Observation(itinerary: "Different itinerary")));
        original.Should().NotBe(Fingerprint(CruiseHistoryTestData.Observation(promotion: "Different promotion")));
        original.Should().NotBe(Fingerprint(CruiseHistoryTestData.Observation(source: new CruiseSource("other", "Other"))));
        original.Should().NotBe(Fingerprint(CruiseHistoryTestData.Observation(perPersonPrice: 949m)));
        original.Should().NotBe(Fingerprint(CruiseHistoryTestData.Observation(
            prices: [new CruisePrice(988m, "USD", "per person")])));
        original.Should().NotBe(Fingerprint(CruiseHistoryTestData.Observation(
            prices: [new CruisePrice(988m, "GBP", "total")])));
        original.Should().NotBe(Fingerprint(CruiseHistoryTestData.Observation(shipName: "Other Ship")));
    }

    [Fact]
    public void Equality_DistinguishesAbsentAndSuppliedRetailSource()
    {
        var supplied = Fingerprint(CruiseHistoryTestData.Observation());
        var absent = Fingerprint(new CruiseObservation(
            CruiseHistoryTestData.Observation().Snapshot,
            CruiseHistoryTestData.FirstObserved));

        supplied.Should().NotBe(absent);
        absent.RetailSourceId.Should().BeNull();
        absent.RetailSourceName.Should().BeNull();
    }

    [Fact]
    public void From_RejectsNullObservation()
    {
        var action = () => CruiseObservationFingerprint.From(null!);

        action.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("observation");
    }

    private static CruiseObservationFingerprint Fingerprint(CruiseObservation observation) =>
        CruiseObservationFingerprint.From(observation);
}
