using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using KrytenAssist.Application.Cruises;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Infrastructure.Cruises.Marella;

public sealed class MarellaCruiseOfTheWeekParser
{
    private const string HeadingSelector = "h1,h2,h3,h4,h5,h6";
    private const string WeeklyDealPrefix = "This week's deal:";
    private const string DepartureDurationLabel = "Departure date and trip duration";

    private static readonly Regex QuotedTitlePattern = new(
        "[\"“](?<title>[^\"”]+)[\"”]",
        RegexOptions.CultureInvariant);

    private static readonly Regex WhitespacePattern = new(
        "\\s+",
        RegexOptions.CultureInvariant);

    private static readonly Regex DeparturePortPattern = new(
        "^From\\s+(?<port>.+?)\\s+on\\s+\\d{1,2}\\s+[A-Za-z]{3}\\s+\\d{4}$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex DepartureDurationPattern = new(
        "(?:(?:Mon|Tue|Wed|Thu|Fri|Sat|Sun)\\s+)?" +
        "(?<date>\\d{1,2}\\s+[A-Za-z]{3}\\s+\\d{4})\\s*-\\s*" +
        "(?<nights>\\d+)\\s+nights?\\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PerPersonPricePattern = new(
        "£\\s*(?<amount>\\d[\\d,]*(?:\\.\\d{1,2})?)\\s*pp\\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex TotalPricePattern = new(
        "£\\s*(?<amount>\\d[\\d,]*(?:\\.\\d{1,2})?)\\s*" +
        "Total price based on 2 sharing\\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly string[] ProviderIdAttributes =
    [
        "data-cruise-id",
        "data-offer-id",
        "data-package-id",
        "data-itinerary-id"
    ];

    private static readonly string[] ProviderIdQueryParameters =
    [
        "cruiseId",
        "cruiseCode",
        "itineraryCode",
        "packageId"
    ];

    public CruiseObservation Parse(
        string html,
        DateTimeOffset observedAt,
        string sourceReference)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentException.ThrowIfNullOrWhiteSpace(html);
        ArgumentNullException.ThrowIfNull(sourceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceReference);

        var document = new HtmlParser().ParseDocument(html);
        var headings = document.QuerySelectorAll(HeadingSelector).ToArray();
        var weeklyHeading = FindWeeklyHeading(headings);
        var weeklyTitle = ExtractWeeklyTitle(weeklyHeading);
        var resultHeading = FindResultHeading(headings, weeklyHeading, weeklyTitle);
        var resultContainer = FindResultContainer(resultHeading);
        var textValues = GetLeafTextValues(resultContainer);

        var title = NormalizeText(resultHeading.TextContent);
        var shipName = ExtractShipName(resultContainer, textValues, title);
        var departurePort = ExtractDeparturePort(textValues);
        var (departureDate, durationNights) = ExtractDepartureAndDuration(textValues);
        var prices = ExtractPrices(textValues);
        var itinerarySummary = ExtractOptionalText(
            resultContainer,
            "[data-itinerary-summary],.itinerary-summary");
        var providerOfferId = ExtractProviderOfferId(
            resultContainer,
            title,
            departureDate);

        var provider = new CruiseProvider("marella", "Marella Cruises");
        var offer = new CruiseOffer(
            provider,
            providerOfferId,
            title,
            shipName,
            departureDate,
            durationNights,
            departurePort,
            itinerarySummary);
        var snapshot = new CruiseSnapshot(
            offer,
            prices,
            NormalizeText(weeklyHeading.TextContent));

        return new CruiseObservation(snapshot, observedAt, sourceReference);
    }

    private static IElement FindWeeklyHeading(IEnumerable<IElement> headings)
    {
        var matches = headings
            .Where(heading => NormalizeText(heading.TextContent).StartsWith(
                WeeklyDealPrefix,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new CruiseOfTheWeekException(
                "The Cruise of the Week promotion could not be found."),
            _ => throw new CruiseOfTheWeekException(
                "The Cruise of the Week promotion was ambiguous.")
        };
    }

    private static string ExtractWeeklyTitle(IElement weeklyHeading)
    {
        var headingText = NormalizeText(weeklyHeading.TextContent);
        var matches = QuotedTitlePattern.Matches(headingText);

        if (matches.Count != 1)
        {
            throw new CruiseOfTheWeekException(
                "The Cruise of the Week title could not be identified.");
        }

        var title = NormalizeText(matches[0].Groups["title"].Value);
        if (title.Length == 0)
        {
            throw new CruiseOfTheWeekException(
                "The Cruise of the Week title could not be identified.");
        }

        return title;
    }

    private static IElement FindResultHeading(
        IEnumerable<IElement> headings,
        IElement weeklyHeading,
        string weeklyTitle)
    {
        var matches = headings
            .Where(heading => !ReferenceEquals(heading, weeklyHeading))
            .Where(heading => string.Equals(
                NormalizeText(heading.TextContent),
                weeklyTitle,
                StringComparison.OrdinalIgnoreCase))
            .Where(heading => TryFindResultContainer(heading) is not null)
            .ToArray();

        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new CruiseOfTheWeekException(
                "The matching Cruise of the Week result could not be found."),
            _ => throw new CruiseOfTheWeekException(
                "The matching Cruise of the Week result was ambiguous.")
        };
    }

    private static IElement FindResultContainer(IElement resultHeading)
    {
        return TryFindResultContainer(resultHeading)
            ?? throw new CruiseOfTheWeekException(
                "The matching Cruise of the Week result could not be found.");
    }

    private static IElement? TryFindResultContainer(IElement resultHeading)
    {
        for (var candidate = resultHeading.ParentElement;
             candidate is not null && !string.Equals(candidate.LocalName, "body", StringComparison.Ordinal);
             candidate = candidate.ParentElement)
        {
            var text = NormalizeText(candidate.TextContent);
            if (text.Contains(DepartureDurationLabel, StringComparison.OrdinalIgnoreCase) &&
                PerPersonPricePattern.IsMatch(text))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> GetLeafTextValues(IElement container)
    {
        return container
            .QuerySelectorAll("*")
            .Where(element => element.Children.Length == 0)
            .Select(element => NormalizeText(element.TextContent))
            .Where(text => text.Length > 0)
            .ToArray();
    }

    private static string ExtractShipName(
        IElement container,
        IReadOnlyList<string> textValues,
        string title)
    {
        var semanticElement = container.QuerySelector(
            "[data-ship-name],[data-ship],.ship-name");
        if (semanticElement is not null)
        {
            var attributeValue = semanticElement.GetAttribute("data-ship-name")
                ?? semanticElement.GetAttribute("data-ship");
            var semanticValue = NormalizeText(attributeValue ?? semanticElement.TextContent);
            if (semanticValue.Length > 0)
            {
                return semanticValue;
            }
        }

        var titleIndex = FindTextIndex(textValues, title);
        for (var index = titleIndex + 1; index < textValues.Count; index++)
        {
            var candidate = textValues[index];
            if (IsDepartureBoundary(candidate))
            {
                break;
            }

            if (IsPlausibleShipName(candidate, title))
            {
                return candidate;
            }
        }

        throw new CruiseOfTheWeekException(
            "The Cruise of the Week ship could not be identified.");
    }

    private static int FindTextIndex(IReadOnlyList<string> values, string expected)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (string.Equals(values[index], expected, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsDepartureBoundary(string value)
    {
        return value.StartsWith("From ", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("To ", StringComparison.OrdinalIgnoreCase) ||
               value.Contains(DepartureDurationLabel, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlausibleShipName(string value, string title)
    {
        return !string.Equals(value, title, StringComparison.OrdinalIgnoreCase) &&
               !value.StartsWith("View ", StringComparison.OrdinalIgnoreCase) &&
               !value.StartsWith("Cruise of the Week", StringComparison.OrdinalIgnoreCase) &&
               !value.Contains('£') &&
               value.Any(char.IsLetter) &&
               value.Any(character => character != '*');
    }

    private static string? ExtractDeparturePort(IEnumerable<string> textValues)
    {
        foreach (var text in textValues)
        {
            var match = DeparturePortPattern.Match(text);
            if (match.Success)
            {
                var port = NormalizeText(match.Groups["port"].Value);
                return port.Length == 0 ? null : port;
            }
        }

        return null;
    }

    private static (DateOnly DepartureDate, int DurationNights) ExtractDepartureAndDuration(
        IEnumerable<string> textValues)
    {
        foreach (var text in textValues)
        {
            var match = DepartureDurationPattern.Match(text);
            if (!match.Success)
            {
                continue;
            }

            if (!DateOnly.TryParseExact(
                    match.Groups["date"].Value,
                    "d MMM yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out var departureDate))
            {
                throw new CruiseOfTheWeekException(
                    "The Cruise of the Week departure date was invalid.");
            }

            if (!int.TryParse(
                    match.Groups["nights"].Value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var durationNights) || durationNights <= 0)
            {
                throw new CruiseOfTheWeekException(
                    "The Cruise of the Week duration was invalid.");
            }

            return (departureDate, durationNights);
        }

        throw new CruiseOfTheWeekException(
            "The Cruise of the Week departure date and duration could not be found.");
    }

    private static IReadOnlyList<CruisePrice> ExtractPrices(IEnumerable<string> textValues)
    {
        var perPersonAmounts = FindAmounts(textValues, PerPersonPricePattern);
        if (perPersonAmounts.Count == 0)
        {
            throw new CruiseOfTheWeekException(
                "The Cruise of the Week per-person price could not be found.");
        }

        if (perPersonAmounts.Distinct().Count() > 1)
        {
            throw new CruiseOfTheWeekException(
                "The Cruise of the Week per-person price was ambiguous.");
        }

        var prices = new List<CruisePrice>
        {
            new(perPersonAmounts[0], "GBP", "per person")
        };

        var totalAmounts = FindAmounts(textValues, TotalPricePattern);
        if (totalAmounts.Distinct().Count() > 1)
        {
            throw new CruiseOfTheWeekException(
                "The Cruise of the Week total price was ambiguous.");
        }

        if (totalAmounts.Count > 0)
        {
            prices.Add(new CruisePrice(
                totalAmounts[0],
                "GBP",
                "total based on 2 sharing"));
        }

        return prices;
    }

    private static IReadOnlyList<decimal> FindAmounts(
        IEnumerable<string> textValues,
        Regex pattern)
    {
        var amounts = new List<decimal>();

        foreach (var text in textValues)
        {
            foreach (Match match in pattern.Matches(text))
            {
                var numericText = match.Groups["amount"].Value.Replace(",", string.Empty);
                if (!decimal.TryParse(
                        numericText,
                        NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture,
                        out var amount))
                {
                    throw new CruiseOfTheWeekException(
                        "A Cruise of the Week price was invalid.");
                }

                amounts.Add(amount);
            }
        }

        return amounts;
    }

    private static string? ExtractOptionalText(IElement container, string selector)
    {
        var matches = container.QuerySelectorAll(selector)
            .Select(element => NormalizeText(
                element.GetAttribute("data-itinerary-summary") ?? element.TextContent))
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return matches.Length == 1 ? matches[0] : null;
    }

    private static string ExtractProviderOfferId(
        IElement container,
        string title,
        DateOnly departureDate)
    {
        foreach (var element in container.QuerySelectorAll("*"))
        {
            foreach (var attributeName in ProviderIdAttributes)
            {
                var value = NormalizeText(element.GetAttribute(attributeName) ?? string.Empty);
                if (value.Length > 0)
                {
                    return value;
                }
            }
        }

        foreach (var link in container.QuerySelectorAll("a[href]"))
        {
            var identifier = ExtractIdentifierFromLink(link.GetAttribute("href"));
            if (identifier is not null)
            {
                return identifier;
            }
        }

        var slug = CreateSlug(title);
        if (slug.Length == 0)
        {
            throw new CruiseOfTheWeekException(
                "The Cruise of the Week identifier could not be created.");
        }

        return $"marella:{slug}:{departureDate:yyyy-MM-dd}";
    }

    private static string? ExtractIdentifierFromLink(string? href)
    {
        if (string.IsNullOrWhiteSpace(href) ||
            !Uri.TryCreate(new Uri("https://www.tui.co.uk"), href, out var uri))
        {
            return null;
        }

        var query = uri.Query.TrimStart('?').Split(
            '&',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var pair in query)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length != 2 ||
                !ProviderIdQueryParameters.Contains(
                    Uri.UnescapeDataString(parts[0]),
                    StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = NormalizeText(Uri.UnescapeDataString(parts[1]));
            if (value.Length > 0)
            {
                return value;
            }
        }

        return null;
    }

    private static string CreateSlug(string value)
    {
        var builder = new StringBuilder();
        var separatorPending = false;

        foreach (var character in value.ToLowerInvariant())
        {
            if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                if (separatorPending && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(character);
                separatorPending = false;
            }
            else
            {
                separatorPending = true;
            }
        }

        return builder.ToString();
    }

    private static string NormalizeText(string value)
    {
        return WhitespacePattern.Replace(value, " ").Trim();
    }
}
