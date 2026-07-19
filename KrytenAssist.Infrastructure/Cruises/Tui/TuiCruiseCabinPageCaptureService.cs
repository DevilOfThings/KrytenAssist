using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KrytenAssist.Application.Cruises;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Infrastructure.Cruises.Tui;

public sealed class TuiCruiseCabinPageCaptureService : ICruiseCabinPageCaptureService
{
    private const int PayloadVersion = 2;
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public Task<CruiseCabinCaptureBatchResult> CaptureAsync(CruiseCabinPageCaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (cancellationToken.IsCancellationRequested) return Task.FromResult(Cancelled());
        if (!TryValidatePage(request, out var page)) return Task.FromResult(CruiseCabinCaptureBatchResult.Unsupported("This page is not supported for TUI cabin capture."));

        Payload? payload;
        try { payload = JsonSerializer.Deserialize<Payload>(request.PagePayload, SerializerOptions); }
        catch (JsonException) { return Task.FromResult(CruiseCabinCaptureBatchResult.Failed("The TUI cabin page data could not be read.")); }
        if (payload?.Version is not (PayloadVersion or 3)) return Task.FromResult(CruiseCabinCaptureBatchResult.Unsupported("This version of the TUI cabin page data is not supported."));
        if (payload.Candidates is null || payload.Candidates.Count == 0) return Task.FromResult(CruiseCabinCaptureBatchResult.Incomplete("No supported TUI cruise cards were found."));
        if (payload.Candidates.Count > CruiseCabinCaptureBatchResult.MaximumCandidateCount) return Task.FromResult(CruiseCabinCaptureBatchResult.Failed("The TUI page returned too many cabin candidates."));

        var contextValues = ParseContext(page);
        var results = new List<CruiseCabinCaptureCandidateResult>();
        var references = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < payload.Candidates.Count; index++)
        {
            if (cancellationToken.IsCancellationRequested) return Task.FromResult(Cancelled());
            var candidate = payload.Candidates[index];
            if (candidate is null) return Task.FromResult(CruiseCabinCaptureBatchResult.Failed("The TUI page returned invalid cabin candidate data."));
            if (!TryValidateCandidate(candidate.SourceReference, out var reference)) return Task.FromResult(CruiseCabinCaptureBatchResult.Failed("A TUI cabin candidate did not contain a trusted itinerary address."));
            if (!references.Add(reference)) continue;
            results.Add(Map(candidate, reference, index, request, contextValues));
        }
        if (cancellationToken.IsCancellationRequested) return Task.FromResult(Cancelled());
        return results.Count == 0
            ? Task.FromResult(CruiseCabinCaptureBatchResult.Incomplete("No supported TUI cruise cards were found."))
            : Task.FromResult(CruiseCabinCaptureBatchResult.Completed(results, payload.WasTruncated));
    }

    private static CruiseCabinCaptureCandidateResult Map(Candidate value, string reference, int index,
        CruiseCabinPageCaptureRequest request, ContextValues context)
    {
        var label = string.IsNullOrWhiteSpace(value.Title) ? $"Cruise candidate {index + 1}" : value.Title.Trim();
        if (label.Length > 512) label = label[..512];
        if (value.CabinEvidence is null)
            return CruiseCabinCaptureCandidateResult.Incomplete(label, reference, ["cabinEvidence"], "The TUI card did not show supported cabin evidence.");
        if (!string.Equals(value.CabinEvidence.RetailerLabel, "Inside Cabin", StringComparison.OrdinalIgnoreCase) ||
            value.CabinEvidence.Quantity != 1 ||
            !string.Equals(value.CabinEvidence.Qualifier, "Cheapest available", StringComparison.OrdinalIgnoreCase))
            return CruiseCabinCaptureCandidateResult.Unsupported(label, reference, "The TUI card used cabin wording that Kryten does not yet support.");

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(value.ShipName) || value.ShipName.Length > 512) missing.Add("shipName");
        if (string.IsNullOrWhiteSpace(value.DepartureDate) || !DateOnly.TryParseExact(value.DepartureDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) missing.Add("departureDate");
        if (value.DurationNights is null or < 1) missing.Add("durationNights");
        if (missing.Count > 0) return CruiseCabinCaptureCandidateResult.Incomplete(label, reference, missing, "The TUI card did not provide the sailing identity required for cabin evidence.");

        try
        {
            var searchContext = new CruiseCabinSearchContext(context.Adults, context.Children,
                context.ChildAges, context.ChildAgesKnown, context.PackageMode, context.Airport, 1);
            var sailing = new CruiseSailingKey("marella", value.ShipName!,
                DateOnly.ParseExact(value.DepartureDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture), value.DurationNights!.Value);
            var states = Enum.GetValues<CruiseCabinType>().Select(type => new CruiseCabinState(type,
                type == CruiseCabinType.Inside ? CruiseCabinAvailabilityState.Available : CruiseCabinAvailabilityState.Unknown));
            var evidenceKey = "tui-cabin:v1:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
                $"{reference}|inside cabin|1|cheapest available")));
            var observation = new CruiseCabinObservation(sailing, request.Source, searchContext,
                CruiseCabinEvidenceCoverage.Partial, states, request.ObservedAt, evidenceKey, reference);
            return CruiseCabinCaptureCandidateResult.Ready(label, reference, observation);
        }
        catch (ArgumentException)
        {
            return CruiseCabinCaptureCandidateResult.Failed(label, reference, "The TUI cabin evidence could not be mapped safely.");
        }
    }

    private static ContextValues ParseContext(Uri page)
    {
        var values = QueryValues(page);
        var adults = OneNumber(values, "noOfAdults", 0, CruiseCabinSearchContext.MaximumPartySize);
        var children = OneNumber(values, "noOfChildren", 0, CruiseCabinSearchContext.MaximumPartySize);
        var airportValue = One(values, "from[]");
        string? airport = null;
        if (airportValue is not null && airportValue.EndsWith(":Airport", StringComparison.OrdinalIgnoreCase))
            airport = airportValue[..^8].Trim();
        if (string.IsNullOrWhiteSpace(airport) || airport.Length > CruiseCabinSearchContext.MaximumAirportIdLength) airport = null;
        var ages = Array.Empty<int>();
        var agesKnown = children == 0;
        if (children is > 0 && One(values, "childrenAge") is { } ageText)
        {
            var parsed = ageText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(text => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var age) ? age : -1).ToArray();
            if (parsed.Length == children && parsed.All(age => age is >= 0 and <= 17)) { ages = parsed; agesKnown = true; }
        }
        return new(adults, children, ages, agesKnown,
            airport is null ? CruiseCabinPackageMode.Unknown : CruiseCabinPackageMode.FlyCruise, airport);
    }

    private static Dictionary<string, List<string>> QueryValues(Uri uri)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var split = item.Split('=', 2); var key = Uri.UnescapeDataString(split[0]); var value = Uri.UnescapeDataString(split.Length == 2 ? split[1] : "");
            if (!result.TryGetValue(key, out var list)) result[key] = list = [];
            list.Add(value);
        }
        return result;
    }

    private static string? One(Dictionary<string, List<string>> values, string key) =>
        values.TryGetValue(key, out var items) && items.Count == 1 && !string.IsNullOrWhiteSpace(items[0]) ? items[0] : null;
    private static int? OneNumber(Dictionary<string, List<string>> values, string key, int minimum, int maximum) =>
        One(values, key) is { } text && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number >= minimum && number <= maximum ? number : null;

    private static bool TryValidatePage(CruiseCabinPageCaptureRequest request, out Uri page)
    {
        page = null!;
        if (!Uri.TryCreate(request.SourceReference, UriKind.Absolute, out var address)) return false;
        var valid = string.Equals(request.SourceIdentifier, TuiCruisePageCaptureService.SupportedSourceIdentifier, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(request.Source.Id, TuiCruisePageCaptureService.SupportedRetailSourceIdentifier, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(request.Source.Name, "TUI", StringComparison.OrdinalIgnoreCase) &&
            address.Scheme == Uri.UriSchemeHttps && string.Equals(address.Host, TuiCruisePageCaptureService.SupportedHost, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(address.AbsolutePath.TrimEnd('/'), "/cruise/packages", StringComparison.OrdinalIgnoreCase);
        if (valid) page = address;
        return valid;
    }

    private static bool TryValidateCandidate(string? value, out string reference)
    {
        reference = string.Empty;
        if (string.IsNullOrWhiteSpace(value) || value.Length > 4_000 || !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || !string.Equals(uri.Host, TuiCruisePageCaptureService.SupportedHost, StringComparison.OrdinalIgnoreCase) ||
            !uri.AbsolutePath.StartsWith("/cruise/bookitineraries/", StringComparison.OrdinalIgnoreCase)) return false;
        var query = QueryValues(uri);
        if (One(query, "itineraryCodeOne") is null && One(query, "itineraryCode") is null) return false;
        reference = value; return true;
    }

    private static CruiseCabinCaptureBatchResult Cancelled() => CruiseCabinCaptureBatchResult.Cancelled("Cabin capture was cancelled.");
    private sealed record ContextValues(int? Adults, int? Children, IReadOnlyList<int> ChildAges, bool ChildAgesKnown, CruiseCabinPackageMode PackageMode, string? Airport);
    private sealed class Payload { public int? Version { get; init; } public bool WasTruncated { get; init; } public List<Candidate?>? Candidates { get; init; } }
    private sealed class Candidate { public string? SourceReference { get; init; } public string? Title { get; init; } public string? ShipName { get; init; } public string? DepartureDate { get; init; } public int? DurationNights { get; init; } public CabinEvidence? CabinEvidence { get; init; } }
    private sealed class CabinEvidence { public string? RetailerLabel { get; init; } public int? Quantity { get; init; } public string? Qualifier { get; init; } }
}
