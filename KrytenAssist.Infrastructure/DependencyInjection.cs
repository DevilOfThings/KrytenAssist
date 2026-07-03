using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KrytenAssist.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PromptCards")
            ?? throw new InvalidOperationException("Connection string 'PromptCards' was not found.");

        var databaseInitializer = new DatabaseInitializer(connectionString);
        databaseInitializer.Initialise();

        services.AddSingleton<IPromptCardRepository>(_ =>
            new SqlitePromptCardRepository(connectionString));
        return services;
    }
}