using System;
using KrytenAssist.Avalonia.Skills.Samples;
using KrytenAssist.Avalonia.Skills.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KrytenAssist.Avalonia.DependencyInjection;

public static class SkillServiceCollectionExtensions
{
    public static IServiceCollection AddSkills(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISkill, EchoSkill>();
        services.AddSingleton<ISkillRegistry>(serviceProvider =>
        {
            var registry = new SkillRegistry();

            foreach (var skill in serviceProvider.GetServices<ISkill>())
            {
                registry.Register(skill);
            }

            return registry;
        });

        return services;
    }
}
