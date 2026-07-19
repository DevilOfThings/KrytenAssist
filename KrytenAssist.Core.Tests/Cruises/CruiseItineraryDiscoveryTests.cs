using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class CruiseItineraryDiscoveryTests
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Itinerary_identity_is_stable_and_excludes_mutable_offer_evidence()
    {
        var first = Occurrence("ABC123", title: "Island Explorer", offer: "package-1");
        var changed = Occurrence(" abc123 ", title: "Renamed offer", offer: "package-2");

        first.ItineraryKey.Should().Be(changed.ItineraryKey);
        first.ItineraryKey.PersistenceKey.Should().Be(changed.ItineraryKey.PersistenceKey);
        first.Fingerprint.Should().NotBe(changed.Fingerprint);
        first.ItineraryKey.PersistenceKey.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Catalogue_identity_is_partitioned_by_retail_source()
    {
        var key = new CruiseItineraryKey("marella", "abc");

        new CruiseItineraryCatalogueKey(new("tui", "TUI"), key).PersistenceKey.Should()
            .NotBe(new CruiseItineraryCatalogueKey(new("other", "Other"), key).PersistenceKey);
        new CruiseItineraryCatalogueKey(new("TUI", "Renamed"), key).PersistenceKey.Should()
            .Be(new CruiseItineraryCatalogueKey(new("tui", "TUI"), key).PersistenceKey);
    }

    [Fact]
    public void Scope_is_order_independent_and_preserves_known_unknown_distinction()
    {
        var known = new CruiseDiscoveryCriterion("Airport", CruiseDiscoveryCriterionState.Known, ["LGW", "MAN"]);
        var dates = new CruiseDiscoveryCriterion("Month", CruiseDiscoveryCriterionState.Known, ["July"]);
        var first = Scope([known, dates]);
        var reordered = Scope([
            new("month", CruiseDiscoveryCriterionState.Known, ["july"]),
            new("airport", CruiseDiscoveryCriterionState.Known, ["man", "lgw"])]);
        var unknown = Scope([new("airport", CruiseDiscoveryCriterionState.Unknown)]);

        first.Fingerprint.Should().Be(reordered.Fingerprint);
        first.Fingerprint.Should().NotBe(unknown.Fingerprint);
        first.Criteria.Select(x => x.Name).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public void Scope_and_check_reject_invalid_or_duplicate_evidence()
    {
        FluentActions.Invoking(() => new CruiseDiscoveryCriterion("airport", CruiseDiscoveryCriterionState.Known))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new CruiseDiscoveryCriterion("airport", CruiseDiscoveryCriterionState.Unknown, ["lgw"]))
            .Should().Throw<ArgumentException>();
        var occurrence = Occurrence("abc");
        FluentActions.Invoking(() => new CruiseDiscoveryCheck(Scope(), ObservedAt, [occurrence, occurrence]))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new CruiseDiscoveryCheck(Scope(), ObservedAt, [Occurrence("abc", observedAt: ObservedAt.AddMinutes(1))]))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Check_identity_is_independent_of_input_order_and_retains_truncation()
    {
        var first = new CruiseDiscoveryCheck(Scope(), ObservedAt, [Occurrence("a"), Occurrence("b")],
            [new("two", "unsupported"), new("one", "missing id")], true);
        var second = new CruiseDiscoveryCheck(Scope(), ObservedAt, [Occurrence("b"), Occurrence("a")],
            [new("one", "missing id"), new("two", "unsupported")], true);

        first.EvidenceKey.Should().Be(second.EvidenceKey);
        first.WasTruncated.Should().BeTrue();
        first.Occurrences.Select(x => x.CatalogueKey.PersistenceKey).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public void First_check_seeds_baseline_even_when_truncated()
    {
        var result = new CruiseNewItineraryDetector().Detect(false, [],
            new CruiseDiscoveryCheck(Scope(), ObservedAt, [Occurrence("new")], wasTruncated: true));

        result.Status.Should().Be(CruiseItineraryDetectionStatus.BaselineSeeded);
        result.Events.Should().BeEmpty();
    }

    [Fact]
    public void Later_check_detects_only_unseen_stable_itineraries_in_deterministic_order()
    {
        var knownOccurrence = Occurrence("known", title: "Old title", offer: "one");
        var check = new CruiseDiscoveryCheck(Scope(), ObservedAt,
            [Occurrence("z-new"), Occurrence("known", title: "New title", offer: "two"), Occurrence("a-new")]);

        var result = new CruiseNewItineraryDetector().Detect(true, [knownOccurrence.CatalogueKey], check);

        result.Status.Should().Be(CruiseItineraryDetectionStatus.FirstObserved);
        result.Events.Should().HaveCount(2).And.OnlyContain(x => x.Occurrence.ItineraryKey.ProviderItineraryId != "known");
        result.Events.Select(x => x.Occurrence.CatalogueKey.PersistenceKey).Should().BeInAscendingOrder(StringComparer.Ordinal);
        result.Events.Select(x => x.EventKey).Should().OnlyContain(x => x.Length == 64);
    }

    [Fact]
    public void Known_itinerary_new_sailing_or_offer_is_not_new_and_source_mismatch_is_rejected()
    {
        var known = Occurrence("known");
        var changed = Occurrence("known", title: "Different", offer: "different");
        var result = new CruiseNewItineraryDetector().Detect(true, [known.CatalogueKey],
            new CruiseDiscoveryCheck(Scope(), ObservedAt, [changed], wasTruncated: true));

        result.Status.Should().Be(CruiseItineraryDetectionStatus.NoNewItineraries);
        result.Events.Should().BeEmpty();
        var otherSource = new CruiseItineraryCatalogueKey(new("other", "Other"), known.ItineraryKey);
        FluentActions.Invoking(() => new CruiseNewItineraryDetector().Detect(true, [otherSource],
            new CruiseDiscoveryCheck(Scope(), ObservedAt, [changed]))).Should().Throw<ArgumentException>();
    }

    private static CruiseDiscoveryScope Scope(IEnumerable<CruiseDiscoveryCriterion>? criteria = null) =>
        new(new CruiseSource("tui", "TUI"), "marella", CruiseDiscoverySurface.CruisePackages, 1, criteria);

    private static CruiseItineraryOccurrence Occurrence(string id, string? title = "Title", string? offer = "package",
        DateTimeOffset? observedAt = null) =>
        new(new CruiseItineraryKey("marella", id), new CruiseSource("tui", "TUI"), observedAt ?? ObservedAt,
            $"evidence-{id}", title, "Marella Explorer", new DateOnly(2027, 1, 1), 7,
            "Palma", "Mediterranean", offer, $"https://www.tui.co.uk/cruise/bookitineraries/example?itineraryCode={id}");
}
