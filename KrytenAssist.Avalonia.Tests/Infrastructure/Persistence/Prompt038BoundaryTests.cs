extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using DismissCruise = KrytenApplication::KrytenAssist.Application.Cruises.DismissCruise;
using RecordObservation = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseObservation;
using RemoveSavedCruise = KrytenApplication::KrytenAssist.Application.Cruises.RemoveSavedCruise;
using RestoreCruise = KrytenApplication::KrytenAssist.Application.Cruises.RestoreCruise;
using SaveCruise = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruise;
using SetFavourite = KrytenApplication::KrytenAssist.Application.Cruises.SetSavedCruiseFavourite;
using SnapshotFactory = KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseSnapshotFactory;
using UpdateEvaluation = KrytenApplication::KrytenAssist.Application.Cruises.UpdateCruiseEvaluation;
using ObservationRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseObservationRepository;
using SavedRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteSavedCruiseRepository;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class Prompt038BoundaryTests
{
    [Fact]
    public async Task Personal_workflow_and_recorded_history_are_physically_independent()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        await using var context = database.CreateContext();
        var savedRepository = new SavedRepository(context);
        var observationRepository = new ObservationRepository(context);
        var observation = CruisePersistenceTestData.Observation();
        var key = CruiseSailingKey.From(observation);
        var factory = new SnapshotFactory();
        var save = new SaveCruise(savedRepository);

        await save.ExecuteAsync(factory.Create(observation, new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero)));

        context.SavedCruises.Should().ContainSingle();
        context.CruiseHistories.Should().BeEmpty();

        var evaluation = new CruiseEvaluation(CruiseInterestLevel.StrongCandidate, 5, 4, 3, 2, "Keep this opinion");
        await new UpdateEvaluation(savedRepository).ExecuteAsync(key, evaluation);
        await new SetFavourite(savedRepository).ExecuteAsync(key, true);
        await new DismissCruise(savedRepository).ExecuteAsync(key);

        await new RecordObservation(observationRepository, new CruisePriceHistoryAnalyzer()).ExecuteAsync(observation);

        var personalAfterRecord = await savedRepository.GetAsync(key);
        personalAfterRecord!.Evaluation.Should().Be(evaluation);
        personalAfterRecord.IsFavourite.Should().BeTrue();
        personalAfterRecord.Status.Should().Be(SavedCruiseStatus.Dismissed);
        context.CruiseHistories.Should().ContainSingle();
        context.CruiseObservations.Should().ContainSingle();

        var otherSource = new CruiseObservation(
            observation.Snapshot,
            observation.ObservedAt.AddHours(2),
            "https://other.example.test/cruise/101842",
            new CruiseSource("other", "Other retailer"));
        await save.ExecuteAsync(factory.Create(otherSource, new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero)));

        var refreshed = await savedRepository.GetAsync(key);
        context.SavedCruises.Should().ContainSingle();
        refreshed!.Snapshot.RetailSource!.Id.Should().Be("other");
        refreshed.Evaluation.Should().Be(evaluation);
        refreshed.IsFavourite.Should().BeTrue();
        refreshed.Status.Should().Be(SavedCruiseStatus.Dismissed);
        context.CruiseObservations.Should().ContainSingle();

        await new RestoreCruise(savedRepository).ExecuteAsync(key);
        context.CruiseObservations.Should().ContainSingle();
        await new RemoveSavedCruise(savedRepository).ExecuteAsync(key);

        context.SavedCruises.Should().BeEmpty();
        context.CruiseHistories.Should().ContainSingle();
        context.CruiseObservations.Should().ContainSingle();
    }

    [Fact]
    public async Task Deleting_recorded_history_does_not_delete_personal_state()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        await using var context = database.CreateContext();
        var savedRepository = new SavedRepository(context);
        var observation = CruisePersistenceTestData.Observation();
        var key = CruiseSailingKey.From(observation);
        var saved = new SavedCruise(new SnapshotFactory().Create(
            observation,
            new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero)),
            evaluation: new CruiseEvaluation(CruiseInterestLevel.Maybe, notes: "Personal state"));
        await savedRepository.UpsertAsync(saved);
        await new ObservationRepository(context).RecordAsync(
            key,
            CruiseObservationFingerprint.From(observation),
            observation);

        context.CruiseHistories.Remove(await context.CruiseHistories.SingleAsync());
        await context.SaveChangesAsync();

        context.CruiseHistories.Should().BeEmpty();
        context.CruiseObservations.Should().BeEmpty();
        (await savedRepository.GetAsync(key)).Should().Be(saved);
        context.SavedCruises.Should().ContainSingle();
    }
}
