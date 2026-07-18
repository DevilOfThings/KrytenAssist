extern alias KrytenApplication;

using System;
using System.Globalization;
using KrytenAssist.Core.Cruises;
using Details = KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseDetails;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class SavedCruiseItemViewModel
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-GB");

    public SavedCruiseItemViewModel(Details details)
    {
        Details = details ?? throw new ArgumentNullException(nameof(details));
    }

    public Details Details { get; }
    public SavedCruise SavedCruise => Details.SavedCruise;
    public CruiseSailingKey SailingKey => SavedCruise.SailingKey;
    public string Title => SavedCruise.Snapshot.Title;
    public string OperatorName => SavedCruise.Snapshot.OperatorName;
    public string ShipName => SailingKey.ShipName;
    public string DepartureText => SailingKey.DepartureDate.ToString("d MMM yyyy", DisplayCulture);
    public string DurationText => $"{SailingKey.DurationNights} nights";
    public string? DeparturePort => SavedCruise.Snapshot.DeparturePort;
    public bool HasDeparturePort => !string.IsNullOrWhiteSpace(DeparturePort);
    public string? ItinerarySummary => SavedCruise.Snapshot.ItinerarySummary;
    public bool HasItinerary => !string.IsNullOrWhiteSpace(ItinerarySummary);
    public string InterestText => SavedCruise.Status == SavedCruiseStatus.Dismissed
        ? "Not for us"
        : SavedCruise.Evaluation.InterestLevel switch
        {
            CruiseInterestLevel.Maybe => "Maybe",
            CruiseInterestLevel.StrongCandidate => "Strong candidate",
            _ => "Unrated"
        };
    public string OverallRatingText => SavedCruise.Evaluation.OverallRating?.ToString(DisplayCulture) ?? "Unrated";
    public string PriceWhenSavedText => FormatPrice(SavedCruise.Snapshot.DisplayedPrice);
    public string SavedAtText => SavedCruise.Snapshot.SavedAt.ToString("d MMM yyyy 'at' HH:mm", DisplayCulture);
    public bool HasRecordedHistory => Details.HasRecordedHistory;
    public string LatestRecordedPriceText => Details.LatestRecordedObservation is null
        ? "No recorded price history for this saved cruise."
        : FormatPrice(Details.LatestRecordedObservation.Snapshot.Prices[0]);
    public string? LatestRecordedAtText => Details.LatestRecordedObservation?.ObservedAt
        .ToString("d MMM yyyy 'at' HH:mm", DisplayCulture);
    public string? LatestRecordedSource => Details.LatestRecordedObservation?.Source?.Name;
    public string RecordedContextText => Details.HasRecordedHistory
        ? $"{Details.RecordedObservationCount} observation{Plural(Details.RecordedObservationCount)} across {Details.RecordedSourceCount} source{Plural(Details.RecordedSourceCount)}"
        : "No recorded price history for this saved cruise.";
    public bool IsFavouriteSailing => SavedCruise.IsFavourite;
    public bool IsFavouriteShip => Details.IsFavouriteShip;
    public string FavouriteText => (IsFavouriteSailing, IsFavouriteShip) switch
    {
        (true, true) => "Favourite cruise and ship",
        (true, false) => "Favourite cruise",
        (false, true) => "Favourite ship",
        _ => "Not favourited"
    };
    public bool IsDismissed => SavedCruise.Status == SavedCruiseStatus.Dismissed;
    public string LifecycleButtonText => IsDismissed ? "Restore to Shortlist" : "Not for us";
    public string? RetailSourceText => SavedCruise.Snapshot.RetailSource?.Name;
    public string? SourceReference => SavedCruise.Snapshot.SourceReference;

    private static string Plural(int value) => value == 1 ? string.Empty : "s";

    private static string FormatPrice(CruisePrice price) =>
        $"{price.Currency.ToUpperInvariant()} {price.Amount:N0}" +
        (string.IsNullOrWhiteSpace(price.Basis) ? string.Empty : $" {price.Basis}");
}
