extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using CabinRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseCabinObservationRepository;
using CabinHistory = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRecordedHistory;
using CabinRepositoryResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRepositoryRecordResult;
using CabinRepositoryState = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRepositoryRecordState;
using CabinStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinOperationStatus;
using RecordCabin = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseCabinObservation;
using GetCabin = KrytenApplication::KrytenAssist.Application.Cruises.GetCruiseCabinHistory;
using ListCabins = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseCabinHistories;
using CaptureResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinCaptureResult;
using CaptureStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinCaptureStatus;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class CruiseCabinApplicationTests
{
    [Theory]
    [InlineData(CabinRepositoryState.FirstObservationRecorded, CabinStatus.FirstObservationRecorded)]
    [InlineData(CabinRepositoryState.ChangedObservationRecorded, CabinStatus.ChangedObservationRecorded)]
    [InlineData(CabinRepositoryState.AlreadyCurrent, CabinStatus.AlreadyCurrent)]
    public async Task Record_MapsRepositoryStateAndPreservesLastSeen(CabinRepositoryState repositoryState, CabinStatus expected)
    {
        var observation = Observation();
        var lastSeen = observation.ObservedAt.AddHours(2);
        var repository = new FakeCabinRepository
        {
            RecordResult = new(repositoryState, new CabinHistory(observation.SeriesKey, lastSeen, [observation]))
        };

        var result = await new RecordCabin(repository, new()).ExecuteAsync(observation);

        result.Status.Should().Be(expected);
        result.Summary!.LastSeenAt.Should().Be(lastSeen);
    }

    [Fact]
    public async Task UseCases_ContainCancellationAndRepositoryFailures()
    {
        var repository = new FakeCabinRepository { Exception = new InvalidOperationException() };
        (await new RecordCabin(repository, new()).ExecuteAsync(Observation())).Status.Should().Be(CabinStatus.Failed);
        (await new GetCabin(repository, new()).ExecuteAsync(Observation().SeriesKey)).Status.Should().Be(CabinStatus.Failed);
        (await new ListCabins(repository, new()).ExecuteAsync()).Status.Should().Be(CabinStatus.Failed);

        using var source = new CancellationTokenSource(); source.Cancel();
        (await new RecordCabin(repository, new()).ExecuteAsync(Observation(), source.Token)).Status.Should().Be(CabinStatus.Cancelled);
    }

    [Fact]
    public async Task GetAndList_ReturnDeterministicHistoryProjections()
    {
        var laterSailing = Observation(ship: "Zulu", departure: new DateOnly(2028, 1, 1));
        var earlierSailing = Observation(ship: "Alpha", departure: new DateOnly(2027, 1, 1));
        var repository = new FakeCabinRepository
        {
            GetResult = new(earlierSailing.SeriesKey, earlierSailing.ObservedAt, [earlierSailing]),
            ListResult = [new(laterSailing.SeriesKey, laterSailing.ObservedAt, [laterSailing]), new(earlierSailing.SeriesKey, earlierSailing.ObservedAt, [earlierSailing])]
        };

        (await new GetCabin(repository, new()).ExecuteAsync(earlierSailing.SeriesKey)).Status.Should().Be(CabinStatus.Found);
        var listed = await new ListCabins(repository, new()).ExecuteAsync();
        listed.Histories.Select(value => value.History.SeriesKey).Should().Equal(earlierSailing.SeriesKey, laterSailing.SeriesKey);
    }

    [Fact]
    public void CaptureResult_SeparatesReadyIncompleteAndControlledFailures()
    {
        CaptureResult.Ready(Observation()).Status.Should().Be(CaptureStatus.Ready);
        CaptureResult.Incomplete(["cabinStates"], "Missing cabin evidence").MissingFields.Should().Equal("cabinStates");
        CaptureResult.Unsupported("Unsupported page").Observation.Should().BeNull();
        FluentActions.Invoking(() => CaptureResult.Incomplete([], "Missing")).Should().Throw<ArgumentException>();
    }

    private static CruiseCabinObservation Observation(CruiseCabinAvailabilityState state = CruiseCabinAvailabilityState.Available,
        string ship = "Explorer", DateOnly? departure = null, DateTimeOffset? observedAt = null)
    {
        var states = Enum.GetValues<CruiseCabinType>().Select(type => new CruiseCabinState(type, state));
        return new(new CruiseSailingKey("marella", ship, departure ?? new DateOnly(2027, 12, 1), 7),
            new CruiseSource("tui", "TUI"), new CruiseCabinSearchContext(), CruiseCabinEvidenceCoverage.Complete,
            states, observedAt ?? new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero), "evidence");
    }

    private sealed class FakeCabinRepository : CabinRepository
    {
        public CabinRepositoryResult? RecordResult { get; init; }
        public CabinHistory? GetResult { get; init; }
        public IReadOnlyList<CabinHistory> ListResult { get; init; } = [];
        public Exception? Exception { get; init; }
        public Task<CabinRepositoryResult> RecordAsync(CruiseCabinObservation observation, CancellationToken cancellationToken = default) =>
            Exception is null ? Task.FromResult(RecordResult!) : Task.FromException<CabinRepositoryResult>(Exception);
        public Task<CabinHistory?> GetAsync(string seriesKey, CancellationToken cancellationToken = default) =>
            Exception is null ? Task.FromResult(GetResult) : Task.FromException<CabinHistory?>(Exception);
        public Task<IReadOnlyList<CabinHistory>> ListAsync(CancellationToken cancellationToken = default) =>
            Exception is null ? Task.FromResult(ListResult) : Task.FromException<IReadOnlyList<CabinHistory>>(Exception);
    }
}
