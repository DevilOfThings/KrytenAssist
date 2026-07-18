extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using RemoveSavedCruise = KrytenApplication::KrytenAssist.Application.Cruises.RemoveSavedCruise;
using SnapshotFactory = KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseSnapshotFactory;
using AlertRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseAlertRepository;
using ObservationRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseObservationRepository;
using SavedRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteSavedCruiseRepository;
using SettingsRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseAlertSettingsRepository;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class Prompt039BoundaryTests
{
    [Fact]
    public async Task Alert_lifecycle_and_settings_remain_independent_when_history_and_personal_state_are_removed()
    {
        await using var database = new CruisePersistenceTestDatabase();
        await database.OpenAndMigrateAsync();
        var observation = CruisePersistenceTestData.Observation();
        var key = CruiseSailingKey.From(observation);
        CruiseAlert alert;

        await using (var context = database.CreateContext())
        {
            await new ObservationRepository(context).RecordAsync(
                key,
                CruiseObservationFingerprint.From(observation),
                observation);
            await new SavedRepository(context).UpsertAsync(new SavedCruise(
                new SnapshotFactory().Create(observation, observation.ObservedAt)));

            alert = new CruiseAlert(
                Guid.NewGuid(),
                new CruiseAlertCandidate(
                    CruiseAlertType.Promotion,
                    key,
                    observation.Source,
                    new CruisePromotionAlertDetails(null, "Audit promotion", "audit-evidence"),
                    observation.ObservedAt,
                    "audit-evidence"),
                observation.ObservedAt.AddMinutes(1));
            await new AlertRepository(context).AddIfAbsentAsync(alert);
            await new AlertRepository(context).UpdateStatusAsync(alert.Id, CruiseAlertStatus.Dismissed);
            await new SettingsRepository(context).SaveAsync(new CruiseAlertSettings(false, true, false, 12.5m));

            context.CruiseHistories.Remove(await context.CruiseHistories.SingleAsync());
            await context.SaveChangesAsync();
            await new RemoveSavedCruise(new SavedRepository(context)).ExecuteAsync(key);
        }

        await using var reopened = database.CreateContext();
        reopened.CruiseHistories.Should().BeEmpty();
        reopened.CruiseObservations.Should().BeEmpty();
        reopened.SavedCruises.Should().BeEmpty();
        (await new AlertRepository(reopened).GetAsync(alert.Id))
            .Should().Be(alert.WithStatus(CruiseAlertStatus.Dismissed));
        (await new SettingsRepository(reopened).GetAsync())
            .Should().Be(new CruiseAlertSettings(false, true, false, 12.5m));
    }
}
