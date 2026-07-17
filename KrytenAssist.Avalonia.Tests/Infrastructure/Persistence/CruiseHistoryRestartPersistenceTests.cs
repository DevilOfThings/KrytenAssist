extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using RepositoryContract = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseObservationRepository;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class CruiseHistoryRestartPersistenceTests
{
    [Fact]
    public async Task CompleteDisposalAndRecreation_PreservesHistoriesSnapshotsAndLatestEvidence()
    {
        await using var database = new CruisePersistenceFileDatabase();
        await database.MigrateAsync();
        var first = CruisePersistenceTestData.Observation(amount: 988m);
        var lower = CruisePersistenceTestData.Observation(
            amount: 949m,
            observedAt: first.ObservedAt.AddDays(1));
        var returned = CruisePersistenceTestData.Observation(
            amount: 988m,
            observedAt: first.ObservedAt.AddDays(2),
            providerOfferId: "latest-package",
            sourceReference: "https://example.test/latest-booking");
        var pastSourceLess = new CruiseObservation(
            CruisePersistenceTestData.Observation(
                amount: 725.25m,
                departureDate: new DateOnly(2025, 1, 4),
                providerOfferId: "past-offer").Snapshot,
            first.ObservedAt.AddHours(1),
            sourceReference: null,
            source: null);

        await using (var firstProcessContext = database.CreateContext())
        {
            RepositoryContract repository = database.CreateRepository(firstProcessContext);
            await RecordAsync(repository, first);
            await RecordAsync(repository, lower);
            await RecordAsync(repository, returned);
            await RecordAsync(repository, pastSourceLess);
        }

        await using (var restartedContext = database.CreateContext())
        {
            RepositoryContract repository = database.CreateRepository(restartedContext);
            var history = await repository.GetAsync(CruiseSailingKey.From(first), first.Source);
            var histories = await repository.ListAsync();

            Assert.NotNull(history);
            Assert.Equal([988m, 949m, 988m], history.Observations.Select(item => item.Snapshot.Prices[0].Amount));
            Assert.All(history.Observations, item => Assert.Equal(first.ObservedAt.Offset, item.ObservedAt.Offset));
            Assert.Equal(returned.ObservedAt, history.LastSeenAt);
            Assert.Equal("latest-package", history.LatestEvidence.ProviderOfferId);
            Assert.Equal("https://example.test/latest-booking", history.LatestEvidence.SourceReference);
            Assert.Equal(returned.ObservedAt, history.LatestEvidence.ObservedAt);
            Assert.Equal(2, histories.Count);
            Assert.Null(histories[0].Source);
            Assert.Equal(new DateOnly(2025, 1, 4), histories[0].SailingKey.DepartureDate);
            Assert.Equal("tui", histories[1].Source!.Id);
        }
    }

    private static Task<KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordResult> RecordAsync(
        RepositoryContract repository,
        CruiseObservation observation) =>
        repository.RecordAsync(
            CruiseSailingKey.From(observation),
            CruiseObservationFingerprint.From(observation),
            observation);
}
