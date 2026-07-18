extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RepositoryContract = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseObservationRepository;
using InfrastructureRegistration = KrytenInfrastructure::KrytenAssist.Infrastructure.DependencyInjection;
using Repository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseObservationRepository;
using SavedContract = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ISavedCruiseRepository;
using FavouriteContract = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.IFavouriteCruiseShipRepository;
using PreferencesContract = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruisePreferencesRepository;
using SavedRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteSavedCruiseRepository;
using FavouriteRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteFavouriteCruiseShipRepository;
using PreferencesRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruisePreferencesRepository;
using AlertContract = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseAlertRepository;
using AlertSettingsContract = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseAlertSettingsRepository;
using CriteriaStateContract = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ISavedCruiseCriteriaStateRepository;
using AlertRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseAlertRepository;
using AlertSettingsRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseAlertSettingsRepository;
using CriteriaStateRepository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteSavedCruiseCriteriaStateRepository;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class CruisePersistenceDependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ResolvesCruiseRepositoryFromIsolatedDatabase()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"kryten-037c-{Guid.NewGuid():N}.db");
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PromptCards"] = $"Data Source={databasePath}"
                })
                .Build();
            var services = new ServiceCollection();

            InfrastructureRegistration.AddInfrastructure(services, configuration);
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<RepositoryContract>();

            Assert.IsType<Repository>(repository);
            Assert.IsType<SavedRepository>(scope.ServiceProvider.GetRequiredService<SavedContract>());
            Assert.IsType<FavouriteRepository>(scope.ServiceProvider.GetRequiredService<FavouriteContract>());
            Assert.IsType<PreferencesRepository>(scope.ServiceProvider.GetRequiredService<PreferencesContract>());
            Assert.IsType<AlertRepository>(scope.ServiceProvider.GetRequiredService<AlertContract>());
            Assert.IsType<AlertSettingsRepository>(scope.ServiceProvider.GetRequiredService<AlertSettingsContract>());
            Assert.IsType<CriteriaStateRepository>(scope.ServiceProvider.GetRequiredService<CriteriaStateContract>());
            Assert.True(File.Exists(databasePath));
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}
