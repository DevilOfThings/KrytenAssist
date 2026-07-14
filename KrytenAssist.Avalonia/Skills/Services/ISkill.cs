using System.Threading;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Skills.Models;

namespace KrytenAssist.Avalonia.Skills.Services;

public interface ISkill
{
    SkillManifest Manifest { get; }

    Task<SkillResult> ExecuteAsync(
        SkillRequest request,
        SkillContext context,
        CancellationToken cancellationToken = default);
}
