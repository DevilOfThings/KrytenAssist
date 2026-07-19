using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KrytenAssist.Application.Cruises;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Infrastructure.Cruises.Tui;

public sealed class TuiCruiseItineraryPageCaptureService : ICruiseItineraryPageCaptureService
{
    private const int PayloadVersion = 3;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly (string Query, string Semantic)[] MaterialCriteria =
    [
        ("from[]", "departure-airport"), ("to[]", "destination"), ("when", "departure-date"),
        ("flexibility", "date-flexibility"), ("flexibleDays", "flexible-days"), ("duration", "duration"),
        ("addAStay", "add-a-stay"), ("until", "end-date"), ("noOfAdults", "adult-count"),
        ("noOfChildren", "child-count"), ("childrenAge", "child-ages"), ("room", "room"),
        ("choiceSearch", "choice-search"), ("searchRequestType", "search-request-type"),
        ("searchType", "search-type"), ("sp", "single-package")
    ];

    public Task<CruiseItineraryCaptureBatchResult> CaptureAsync(CruiseItineraryPageCaptureRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (cancellationToken.IsCancellationRequested) return Task.FromResult(Cancelled());
        if (!TryValidatePage(request, out var page)) return Task.FromResult(CruiseItineraryCaptureBatchResult.Unsupported("This page is not supported for TUI itinerary capture."));
        if (!TryMapScope(page, request.Source, out var scope, out var scopeMessage)) return Task.FromResult(CruiseItineraryCaptureBatchResult.Incomplete(scopeMessage!));
        Payload? payload;
        try { payload = JsonSerializer.Deserialize<Payload>(request.PagePayload, JsonOptions); }
        catch (JsonException) { return Task.FromResult(CruiseItineraryCaptureBatchResult.Failed("The TUI itinerary page data could not be read.")); }
        if (payload?.Version != PayloadVersion) return Task.FromResult(CruiseItineraryCaptureBatchResult.Unsupported("This version of the TUI itinerary page data is not supported."));
        if (payload.Candidates is null || payload.Candidates.Count == 0) return Task.FromResult(CruiseItineraryCaptureBatchResult.Incomplete("No supported TUI itinerary cards were found."));
        if (payload.Candidates.Count > CruiseDiscoveryCheck.MaximumOccurrenceCount) return Task.FromResult(CruiseItineraryCaptureBatchResult.Failed("The TUI page returned too many itinerary candidates."));

        var mapped = new List<CruiseItineraryCaptureCandidateResult>();
        foreach (var candidate in payload.Candidates)
        {
            if (cancellationToken.IsCancellationRequested) return Task.FromResult(Cancelled());
            if (candidate is null) return Task.FromResult(CruiseItineraryCaptureBatchResult.Failed("The TUI page returned invalid itinerary candidate data."));
            mapped.Add(MapCandidate(candidate, request));
        }
        mapped = Deduplicate(mapped);
        return Task.FromResult(CruiseItineraryCaptureBatchResult.Completed(scope!, mapped, payload.WasTruncated));
    }

    private static CruiseItineraryCaptureCandidateResult MapCandidate(Candidate value, CruiseItineraryPageCaptureRequest request)
    {
        var label = SafeLabel(value.Title);
        if (!TryTrustedItinerary(value.SourceReference, out var reference, out var urlCode))
            return CruiseItineraryCaptureCandidateResult.Failed(label, "The TUI itinerary candidate did not contain a trusted itinerary address.");
        if (string.IsNullOrWhiteSpace(value.ProviderItineraryId))
            return CruiseItineraryCaptureCandidateResult.Ineligible(label, ["providerItineraryId"], "The TUI card did not provide a stable itinerary identity.");
        if (value.ProviderItineraryId.Length > CruiseItineraryKey.MaximumProviderItineraryIdLength)
            return CruiseItineraryCaptureCandidateResult.Failed(label, "The TUI itinerary identity was too long.");
        if (!string.Equals(Normalize(value.ProviderItineraryId), Normalize(urlCode), StringComparison.Ordinal))
            return CruiseItineraryCaptureCandidateResult.Failed(label, "The TUI itinerary identity did not match its trusted address.");
        if (Oversized(value.Title, 1000) || Oversized(value.ShipName, 1000) || Oversized(value.DeparturePort, 1000) ||
            Oversized(value.ItinerarySummary, 4000) || Oversized(value.ProviderOfferId, 1000))
            return CruiseItineraryCaptureCandidateResult.Failed(label, "The TUI itinerary card contained oversized details.");
        DateOnly? departure = null;
        if (value.DepartureDate is not null)
        {
            if (!DateOnly.TryParseExact(value.DepartureDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return CruiseItineraryCaptureCandidateResult.Incomplete(label, ["departureDate"], "The TUI itinerary card contained an invalid departure date.");
            departure = parsed;
        }
        if (value.DurationNights is < 1 or > CruiseItineraryOccurrence.MaximumDurationNights)
            return CruiseItineraryCaptureCandidateResult.Incomplete(label, ["durationNights"], "The TUI itinerary card contained an invalid duration.");
        try
        {
            var evidence = EvidenceKey(reference, value);
            var occurrence = new CruiseItineraryOccurrence(new("marella", value.ProviderItineraryId), request.Source,
                request.ObservedAt, evidence, Optional(value.Title), Optional(value.ShipName), departure, value.DurationNights,
                Optional(value.DeparturePort), Optional(value.ItinerarySummary), Optional(value.ProviderOfferId), reference);
            return CruiseItineraryCaptureCandidateResult.Ready(label, occurrence);
        }
        catch (ArgumentException) { return CruiseItineraryCaptureCandidateResult.Failed(label, "The TUI itinerary card could not be mapped safely."); }
    }

    private static List<CruiseItineraryCaptureCandidateResult> Deduplicate(List<CruiseItineraryCaptureCandidateResult> values)
    {
        var result = values.Where(x => x.Status != CruiseItineraryCaptureCandidateStatus.Ready).ToList();
        foreach (var group in values.Where(x => x.Status == CruiseItineraryCaptureCandidateStatus.Ready)
                     .GroupBy(x => x.Occurrence!.CatalogueKey.PersistenceKey, StringComparer.Ordinal).OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var ordered = group.OrderBy(x => x.Occurrence!.SourceReference, StringComparer.Ordinal).ThenBy(x => x.Occurrence!.Fingerprint, StringComparer.Ordinal).ToArray();
            result.Add(ordered[0]);
            result.AddRange(ordered.Skip(1).Select(x => CruiseItineraryCaptureCandidateResult.Ineligible(
                x.DisplayLabel, ["duplicateItineraryId"], "The TUI card duplicated an itinerary identity already captured on this page.")));
        }
        return result.OrderBy(x => x.Occurrence?.CatalogueKey.PersistenceKey ?? "~" + x.DisplayLabel, StringComparer.Ordinal).ToList();
    }

    private static bool TryMapScope(Uri page, CruiseSource source, out CruiseDiscoveryScope? scope, out string? message)
    {
        scope = null; message = null;
        try
        {
            var query = QueryValues(page);
            var knownKeys = MaterialCriteria.Select(x => x.Query).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var unexpected = query.Keys.FirstOrDefault(key => !knownKeys.Contains(key) && !Ignored(key));
            if (unexpected is not null) { message = $"The TUI discovery scope contained an unsupported query key: {Bounded(unexpected, 100)}."; return false; }
            var criteria = MaterialCriteria.Select(pair =>
            {
                var values = query.TryGetValue(pair.Query, out var items) ? items.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => ScopeValue(pair.Query, x)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray() : [];
                return values.Length == 0 ? new CruiseDiscoveryCriterion(pair.Semantic, CruiseDiscoveryCriterionState.Unknown) : new CruiseDiscoveryCriterion(pair.Semantic, CruiseDiscoveryCriterionState.Known, values);
            });
            scope = new CruiseDiscoveryScope(source, "marella", CruiseDiscoverySurface.CruisePackages, PayloadVersion, criteria); return true;
        }
        catch (Exception exception) when (exception is UriFormatException or ArgumentException) { message = "The TUI discovery scope could not be mapped safely."; return false; }
    }

    private static Dictionary<string, List<string>> QueryValues(Uri uri)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var split = item.Split('=', 2); var key = Uri.UnescapeDataString(split[0]); var value = Uri.UnescapeDataString(split.Length == 2 ? split[1] : "");
            if (key.Length > 100 || value.Length > CruiseDiscoveryCriterion.MaximumValueLength) throw new ArgumentException("Query value is too long.");
            if (!result.TryGetValue(key, out var values)) result[key] = values = []; values.Add(value);
        }
        return result;
    }

    private static bool TryValidatePage(CruiseItineraryPageCaptureRequest request, out Uri page)
    {
        page = null!; if (!Uri.TryCreate(request.SourceReference, UriKind.Absolute, out var value)) return false;
        var valid = string.Equals(request.SourceIdentifier, TuiCruisePageCaptureService.SupportedSourceIdentifier, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(request.Source.Id, "tui", StringComparison.OrdinalIgnoreCase) && string.Equals(request.Source.Name, "TUI", StringComparison.OrdinalIgnoreCase) &&
            value.Scheme == Uri.UriSchemeHttps && string.Equals(value.Host, TuiCruisePageCaptureService.SupportedHost, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(value.AbsolutePath.TrimEnd('/'), "/cruise/packages", StringComparison.OrdinalIgnoreCase);
        if (valid) page = value; return valid;
    }

    private static bool TryTrustedItinerary(string? text, out string reference, out string code)
    {
        reference = code = string.Empty;
        if (string.IsNullOrWhiteSpace(text) || text.Length > 4000 || !Uri.TryCreate(text, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uri.Host, TuiCruisePageCaptureService.SupportedHost, StringComparison.OrdinalIgnoreCase) || !uri.AbsolutePath.StartsWith("/cruise/bookitineraries/", StringComparison.OrdinalIgnoreCase)) return false;
        try { var query = QueryValues(uri); code = One(query, "itineraryCodeOne") ?? One(query, "itineraryCode") ?? ""; }
        catch (Exception exception) when (exception is UriFormatException or ArgumentException) { return false; }
        if (string.IsNullOrWhiteSpace(code)) return false; reference = text; return true;
    }

    private static string EvidenceKey(string reference, Candidate value) => "tui-itinerary:v1:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', reference, Normalize(value.ProviderItineraryId!), Normalize(value.Title), Normalize(value.ShipName), value.DepartureDate ?? "-", value.DurationNights?.ToString(CultureInfo.InvariantCulture) ?? "-", Normalize(value.DeparturePort), Normalize(value.ItinerarySummary), Normalize(value.ProviderOfferId)))));
    private static string ScopeValue(string key, string value) { var result = value.Trim(); if (key.Equals("from[]", StringComparison.OrdinalIgnoreCase) && result.EndsWith(":Airport", StringComparison.OrdinalIgnoreCase)) result = result[..^8].Trim(); return result; }
    private static string? One(Dictionary<string, List<string>> values, string key)
    {
        if (!values.TryGetValue(key, out var items)) return null;
        var distinct = items.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return distinct.Length == 1 ? distinct[0] : null;
    }
    private static bool Ignored(string key) => key.Equals("sort", StringComparison.OrdinalIgnoreCase) || key.Equals("view", StringComparison.OrdinalIgnoreCase) || key.Equals("gclid", StringComparison.OrdinalIgnoreCase) || key.Equals("msclkid", StringComparison.OrdinalIgnoreCase) || key.StartsWith("utm_", StringComparison.OrdinalIgnoreCase);
    private static string SafeLabel(string? value) => string.IsNullOrWhiteSpace(value) ? "TUI itinerary candidate" : Bounded(value.Trim(), 1000);
    private static string Bounded(string value, int maximum) => value.Length <= maximum ? value : value[..maximum];
    private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
    private static bool Oversized(string? value, int maximum) => value?.Length > maximum;
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static CruiseItineraryCaptureBatchResult Cancelled() => CruiseItineraryCaptureBatchResult.Cancelled("Itinerary capture was cancelled.");

    private sealed class Payload { public int? Version { get; init; } public bool WasTruncated { get; init; } public List<Candidate?>? Candidates { get; init; } }
    private sealed class Candidate { public string? SourceReference { get; init; } public string? ProviderItineraryId { get; init; } public string? ProviderOfferId { get; init; } public string? Title { get; init; } public string? ShipName { get; init; } public string? DepartureDate { get; init; } public int? DurationNights { get; init; } public string? DeparturePort { get; init; } public string? ItinerarySummary { get; init; } }
}
