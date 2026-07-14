using System;

namespace KrytenAssist.Avalonia.Navigation.Models;

public sealed record DashboardSkillCard
{
    public DashboardSkillCard(
        string skillId,
        string name,
        string description,
        string version)
    {
        ArgumentNullException.ThrowIfNull(skillId);
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        SkillId = skillId;
        Name = name;
        Description = description;
        Version = version;
    }

    public string SkillId { get; }

    public string Name { get; }

    public string Description { get; }

    public string Version { get; }
}
