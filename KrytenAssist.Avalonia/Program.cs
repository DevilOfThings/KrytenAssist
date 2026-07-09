using Avalonia;
using System;
using KrytenAssist.Avalonia.Services;
using KrytenAssist.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;


namespace KrytenAssist.Avalonia;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    public static IServiceProvider Services { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        Services = BuildServices();

        _ = Services.GetRequiredService<IEmbeddingService>();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IPromptCardStore, JsonPromptCardStore>();
        services.AddTransient<MainWindowViewModel>();
        services.AddSingleton<IEmbeddingService, DeterministicEmbeddingService>();
        services.AddSingleton<CosineSimilarityService>();

        return services.BuildServiceProvider();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
