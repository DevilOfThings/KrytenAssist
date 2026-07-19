extern alias KrytenApplication;

using KrytenAssist.Avalonia.DependencyInjection;
using KrytenAssist.Avalonia.ViewModels;
using KrytenAssist.Avalonia.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GetHistory = KrytenApplication::KrytenAssist.Application.Cruises.GetCruiseHistory;
using ListHistories = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseHistories;
using RecordObservation = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseObservation;
using RecordAndEvaluate = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseObservationAndEvaluateAlerts;
using SaveAndEvaluate = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruiseAndEvaluateCriteria;
using RestoreAndEvaluate = KrytenApplication::KrytenAssist.Application.Cruises.RestoreCruiseAndEvaluateCriteria;
using SavePreferencesAndEvaluate = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruisePreferencesAndEvaluateCriteria;
using SaveCruise = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruise;
using SavePreferences = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruisePreferences;
using ListSaved = KrytenApplication::KrytenAssist.Application.Cruises.ListSavedCruises;
using ListSavedDetails = KrytenApplication::KrytenAssist.Application.Cruises.ListSavedCruiseDetails;
using RecordCabin = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseCabinObservation;
using GetCabin = KrytenApplication::KrytenAssist.Application.Cruises.GetCruiseCabinHistory;
using ListCabins = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseCabinHistories;
using EvaluateCabin = KrytenApplication::KrytenAssist.Application.Cruises.EvaluateCruiseCabinAvailabilityAlerts;
using RecordCabinAndEvaluate = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseCabinObservationAndEvaluateAlerts;

namespace KrytenAssist.Avalonia.Tests.DependencyInjection;

public sealed class CruiseHistoryDesktopCompositionTests
{
    [Fact]
    public void AddDesktopPersistence_ResolvesHistoryServicesAndMigratesOnlyIsolatedDatabase()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"kryten-037e-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(directory, "desktop.db");
        try
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();
            services.AddSingleton<IClock, FixedClock>();

            services.AddDesktopPersistence(configuration, databasePath);
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            Assert.NotNull(scope.ServiceProvider.GetRequiredService<RecordObservation>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<RecordAndEvaluate>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<SaveAndEvaluate>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<RestoreAndEvaluate>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<SavePreferencesAndEvaluate>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<GetHistory>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ListHistories>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<CruiseHistoryViewModel>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<SaveCruise>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ListSaved>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ListSavedDetails>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<SavePreferences>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<RecordCabin>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<GetCabin>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ListCabins>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<EvaluateCabin>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<RecordCabinAndEvaluate>());
            var editor = scope.ServiceProvider.GetRequiredService<CruiseSaveAndEvaluationViewModel>();
            var preferences = scope.ServiceProvider.GetRequiredService<CruisePreferencesViewModel>();
            var organiser = scope.ServiceProvider.GetRequiredService<SavedCruisesViewModel>();
            var alertCoordinator = scope.ServiceProvider.GetRequiredService<CruiseAlertCoordinator>();
            var alertSettings = scope.ServiceProvider.GetRequiredService<CruiseAlertSettingsViewModel>();
            var alertCentre = scope.ServiceProvider.GetRequiredService<CruiseAlertCentreViewModel>();
            var cabinAvailability = scope.ServiceProvider.GetRequiredService<CruiseCabinAvailabilityViewModel>();
            Assert.Same(editor, organiser.Evaluation);
            Assert.Same(preferences, organiser.Preferences);
            Assert.Same(alertSettings, alertCentre.Settings);
            Assert.Same(alertCoordinator, scope.ServiceProvider.GetRequiredService<CruiseAlertCoordinator>());
            Assert.NotNull(cabinAvailability);
            Assert.True(File.Exists(databasePath));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void DefaultDatabasePath_UsesPerUserLocalApplicationData()
    {
        var path = DesktopPersistenceServiceCollectionExtensions.GetDefaultDatabasePath();
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        Assert.StartsWith(Path.GetFullPath(root), Path.GetFullPath(path), StringComparison.Ordinal);
        Assert.EndsWith(Path.Combine("KrytenAssist", "krytenassist.db"), path, StringComparison.Ordinal);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset Now => new(2026, 7, 17, 10, 0, 0, TimeSpan.Zero);
    }
}
