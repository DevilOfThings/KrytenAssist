extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using Repository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseCabinObservationRepository;
using RecordState = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRepositoryRecordState;
using RecordResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRepositoryRecordResult;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class CruiseCabinConcurrencyTests
{
    [Fact]
    public async Task ConcurrentFirstObservation_CreatesOneSnapshotWithControlledOutcomes()
    {
        await using var database = new CruisePersistenceFileDatabase(); await database.MigrateAsync();
        var observation = CruiseCabinPersistenceTestData.Observation();
        var results = await RecordTogether(database, observation, observation);
        Assert.Equal([RecordState.FirstObservationRecorded, RecordState.AlreadyCurrent], results.Select(x => x.State).Order().ToArray());
        await AssertCounts(database, 1, 1, 5);
    }

    [Fact]
    public async Task ConcurrentIdenticalChange_CreatesOneChangedSnapshot()
    {
        await using var database = new CruisePersistenceFileDatabase(); await database.MigrateAsync();
        var first = CruiseCabinPersistenceTestData.Complete(CruiseCabinAvailabilityState.Available);
        var changed = CruiseCabinPersistenceTestData.Complete(CruiseCabinAvailabilityState.Unavailable, first.ObservedAt.AddHours(1));
        await RecordOnce(database, first);
        var results = await RecordTogether(database, changed, changed);
        Assert.Equal([RecordState.ChangedObservationRecorded, RecordState.AlreadyCurrent], results.Select(x => x.State).Order().ToArray());
        await AssertCounts(database, 1, 2, 10);
    }

    [Fact]
    public async Task ConcurrentDistinctChanges_RetainBothWithContiguousSequences()
    {
        await using var database = new CruisePersistenceFileDatabase(); await database.MigrateAsync();
        var first = CruiseCabinPersistenceTestData.Observation();
        var balcony = CruiseCabinPersistenceTestData.Observation(knownCabin: CruiseCabinType.Balcony,
            observedAt: first.ObservedAt.AddHours(1), evidenceKey: "balcony");
        var suite = CruiseCabinPersistenceTestData.Observation(knownCabin: CruiseCabinType.Suite,
            observedAt: first.ObservedAt.AddHours(2), evidenceKey: "suite");
        await RecordOnce(database, first);
        var results = await RecordTogether(database, balcony, suite);
        Assert.All(results, value => Assert.Equal(RecordState.ChangedObservationRecorded, value.State));
        await using var context = database.CreateContext();
        var sequences = await context.CruiseCabinObservations.OrderBy(x => x.Sequence).Select(x => x.Sequence).ToArrayAsync();
        Assert.Equal([1, 2, 3], sequences);
        await AssertCounts(database, 1, 3, 15);
    }

    private static async Task<RecordResult[]> RecordTogether(CruisePersistenceFileDatabase database,
        CruiseCabinObservation first, CruiseCabinObservation second)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var one = RecordAfter(database, first, gate.Task); var two = RecordAfter(database, second, gate.Task);
        gate.SetResult(); return await Task.WhenAll(one, two);
    }

    private static async Task<RecordResult> RecordAfter(CruisePersistenceFileDatabase database,
        CruiseCabinObservation observation, Task gate)
    {
        await using var context = database.CreateContext(); await gate;
        return await new Repository(context).RecordAsync(observation);
    }

    private static async Task RecordOnce(CruisePersistenceFileDatabase database, CruiseCabinObservation observation)
    {
        await using var context = database.CreateContext(); await new Repository(context).RecordAsync(observation);
    }

    private static async Task AssertCounts(CruisePersistenceFileDatabase database, int series, int observations, int states)
    {
        await using var context = database.CreateContext();
        Assert.Equal(series, await context.CruiseCabinSeries.CountAsync());
        Assert.Equal(observations, await context.CruiseCabinObservations.CountAsync());
        Assert.Equal(states, await context.CruiseCabinObservationStates.CountAsync());
    }
}
