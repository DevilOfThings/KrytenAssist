extern alias KrytenInfrastructure;
extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using Repository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseCabinObservationRepository;
using RecordState = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRepositoryRecordState;
using LatestEvidence = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinLatestEvidence;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class CruiseCabinPersistenceTests
{
    [Fact]
    public async Task FirstObservation_RoundTripsCompleteContextEvidenceAndOffsetAfterRestart()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        var observation = CruiseCabinPersistenceTestData.Observation();
        await using (var context = database.CreateContext())
        {
            var result = await new Repository(context).RecordAsync(observation);
            result.State.Should().Be(RecordState.FirstObservationRecorded);
        }

        await using var reopened = database.CreateContext();
        var history = await new Repository(reopened).GetAsync(observation.SeriesKey);
        history.Should().NotBeNull();
        history!.Observations.Should().ContainSingle().Which.Should().BeEquivalentTo(observation);
        history.LatestEvidence.Should().Be(new LatestEvidence(
            observation.EvidenceKey, observation.SourceReference, observation.ObservedAt));
        history.LatestObservation.SearchContext.ChildAges.Should().Equal(4, 7);
        history.LatestObservation.ObservedAt.Offset.Should().Be(TimeSpan.FromHours(2));
        (await reopened.CruiseCabinObservationStates.CountAsync()).Should().Be(5);
        (await reopened.CruiseCabinContextChildAges.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task EquivalentCurrent_AdvancesLatestEvidenceWithoutAddingSnapshot()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        var first = CruiseCabinPersistenceTestData.Observation();
        var refreshed = CruiseCabinPersistenceTestData.Observation(observedAt: first.ObservedAt.AddHours(2),
            evidenceKey: "refreshed", sourceReference: "https://www.tui.co.uk/cruise/packages/refreshed");
        await using var context = database.CreateContext(); var repository = new Repository(context);
        await repository.RecordAsync(first);
        var result = await repository.RecordAsync(refreshed);

        result.State.Should().Be(RecordState.AlreadyCurrent);
        result.History.Observations.Should().ContainSingle().Which.Should().BeEquivalentTo(first);
        result.History.LastSeenAt.Should().Be(refreshed.ObservedAt);
        result.History.LatestEvidence.EvidenceKey.Should().Be("refreshed");
        (await context.CruiseCabinObservations.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task MeaningfulChanges_AppendAndReturningFingerprintIsRetained()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        var available = CruiseCabinPersistenceTestData.Complete(CruiseCabinAvailabilityState.Available);
        var unavailable = CruiseCabinPersistenceTestData.Complete(CruiseCabinAvailabilityState.Unavailable, available.ObservedAt.AddHours(1), "unavailable");
        var availableAgain = CruiseCabinPersistenceTestData.Complete(CruiseCabinAvailabilityState.Available, available.ObservedAt.AddHours(2), "available-again");
        await using var context = database.CreateContext(); var repository = new Repository(context);
        await repository.RecordAsync(available); await repository.RecordAsync(unavailable);
        var result = await repository.RecordAsync(availableAgain);

        result.State.Should().Be(RecordState.ChangedObservationRecorded);
        result.History.Observations.Select(x => x.StateFingerprint).Should().Equal(
            available.StateFingerprint, unavailable.StateFingerprint, available.StateFingerprint);
        (await context.CruiseCabinObservations.CountAsync()).Should().Be(3);
        (await context.CruiseCabinObservationStates.CountAsync()).Should().Be(15);
    }

    [Fact]
    public async Task EarlierEvidence_DoesNotRegressLastSeenOrLatestEvidence()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        var current = CruiseCabinPersistenceTestData.Complete(CruiseCabinAvailabilityState.Available, evidenceKey: "current");
        var older = CruiseCabinPersistenceTestData.Complete(CruiseCabinAvailabilityState.Unavailable,
            current.ObservedAt.AddHours(-1), "older");
        await using var context = database.CreateContext(); var repository = new Repository(context);
        await repository.RecordAsync(current);
        var result = await repository.RecordAsync(older);
        result.History.LastSeenAt.Should().Be(current.ObservedAt);
        result.History.LatestEvidence.EvidenceKey.Should().Be("current");
        result.History.Observations.Select(x => x.EvidenceKey).Should().Equal("older", "current");
    }

    [Fact]
    public async Task MaterialContextAndSource_CreateIndependentDeterministicallyListedSeries()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        var baseline = CruiseCabinPersistenceTestData.Observation();
        var otherContext = CruiseCabinPersistenceTestData.Observation(context: new CruiseCabinSearchContext(4, 0, [], true));
        var otherSource = CruiseCabinPersistenceTestData.Observation(source: new CruiseSource("other", "Other"));
        await using var context = database.CreateContext(); var repository = new Repository(context);
        await repository.RecordAsync(otherSource); await repository.RecordAsync(otherContext); await repository.RecordAsync(baseline);
        var values = await repository.ListAsync();
        values.Should().HaveCount(3);
        values.Select(x => x.LatestObservation.Source.Id).Should().Equal("other", "tui", "tui");
        values.Where(x => x.LatestObservation.Source.Id == "tui").Select(x => x.LatestObservation.SearchContext.Fingerprint)
            .Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public async Task MissingAndPreCancelledQueriesAreControlledByRepositoryContract()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        await using var context = database.CreateContext(); var repository = new Repository(context);
        (await repository.GetAsync(new string('a', 64))).Should().BeNull();
        using var source = new CancellationTokenSource(); source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.ListAsync(source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.RecordAsync(CruiseCabinPersistenceTestData.Observation(), source.Token));
    }

    [Fact]
    public async Task CorruptStoredFingerprintIsRejectedOnReconstruction()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        var observation = CruiseCabinPersistenceTestData.Observation();
        await using var context = database.CreateContext(); await new Repository(context).RecordAsync(observation);
        await context.Database.ExecuteSqlRawAsync("PRAGMA ignore_check_constraints = ON");
        await context.Database.ExecuteSqlRawAsync("UPDATE CruiseCabinObservations SET StateFingerprint = {0}", new string('b', 64));
        await context.Database.ExecuteSqlRawAsync("PRAGMA ignore_check_constraints = OFF");
        await FluentActions.Awaiting(() => new Repository(context).GetAsync(observation.SeriesKey)).Should().ThrowAsync<InvalidDataException>();
    }
}
