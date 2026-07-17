extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using RepositoryContract = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseObservationRepository;
using RecordState = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordState;
using DatabaseContext = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.KrytenAssistDbContext;
using Repository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseObservationRepository;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class SqliteCruiseObservationRepositoryTests
{
    [Fact]
    public async Task RecordAndGet_RoundTripCompleteObservationAndPricesExactly()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        var observation = CruisePersistenceTestData.Observation(
            prices:
            [
                new CruisePrice(0.0000000000000000000000000001m, "GBP", "deposit"),
                new CruisePrice(987.46m, "GBP", "per person")
            ]);
        var key = CruiseSailingKey.From(observation);
        var fingerprint = CruiseObservationFingerprint.From(observation);
        await using (var context = database.CreateContext())
        {
            RepositoryContract repository = new Repository(context);
            var recorded = await repository.RecordAsync(key, fingerprint, observation);

            Assert.Equal(RecordState.FirstObservationRecorded, recorded.State);
            Assert.Equal(observation.ObservedAt, recorded.History.LastSeenAt);
            AssertObservation(observation, recorded.History.Observations.Single());
        }

        await using (var reopened = database.CreateContext())
        {
            RepositoryContract repository = new Repository(reopened);
            var history = await repository.GetAsync(key, observation.Source);

            Assert.NotNull(history);
            Assert.Equal(key, history.SailingKey);
            Assert.Equal(observation.Source, history.Source);
            AssertObservation(observation, history.Observations.Single());
        }
    }

    [Fact]
    public async Task Record_SourceLessObservationRoundTripsAsNullSource()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        var observation = CruisePersistenceTestData.SourceLessObservation();
        await using var context = database.CreateContext();
        RepositoryContract repository = new Repository(context);

        await repository.RecordAsync(
            CruiseSailingKey.From(observation),
            CruiseObservationFingerprint.From(observation),
            observation);
        var history = await repository.GetAsync(CruiseSailingKey.From(observation), null);

        Assert.NotNull(history);
        Assert.Null(history.Source);
        Assert.Null(history.Observations.Single().Source);
    }

    [Fact]
    public async Task Record_ChangedAndAlreadyCurrentMaintainSnapshotsAndLastSeen()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        var first = CruisePersistenceTestData.Observation();
        var changed = CruisePersistenceTestData.Observation(
            amount: 949m,
            observedAt: first.ObservedAt.AddDays(2));
        var repeat = CruisePersistenceTestData.Observation(
            amount: 949m,
            observedAt: changed.ObservedAt.AddDays(3),
            providerOfferId: "different-package",
            sourceReference: "https://example.test/new-reference");
        var olderRepeat = CruisePersistenceTestData.Observation(
            amount: 949m,
            observedAt: changed.ObservedAt.AddDays(1));
        await using var context = database.CreateContext();
        RepositoryContract repository = new Repository(context);

        await RecordAsync(repository, first);
        var changedResult = await RecordAsync(repository, changed);
        var repeatResult = await RecordAsync(repository, repeat);
        var olderResult = await RecordAsync(repository, olderRepeat);

        Assert.Equal(RecordState.ChangedObservationRecorded, changedResult.State);
        Assert.Equal(RecordState.AlreadyCurrent, repeatResult.State);
        Assert.Equal(RecordState.AlreadyCurrent, olderResult.State);
        Assert.Equal(2, olderResult.History.Observations.Count);
        Assert.Equal(repeat.ObservedAt, olderResult.History.LastSeenAt);
        Assert.Equal(2, await context.CruiseObservations.CountAsync());
    }

    [Fact]
    public async Task Record_DifferentRetailSourcesCreateSeparateHistories()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        var tui = CruisePersistenceTestData.Observation();
        var iglu = new CruiseObservation(
            tui.Snapshot,
            tui.ObservedAt,
            tui.SourceReference,
            new CruiseSource("iglu", "Iglu Cruise"));
        await using var context = database.CreateContext();
        RepositoryContract repository = new Repository(context);

        await RecordAsync(repository, tui);
        await RecordAsync(repository, iglu);
        var histories = await repository.ListAsync();

        Assert.Equal(2, histories.Count);
        Assert.Equal(["iglu", "tui"], histories.Select(history => history.Source!.Id).Order().ToArray());
    }

    [Fact]
    public async Task List_ReturnsEmptyThenIncludesPastAndFutureSailingsDeterministically()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        await using var context = database.CreateContext();
        RepositoryContract repository = new Repository(context);

        Assert.Empty(await repository.ListAsync());
        await RecordAsync(repository, CruisePersistenceTestData.Observation(departureDate: new DateOnly(2027, 1, 1)));
        await RecordAsync(repository, CruisePersistenceTestData.Observation(
            departureDate: new DateOnly(2025, 1, 1),
            providerOfferId: "past"));
        var histories = await repository.ListAsync();

        Assert.Equal(new DateOnly(2025, 1, 1), histories[0].SailingKey.DepartureDate);
        Assert.Equal(new DateOnly(2027, 1, 1), histories[1].SailingKey.DepartureDate);
    }

    [Fact]
    public async Task Record_PreCancelledTokenDoesNotMutateDatabase()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        var observation = CruisePersistenceTestData.Observation();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await using var context = database.CreateContext();
        RepositoryContract repository = new Repository(context);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.RecordAsync(
            CruiseSailingKey.From(observation),
            CruiseObservationFingerprint.From(observation),
            observation,
            cancellation.Token));

        Assert.Equal(0, await context.CruiseHistories.CountAsync());
    }

    [Fact]
    public async Task Record_ConstraintFailureRollsBackEntireAggregate()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        var observation = CruisePersistenceTestData.Observation(title: new string('x', 1001));
        await using (var context = database.CreateContext())
        {
            RepositoryContract repository = new Repository(context);
            await Assert.ThrowsAsync<DbUpdateException>(() => RecordAsync(repository, observation));
        }

        await using var verification = database.CreateContext();
        Assert.Equal(0, await verification.CruiseHistories.CountAsync());
        Assert.Equal(0, await verification.CruiseObservations.CountAsync());
        Assert.Equal(0, await verification.CruiseObservationPrices.CountAsync());
    }

    private static Task<KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordResult> RecordAsync(
        RepositoryContract repository,
        CruiseObservation observation) =>
        repository.RecordAsync(
            CruiseSailingKey.From(observation),
            CruiseObservationFingerprint.From(observation),
            observation);

    private static void AssertObservation(CruiseObservation expected, CruiseObservation actual)
    {
        Assert.Equal(expected.ObservedAt, actual.ObservedAt);
        Assert.Equal(expected.ObservedAt.Offset, actual.ObservedAt.Offset);
        Assert.Equal(expected.SourceReference, actual.SourceReference);
        Assert.Equal(expected.Source, actual.Source);
        Assert.Equal(expected.Snapshot.Offer, actual.Snapshot.Offer);
        Assert.Equal(expected.Snapshot.PromotionSummary, actual.Snapshot.PromotionSummary);
        Assert.Equal(expected.Snapshot.Prices, actual.Snapshot.Prices);
    }
}
