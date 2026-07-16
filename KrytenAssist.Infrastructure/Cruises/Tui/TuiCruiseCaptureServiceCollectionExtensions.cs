using KrytenAssist.Application.Cruises;
using Microsoft.Extensions.DependencyInjection;

namespace KrytenAssist.Infrastructure.Cruises.Tui;

public static class TuiCruiseCaptureServiceCollectionExtensions
{
    public static IServiceCollection AddTuiCruiseCapture(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ICruisePageCaptureService, TuiCruisePageCaptureService>();
        return services;
    }
}
