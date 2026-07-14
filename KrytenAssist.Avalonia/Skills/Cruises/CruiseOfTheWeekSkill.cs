extern alias KrytenApplication;

using System;
using System.Threading;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Skills.Models;
using KrytenAssist.Avalonia.Skills.Services;
using CruiseOfTheWeekException =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseOfTheWeekException;
using ICruiseOfTheWeekProvider =
    KrytenApplication::KrytenAssist.Application.Cruises.ICruiseOfTheWeekProvider;

namespace KrytenAssist.Avalonia.Skills.Cruises;

public sealed class CruiseOfTheWeekSkill : ISkill
{
    private static readonly SkillManifest CruiseOfTheWeekManifest = new(
        Id: "cruise.of-the-week",
        Name: "Cruise of the Week",
        Description: "Retrieves Marella Cruises' current Cruise of the Week.",
        Version: "1.0.0");

    private readonly ICruiseOfTheWeekProvider _provider;

    public CruiseOfTheWeekSkill(ICruiseOfTheWeekProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _provider = provider;
    }

    public SkillManifest Manifest { get; } = CruiseOfTheWeekManifest;

    public async Task<SkillResult> ExecuteAsync(
        SkillRequest request,
        SkillContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(
                request.Operation,
                "get-current",
                StringComparison.OrdinalIgnoreCase))
        {
            return SkillResult.Failure(
                $"Operation '{request.Operation}' is not supported by the Cruise of the Week Skill.");
        }

        if (request.Parameters.Count > 0)
        {
            return SkillResult.Failure(
                "The Cruise of the Week Skill does not accept parameters.");
        }

        try
        {
            var observation = await _provider.GetCurrentAsync(
                context.RequestedAt,
                cancellationToken);

            return SkillResult.Success(
                data: observation,
                message: "Cruise of the Week retrieved successfully.");
        }
        catch (CruiseOfTheWeekException exception)
        {
            return SkillResult.Failure(exception.Message);
        }
    }
}
