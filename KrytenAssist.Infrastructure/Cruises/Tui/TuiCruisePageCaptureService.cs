using System.Globalization;
using System.Text.Json;
using KrytenAssist.Application.Cruises;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Infrastructure.Cruises.Tui;

public sealed class TuiCruisePageCaptureService : ICruisePageCaptureService
{
    public const string SupportedSourceIdentifier = "marella-cruise-of-the-week";
    public const string SupportedRetailSourceIdentifier = "tui";
    public const string SupportedHost = "www.tui.co.uk";
    public const int SupportedPayloadVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<CruiseCaptureResult> CaptureAsync(
        CruisePageCaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(Cancelled());
        }

        var unsupported = ValidateSource(request);
        if (unsupported is not null)
        {
            return Task.FromResult(unsupported);
        }

        TuiCapturePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TuiCapturePayload>(
                request.PagePayload,
                SerializerOptions);
        }
        catch (JsonException)
        {
            return Task.FromResult(CruiseCaptureResult.Failed(
                "The TUI page data could not be read. Refresh the page and try again."));
        }

        if (payload?.Version != SupportedPayloadVersion)
        {
            return Task.FromResult(CruiseCaptureResult.Unsupported(
                "This version of the TUI page data is not supported."));
        }

        if (payload.Candidates is null || payload.Candidates.Count == 0)
        {
            return Task.FromResult(CruiseCaptureResult.Incomplete(
                "Kryten could not identify a cruise on this TUI page.",
                ["candidates"]));
        }

        if (payload.Candidates.Count > 1)
        {
            return Task.FromResult(CruiseCaptureResult.Ambiguous(
                "More than one cruise was found. Open a specific itinerary and try again."));
        }

        if (payload.Candidates[0] is not { } candidate)
        {
            return Task.FromResult(CruiseCaptureResult.Failed(
                "The TUI page returned invalid cruise data. Refresh the page and try again."));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(Cancelled());
        }

        var missingFields = FindMissingFields(candidate, cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(Cancelled());
        }

        if (missingFields.Count > 0)
        {
            return Task.FromResult(CruiseCaptureResult.Incomplete(
                "The TUI page did not provide all required cruise details.",
                missingFields));
        }

        try
        {
            var observation = MapObservation(candidate, request);
            return Task.FromResult(CruiseCaptureResult.Succeeded(observation));
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(CruiseCaptureResult.Failed(
                "The TUI cruise details could not be captured. Refresh the page and try again."));
        }
    }

    private static CruiseCaptureResult? ValidateSource(CruisePageCaptureRequest request)
    {
        if (!string.Equals(
                request.SourceIdentifier,
                SupportedSourceIdentifier,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                request.Source.Id,
                SupportedRetailSourceIdentifier,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Source.Name, "TUI", StringComparison.OrdinalIgnoreCase) ||
            !Uri.TryCreate(request.SourceReference, UriKind.Absolute, out var address) ||
            address.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(address.Host, SupportedHost, StringComparison.OrdinalIgnoreCase))
        {
            return CruiseCaptureResult.Unsupported(
                "This cruise source is not supported for TUI capture.");
        }

        return null;
    }

    private static IReadOnlyList<string> FindMissingFields(
        TuiCruiseCandidate candidate,
        CancellationToken cancellationToken)
    {
        var missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddIfBlank(missing, candidate.ProviderOfferId, "providerOfferId");
        AddIfBlank(missing, candidate.Title, "title");
        AddIfBlank(missing, candidate.ShipName, "shipName");

        if (string.IsNullOrWhiteSpace(candidate.DepartureDate) ||
            !DateOnly.TryParseExact(
                candidate.DepartureDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            missing.Add("departureDate");
        }

        if (candidate.DurationNights is null or < 1)
        {
            missing.Add("durationNights");
        }

        if (candidate.Prices is null || candidate.Prices.Count == 0)
        {
            missing.Add("prices");
        }
        else
        {
            foreach (var price in candidate.Prices)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (price?.Amount is null or < 0)
                {
                    missing.Add("prices.amount");
                }

                if (price is null || !IsValidCurrency(price.Currency))
                {
                    missing.Add("prices.currency");
                }
            }
        }

        return missing.ToArray();
    }

    private static CruiseObservation MapObservation(
        TuiCruiseCandidate candidate,
        CruisePageCaptureRequest request)
    {
        var departureDate = DateOnly.ParseExact(
            candidate.DepartureDate!,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);
        var prices = candidate.Prices!
            .Select(price => new CruisePrice(
                price!.Amount!.Value,
                price.Currency!,
                Optional(price.Basis)))
            .ToArray();
        var offer = new CruiseOffer(
            new CruiseProvider("marella", "Marella Cruises"),
            candidate.ProviderOfferId!,
            candidate.Title!,
            candidate.ShipName!,
            departureDate,
            candidate.DurationNights!.Value,
            Optional(candidate.DeparturePort),
            Optional(candidate.ItinerarySummary));
        var snapshot = new CruiseSnapshot(
            offer,
            prices,
            Optional(candidate.PromotionSummary));

        return new CruiseObservation(
            snapshot,
            request.ObservedAt,
            request.SourceReference,
            request.Source);
    }

    private static void AddIfBlank(HashSet<string> fields, string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            fields.Add(name);
        }
    }

    private static bool IsValidCurrency(string? value) =>
        value is { Length: 3 } && value.All(char.IsAsciiLetter);

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static CruiseCaptureResult Cancelled() =>
        CruiseCaptureResult.Cancelled("Cruise capture was cancelled.");

    private sealed class TuiCapturePayload
    {
        public int? Version { get; init; }

        public List<TuiCruiseCandidate?>? Candidates { get; init; }
    }

    private sealed class TuiCruiseCandidate
    {
        public string? ProviderOfferId { get; init; }

        public string? Title { get; init; }

        public string? ShipName { get; init; }

        public string? DepartureDate { get; init; }

        public int? DurationNights { get; init; }

        public string? DeparturePort { get; init; }

        public string? ItinerarySummary { get; init; }

        public List<TuiCruisePrice?>? Prices { get; init; }

        public string? PromotionSummary { get; init; }
    }

    private sealed class TuiCruisePrice
    {
        public decimal? Amount { get; init; }

        public string? Currency { get; init; }

        public string? Basis { get; init; }
    }
}
