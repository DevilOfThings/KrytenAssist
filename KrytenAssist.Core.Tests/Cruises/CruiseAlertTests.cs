using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class CruiseAlertTests
{
    private readonly CruiseObservationAlertDetector _detector = new(new CruisePriceHistoryAnalyzer());

    [Fact]
    public void Settings_HaveStableDefaultsAndValidateInclusivePercentage()
    {
        new CruiseAlertSettings().Should().Be(new CruiseAlertSettings(true, true, true, 0));
        new CruiseAlertSettings(minimumPriceDropPercentage: 100).MinimumPriceDropPercentage.Should().Be(100);
        new CruiseAlertSettings(false, true, false, 12.5m).Fingerprint
            .Should().Be(new CruiseAlertSettings(false, true, false, 12.50m).Fingerprint);
        FluentActions.Invoking(() => new CruiseAlertSettings(minimumPriceDropPercentage: -0.1m)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new CruiseAlertSettings(minimumPriceDropPercentage: 100.1m)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PriceDetails_ValidateComparisonAndCalculateRoundedReduction()
    {
        var details = new CruisePriceDropAlertDetails(
            new CruisePrice(3m, "GBP", " Per Person "),
            new CruisePrice(2m, "GBP", "per person"),
            "evidence");

        details.Reduction.Should().Be(1m);
        details.PercentageReduction.Should().Be(33.3333m);
        FluentActions.Invoking(() => new CruisePriceDropAlertDetails(
            new CruisePrice(2m, "GBP", "per person"),
            new CruisePrice(2m, "GBP", "per person"), "evidence"))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PromotionDetails_AreBoundedAndCandidateTypeMustMatch()
    {
        FluentActions.Invoking(() => new CruisePromotionAlertDetails(null, " ", "evidence"))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new CruisePromotionAlertDetails(null, new string('x', 4001), "evidence"))
            .Should().Throw<ArgumentException>();

        var observation = CruiseHistoryTestData.Observation();
        var details = new CruisePromotionAlertDetails(null, "Offer", "evidence");
        FluentActions.Invoking(() => new CruiseAlertCandidate(
            CruiseAlertType.PriceDrop, CruiseSailingKey.From(observation), observation.Source,
            details, observation.ObservedAt, "evidence"))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EventKeys_AreStableAndDifferByIdentityComponents()
    {
        var observation = CruiseHistoryTestData.Observation();
        var key = CruiseSailingKey.From(observation);
        var baseline = CruiseAlertEventKey.Create(CruiseAlertType.PriceDrop, key, observation.Source, "evidence");

        baseline.Should().HaveLength(64).And.Be(CruiseAlertEventKey.Create(CruiseAlertType.PriceDrop, key, new CruiseSource(" TUI ", "Renamed"), "evidence"));
        CruiseAlertEventKey.Create(CruiseAlertType.Promotion, key, observation.Source, "evidence").Should().NotBe(baseline);
        CruiseAlertEventKey.Create(CruiseAlertType.PriceDrop, key, new CruiseSource("other", "Other"), "evidence").Should().NotBe(baseline);
        CruiseAlertEventKey.Create(CruiseAlertType.PriceDrop, key, observation.Source, "other").Should().NotBe(baseline);
        CruiseAlertEventKey.Create(CruiseAlertType.SavedCriteria, key, null, "evidence", "criteria-a")
            .Should().NotBe(CruiseAlertEventKey.Create(CruiseAlertType.SavedCriteria, key, null, "evidence", "criteria-b"));
    }

    [Fact]
    public void LifecycleMutation_PreservesAlertIdentityAndEvidence()
    {
        var previous = CruiseHistoryTestData.Observation(perPersonPrice: 1000m);
        var current = CruiseHistoryTestData.Observation(perPersonPrice: 900m, observedAt: previous.ObservedAt.AddDays(1));
        var candidate = _detector.Detect(previous, current, new CruiseAlertSettings()).Single(x => x.Type == CruiseAlertType.PriceDrop);
        var alert = new CruiseAlert(Guid.NewGuid(), candidate, current.ObservedAt.AddMinutes(1));

        var read = alert.WithStatus(CruiseAlertStatus.Read);

        read.Should().BeEquivalentTo(alert, options => options.Excluding(x => x.Status));
        read.Status.Should().Be(CruiseAlertStatus.Read);
        FluentActions.Invoking(() => alert.WithStatus((CruiseAlertStatus)99)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void NewItineraryAlert_UsesRouteSubjectAndConfirmedEvidenceIdentity()
    {
        var time = new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.FromHours(1));
        var source = new CruiseSource("tui", "TUI");
        var occurrence = new CruiseItineraryOccurrence(new("marella", "ATL-01"), source, time,
            "provider-evidence", "Atlantic Islands", "Marella Explorer", sourceReference: "https://www.tui.co.uk/cruise/example");
        var discovered = new CruiseItineraryFirstObservedEvent(occurrence, new string('a', 64), new string('b', 64));

        var candidate = new CruiseNewItineraryAlertDetector().Detect([discovered], new()).Single();

        candidate.Type.Should().Be(CruiseAlertType.NewItinerary);
        candidate.SailingKey.Should().BeNull();
        candidate.ItineraryCatalogueKey.Should().Be(occurrence.CatalogueKey);
        candidate.EventKey.Should().Be(CruiseAlertEventKey.CreateNewItinerary(occurrence.CatalogueKey, discovered.EventKey));
        candidate.Details.Should().BeOfType<CruiseNewItineraryAlertDetails>()
            .Which.FirstObservedEventKey.Should().Be(discovered.EventKey);
        new CruiseNewItineraryAlertDetector().Detect([discovered], new(newItineraryEnabled: false)).Should().BeEmpty();
    }

    [Fact]
    public void AlertSubjects_RejectTypeAndIdentityMismatches()
    {
        var time = DateTimeOffset.UtcNow; var source = new CruiseSource("tui", "TUI");
        var occurrence = new CruiseItineraryOccurrence(new("marella", "one"), source, time, "evidence");
        var discovered = new CruiseItineraryFirstObservedEvent(occurrence, new string('a', 64), new string('b', 64));
        var details = (CruiseNewItineraryAlertDetails)new CruiseNewItineraryAlertDetector().Detect([discovered], new()).Single().Details;
        FluentActions.Invoking(() => new CruiseAlertCandidate(CruiseAlertType.NewItinerary,
            CruiseSailingKey.From(CruiseHistoryTestData.Observation()), source, details, time, discovered.EventKey)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new CruiseAlertCandidate(CruiseAlertType.NewItinerary,
            new CruiseItineraryAlertSubject(new(source, new("marella", "other"))), source, details, time, discovered.EventKey)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ObservationDetector_CreatesPriceAndPromotionAtThreshold()
    {
        var previous = CruiseHistoryTestData.Observation(perPersonPrice: 1000m, promotion: "Old offer");
        var current = CruiseHistoryTestData.Observation(perPersonPrice: 900m, promotion: "New offer", observedAt: previous.ObservedAt.AddDays(1));

        var candidates = _detector.Detect(previous, current, new CruiseAlertSettings(minimumPriceDropPercentage: 10));

        candidates.Select(x => x.Type).Should().BeEquivalentTo([CruiseAlertType.PriceDrop, CruiseAlertType.Promotion]);
    }

    [Fact]
    public void ObservationDetector_IgnoresBelowThresholdEquivalentPromotionAndDisabledTypes()
    {
        var previous = CruiseHistoryTestData.Observation(perPersonPrice: 1000m, promotion: " Special   OFFER ");
        var current = CruiseHistoryTestData.Observation(perPersonPrice: 901m, promotion: "special offer", observedAt: previous.ObservedAt.AddDays(1));

        _detector.Detect(previous, current, new CruiseAlertSettings(minimumPriceDropPercentage: 10)).Should().BeEmpty();
        _detector.Detect(previous, current, new CruiseAlertSettings(false, false)).Should().BeEmpty();
    }

    [Fact]
    public void ObservationDetector_RejectsMixedSailingOrSource()
    {
        var previous = CruiseHistoryTestData.Observation();
        FluentActions.Invoking(() => _detector.Detect(previous, CruiseHistoryTestData.Observation(shipName: "Other"), new()))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => _detector.Detect(previous, CruiseHistoryTestData.Observation(source: new CruiseSource("other", "Other")), new()))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ObservationDetector_DoesNotAlertForHigherIncomparableOrPromotionDisappearance()
    {
        var previous = CruiseHistoryTestData.Observation(perPersonPrice: 900m, promotion: "Offer");
        var higher = CruiseHistoryTestData.Observation(perPersonPrice: 1000m, promotion: null, observedAt: previous.ObservedAt.AddDays(1));
        var incomparable = CruiseHistoryTestData.Observation(observedAt: previous.ObservedAt.AddDays(1), promotion: null,
            prices: [new CruisePrice(800m, "USD", "per person")]);

        _detector.Detect(previous, higher, new()).Should().BeEmpty();
        _detector.Detect(previous, incomparable, new()).Should().BeEmpty();
    }
}
