extern alias KrytenApplication;

using System;
using System.Globalization;
using System.Linq;
using KrytenAssist.Core.Cruises;
using Details = KrytenApplication::KrytenAssist.Application.Cruises.CruiseFirstObservedItineraryDetails;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseNewItineraryItemViewModel
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-GB");
    public CruiseNewItineraryItemViewModel(Details details)
    {
        Details = details ?? throw new ArgumentNullException(nameof(details));
        var entry = details.Entry; var first = entry.FirstOccurrence; var latest = entry.LatestOccurrence;
        Title = first.Title ?? entry.CatalogueKey.ItineraryKey.ProviderItineraryId;
        OperatorId = entry.CatalogueKey.ItineraryKey.OperatorId;
        ProviderItineraryId = entry.CatalogueKey.ItineraryKey.ProviderItineraryId;
        SourceName = latest.Source.Name;
        FirstSeenText = Format(entry.FirstSeenAt); LastSeenText = Format(entry.LastSeenAt);
        CapturedEvidenceText = string.Join(" · ", new[] { first.ShipName, first.DepartureDate?.ToString("dd MMM yyyy", Culture), first.DurationNights is null ? null : $"{first.DurationNights} nights" }.Where(value => value is not null));
        ScopeText = string.Join(Environment.NewLine, details.ConfirmingCheck.Scope.Criteria.Select(criterion =>
            $"{criterion.Name}: {(criterion.State == CruiseDiscoveryCriterionState.Unknown ? "Unknown" : string.Join(", ", criterion.Values))}"));
        SourceReference = latest.SourceReference;
    }
    internal Details Details { get; }
    public string CatalogueKey => Details.Entry.CatalogueKey.PersistenceKey;
    public string Title { get; }
    public string OperatorId { get; }
    public string ProviderItineraryId { get; }
    public string SourceName { get; }
    public string FirstSeenText { get; }
    public string LastSeenText { get; }
    public string CapturedEvidenceText { get; }
    public bool HasCapturedEvidence => !string.IsNullOrWhiteSpace(CapturedEvidenceText);
    public string ScopeText { get; }
    public string ScopeFingerprint => Details.ConfirmingCheck.Scope.Fingerprint;
    public bool WasTruncated => Details.ConfirmingCheck.WasTruncated;
    public int RejectedCount => Details.ConfirmingCheck.Rejections.Count;
    public string EvidenceContext => WasTruncated
        ? $"The confirming check was bounded and truncated; {RejectedCount} candidate(s) were rejected."
        : $"The confirming check was bounded; {RejectedCount} candidate(s) were rejected.";
    public string? SourceReference { get; }
    public string FirstObservedHeading => $"First observed by Kryten on {FirstSeenText}.";
    public string Disclaimer => "This does not prove when TUI published the itinerary. Absence does not mean withdrawn, cancelled or sold out.";
    private static string Format(DateTimeOffset value) => value.ToLocalTime().ToString("dd MMM yyyy HH:mm", Culture);
}
