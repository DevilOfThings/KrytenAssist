extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using RecordUseCase = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseObservation;
using RecordStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRecordStatus;
using RepositoryResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordResult;
using RepositoryState = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordState;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class RecordCruiseObservationTests
{
    [Fact]
    public async Task ExecuteAsync_PassesExactDomainEvidenceOnceAndMapsFirstObservation()
    {
        var observation = CruiseHistoryApplicationTestData.Observation();
        var history = CruiseHistoryApplicationTestData.History(observation);
        var repository = new FakeCruiseObservationRepository
        {
            RecordResult = new RepositoryResult(RepositoryState.FirstObservationRecorded, history)
        };
        var useCase = new RecordUseCase(repository, new CruisePriceHistoryAnalyzer());
        using var cancellation = new CancellationTokenSource();

        var result = await useCase.ExecuteAsync(observation, cancellation.Token);

        Assert.Equal(1, repository.RecordCalls);
        Assert.Equal(CruiseSailingKey.From(observation), repository.RecordedKey);
        Assert.Equal(CruiseObservationFingerprint.From(observation), repository.RecordedFingerprint);
        Assert.Same(observation, repository.RecordedObservation);
        Assert.Equal(cancellation.Token, repository.RecordedToken);
        Assert.Equal(RecordStatus.FirstObservationRecorded, result.Status);
        Assert.True(result.SnapshotInserted);
        Assert.Equal(history.LastSeenAt, result.LastSeenAt);
        Assert.Equal(1, result.Summary?.ObservationCount);
    }

    [Theory]
    [InlineData(RepositoryState.ChangedObservationRecorded, RecordStatus.ChangedObservationRecorded, true)]
    [InlineData(RepositoryState.AlreadyCurrent, RecordStatus.AlreadyCurrent, false)]
    public async Task ExecuteAsync_MapsChangedAndAlreadyCurrent(
        RepositoryState repositoryState,
        RecordStatus expectedStatus,
        bool snapshotInserted)
    {
        var first = CruiseHistoryApplicationTestData.Observation();
        var changed = CruiseHistoryApplicationTestData.Observation(
            observedAt: CruiseHistoryApplicationTestData.FirstObserved.AddDays(7),
            promotion: "Changed promotion");
        var history = CruiseHistoryApplicationTestData.History(first, changed);
        var repository = new FakeCruiseObservationRepository
        {
            RecordResult = new RepositoryResult(repositoryState, history)
        };

        var result = await new RecordUseCase(repository, new CruisePriceHistoryAnalyzer())
            .ExecuteAsync(changed);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(snapshotInserted, result.SnapshotInserted);
        Assert.Equal(
            CruisePriceTrendDirection.Unchanged,
            result.Summary?.Movement.Direction);
    }

    [Fact]
    public async Task ExecuteAsync_PreCancellationAvoidsRepository()
    {
        var repository = new FakeCruiseObservationRepository();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = await new RecordUseCase(repository, new CruisePriceHistoryAnalyzer())
            .ExecuteAsync(CruiseHistoryApplicationTestData.Observation(), cancellation.Token);

        Assert.Equal(RecordStatus.Cancelled, result.Status);
        Assert.Equal(0, repository.RecordCalls);
        Assert.Null(result.Summary);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_MapsRepositoryCancellationAndFailureSafely(bool cancellation)
    {
        var repository = new FakeCruiseObservationRepository
        {
            RecordException = cancellation
                ? new OperationCanceledException()
                : new InvalidOperationException("secret database path")
        };

        var result = await new RecordUseCase(repository, new CruisePriceHistoryAnalyzer())
            .ExecuteAsync(CruiseHistoryApplicationTestData.Observation());

        Assert.Equal(cancellation ? RecordStatus.Cancelled : RecordStatus.Failed, result.Status);
        Assert.DoesNotContain("secret", result.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.Summary);
    }

    [Fact]
    public async Task ConstructorAndExecuteAsync_RejectNullRequiredValues()
    {
        var repository = new FakeCruiseObservationRepository();

        Assert.Throws<ArgumentNullException>(() => new RecordUseCase(null!, new CruisePriceHistoryAnalyzer()));
        Assert.Throws<ArgumentNullException>(() => new RecordUseCase(repository, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            new RecordUseCase(repository, new CruisePriceHistoryAnalyzer()).ExecuteAsync(null!));
    }
}
