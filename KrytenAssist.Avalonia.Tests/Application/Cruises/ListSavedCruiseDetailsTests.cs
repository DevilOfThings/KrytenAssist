extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using ListStatus = KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseDetailsListStatus;
using ListUseCase = KrytenApplication::KrytenAssist.Application.Cruises.ListSavedCruiseDetails;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class ListSavedCruiseDetailsTests
{
    [Fact]
    public async Task Joins_all_source_histories_by_sailing_and_selects_latest_recorded_observation()
    {
        var early = Observation(900, new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero), new CruiseSource("tui", "TUI"));
        var latest = Observation(850, new DateTimeOffset(2026, 7, 3, 9, 0, 0, TimeSpan.Zero), new CruiseSource("other", "Other"));
        var savedRepository = Saved(new SavedCruise(Snapshot(CruiseSailingKey.From(early))));
        var ships = new FakeFavouriteCruiseShipRepository();
        ships.Items.Add(CruiseShipKey.From(CruiseSailingKey.From(early)));
        var observations = new FakeCruiseObservationRepository
        {
            ListResult = [CruiseHistoryApplicationTestData.History(early), CruiseHistoryApplicationTestData.History(latest)]
        };

        var result = await new ListUseCase(savedRepository, ships, observations, new CruisePriceHistoryAnalyzer()).ExecuteAsync();

        result.Status.Should().Be(ListStatus.Success);
        var detail = result.Details.Should().ContainSingle().Subject;
        detail.IsFavouriteShip.Should().BeTrue();
        detail.Histories.Should().HaveCount(2);
        detail.RecordedSourceCount.Should().Be(2);
        detail.RecordedObservationCount.Should().Be(2);
        detail.LatestRecordedObservation.Should().BeSameAs(latest);
        observations.RecordCalls.Should().Be(0);
    }

    [Fact]
    public async Task Unrelated_history_is_not_joined_and_saved_snapshot_remains_useful()
    {
        var savedObservation = Observation();
        var unrelated = CruiseHistoryApplicationTestData.Observation(ship: "Explorer");
        var savedRepository = Saved(new SavedCruise(Snapshot(CruiseSailingKey.From(savedObservation))));
        var observations = new FakeCruiseObservationRepository
        {
            ListResult = [CruiseHistoryApplicationTestData.History(unrelated)]
        };

        var result = await new ListUseCase(savedRepository, new FakeFavouriteCruiseShipRepository(), observations, new CruisePriceHistoryAnalyzer()).ExecuteAsync();

        var detail = result.Details.Should().ContainSingle().Subject;
        detail.HasRecordedHistory.Should().BeFalse();
        detail.LatestRecordedObservation.Should().BeNull();
        detail.SavedCruise.Snapshot.DisplayedPrice.Amount.Should().Be(999);
    }

    [Fact]
    public async Task Cancellation_and_failures_are_controlled()
    {
        var cancelled = await new ListUseCase(
            new FakeSavedCruiseRepository(), new FakeFavouriteCruiseShipRepository(),
            new FakeCruiseObservationRepository(), new CruisePriceHistoryAnalyzer())
            .ExecuteAsync(new CancellationToken(true));
        cancelled.Status.Should().Be(ListStatus.Cancelled);

        var failing = new FakeSavedCruiseRepository { Exception = new InvalidOperationException() };
        var failed = await new ListUseCase(
            failing, new FakeFavouriteCruiseShipRepository(),
            new FakeCruiseObservationRepository(), new CruisePriceHistoryAnalyzer()).ExecuteAsync();
        failed.Status.Should().Be(ListStatus.Failed);
    }

    private static CruiseObservation Observation(
        decimal price = 999,
        DateTimeOffset? observedAt = null,
        CruiseSource? source = null) =>
        CruiseHistoryApplicationTestData.Observation(
            price,
            observedAt ?? CruiseHistoryApplicationTestData.FirstObserved,
            ship: "Voyager",
            departure: new DateOnly(2027, 8, 2),
            source: source ?? new CruiseSource("tui", "TUI"));

    private static SavedCruiseSnapshot Snapshot(CruiseSailingKey key) =>
        new(key, "Mediterranean Escape", "Marella Cruises", new CruisePrice(999, "GBP", "per person"),
            new DateTimeOffset(2026, 7, 18, 9, 0, 0, TimeSpan.Zero));

    private static FakeSavedCruiseRepository Saved(SavedCruise savedCruise)
    {
        var repository = new FakeSavedCruiseRepository();
        repository.Items.Add(savedCruise.SailingKey, savedCruise);
        return repository;
    }
}
