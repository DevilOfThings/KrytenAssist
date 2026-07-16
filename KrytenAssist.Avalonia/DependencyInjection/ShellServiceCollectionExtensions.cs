extern alias KrytenInfrastructure;

using System;
using KrytenAssist.Avalonia.Cruises.Discovery;
using KrytenAssist.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using TuiCruiseCaptureServiceCollectionExtensions = KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Tui.TuiCruiseCaptureServiceCollectionExtensions;

namespace KrytenAssist.Avalonia.DependencyInjection;

public static class ShellServiceCollectionExtensions
{
    public static IServiceCollection AddShell(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        TuiCruiseCaptureServiceCollectionExtensions.AddTuiCruiseCapture(services);
        services.AddTransient<MainWindowViewModel>();
        services.AddSingleton<CruiseDiscoverySourceCatalog>();
        services.AddSingleton<CruiseTrustedHostPolicy>();
        services.AddTransient<CruiseOfTheWeekViewModel>();
        services.AddTransient<ShellViewModel>();

        return services;
    }
}
