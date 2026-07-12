using KrytenAssist.Avalonia.Services;
using Microsoft.Extensions.DependencyInjection;

public static class RuntimeContextServiceCollectionExtensions
{
    public static IServiceCollection AddRuntimeContext(this IServiceCollection services)
    {
        services.AddSingleton<IRuntimeContextProvider, DefaultRuntimeContextProvider>();

        return services;
    }
}