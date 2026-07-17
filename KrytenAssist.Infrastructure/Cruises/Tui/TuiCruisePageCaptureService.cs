using System.Globalization;
using System.Text.Json;
using KrytenAssist.Application.Cruises;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Infrastructure.Cruises.Tui;

public sealed class TuiCruisePageCaptureService :
    ICruisePageCaptureService,
    ICruisePageBatchCaptureService
{
    public const string SupportedSourceIdentifier = "marella-cruise-of-the-week";
    public const string SupportedRetailSourceIdentifier = "tui";
    public const string SupportedHost = "www.tui.co.uk";
    public const int SupportedPayloadVersion = 1;
    public const int MaximumPayloadFieldLength = 512;

    private const string UnsupportedSourceMessage =
        "This cruise source is not supported for TUI capture.";
    private const string UnsupportedPayloadMessage =
        "This version of the TUI page data is not supported.";
    private const string InvalidPayloadMessage =
        "The TUI page data could not be read. Refresh the page and try again.";
    private const string MissingCandidateMessage =
        "Kryten could not identify a cruise on this TUI page.";
    private const string MissingCandidateBatchMessage =
        "Kryten could not identify supported cruise deal cards on this TUI page.";

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
            return Task.FromResult(SingleCancelled());
        }

        if (!IsSupportedSource(request))
        {
            return Task.FromResult(CruiseCaptureResult.Unsupported(UnsupportedSourceMessage));
        }

        var read = ReadPayload(request.PagePayload);
        if (read.IsMalformed)
        {
            return Task.FromResult(CruiseCaptureResult.Failed(InvalidPayloadMessage));
        }

        if (read.Payload?.Version != SupportedPayloadVersion)
        {
            return Task.FromResult(CruiseCaptureResult.Unsupported(UnsupportedPayloadMessage));
        }

        var candidates = read.Payload.Candidates;
        if (candidates is null || candidates.Count == 0)
        {
            return Task.FromResult(CruiseCaptureResult.Incomplete(
                MissingCandidateMessage,
                ["candidates"]));
        }

        if (candidates.Count > 1)
        {
            return Task.FromResult(CruiseCaptureResult.Ambiguous(
                "More than one cruise was found. Open a specific itinerary and try again."));
        }

        if (candidates[0] is not { } candidate)
        {
            return Task.FromResult(CruiseCaptureResult.Failed(
                "The TUI page returned invalid cruise data. Refresh the page and try again."));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(SingleCancelled());
        }

        var missingFields = FindMissingFields(candidate, cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(SingleCancelled());
        }

        if (missingFields.Count > 0)
        {
            return Task.FromResult(CruiseCaptureResult.Incomplete(
                "The TUI page did not provide all required cruise details.",
                missingFields));
        }

        try
        {
            var observation = MapObservation(candidate, request, request.SourceReference);
            return Task.FromResult(CruiseCaptureResult.Succeeded(observation));
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(CruiseCaptureResult.Failed(
                "The TUI cruise details could not be captured. Refresh the page and try again."));
        }
    }

    Task<CruiseCaptureBatchResult> ICruisePageBatchCaptureService.CaptureAsync(
        CruisePageCaptureRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(BatchCancelled());
        }

        if (!IsSupportedSource(request))
        {
            return Task.FromResult(CruiseCaptureBatchResult.Unsupported(UnsupportedSourceMessage));
        }

        var read = ReadPayload(request.PagePayload);
        if (read.IsMalformed)
        {
            return Task.FromResult(CruiseCaptureBatchResult.Failed(InvalidPayloadMessage));
        }

        if (read.Payload?.Version != SupportedPayloadVersion)
        {
            return Task.FromResult(CruiseCaptureBatchResult.Unsupported(UnsupportedPayloadMessage));
        }

        var candidates = read.Payload.Candidates;
        if (candidates is null || candidates.Count == 0)
        {
            return Task.FromResult(CruiseCaptureBatchResult.Incomplete(
                MissingCandidateBatchMessage));
        }

        if (candidates.Count > CruiseCaptureBatchResult.MaximumCandidateCount)
        {
            return Task.FromResult(CruiseCaptureBatchResult.Failed(
                "The TUI page returned more cruise cards than Kryten can capture safely."));
        }

        var results = new List<CruiseCaptureCandidateResult>(candidates.Count);
        var seenReferences = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < candidates.Count; index++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromResult(BatchCancelled());
            }

            var candidate = candidates[index];
            if (candidate is null)
            {
                return Task.FromResult(CruiseCaptureBatchResult.Failed(
                    "The TUI page returned invalid cruise-card data."));
            }

            if (!TryGetTrustedCandidateReference(candidate.SourceReference, out var sourceReference))
            {
                return Task.FromResult(CruiseCaptureBatchResult.Failed(
                    "A TUI cruise card did not contain a trusted itinerary address."));
            }

            if (!seenReferences.Add(sourceReference))
            {
                continue;
            }

            results.Add(MapCandidate(candidate, request, sourceReference, index, cancellationToken));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(BatchCancelled());
        }

        if (results.Count == 0)
        {
            return Task.FromResult(CruiseCaptureBatchResult.Incomplete(
                MissingCandidateBatchMessage));
        }

        return Task.FromResult(CruiseCaptureBatchResult.Completed(
            results,
            read.Payload.WasTruncated));
    }

    private static CruiseCaptureCandidateResult MapCandidate(
        TuiCruiseCandidate candidate,
        CruisePageCaptureRequest request,
        string sourceReference,
        int index,
        CancellationToken cancellationToken)
    {
        var displayLabel = SafeDisplayLabel(candidate.Title, index);
        if (HasOversizedOptionalField(candidate))
        {
            return CruiseCaptureCandidateResult.Failed(
                displayLabel,
                sourceReference,
                "The TUI cruise card contained details that could not be mapped safely.");
        }

        var missingFields = FindMissingFields(candidate, cancellationToken);
        if (missingFields.Count > 0)
        {
            return CruiseCaptureCandidateResult.Incomplete(
                displayLabel,
                sourceReference,
                "The TUI cruise card did not provide all required cruise details.",
                missingFields);
        }

        try
        {
            var observation = MapObservation(candidate, request, sourceReference);
            return CruiseCaptureCandidateResult.Ready(
                displayLabel,
                sourceReference,
                observation);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return CruiseCaptureCandidateResult.Failed(
                displayLabel,
                sourceReference,
                "The TUI cruise card could not be mapped safely.");
        }
    }

    private static PayloadReadResult ReadPayload(string pagePayload)
    {
        try
        {
            return new PayloadReadResult(
                JsonSerializer.Deserialize<TuiCapturePayload>(pagePayload, SerializerOptions),
                false);
        }
        catch (JsonException)
        {
            return new PayloadReadResult(null, true);
        }
    }

    private static bool IsSupportedSource(CruisePageCaptureRequest request) =>
        string.Equals(
            request.SourceIdentifier,
            SupportedSourceIdentifier,
            StringComparison.OrdinalIgnoreCase) &&
        string.Equals(
            request.Source.Id,
            SupportedRetailSourceIdentifier,
            StringComparison.OrdinalIgnoreCase) &&
        string.Equals(request.Source.Name, "TUI", StringComparison.OrdinalIgnoreCase) &&
        Uri.TryCreate(request.SourceReference, UriKind.Absolute, out var address) &&
        address.Scheme == Uri.UriSchemeHttps &&
        string.Equals(address.Host, SupportedHost, StringComparison.OrdinalIgnoreCase);

    private static bool TryGetTrustedCandidateReference(
        string? value,
        out string sourceReference)
    {
        sourceReference = string.Empty;
        try
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Length > CruiseCaptureCandidateResult.MaximumSourceReferenceLength ||
                !Uri.TryCreate(value, UriKind.Absolute, out var address) ||
                address.Scheme != Uri.UriSchemeHttps ||
                !string.Equals(address.Host, SupportedHost, StringComparison.OrdinalIgnoreCase) ||
                !address.AbsolutePath.StartsWith(
                    "/cruise/bookitineraries/",
                    StringComparison.OrdinalIgnoreCase) ||
                !(HasQueryValue(address, "itineraryCodeOne") ||
                  HasQueryValue(address, "itineraryCode")))
            {
                return false;
            }

            sourceReference = value;
            return true;
        }
        catch (UriFormatException)
        {
            return false;
        }
    }

    private static bool HasQueryValue(Uri address, string name)
    {
        var query = address.Query.AsSpan().TrimStart('?');
        foreach (var item in query.ToString().Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = item.IndexOf('=');
            var key = separator < 0 ? item : item[..separator];
            var value = separator < 0 ? string.Empty : item[(separator + 1)..];
            if (string.Equals(Uri.UnescapeDataString(key), name, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(Uri.UnescapeDataString(value)))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<string> FindMissingFields(
        TuiCruiseCandidate candidate,
        CancellationToken cancellationToken)
    {
        var missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddIfBlankOrTooLong(missing, candidate.ProviderOfferId, "providerOfferId");
        AddIfBlankOrTooLong(missing, candidate.Title, "title");
        AddIfBlankOrTooLong(missing, candidate.ShipName, "shipName");

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
        CruisePageCaptureRequest request,
        string sourceReference)
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
            sourceReference,
            request.Source);
    }

    private static string SafeDisplayLabel(string? title, int index)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return $"Cruise candidate {index + 1}";
        }

        return title.Length <= CruiseCaptureCandidateResult.MaximumDisplayLabelLength
            ? title
            : title[..CruiseCaptureCandidateResult.MaximumDisplayLabelLength];
    }

    private static void AddIfBlankOrTooLong(
        HashSet<string> fields,
        string? value,
        string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaximumPayloadFieldLength)
        {
            fields.Add(name);
        }
    }

    private static bool HasOversizedOptionalField(TuiCruiseCandidate candidate) =>
        IsOversized(candidate.DeparturePort) ||
        IsOversized(candidate.ItinerarySummary) ||
        IsOversized(candidate.PromotionSummary) ||
        (candidate.Prices?.Any(price => IsOversized(price?.Basis)) ?? false);

    private static bool IsOversized(string? value) =>
        value?.Length > MaximumPayloadFieldLength;

    private static bool IsValidCurrency(string? value) =>
        value is { Length: 3 } && value.All(char.IsAsciiLetter);

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static CruiseCaptureResult SingleCancelled() =>
        CruiseCaptureResult.Cancelled("Cruise capture was cancelled.");

    private static CruiseCaptureBatchResult BatchCancelled() =>
        CruiseCaptureBatchResult.Cancelled("Cruise capture was cancelled.");

    private sealed record PayloadReadResult(TuiCapturePayload? Payload, bool IsMalformed);

    private sealed class TuiCapturePayload
    {
        public int? Version { get; init; }

        public bool WasTruncated { get; init; }

        public List<TuiCruiseCandidate?>? Candidates { get; init; }
    }

    private sealed class TuiCruiseCandidate
    {
        public string? SourceReference { get; init; }

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
