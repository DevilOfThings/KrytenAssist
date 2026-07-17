extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using System.Data.Common;
using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RepositoryContract = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseObservationRepository;
using Repository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseObservationRepository;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class CruiseObservationCancellationTests
{
    [Fact]
    public async Task CancellationAfterSave_RollsBackMutationAndAllowsFreshRetry()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        var first = CruisePersistenceTestData.Observation();
        var changed = CruisePersistenceTestData.Observation(
            amount: 949m,
            observedAt: first.ObservedAt.AddDays(1),
            providerOfferId: "changed-package",
            sourceReference: "https://example.test/changed");
        await using (var seedContext = database.CreateContext())
        {
            await RecordAsync(new Repository(seedContext), first);
        }

        var interceptor = new CancelAfterSaveInterceptor();
        using var cancellation = new CancellationTokenSource();
        await using (var cancelledContext = database.CreateContext(interceptor))
        {
            var recording = RecordAsync(new Repository(cancelledContext), changed, cancellation.Token);
            await interceptor.Reached;
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => recording);
        }

        await using (var verification = database.CreateContext())
        {
            Assert.Equal(1, await verification.CruiseObservations.CountAsync());
            Assert.Equal(2, await verification.CruiseObservationPrices.CountAsync());
            var history = await new Repository(verification).GetAsync(
                CruiseSailingKey.From(first),
                first.Source);
            Assert.NotNull(history);
            Assert.Equal(first.Snapshot.Offer.ProviderOfferId, history.LatestEvidence.ProviderOfferId);
        }

        await using (var retryContext = database.CreateContext())
        {
            var retried = await RecordAsync(new Repository(retryContext), changed);
            Assert.Equal(2, retried.History.Observations.Count);
            Assert.Equal("changed-package", retried.History.LatestEvidence.ProviderOfferId);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancellationDuringQuery_PropagatesWithoutMutation(bool get)
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        var observation = CruisePersistenceTestData.Observation();
        await using (var seedContext = database.CreateContext())
        {
            await RecordAsync(new Repository(seedContext), observation);
        }

        var interceptor = new CancelDuringReadInterceptor();
        using var cancellation = new CancellationTokenSource();
        await using var context = database.CreateContext(interceptor);
        RepositoryContract repository = new Repository(context);
        Task operation = get
            ? repository.GetAsync(CruiseSailingKey.From(observation), observation.Source, cancellation.Token)
            : repository.ListAsync(cancellation.Token);

        await interceptor.Reached;
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        await using var verification = database.CreateContext();
        Assert.Equal(1, await verification.CruiseHistories.CountAsync());
        Assert.Equal(1, await verification.CruiseObservations.CountAsync());
    }

    private static Task<KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordResult> RecordAsync(
        RepositoryContract repository,
        CruiseObservation observation,
        CancellationToken cancellationToken = default) =>
        repository.RecordAsync(
            CruiseSailingKey.From(observation),
            CruiseObservationFingerprint.From(observation),
            observation,
            cancellationToken);

    private sealed class CancelAfterSaveInterceptor : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _reached = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Reached => _reached.Task;

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            _reached.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return result;
        }
    }

    private sealed class CancelDuringReadInterceptor : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _reached = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Reached => _reached.Task;

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            _reached.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return result;
        }
    }
}
