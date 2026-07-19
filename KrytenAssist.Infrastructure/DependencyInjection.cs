using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace KrytenAssist.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PromptCards")
            ?? throw new InvalidOperationException("Connection string 'PromptCards' was not found.");
        services.AddDbContext<KrytenAssistDbContext>(options =>
            options.UseSqlite(connectionString));
        var databaseInitializer = new DatabaseInitializer(connectionString);
        databaseInitializer.Initialise();

        services.AddScoped<IPromptCardRepository, SqlitePromptCardRepository>();
        services.AddScoped<ICruiseObservationRepository, SqliteCruiseObservationRepository>();
        services.AddScoped<ISavedCruiseRepository, SqliteSavedCruiseRepository>();
        services.AddScoped<IFavouriteCruiseShipRepository, SqliteFavouriteCruiseShipRepository>();
        services.AddScoped<ICruisePreferencesRepository, SqliteCruisePreferencesRepository>();
        services.AddScoped<ICruiseAlertRepository, SqliteCruiseAlertRepository>();
        services.AddScoped<ICruiseAlertSettingsRepository, SqliteCruiseAlertSettingsRepository>();
        services.AddScoped<ISavedCruiseCriteriaStateRepository, SqliteSavedCruiseCriteriaStateRepository>();
        services.AddScoped<ICruiseCabinObservationRepository, SqliteCruiseCabinObservationRepository>();
        return services;
    }
}
