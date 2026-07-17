extern alias KrytenApplication;

using System;
using System.Globalization;
using KrytenAssist.Avalonia.Tools;
using KrytenAssist.Core.Cruises;
using HistoryDetails = KrytenApplication::KrytenAssist.Application.Cruises.CruiseHistoryDetails;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseHistoryItemViewModel
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-GB");

    public CruiseHistoryItemViewModel(HistoryDetails details, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(clock);
        Details = details;
        var current = details.History.Observations[^1];
        var offer = current.Snapshot.Offer;
        Title = offer.Title;
        Operator = offer.Provider.Name;
        Ship = offer.ShipName;
        DepartureDate = offer.DepartureDate.ToString("d MMMM yyyy", DisplayCulture);
        Duration = $"{offer.DurationNights} nights";
        RetailSource = details.History.Source?.Name ?? "Source unavailable";
        CurrentPrice = FormatPrice(details.Summary.CurrentPrice);
        LowestPrice = FormatPrice(details.Summary.LowestPrice);
        HighestPrice = FormatPrice(details.Summary.HighestPrice);
        Trend = FormatMovement(details.Summary.Movement);
        FirstObserved = FormatTimestamp(details.Summary.FirstObservedAt);
        LastObserved = FormatTimestamp(details.Summary.LastObservedAt);
        LastSeen = FormatTimestamp(details.History.LastSeenAt);
        ObservationCount = details.Summary.ObservationCount.ToString(DisplayCulture);
        LatestProviderOfferId = details.History.LatestEvidence.ProviderOfferId;
        LatestSourceReference = details.History.LatestEvidence.SourceReference;
        HasLatestSourceReference = !string.IsNullOrWhiteSpace(LatestSourceReference);
        IsPastSailing = offer.DepartureDate < DateOnly.FromDateTime(clock.Now.DateTime);
        SailingStatus = IsPastSailing ? "Past sailing" : "Upcoming sailing";
        HasComparablePrice = details.Summary.CurrentPrice is not null;
    }

    internal HistoryDetails Details { get; }
    public string Title { get; }
    public string Operator { get; }
    public string Ship { get; }
    public string DepartureDate { get; }
    public string Duration { get; }
    public string RetailSource { get; }
    public string CurrentPrice { get; }
    public string LowestPrice { get; }
    public string HighestPrice { get; }
    public string Trend { get; }
    public string FirstObserved { get; }
    public string LastObserved { get; }
    public string LastSeen { get; }
    public string ObservationCount { get; }
    public string LatestProviderOfferId { get; }
    public string? LatestSourceReference { get; }
    public bool HasLatestSourceReference { get; }
    public bool HasComparablePrice { get; }
    public bool IsPastSailing { get; }
    public string SailingStatus { get; }

    public bool Matches(CruiseSailingKey sailingKey, CruiseSource? source) =>
        Details.History.SailingKey == sailingKey
        && string.Equals(
            Details.History.Source?.Id,
            CruiseObservationFingerprint.RetailSourceKey(source),
            StringComparison.Ordinal);

    public static string FormatPrice(CruisePrice? price)
    {
        if (price is null)
        {
            return "Comparable price unavailable";
        }

        var amount = price.Amount.ToString("N2", DisplayCulture).TrimEnd('0').TrimEnd('.');
        var currency = string.Equals(price.Currency, "GBP", StringComparison.Ordinal)
            ? "£"
            : $"{price.Currency} ";
        return $"{currency}{amount}{(price.Basis is null ? string.Empty : $" {price.Basis}")}";
    }

    public static string FormatMovement(CruisePriceMovement movement) =>
        movement.Direction switch
        {
            CruisePriceTrendDirection.FirstObservation => "First observation",
            CruisePriceTrendDirection.Lower => $"Down {FormatDelta(movement)}",
            CruisePriceTrendDirection.Higher => $"Up {FormatDelta(movement)}",
            CruisePriceTrendDirection.Unchanged => "Unchanged",
            _ => "Comparable price unavailable"
        };

    private static string FormatDelta(CruisePriceMovement movement)
    {
        var price = movement.CurrentPrice;
        if (price is null || movement.Delta is null)
        {
            return "an unavailable amount";
        }

        return FormatPrice(new CruisePrice(movement.Delta.Value, price.Currency));
    }

    private static string FormatTimestamp(DateTimeOffset value) =>
        value.ToString("d MMMM yyyy 'at' HH:mm zzz", DisplayCulture);
}
