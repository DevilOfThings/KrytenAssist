extern alias KrytenApplication;

using System;
using System.Linq;
using KrytenAssist.Core.Cruises;
using Candidate = KrytenApplication::KrytenAssist.Application.Cruises.CruiseItineraryCaptureCandidateResult;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseItineraryCaptureReviewItemViewModel
{
    public CruiseItineraryCaptureReviewItemViewModel(Candidate candidate)
    {
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        StatusText = candidate.Status.ToString();
        Message = candidate.Message ?? (candidate.Status == KrytenApplication::KrytenAssist.Application.Cruises.CruiseItineraryCaptureCandidateStatus.Ready ? "Ready to record as route evidence." : string.Empty);
        MissingFieldsText = string.Join(", ", candidate.MissingFields);
    }
    internal Candidate Candidate { get; }
    public string Label => Candidate.DisplayLabel;
    public string StatusText { get; }
    public string Message { get; }
    public string MissingFieldsText { get; }
    public bool HasMissingFields => Candidate.MissingFields.Count > 0;
    public bool IsReady => Candidate.Occurrence is not null;
    public string? ProviderItineraryId => Candidate.Occurrence?.ItineraryKey.ProviderItineraryId;
    public string? EvidenceSummary => Candidate.Occurrence is not { } value ? null :
        string.Join(" · ", new[] { value.Title, value.ShipName, value.DepartureDate?.ToString("dd MMM yyyy"), value.DurationNights is null ? null : $"{value.DurationNights} nights", value.DeparturePort }.Where(item => item is not null));
    public bool HasEvidenceSummary => !string.IsNullOrWhiteSpace(EvidenceSummary);
}
