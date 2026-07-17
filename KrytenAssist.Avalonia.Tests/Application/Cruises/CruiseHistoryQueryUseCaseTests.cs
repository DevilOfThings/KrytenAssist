extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using GetUseCase = KrytenApplication::KrytenAssist.Application.Cruises.GetCruiseHistory;
using QueryStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseHistoryQueryStatus;
using ListUseCase = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseHistories;
using ListStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseHistoryListStatus;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class CruiseHistoryQueryUseCaseTests
{
    [Fact]
    public async Task Get_PassesExactIdentityAndReturnsFoundDetails()
    {
        var observation = CruiseHistoryApplicationTestData.Observation();
        var history = CruiseHistoryApplicationTestData.History(observation);
        var repository = new FakeCruiseObservationRepository { GetResult = history };
        var key = CruiseSailingKey.From(observation);
        var source = observation.Source;

        var result = await new GetUseCase(repository, new CruisePriceHistoryAnalyzer())
            .ExecuteAsync(key, source);

        Assert.Equal(1, repository.GetCalls);
        Assert.Same(key, repository.RequestedKey);
        Assert.Same(source, repository.RequestedSource);
        Assert.Equal(QueryStatus.Found, result.Status);
        Assert.Same(history, result.Details?.History);
        Assert.Equal(history.LastSeenAt, result.Details?.LastSeenAt);
    }

    [Fact]
    public async Task Get_ReturnsControlledNotFoundCancellationAndFailure()
    {
        var observation = CruiseHistoryApplicationTestData.Observation();
        var key = CruiseSailingKey.From(observation);
        var notFoundRepository = new FakeCruiseObservationRepository();
        var notFound = await new GetUseCase(notFoundRepository, new CruisePriceHistoryAnalyzer())
            .ExecuteAsync(key, observation.Source);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = await new GetUseCase(notFoundRepository, new CruisePriceHistoryAnalyzer())
            .ExecuteAsync(key, observation.Source, cancellation.Token);
        var failedRepository = new FakeCruiseObservationRepository
        {
            GetException = new InvalidOperationException("secret sql")
        };
        var failed = await new GetUseCase(failedRepository, new CruisePriceHistoryAnalyzer())
            .ExecuteAsync(key, observation.Source);

        Assert.Equal(QueryStatus.NotFound, notFound.Status);
        Assert.Null(notFound.Details);
        Assert.Equal(QueryStatus.Cancelled, cancelled.Status);
        Assert.Equal(1, notFoundRepository.GetCalls);
        Assert.Equal(QueryStatus.Failed, failed.Status);
        Assert.DoesNotContain("sql", failed.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task List_ReturnsEmptySuccessAndCallsRepositoryOnce()
    {
        var repository = new FakeCruiseObservationRepository();

        var result = await new ListUseCase(repository, new CruisePriceHistoryAnalyzer())
            .ExecuteAsync();

        Assert.Equal(ListStatus.Success, result.Status);
        Assert.Empty(result.Histories);
        Assert.Equal(1, repository.ListCalls);
    }

    [Fact]
    public async Task List_SummarizesAndOrdersPastAndFutureHistoriesDeterministically()
    {
        var later = CruiseHistoryApplicationTestData.Observation(
            departure: new DateOnly(2027, 1, 8),
            observedAt: CruiseHistoryApplicationTestData.FirstObserved.AddDays(1));
        var earlier = CruiseHistoryApplicationTestData.Observation(
            departure: new DateOnly(2025, 1, 8),
            title: "Past sailing");
        var repositoryList = new List<KrytenApplication::KrytenAssist.Application.Cruises.CruiseRecordedHistory>
        {
            CruiseHistoryApplicationTestData.History(later),
            CruiseHistoryApplicationTestData.History(earlier)
        };
        var repository = new FakeCruiseObservationRepository { ListResult = repositoryList };

        var result = await new ListUseCase(repository, new CruisePriceHistoryAnalyzer())
            .ExecuteAsync();
        repositoryList.Clear();

        Assert.Equal(ListStatus.Success, result.Status);
        Assert.Equal(2, result.Histories.Count);
        Assert.Equal(new DateOnly(2025, 1, 8), result.Histories[0].Summary.SailingKey.DepartureDate);
        Assert.Equal(new DateOnly(2027, 1, 8), result.Histories[1].Summary.SailingKey.DepartureDate);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task List_MapsCancellationAndFailureWithoutPartialItems(bool cancellation)
    {
        var repository = new FakeCruiseObservationRepository
        {
            ListException = cancellation
                ? new OperationCanceledException()
                : new InvalidOperationException("secret database")
        };

        var result = await new ListUseCase(repository, new CruisePriceHistoryAnalyzer())
            .ExecuteAsync();

        Assert.Equal(cancellation ? ListStatus.Cancelled : ListStatus.Failed, result.Status);
        Assert.Empty(result.Histories);
        Assert.DoesNotContain("secret", result.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
