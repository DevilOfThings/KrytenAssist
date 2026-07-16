using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class CruisePriceHistoryAnalyzerTests
{
    private readonly CruisePriceHistoryAnalyzer _analyzer = new();

    [Fact]
    public void SelectComparablePrice_PrefersOneDistinctGbpPerPersonPrice()
    {
        var snapshot = Snapshot(
            new CruisePrice(1975m, "GBP", "total based on 2 sharing"),
            new CruisePrice(988m, "GBP", " Per   Person "),
            new CruisePrice(988m, "GBP", "per person"));

        var selected = _analyzer.SelectComparablePrice(snapshot);

        selected.Should().Be(new CruisePrice(988m, "GBP", "per person"));
    }

    [Fact]
    public void SelectComparablePrice_RejectsConflictingGbpPerPersonPrices()
    {
        var snapshot = Snapshot(
            new CruisePrice(988m, "GBP", "per person"),
            new CruisePrice(949m, "GBP", "per person"));

        _analyzer.SelectComparablePrice(snapshot).Should().BeNull();
    }

    [Fact]
    public void SelectComparablePrice_AllowsOnlySingleFallbackWithExplicitBasis()
    {
        _analyzer.SelectComparablePrice(Snapshot(new CruisePrice(1200m, "USD", "per cabin")))
            .Should().Be(new CruisePrice(1200m, "USD", "per cabin"));
        _analyzer.SelectComparablePrice(Snapshot(new CruisePrice(1975m, "GBP", "total")))
            .Should().Be(new CruisePrice(1975m, "GBP", "total"));
        _analyzer.SelectComparablePrice(Snapshot(new CruisePrice(988m, "GBP")))
            .Should().BeNull();
        _analyzer.SelectComparablePrice(Snapshot(
                new CruisePrice(1975m, "GBP", "total"),
                new CruisePrice(988m, "USD", "per person")))
            .Should().BeNull();
    }

    [Fact]
    public void Analyze_FirstObservationBuildsCompleteSummary()
    {
        var observation = CruiseHistoryTestData.Observation();

        var summary = _analyzer.Analyze([observation]);

        summary.SailingKey.Should().Be(CruiseSailingKey.From(observation));
        summary.Source.Should().Be(new CruiseSource("tui", "TUI"));
        summary.FirstObservedAt.Should().Be(CruiseHistoryTestData.FirstObserved);
        summary.LastObservedAt.Should().Be(CruiseHistoryTestData.FirstObserved);
        summary.ObservationCount.Should().Be(1);
        summary.CurrentPrice.Should().Be(new CruisePrice(988m, "GBP", "per person"));
        summary.LowestPrice.Should().Be(summary.CurrentPrice);
        summary.HighestPrice.Should().Be(summary.CurrentPrice);
        summary.Movement.Direction.Should().Be(CruisePriceTrendDirection.FirstObservation);
        summary.Movement.Delta.Should().BeNull();
    }

    [Theory]
    [InlineData(949, CruisePriceTrendDirection.Lower, 39)]
    [InlineData(1020, CruisePriceTrendDirection.Higher, 32)]
    [InlineData(988, CruisePriceTrendDirection.Unchanged, 0)]
    public void Analyze_ReportsMovementAgainstImmediatelyPreviousObservation(
        decimal currentAmount,
        CruisePriceTrendDirection direction,
        decimal delta)
    {
        var first = CruiseHistoryTestData.Observation();
        var current = CruiseHistoryTestData.Observation(
            perPersonPrice: currentAmount,
            observedAt: CruiseHistoryTestData.FirstObserved.AddDays(7),
            promotion: "Changed promotion");

        var summary = _analyzer.Analyze([current, first]);

        summary.CurrentPrice!.Amount.Should().Be(currentAmount);
        summary.Movement.Direction.Should().Be(direction);
        summary.Movement.PreviousPrice!.Amount.Should().Be(988m);
        summary.Movement.Delta.Should().Be(delta);
    }

    [Fact]
    public void Analyze_UsesMatchingSeriesForCurrentLowestAndHighest()
    {
        var first = CruiseHistoryTestData.Observation(
            perPersonPrice: 988m,
            observedAt: CruiseHistoryTestData.FirstObserved);
        var lowest = CruiseHistoryTestData.Observation(
            perPersonPrice: 900m,
            observedAt: CruiseHistoryTestData.FirstObserved.AddDays(7));
        var latest = CruiseHistoryTestData.Observation(
            perPersonPrice: 949m,
            observedAt: CruiseHistoryTestData.FirstObserved.AddDays(14));

        var summary = _analyzer.Analyze([latest, first, lowest]);

        summary.ObservationCount.Should().Be(3);
        summary.CurrentPrice!.Amount.Should().Be(949m);
        summary.LowestPrice!.Amount.Should().Be(900m);
        summary.HighestPrice!.Amount.Should().Be(988m);
        summary.Movement.Direction.Should().Be(CruisePriceTrendDirection.Higher);
        summary.Movement.Delta.Should().Be(49m);
    }

    [Fact]
    public void Analyze_ExcludesIncompatibleHistoricalSeriesAndDoesNotSkipForTrend()
    {
        var first = CruiseHistoryTestData.Observation(
            observedAt: CruiseHistoryTestData.FirstObserved,
            prices: [new CruisePrice(700m, "USD", "per person")]);
        var incompatiblePrevious = CruiseHistoryTestData.Observation(
            observedAt: CruiseHistoryTestData.FirstObserved.AddDays(7),
            prices: [new CruisePrice(1975m, "GBP", "total")]);
        var latest = CruiseHistoryTestData.Observation(
            perPersonPrice: 949m,
            observedAt: CruiseHistoryTestData.FirstObserved.AddDays(14));

        var summary = _analyzer.Analyze([first, incompatiblePrevious, latest]);

        summary.LowestPrice!.Amount.Should().Be(949m);
        summary.HighestPrice!.Amount.Should().Be(949m);
        summary.Movement.Direction.Should().Be(CruisePriceTrendDirection.Unavailable);
        summary.Movement.Delta.Should().BeNull();
    }

    [Fact]
    public void Analyze_LatestUnavailablePriceProducesUnavailableHeadlineHistory()
    {
        var first = CruiseHistoryTestData.Observation();
        var latest = CruiseHistoryTestData.Observation(
            observedAt: CruiseHistoryTestData.FirstObserved.AddDays(7),
            prices: [new CruisePrice(988m, "GBP")]);

        var summary = _analyzer.Analyze([first, latest]);

        summary.ObservationCount.Should().Be(2);
        summary.CurrentPrice.Should().BeNull();
        summary.LowestPrice.Should().BeNull();
        summary.HighestPrice.Should().BeNull();
        summary.Movement.Direction.Should().Be(CruisePriceTrendDirection.Unavailable);
    }

    [Fact]
    public void Analyze_PreservesExactTimestampOffsetsAndIsInputOrderIndependent()
    {
        var firstTime = new DateTimeOffset(2026, 7, 16, 10, 30, 0, TimeSpan.FromHours(1));
        var lastTime = new DateTimeOffset(2026, 7, 23, 9, 15, 0, TimeSpan.FromHours(-4));
        var first = CruiseHistoryTestData.Observation(observedAt: firstTime);
        var last = CruiseHistoryTestData.Observation(perPersonPrice: 949m, observedAt: lastTime);

        var forward = _analyzer.Analyze([first, last]);
        var reversed = _analyzer.Analyze([last, first]);

        forward.Should().Be(reversed);
        forward.FirstObservedAt.Should().Be(firstTime);
        forward.LastObservedAt.Should().Be(lastTime);
    }

    [Fact]
    public void Analyze_EqualTimestampsUseDeterministicMeaningfulTieBreaker()
    {
        var one = CruiseHistoryTestData.Observation(perPersonPrice: 988m);
        var two = CruiseHistoryTestData.Observation(perPersonPrice: 949m, promotion: "Another promotion");

        var forward = _analyzer.Analyze([one, two]);
        var reversed = _analyzer.Analyze([two, one]);

        forward.Should().Be(reversed);
    }

    [Fact]
    public void Analyze_SupportsConsistentlyAbsentRetailSource()
    {
        var sourceObservation = CruiseHistoryTestData.Observation();
        var first = new CruiseObservation(sourceObservation.Snapshot, CruiseHistoryTestData.FirstObserved);
        var second = new CruiseObservation(
            CruiseHistoryTestData.Observation(perPersonPrice: 949m).Snapshot,
            CruiseHistoryTestData.FirstObserved.AddDays(7));

        var summary = _analyzer.Analyze([first, second]);

        summary.Source.Should().BeNull();
        summary.ObservationCount.Should().Be(2);
    }

    [Fact]
    public void Analyze_RejectsEmptyMixedSailingAndMixedSourceInput()
    {
        var empty = () => _analyzer.Analyze([]);
        var mixedSailing = () => _analyzer.Analyze([
            CruiseHistoryTestData.Observation(),
            CruiseHistoryTestData.Observation(shipName: "Other Ship")
        ]);
        var mixedSource = () => _analyzer.Analyze([
            CruiseHistoryTestData.Observation(),
            CruiseHistoryTestData.Observation(source: new CruiseSource("other", "Other"))
        ]);

        empty.Should().Throw<ArgumentException>();
        mixedSailing.Should().Throw<ArgumentException>();
        mixedSource.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Analyze_CopiesInputBeforeProducingSummary()
    {
        var input = new List<CruiseObservation> { CruiseHistoryTestData.Observation() };
        var summary = _analyzer.Analyze(input);

        input.Add(CruiseHistoryTestData.Observation(
            perPersonPrice: 1m,
            observedAt: CruiseHistoryTestData.FirstObserved.AddDays(1)));

        summary.ObservationCount.Should().Be(1);
        summary.CurrentPrice!.Amount.Should().Be(988m);
    }

    private static CruiseSnapshot Snapshot(params CruisePrice[] prices) =>
        new(
            new CruiseOffer(
                new CruiseProvider("marella", "Marella Cruises"),
                "fictional",
                "Atlantic Discovery",
                "Marella Example",
                new DateOnly(2026, 12, 18),
                7),
            prices);
}
