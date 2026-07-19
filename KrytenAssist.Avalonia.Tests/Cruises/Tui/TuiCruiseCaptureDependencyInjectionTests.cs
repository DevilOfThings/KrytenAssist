extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using Microsoft.Extensions.DependencyInjection;
using ICruisePageBatchCaptureService =
    KrytenApplication::KrytenAssist.Application.Cruises.ICruisePageBatchCaptureService;
using ICruisePageCaptureService =
    KrytenApplication::KrytenAssist.Application.Cruises.ICruisePageCaptureService;
using ICruiseCabinPageCaptureService =
    KrytenApplication::KrytenAssist.Application.Cruises.ICruiseCabinPageCaptureService;
using ICruiseItineraryPageCaptureService =
    KrytenApplication::KrytenAssist.Application.Cruises.ICruiseItineraryPageCaptureService;
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
        var batch = provider.GetRequiredService<ICruisePageBatchCaptureService>();
        var implementation = provider.GetRequiredService<TuiCruisePageCaptureService>();
        var cabin = provider.GetRequiredService<ICruiseCabinPageCaptureService>();
        var itinerary = provider.GetRequiredService<ICruiseItineraryPageCaptureService>();

        Assert.Same(services, returned);
        Assert.IsType<TuiCruisePageCaptureService>(first);
        Assert.Same(first, second);
        Assert.Same(first, batch);
        Assert.Same(first, implementation);
        Assert.IsType<KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Tui.TuiCruiseCabinPageCaptureService>(cabin);
        Assert.IsType<KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Tui.TuiCruiseItineraryPageCaptureService>(itinerary);
    }

    [Fact]
    public void AddTuiCruiseCapture_RejectsNullServices()
    {
        Assert.Throws<ArgumentNullException>(() =>
            TuiCruiseCaptureServiceCollectionExtensions.AddTuiCruiseCapture(null!));
    }
}
