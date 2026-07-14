using Avalonia;
using System;
using KrytenAssist.Avalonia.DependencyInjection;
using KrytenAssist.Avalonia.Services;
using KrytenAssist.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using KrytenAssist.Avalonia.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

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
        _ = Services.GetRequiredService<IConversationService>();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        services.Configure<EmbeddingOptions>(configuration.GetSection("Embedding"));
        services.Configure<ConversationOptions>(configuration.GetSection("Conversation"));

        services.AddSingleton<IPromptCardStore, JsonPromptCardStore>();
        services.AddTransient<MainWindowViewModel>();
        
        services.AddSingleton<OpenAIEmbeddingService>();
        services.AddSingleton<DeterministicEmbeddingService>();
        services.AddSingleton<ResilientEmbeddingService>();

        services.AddSingleton<IEmbeddingService>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<EmbeddingOptions>>().Value;

            return options.Provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase)
                ? provider.GetRequiredService<ResilientEmbeddingService>()
                : provider.GetRequiredService<DeterministicEmbeddingService>();
        });
        
        services.AddSingleton<CosineSimilarityService>();
        services.AddSingleton<IConversationService, OpenAIConversationService>();
        services.AddSingleton<IConversationMemory, InMemoryConversationMemory>();


        services.AddKrytenTools();
        services.AddRuntimeContext();
        services.AddSkills();
        
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
