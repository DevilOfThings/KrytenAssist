using System;
using System.Globalization;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseAlertItemViewModel
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-GB");

    public CruiseAlertItemViewModel(CruiseAlert alert)
    {
        Alert = alert ?? throw new ArgumentNullException(nameof(alert));
        TypeLabel = alert.Type switch
        {
            CruiseAlertType.PriceDrop => "Price drop",
            CruiseAlertType.Promotion => "Promotion",
            CruiseAlertType.SavedCriteria => "Saved criteria",
            CruiseAlertType.NewItinerary => "New itinerary",
            _ => alert.Type.ToString()
        };
        StatusLabel = alert.Status.ToString();
        Summary = CreateSummary(alert);
        DetailText = CreateDetail(alert);
    }

    public CruiseAlert Alert { get; }
    public Guid Id => Alert.Id;
    public CruiseAlertType Type => Alert.Type;
    public CruiseAlertStatus Status => Alert.Status;
    public string TypeLabel { get; }
    public string StatusLabel { get; }
    public string ShipName => Alert.SailingKey?.ShipName ?? (Alert.Details as CruiseNewItineraryAlertDetails)?.ShipName ?? "Itinerary";
    public string SailingText => Alert.SailingKey is { } sailing
        ? $"{ShipName} · {sailing.DepartureDate:dd MMM yyyy}"
        : $"{ShipName} · {Alert.ItineraryCatalogueKey!.ItineraryKey.ProviderItineraryId}";
    public string Summary { get; }
    public string DetailText { get; }
    public string EventTimeText => Alert.EventTime.ToLocalTime().ToString("dd MMM yyyy HH:mm", Culture);
    public string CreatedAtText => Alert.CreatedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm", Culture);
    public string? SourceName => Alert.Source?.Name;
    public bool HasSource => SourceName is not null;
    public bool IsUnread => Status == CruiseAlertStatus.Unread;
    public bool IsRead => Status == CruiseAlertStatus.Read;
    public bool IsDismissed => Status == CruiseAlertStatus.Dismissed;

    private static string CreateSummary(CruiseAlert alert) => alert.Details switch
    {
        CruisePriceDropAlertDetails details =>
            $"Price dropped from {Price(details.PreviousPrice)} to {Price(details.CurrentPrice)} ({details.PercentageReduction.ToString("0.####", Culture)}%).",
        CruisePromotionAlertDetails details => $"New promotion: {details.CurrentSummary}",
        CruiseSavedCriteriaAlertDetails => "This shortlisted sailing met your supported saved criteria.",
        CruiseNewItineraryAlertDetails => "New itinerary observed. First observed by Kryten.",
        _ => "Cruise alert"
    };

    private static string CreateDetail(CruiseAlert alert)
    {
        var common = alert.SailingKey is { } sailing
            ? $"Operator: {sailing.OperatorId}\nShip: {sailing.ShipName}\nDeparture: {sailing.DepartureDate:dd MMM yyyy}\nDuration: {sailing.DurationNights} nights\nEvent: {alert.EventTime.ToLocalTime():dd MMM yyyy HH:mm}\nCreated: {alert.CreatedAt.ToLocalTime():dd MMM yyyy HH:mm}"
            : $"Operator: {alert.ItineraryCatalogueKey!.ItineraryKey.OperatorId}\nItinerary: {alert.ItineraryCatalogueKey.ItineraryKey.ProviderItineraryId}\nEvent: {alert.EventTime.ToLocalTime():dd MMM yyyy HH:mm}\nCreated: {alert.CreatedAt.ToLocalTime():dd MMM yyyy HH:mm}";
        return alert.Details switch
        {
            CruisePriceDropAlertDetails details =>
                $"{common}\nSource: {alert.Source!.Name}\nPrevious price: {Price(details.PreviousPrice)}\nCurrent price: {Price(details.CurrentPrice)}\nReduction: {Money(details.Reduction, details.CurrentPrice.Currency)}\nPercentage reduction: {details.PercentageReduction.ToString("0.####", Culture)}%",
            CruisePromotionAlertDetails details =>
                $"{common}\nSource: {alert.Source!.Name}\nPrevious promotion: {details.PreviousSummary ?? "No previous promotion recorded"}\nCurrent promotion: {details.CurrentSummary}",
            CruiseSavedCriteriaAlertDetails details => SavedCriteriaDetail(common, details),
            _ => common
        };
    }

    private static string SavedCriteriaDetail(string common, CruiseSavedCriteriaAlertDetails details)
    {
        var month = details.MonthConfiguredAndMatched ? "Configured departure month matched" : "No departure month criterion was configured";
        var budget = details.ConfiguredBudget is null
            ? "No budget criterion was configured"
            : $"Budget: {Money(details.ConfiguredBudget.Amount, details.ConfiguredBudget.Currency)} {Basis(details.ConfiguredBudget.Basis)}\nMatched price: {Price(details.MatchedPrice!)}";
        var origin = details.EvidenceOrigin == CruiseAlertEvidenceOrigin.RecordedObservation
            ? "Recorded observation"
            : "Price when saved";
        var cabin = details.CabinPreferencesUnavailable
            ? "\nCabin preferences existed but cabin evidence was unavailable to evaluate."
            : string.Empty;
        return $"{common}\n{month}\n{budget}\nEvidence: {origin}\nEvidence time: {details.EvidenceTime.ToLocalTime():dd MMM yyyy HH:mm}{cabin}";
    }

    private static string Price(CruisePrice price) => $"{Money(price.Amount, price.Currency)}{(string.IsNullOrWhiteSpace(price.Basis) ? string.Empty : $" {price.Basis}")}";
    private static string Money(decimal amount, string currency) => currency == "GBP"
        ? amount.ToString("£0.##", Culture)
        : $"{currency} {amount.ToString("0.##", Culture)}";
    private static string Basis(CruiseBudgetBasis basis) => basis == CruiseBudgetBasis.PerPerson ? "per person" : "total booking";
}
