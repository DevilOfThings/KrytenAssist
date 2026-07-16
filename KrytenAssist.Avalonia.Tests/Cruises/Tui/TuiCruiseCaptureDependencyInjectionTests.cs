extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using Microsoft.Extensions.DependencyInjection;
using ICruisePageCaptureService =
    KrytenApplication::KrytenAssist.Application.Cruises.ICruisePageCaptureService;
using TuiCruiseCaptureServiceCollectionExtensions =
    KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Tui.TuiCruiseCaptureServiceCollectionExtensions;
using TuiCruisePageCaptureService =
    KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Tui.TuiCruisePageCaptureService;

namespace KrytenAssist.Avalonia.Tests.Cruises.Tui;

public sealed class TuiCruiseCaptureDependencyInjectionTests
{
    [Fact]
    public void AddTuiCruiseCapture_RegistersStatelessSingletonWithoutExternalWork()
    {
        var services = new ServiceCollection();

        var returned = TuiCruiseCaptureServiceCollectionExtensions
            .AddTuiCruiseCapture(services);
        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<ICruisePageCaptureService>();
        var second = provider.GetRequiredService<ICruisePageCaptureService>();

        Assert.Same(services, returned);
        Assert.IsType<TuiCruisePageCaptureService>(first);
        Assert.Same(first, second);
    }

    [Fact]
    public void AddTuiCruiseCapture_RejectsNullServices()
    {
        Assert.Throws<ArgumentNullException>(() =>
            TuiCruiseCaptureServiceCollectionExtensions.AddTuiCruiseCapture(null!));
    }
}
