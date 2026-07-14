using System;
using System.Collections.Generic;

namespace KrytenAssist.Avalonia.Skills.Services;

public sealed class SkillRegistry : ISkillRegistry
{
    private readonly List<ISkill> _skills = new();
    private readonly IReadOnlyCollection<ISkill> _readOnlySkills;
    private readonly Dictionary<string, ISkill> _skillsById =
        new(StringComparer.OrdinalIgnoreCase);

    public SkillRegistry()
    {
        _readOnlySkills = _skills.AsReadOnly();
    }

    public IReadOnlyCollection<ISkill> Skills => _readOnlySkills;

    public void Register(ISkill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        if (!_skillsById.TryAdd(skill.Manifest.Id, skill))
        {
            throw new InvalidOperationException(
                $"Duplicate skill registration detected for '{skill.Manifest.Id}'.");
        }

        _skills.Add(skill);
    }

    public ISkill? Find(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        return _skillsById.GetValueOrDefault(id);
    }
}
