using KrytenAssist.Application.Cruises;
using Microsoft.Extensions.DependencyInjection;

namespace KrytenAssist.Infrastructure.Cruises.Tui;

public static class TuiCruiseCaptureServiceCollectionExtensions
{
    public static IServiceCollection AddTuiCruiseCapture(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<TuiCruisePageCaptureService>();
        services.AddSingleton<ICruisePageCaptureService>(provider =>
            provider.GetRequiredService<TuiCruisePageCaptureService>());
        services.AddSingleton<ICruisePageBatchCaptureService>(provider =>
            provider.GetRequiredService<TuiCruisePageCaptureService>());
        return services;
    }
}
