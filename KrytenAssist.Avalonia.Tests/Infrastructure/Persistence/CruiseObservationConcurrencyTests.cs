extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using RecordResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordResult;
using RecordState = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordState;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class CruiseObservationConcurrencyTests
{
    [Fact]
    public async Task ConcurrentFirstObservation_CreatesOneSnapshotWithControlledOutcomes()
    {
        await using var database = new CruisePersistenceFileDatabase();
        await database.MigrateAsync();
        var observation = CruisePersistenceTestData.Observation();

        var results = await RecordConcurrentlyAsync(database, observation, observation);

        Assert.Equal(
            [RecordState.FirstObservationRecorded, RecordState.AlreadyCurrent],
            results.Select(result => result.State).Order().ToArray());
        await AssertStoredAggregateAsync(database, expectedHistories: 1, expectedObservations: 1, expectedPrices: 2);
    }

    [Fact]
    public async Task ConcurrentIdenticalChange_CreatesOneChangedSnapshot()
    {
        await using var database = new CruisePersistenceFileDatabase();
        await database.MigrateAsync();
        var first = CruisePersistenceTestData.Observation();
        var changed = CruisePersistenceTestData.Observation(
            amount: 949m,
            observedAt: first.ObservedAt.AddDays(1));
        await RecordOnceAsync(database, first);

        var results = await RecordConcurrentlyAsync(database, changed, changed);

        Assert.Equal(
            [RecordState.ChangedObservationRecorded, RecordState.AlreadyCurrent],
            results.Select(result => result.State).Order().ToArray());
        await AssertStoredAggregateAsync(database, expectedHistories: 1, expectedObservations: 2, expectedPrices: 4);
    }

    [Fact]
    public async Task ConcurrentDistinctChanges_RetainBothWithoutOrphans()
    {
        await using var database = new CruisePersistenceFileDatabase();
        await database.MigrateAsync();
        var first = CruisePersistenceTestData.Observation();
        var lower = CruisePersistenceTestData.Observation(
            amount: 949m,
            observedAt: first.ObservedAt.AddDays(1));
        var higher = CruisePersistenceTestData.Observation(
            amount: 1025m,
            observedAt: first.ObservedAt.AddDays(2),
            promotion: "Different promotion");
        await RecordOnceAsync(database, first);

        var results = await RecordConcurrentlyAsync(database, lower, higher);

        Assert.All(results, result => Assert.Equal(RecordState.ChangedObservationRecorded, result.State));
        await using var context = database.CreateContext();
        var sequences = await context.CruiseObservations
            .OrderBy(item => item.Sequence)
            .Select(item => item.Sequence)
            .ToArrayAsync();
        Assert.Equal([1, 2, 3], sequences);
        await AssertStoredAggregateAsync(database, expectedHistories: 1, expectedObservations: 3, expectedPrices: 6);
    }

    private static async Task<RecordResult[]> RecordConcurrentlyAsync(
        CruisePersistenceFileDatabase database,
        CruiseObservation first,
        CruiseObservation second)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstTask = RecordAfterGateAsync(database, first, gate.Task);
        var secondTask = RecordAfterGateAsync(database, second, gate.Task);
        gate.SetResult();
        return await Task.WhenAll(firstTask, secondTask);
    }

    private static async Task<RecordResult> RecordAfterGateAsync(
        CruisePersistenceFileDatabase database,
        CruiseObservation observation,
        Task gate)
    {
        await using var context = database.CreateContext();
        var repository = database.CreateRepository(context);
        await gate;
        return await repository.RecordAsync(
            CruiseSailingKey.From(observation),
            CruiseObservationFingerprint.From(observation),
            observation);
    }

    private static async Task RecordOnceAsync(
        CruisePersistenceFileDatabase database,
        CruiseObservation observation)
    {
        await using var context = database.CreateContext();
        await database.CreateRepository(context).RecordAsync(
            CruiseSailingKey.From(observation),
            CruiseObservationFingerprint.From(observation),
            observation);
    }

    private static async Task AssertStoredAggregateAsync(
        CruisePersistenceFileDatabase database,
        int expectedHistories,
        int expectedObservations,
        int expectedPrices)
    {
        await using var context = database.CreateContext();
        Assert.Equal(expectedHistories, await context.CruiseHistories.CountAsync());
        Assert.Equal(expectedObservations, await context.CruiseObservations.CountAsync());
        Assert.Equal(expectedPrices, await context.CruiseObservationPrices.CountAsync());
        Assert.Equal(expectedPrices, await context.CruiseObservationPrices.CountAsync(price => price.CruiseObservationId > 0));
    }
}
