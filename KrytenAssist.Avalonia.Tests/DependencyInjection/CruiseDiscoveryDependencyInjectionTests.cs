extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using KrytenAssist.Avalonia.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using CaptureService = KrytenApplication::KrytenAssist.Application.Cruises.ICruisePageCaptureService;
using TuiCaptureService = KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Tui.TuiCruisePageCaptureService;

namespace KrytenAssist.Avalonia.Tests.DependencyInjection;

public sealed class CruiseDiscoveryDependencyInjectionTests
{
    [Fact]
    public void AddShell_ComposesTuiCaptureAdapterWithoutExternalWork()
    {
        var services = new ServiceCollection();

        var returned = services.AddShell();
        using var provider = services.BuildServiceProvider();
        var captureService = provider.GetRequiredService<CaptureService>();

        Assert.Same(services, returned);
        Assert.IsType<TuiCaptureService>(captureService);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(HttpClient));
    }
}
