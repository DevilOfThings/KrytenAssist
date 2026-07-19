using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class CruiseCabinDomainTests
{
    [Fact]
    public void Context_PreservesKnownAndUnknownAgesAndHasStableNormalizedIdentity()
    {
        var known = new CruiseCabinSearchContext(2, 0, [], true, CruiseCabinPackageMode.FlyCruise, " STN ", 1);
        var equivalent = new CruiseCabinSearchContext(2, 0, [], true, CruiseCabinPackageMode.FlyCruise, "stn", 1);
        var unknown = new CruiseCabinSearchContext(2, 0);

        known.Should().Be(equivalent);
        known.Fingerprint.Should().Be(equivalent.Fingerprint).And.NotBe(unknown.Fingerprint);
        FluentActions.Invoking(() => new CruiseCabinSearchContext(childCount: 1, childAges: [18], childAgesKnown: true))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new CruiseCabinSearchContext(childCount: 2, childAges: [4], childAgesKnown: true))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Observation_RequiresCompleteCategorySetAndConsistentCoverage()
    {
        FluentActions.Invoking(() => Observation(States(CruiseCabinAvailabilityState.Unknown), CruiseCabinEvidenceCoverage.Partial))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Observation(States(CruiseCabinAvailabilityState.Available), CruiseCabinEvidenceCoverage.Partial))
            .Should().Throw<ArgumentException>();
        var incomplete = States(CruiseCabinAvailabilityState.Available).Take(4);
        FluentActions.Invoking(() => Observation(incomplete, CruiseCabinEvidenceCoverage.Complete))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Observation_DefensivelyCopiesAndOrdersStates()
    {
        var states = States(CruiseCabinAvailabilityState.Available).Reverse().ToList();
        var observation = Observation(states, CruiseCabinEvidenceCoverage.Complete);
        states.Clear();
        observation.States.Select(value => value.CabinType).Should().BeInAscendingOrder();
        observation.States.Should().HaveCount(5);
    }

    [Fact]
    public void Identity_SeparatesSeriesFromMeaningfulState()
    {
        var first = Observation(States(CruiseCabinAvailabilityState.Available), CruiseCabinEvidenceCoverage.Complete);
        var refreshed = Observation(States(CruiseCabinAvailabilityState.Available), CruiseCabinEvidenceCoverage.Complete,
            first.ObservedAt.AddDays(1), "new evidence", "https://example.test/new");
        var changed = Observation(States(CruiseCabinAvailabilityState.Unavailable), CruiseCabinEvidenceCoverage.Complete);
        var otherContext = Observation(States(CruiseCabinAvailabilityState.Available), CruiseCabinEvidenceCoverage.Complete,
            context: new CruiseCabinSearchContext(2));

        refreshed.SeriesKey.Should().Be(first.SeriesKey);
        refreshed.StateFingerprint.Should().Be(first.StateFingerprint);
        changed.StateFingerprint.Should().NotBe(first.StateFingerprint);
        otherContext.SeriesKey.Should().NotBe(first.SeriesKey);
    }

    [Fact]
    public void Analyzer_DistinguishesInventoryTransitionsFromKnowledgeChanges()
    {
        var previous = Observation(States(CruiseCabinAvailabilityState.Unknown, (CruiseCabinType.Balcony, CruiseCabinAvailabilityState.Unavailable)), CruiseCabinEvidenceCoverage.Partial);
        var current = Observation(States(CruiseCabinAvailabilityState.Unknown,
            (CruiseCabinType.Inside, CruiseCabinAvailabilityState.Available),
            (CruiseCabinType.Balcony, CruiseCabinAvailabilityState.Available)), CruiseCabinEvidenceCoverage.Partial, previous.ObservedAt.AddHours(1));

        var changes = new CruiseCabinHistoryAnalyzer().Compare(previous, current);
        changes.Should().HaveCount(2);
        changes.Single(value => value.CabinType == CruiseCabinType.Inside).IsExplicitInventoryTransition.Should().BeFalse();
        changes.Single(value => value.CabinType == CruiseCabinType.Balcony).IsExplicitInventoryTransition.Should().BeTrue();
    }

    [Fact]
    public void Analyzer_RejectsMixedSeriesAndOrdersEqualTimesDeterministically()
    {
        var first = Observation(States(CruiseCabinAvailabilityState.Available), CruiseCabinEvidenceCoverage.Complete);
        var other = Observation(States(CruiseCabinAvailabilityState.Unavailable), CruiseCabinEvidenceCoverage.Complete,
            context: new CruiseCabinSearchContext(adultCount: 4));
        FluentActions.Invoking(() => new CruiseCabinHistoryAnalyzer().Compare(first, other)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AlertDetector_CreatesOneTypedCandidatePerPreferredExplicitTransition()
    {
        var previous = Observation(States(CruiseCabinAvailabilityState.Unavailable), CruiseCabinEvidenceCoverage.Complete);
        var current = Observation(States(CruiseCabinAvailabilityState.Unavailable,
            (CruiseCabinType.Balcony, CruiseCabinAvailabilityState.Available),
            (CruiseCabinType.Suite, CruiseCabinAvailabilityState.Available)), CruiseCabinEvidenceCoverage.Complete, previous.ObservedAt.AddHours(1));
        var candidates = new CruiseCabinAvailabilityAlertDetector(new()).Detect(previous, current, Saved(),
            new CruisePreferences(preferredCabins: [CruiseCabinType.Balcony, CruiseCabinType.Suite]), new());

        candidates.Should().HaveCount(2).And.OnlyContain(value => value.Type == CruiseAlertType.CabinAvailability);
        candidates.Select(value => value.EventKey).Should().OnlyHaveUniqueItems();
        ((CruiseCabinAvailabilityAlertDetails)candidates[0].Details).Direction.Should().Be(CruiseCabinAlertDirection.BecameAvailable);
    }

    [Fact]
    public void AlertDetector_SuppressesUnknownNonPreferredDismissedAndDisabledCases()
    {
        var previous = Observation(States(CruiseCabinAvailabilityState.Unknown, (CruiseCabinType.Inside, CruiseCabinAvailabilityState.Available)), CruiseCabinEvidenceCoverage.Partial);
        var current = Observation(States(CruiseCabinAvailabilityState.Unknown, (CruiseCabinType.Inside, CruiseCabinAvailabilityState.Unavailable)), CruiseCabinEvidenceCoverage.Partial, previous.ObservedAt.AddHours(1));
        var detector = new CruiseCabinAvailabilityAlertDetector(new());
        detector.Detect(previous, current, Saved(), new CruisePreferences(preferredCabins: [CruiseCabinType.Balcony]), new()).Should().BeEmpty();
        detector.Detect(previous, current, Saved(SavedCruiseStatus.Dismissed), new CruisePreferences(preferredCabins: [CruiseCabinType.Inside]), new()).Should().BeEmpty();
        detector.Detect(previous, current, Saved(), new CruisePreferences(preferredCabins: [CruiseCabinType.Inside]), new(cabinAvailabilityEnabled: false)).Should().BeEmpty();
    }

    public static CruiseCabinObservation Observation(IEnumerable<CruiseCabinState> states, CruiseCabinEvidenceCoverage coverage,
        DateTimeOffset? observedAt = null, string evidenceKey = "evidence", string? sourceReference = null, CruiseCabinSearchContext? context = null) =>
        new(new CruiseSailingKey("marella", "explorer", new DateOnly(2027, 12, 1), 7), new CruiseSource("tui", "TUI"),
            context ?? new CruiseCabinSearchContext(), coverage, states, observedAt ?? new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero), evidenceKey, sourceReference);

    public static CruiseCabinState[] States(CruiseCabinAvailabilityState defaultState, params (CruiseCabinType Type, CruiseCabinAvailabilityState State)[] overrides) =>
        Enum.GetValues<CruiseCabinType>().Select(type => new CruiseCabinState(type,
            overrides.FirstOrDefault(value => value.Type == type) is var found && overrides.Any(value => value.Type == type) ? found.State : defaultState)).ToArray();

    public static SavedCruise Saved(SavedCruiseStatus status = SavedCruiseStatus.Shortlisted) => new(
        new SavedCruiseSnapshot(new CruiseSailingKey("marella", "explorer", new DateOnly(2027, 12, 1), 7), "Cruise", "Marella", new CruisePrice(900m, "GBP", "per person"),
            new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero)), status);
}
