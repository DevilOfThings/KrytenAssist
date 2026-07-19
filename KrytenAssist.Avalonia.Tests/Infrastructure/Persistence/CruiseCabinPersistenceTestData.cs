using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

internal static class CruiseCabinPersistenceTestData
{
    internal static readonly DateTimeOffset ObservedAt = new(2026, 7, 19, 18, 0, 0, TimeSpan.FromHours(2));

    internal static CruiseCabinObservation Observation(
        CruiseCabinAvailabilityState defaultState = CruiseCabinAvailabilityState.Unknown,
        CruiseCabinType knownCabin = CruiseCabinType.Inside,
        CruiseCabinAvailabilityState knownState = CruiseCabinAvailabilityState.Available,
        DateTimeOffset? observedAt = null,
        string evidenceKey = "tui-package:cabin-evidence",
        string? sourceReference = "https://www.tui.co.uk/cruise/packages/example",
        CruiseCabinSearchContext? context = null,
        CruiseSource? source = null,
        string ship = "marella explorer",
        DateOnly? departureDate = null)
    {
        var states = Enum.GetValues<CruiseCabinType>()
            .Select(type => new CruiseCabinState(type, type == knownCabin ? knownState : defaultState))
            .ToArray();
        var coverage = states.Any(x => x.Availability == CruiseCabinAvailabilityState.Unknown)
            ? CruiseCabinEvidenceCoverage.Partial : CruiseCabinEvidenceCoverage.Complete;
        return new CruiseCabinObservation(
            new CruiseSailingKey("marella", ship, departureDate ?? new DateOnly(2027, 10, 1), 7),
            source ?? new CruiseSource("tui", "TUI Cruises"),
            context ?? new CruiseCabinSearchContext(2, 2, [4, 7], true,
                CruiseCabinPackageMode.FlyCruise, "STN", 1),
            coverage, states, observedAt ?? ObservedAt, evidenceKey, sourceReference);
    }

    internal static CruiseCabinObservation Complete(CruiseCabinAvailabilityState state,
        DateTimeOffset? observedAt = null, string evidenceKey = "complete") =>
        Observation(state, CruiseCabinType.Inside, state, observedAt, evidenceKey);
}
