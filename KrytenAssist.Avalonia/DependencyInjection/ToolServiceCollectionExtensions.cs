using KrytenAssist.Avalonia.Services;
using KrytenAssist.Avalonia.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace KrytenAssist.Avalonia.DependencyInjection;

public static class ToolServiceCollectionExtensions
{
    public static IServiceCollection AddKrytenTools(this IServiceCollection services)
    {
        // Infrastructure
        services.AddSingleton<IToolRegistry, ToolRegistry>();

        // Tool implementations
        services.AddSingleton<ITool, CurrentDateTimeTool>();
        services.AddSingleton<ITool, CalculatorTool>();
        services.AddSingleton<ITool, ApplicationInfoTool>();
        services.AddSingleton<IClock, SystemClock>();
        
        return services;
    }
}