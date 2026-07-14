using System.Collections.Generic;

namespace KrytenAssist.Avalonia.Skills.Services;

public interface ISkillRegistry
{
    IReadOnlyCollection<ISkill> Skills { get; }

    void Register(ISkill skill);

    ISkill? Find(string id);
}
