using System;
using KrytenAssist.Avalonia.Cruises.Discovery;
using KrytenAssist.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KrytenAssist.Avalonia.DependencyInjection;

public static class ShellServiceCollectionExtensions
{
    public static IServiceCollection AddShell(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<MainWindowViewModel>();
        services.AddSingleton<CruiseDiscoverySourceCatalog>();
        services.AddSingleton<CruiseTrustedHostPolicy>();
        services.AddTransient<CruiseOfTheWeekViewModel>();
        services.AddTransient<ShellViewModel>();

        return services;
    }
}
