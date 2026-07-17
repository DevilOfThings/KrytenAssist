extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using RecordedHistory = KrytenApplication::KrytenAssist.Application.Cruises.CruiseRecordedHistory;
using LatestEvidence = KrytenApplication::KrytenAssist.Application.Cruises.CruiseLatestEvidence;
using RepositoryResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordResult;
using RepositoryState = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordState;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class CruiseRecordedHistoryTests
{
    [Fact]
    public void Constructor_OrdersDefensivelyAndRetainsLastSeenSeparately()
    {
        var first = CruiseHistoryApplicationTestData.Observation();
        var second = CruiseHistoryApplicationTestData.Observation(
            949m,
            CruiseHistoryApplicationTestData.FirstObserved.AddDays(7));
        var supplied = new List<CruiseObservation> { second, first };
        var lastSeen = second.ObservedAt.AddDays(1);

        var history = new RecordedHistory(CruiseSailingKey.From(first), lastSeen, supplied);
        supplied.Clear();

        Assert.Equal([first, second], history.Observations);
        Assert.Equal(lastSeen, history.LastSeenAt);
        Assert.Equal(new CruiseSource("tui", "TUI"), history.Source);
        Assert.Equal(2, history.Analyze(new CruisePriceHistoryAnalyzer()).ObservationCount);
    }

    [Fact]
    public void Constructor_UsesFingerprintForDeterministicEqualTimestampOrdering()
    {
        var one = CruiseHistoryApplicationTestData.Observation(988m, promotion: "One");
        var two = CruiseHistoryApplicationTestData.Observation(949m, promotion: "Two");

        var forward = new RecordedHistory(CruiseSailingKey.From(one), one.ObservedAt, [one, two]);
        var reversed = new RecordedHistory(CruiseSailingKey.From(one), one.ObservedAt, [two, one]);

        Assert.Equal(forward.Observations, reversed.Observations);
    }

    [Fact]
    public void Constructor_SupportsConsistentlyAbsentSource()
    {
        var captured = CruiseHistoryApplicationTestData.Observation();
        var observation = new CruiseObservation(captured.Snapshot, captured.ObservedAt);

        var history = new RecordedHistory(
            CruiseSailingKey.From(observation), observation.ObservedAt, [observation]);

        Assert.Null(history.Source);
    }

    [Fact]
    public void Constructor_RetainsExplicitLatestEvidenceSeparately()
    {
        var observation = CruiseHistoryApplicationTestData.Observation();
        var lastSeen = observation.ObservedAt.AddDays(2);
        var evidence = new LatestEvidence("new-package", "https://example.test/latest", lastSeen);

        var history = new RecordedHistory(
            CruiseSailingKey.From(observation),
            lastSeen,
            [observation],
            evidence);

        Assert.Same(evidence, history.LatestEvidence);
        Assert.Equal(observation.Snapshot.Offer.ProviderOfferId, history.Observations[0].Snapshot.Offer.ProviderOfferId);
        Assert.Throws<ArgumentException>(() => new RecordedHistory(
            CruiseSailingKey.From(observation),
            lastSeen,
            [observation],
            new LatestEvidence("future", null, lastSeen.AddTicks(1))));
    }

    [Fact]
    public void Constructor_RejectsEmptyMixedAndInvalidLastSeenInput()
    {
        var first = CruiseHistoryApplicationTestData.Observation();
        var differentSailing = CruiseHistoryApplicationTestData.Observation(ship: "Other Ship");
        var differentSource = CruiseHistoryApplicationTestData.Observation(
            source: new CruiseSource("other", "Other"));

        Assert.Throws<ArgumentException>(() =>
            new RecordedHistory(CruiseSailingKey.From(first), first.ObservedAt, []));
        Assert.Throws<ArgumentException>(() =>
            new RecordedHistory(CruiseSailingKey.From(first), first.ObservedAt, [first, differentSailing]));
        Assert.Throws<ArgumentException>(() =>
            new RecordedHistory(CruiseSailingKey.From(first), first.ObservedAt, [first, differentSource]));
        Assert.Throws<ArgumentException>(() =>
            new RecordedHistory(CruiseSailingKey.From(first), first.ObservedAt.AddTicks(-1), [first]));
    }

    [Fact]
    public void RepositoryResult_RejectsUnknownStateAndNullHistory()
    {
        var history = CruiseHistoryApplicationTestData.History(
            CruiseHistoryApplicationTestData.Observation());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RepositoryResult((RepositoryState)999, history));
        Assert.Throws<ArgumentNullException>(() =>
            new RepositoryResult(RepositoryState.AlreadyCurrent, null!));
    }
}
