using System;

namespace KrytenAssist.Avalonia.Navigation.Models;

public sealed record NavigationItem
{
    public NavigationItem(
        string id,
        string title,
        NavigationDestinationKind kind,
        string? skillId = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown navigation destination kind.");
        }

        if (kind == NavigationDestinationKind.Skill)
        {
            ArgumentNullException.ThrowIfNull(skillId);
            ArgumentException.ThrowIfNullOrWhiteSpace(skillId);
        }
        else if (skillId is not null)
        {
            throw new ArgumentException(
                "Only Skill navigation destinations may have a Skill identifier.",
                nameof(skillId));
        }

        Id = id;
        Title = title;
        Kind = kind;
        SkillId = skillId;
    }

    public string Id { get; }

    public string Title { get; }

    public NavigationDestinationKind Kind { get; }

    public string? SkillId { get; }
}
