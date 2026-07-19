extern alias KrytenApplication;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using KrytenAssist.Core.Cruises;
using CabinDetails = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinHistoryDetails;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed record CruiseCabinCategoryStateViewModel(string CabinType, string StateText);

public sealed record CruiseCabinChangeViewModel(string CabinType, string PreviousState, string CurrentState, string ChangeText);

public sealed class CruiseCabinTimelineItemViewModel
{
    internal CruiseCabinTimelineItemViewModel(CruiseCabinObservation observation, bool isLatest)
    {
        IsLatest = isLatest;
        EvidenceTimeText = CruiseCabinAvailabilityItemViewModel.Time(observation.ObservedAt);
        CoverageText = CruiseCabinAvailabilityItemViewModel.Coverage(observation.Coverage);
        EvidenceKey = observation.EvidenceKey;
        SourceReference = observation.SourceReference ?? "Not recorded";
        HasSourceReference = observation.SourceReference is not null;
        CategorySummary = string.Join(" · ", observation.States.Select(state =>
            $"{CruiseCabinAvailabilityItemViewModel.Cabin(state.CabinType)}: {CruiseCabinAvailabilityItemViewModel.ShortState(state.Availability)}"));
    }

    public bool IsLatest { get; }
    public string LatestLabel => IsLatest ? "Latest meaningful evidence" : "Earlier evidence";
    public string EvidenceTimeText { get; }
    public string CoverageText { get; }
    public string EvidenceKey { get; }
    public string SourceReference { get; }
    public bool HasSourceReference { get; }
    public string CategorySummary { get; }
}

public sealed class CruiseCabinAvailabilityItemViewModel
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-GB");

    public CruiseCabinAvailabilityItemViewModel(CabinDetails details, CruisePreferences? preferences, bool preferenceUnavailable)
    {
        var history = details.History;
        var summary = details.Summary;
        var latest = summary.LatestObservation;
        var key = latest.SailingKey;
        var context = latest.SearchContext;

        SeriesKey = history.SeriesKey;
        ContextFingerprint = context.Fingerprint;
        OperatorId = key.OperatorId;
        ShipName = key.ShipName;
        DepartureDate = key.DepartureDate;
        DurationNights = key.DurationNights;
        SailingText = $"{ShipName} · {DepartureDate.ToString("d MMMM yyyy", DisplayCulture)} · {Nights(DurationNights)}";
        RetailSourceName = latest.Source.Name;
        RetailSourceId = latest.Source.Id;
        ContextSummary = CompactContext(context);
        AdultCountText = Number(context.AdultCount);
        ChildCountText = Number(context.ChildCount);
        ChildAgesText = ChildAges(context);
        PackageModeText = Package(context.PackageMode);
        DepartureAirportText = context.DepartureAirportId?.ToUpperInvariant() ?? "Unknown";
        CabinQuantityText = Number(context.CabinQuantity);
        CoverageText = Coverage(latest.Coverage);
        CoverageExplanation = latest.Coverage == CruiseCabinEvidenceCoverage.Partial
            ? "This source showed only some cabin facts for this search. Unknown categories were not proven unavailable."
            : "This source explicitly represented every supported category at this evidence time. It may not remain current.";
        LatestEvidenceTimeText = Time(latest.ObservedAt);
        LastCheckedTimeText = Time(summary.LastSeenAt);
        FirstObservedTimeText = Time(summary.FirstObservedAt);
        EvidenceKey = history.LatestEvidence.EvidenceKey;
        SourceReference = history.LatestEvidence.SourceReference ?? "Not recorded";
        HasSourceReference = history.LatestEvidence.SourceReference is not null;
        ObservationCount = summary.ObservationCount;
        ObservationCountText = $"{ObservationCount} recorded observation{(ObservationCount == 1 ? string.Empty : "s")}";
        CategoryStates = new ReadOnlyCollection<CruiseCabinCategoryStateViewModel>(Enum.GetValues<CruiseCabinType>()
            .Select(type => new CruiseCabinCategoryStateViewModel(Cabin(type), State(latest.StateFor(type)))).ToArray());
        LatestCategorySummary = string.Join(" · ", Enum.GetValues<CruiseCabinType>()
            .Select(type => $"{Cabin(type)}: {ShortState(latest.StateFor(type))}"));
        PreferenceStatus = Preference(latest, preferences, preferenceUnavailable);

        var observations = history.Observations.OrderBy(value => value.ObservedAt)
            .ThenBy(value => value.StateFingerprint, StringComparer.Ordinal).ToArray();
        LatestChanges = new ReadOnlyCollection<CruiseCabinChangeViewModel>(CreateChanges(observations).ToArray());
        ChangeSummary = observations.Length == 1
            ? "No previous recorded cabin evidence for this search."
            : LatestChanges.Count == 0
                ? "No category-state change in the retained latest evidence."
                : $"{LatestChanges.Count} category state{(LatestChanges.Count == 1 ? string.Empty : "s")} changed in the latest evidence.";
        Timeline = new ReadOnlyCollection<CruiseCabinTimelineItemViewModel>(observations.Reverse()
            .Select((observation, index) => new CruiseCabinTimelineItemViewModel(observation, index == 0)).ToArray());
    }

    public string SeriesKey { get; }
    public string ContextFingerprint { get; }
    public string OperatorId { get; }
    public string ShipName { get; }
    public DateOnly DepartureDate { get; }
    public int DurationNights { get; }
    public string SailingText { get; }
    public string RetailSourceName { get; }
    public string RetailSourceId { get; }
    public string ContextSummary { get; }
    public string AdultCountText { get; }
    public string ChildCountText { get; }
    public string ChildAgesText { get; }
    public string PackageModeText { get; }
    public string DepartureAirportText { get; }
    public string CabinQuantityText { get; }
    public string CoverageText { get; }
    public string CoverageExplanation { get; }
    public string LatestEvidenceTimeText { get; }
    public string LastCheckedTimeText { get; }
    public string FirstObservedTimeText { get; }
    public string EvidenceKey { get; }
    public string SourceReference { get; }
    public bool HasSourceReference { get; }
    public int ObservationCount { get; }
    public string ObservationCountText { get; }
    public string LatestCategorySummary { get; }
    public string PreferenceStatus { get; }
    public string ChangeSummary { get; }
    public IReadOnlyList<CruiseCabinCategoryStateViewModel> CategoryStates { get; }
    public IReadOnlyList<CruiseCabinChangeViewModel> LatestChanges { get; }
    public IReadOnlyList<CruiseCabinTimelineItemViewModel> Timeline { get; }

    internal static string Cabin(CruiseCabinType type) => type switch
    {
        CruiseCabinType.Inside => "Inside",
        CruiseCabinType.Outside => "Outside",
        CruiseCabinType.Balcony => "Balcony",
        CruiseCabinType.Suite => "Suite",
        CruiseCabinType.Solo => "Solo",
        _ => "Unknown"
    };

    internal static string State(CruiseCabinAvailabilityState state) => state switch
    {
        CruiseCabinAvailabilityState.Available => "Available when recorded for this search",
        CruiseCabinAvailabilityState.Unavailable => "Unavailable when recorded for this search",
        _ => "Unknown"
    };

    internal static string ShortState(CruiseCabinAvailabilityState state) => state switch
    {
        CruiseCabinAvailabilityState.Available => "Available",
        CruiseCabinAvailabilityState.Unavailable => "Unavailable",
        _ => "Unknown"
    };

    internal static string Coverage(CruiseCabinEvidenceCoverage coverage) =>
        coverage == CruiseCabinEvidenceCoverage.Complete ? "Complete evidence" : "Partial evidence";

    internal static string Time(DateTimeOffset value) => value.ToLocalTime().ToString("d MMMM yyyy, HH:mm zzz", DisplayCulture);

    private static string CompactContext(CruiseCabinSearchContext context)
    {
        var adults = context.AdultCount is { } adultCount ? $"{adultCount} adult{(adultCount == 1 ? string.Empty : "s")}" : "Adults: Unknown";
        var children = context.ChildCount is { } childCount ? $"{childCount} child{(childCount == 1 ? string.Empty : "ren")}" : "Children: Unknown";
        var ages = context.ChildAgesKnown
            ? context.ChildAges.Count == 0 ? "child ages known (none)" : $"child ages {string.Join(", ", context.ChildAges)}"
            : "child ages Unknown";
        var airport = context.DepartureAirportId is null ? "departure airport Unknown" : $"departing {context.DepartureAirportId.ToUpperInvariant()}";
        var cabins = context.CabinQuantity is { } count ? $"{count} cabin{(count == 1 ? string.Empty : "s")}" : "cabins Unknown";
        return $"{adults} · {children} · {ages} · {Package(context.PackageMode)} · {airport} · {cabins}";
    }

    private static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "Unknown";
    private static string Nights(int value) => $"{value} night{(value == 1 ? string.Empty : "s")}";
    private static string ChildAges(CruiseCabinSearchContext context) => !context.ChildAgesKnown
        ? "Unknown"
        : context.ChildAges.Count == 0 ? "Known (none)" : string.Join(", ", context.ChildAges);
    private static string Package(CruiseCabinPackageMode mode) => mode switch
    {
        CruiseCabinPackageMode.FlyCruise => "Fly cruise",
        CruiseCabinPackageMode.CruiseOnly => "Cruise only",
        CruiseCabinPackageMode.CruiseAndStay => "Cruise and stay",
        _ => "Unknown"
    };

    private static string Preference(CruiseCabinObservation latest, CruisePreferences? preferences, bool unavailable)
    {
        if (unavailable) return "Preference matching is temporarily unavailable.";
        var preferred = preferences?.PreferredCabins ?? [];
        if (preferred.Count == 0) return "No preferred cabin types configured";
        var matches = preferred.Where(type => latest.StateFor(type) == CruiseCabinAvailabilityState.Available).ToArray();
        if (matches.Length > 0)
            return $"Matches your cabin preferences for this search: {string.Join(", ", matches.Select(Cabin))}";
        if (preferred.Any(type => latest.StateFor(type) == CruiseCabinAvailabilityState.Unknown))
            return "Preference match unknown — some preferred cabin states were not shown";
        return "No preferred cabin type was available when recorded for this search";
    }

    private static IEnumerable<CruiseCabinChangeViewModel> CreateChanges(IReadOnlyList<CruiseCabinObservation> observations)
    {
        if (observations.Count < 2) yield break;
        var previous = observations[^2];
        var current = observations[^1];
        foreach (var type in Enum.GetValues<CruiseCabinType>())
        {
            var before = previous.StateFor(type);
            var after = current.StateFor(type);
            if (before == after) continue;
            var wording = (before, after) switch
            {
                (CruiseCabinAvailabilityState.Unavailable, CruiseCabinAvailabilityState.Available) => "Became available when recorded",
                (CruiseCabinAvailabilityState.Available, CruiseCabinAvailabilityState.Unavailable) => "Became unavailable when recorded",
                (CruiseCabinAvailabilityState.Unknown, _) => "New evidence recorded",
                (_, CruiseCabinAvailabilityState.Unknown) => "The latest evidence no longer confirms the previous state",
                _ => "Cabin evidence changed"
            };
            yield return new(Cabin(type), ShortState(before), ShortState(after), wording);
        }
    }
}
