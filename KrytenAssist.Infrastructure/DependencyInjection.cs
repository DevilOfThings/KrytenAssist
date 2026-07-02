using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace KrytenAssist.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IPromptCardRepository, InMemoryPromptCardRepository>();

        return services;
    }
}