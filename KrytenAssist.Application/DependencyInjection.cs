using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace KrytenAssist.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<PromptCards.CreatePromptCard>();
        services.AddScoped<PromptCards.UpdatePromptCard>();
        services.AddScoped<PromptCards.DeletePromptCard>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}