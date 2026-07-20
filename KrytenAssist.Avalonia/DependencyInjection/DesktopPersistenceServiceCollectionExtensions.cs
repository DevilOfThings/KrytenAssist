extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using KrytenAssist.Avalonia.ViewModels;
using ApplicationRegistration = KrytenApplication::KrytenAssist.Application.DependencyInjection;
using InfrastructureRegistration = KrytenInfrastructure::KrytenAssist.Infrastructure.DependencyInjection;

namespace KrytenAssist.Avalonia.DependencyInjection;

public static class DesktopPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddDesktopPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        string? databasePath = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        databasePath ??= GetDefaultDatabasePath();
        var fullPath = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("The desktop database directory is unavailable.");
        Directory.CreateDirectory(directory);
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath
        }.ToString();
        var desktopConfiguration = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PromptCards"] = connectionString
            })
            .Build();

        ApplicationRegistration.AddApplication(services);
        InfrastructureRegistration.AddInfrastructure(services, desktopConfiguration);
        services.AddTransient<CruiseHistoryViewModel>();
        services.AddScoped<CruiseSaveAndEvaluationViewModel>();
        services.AddScoped<CruisePreferencesViewModel>();
        services.AddTransient<SavedCruisesViewModel>();
        services.AddScoped<CruiseAlertCoordinator>();
        services.AddScoped<CruiseAlertSettingsViewModel>();
        services.AddScoped<CruiseAlertCentreViewModel>();
        services.AddScoped<CruiseCabinAvailabilityViewModel>();
        services.AddScoped<CruiseNewItinerariesViewModel>();
        services.AddScoped<CruiseItineraryCaptureReviewViewModel>();
        return services;
    }

    public static string GetDefaultDatabasePath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KrytenAssist",
            "krytenassist.db");
}
