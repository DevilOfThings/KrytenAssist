using System;
using System.Threading;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Skills.Models;
using KrytenAssist.Avalonia.Skills.Services;

namespace KrytenAssist.Avalonia.Skills.Samples;

public sealed class EchoSkill : ISkill
{
    private static readonly SkillManifest EchoManifest = new(
        Id: "sample.echo",
        Name: "Echo",
        Description: "Returns a supplied message to validate the Skills framework.",
        Version: "1.0.0");

    public SkillManifest Manifest { get; } = EchoManifest;

    public Task<SkillResult> ExecuteAsync(
        SkillRequest request,
        SkillContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(request.Operation, "echo", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(SkillResult.Failure(
                $"Operation '{request.Operation}' is not supported by the Echo Skill."));
        }

        if (!request.Parameters.TryGetValue("message", out var value) ||
            value is not string message ||
            string.IsNullOrWhiteSpace(message))
        {
            return Task.FromResult(SkillResult.Failure(
                "A non-empty 'message' parameter is required."));
        }

        return Task.FromResult(SkillResult.Success(
            data: message,
            message: "Echo completed successfully."));
    }
}
