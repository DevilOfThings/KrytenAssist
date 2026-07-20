using System;
using System.Collections.Generic;

namespace KrytenAssist.Avalonia.Cruises.Discovery;

public sealed class CruiseDiscoverySourceCatalog
{
    private static readonly CruiseDiscoverySource MarellaCruiseOfTheWeek = new(
        "marella-cruise-of-the-week",
        "Marella Cruise of the Week",
        "Browse TUI's current featured Marella cruise offer.",
        "www.tui.co.uk",
        new Uri(
            "https://www.tui.co.uk/cruise/packages?from%5B%5D=STN%3AAirport&to%5B%5D=&when=01-10-2026&flexibility=false&flexibleDays=1&duration=1-7&addAStay=0&until=&choiceSearch=true&noOfAdults=2&noOfChildren=0&childrenAge=&searchRequestType=ins&searchType=search&sp=true&room=",
            UriKind.Absolute),
        supportsLinkDiagnostics: true);

    public CruiseDiscoverySourceCatalog()
        : this([MarellaCruiseOfTheWeek])
    {
    }

    internal CruiseDiscoverySourceCatalog(IReadOnlyList<CruiseDiscoverySource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        if (sources.Count == 0)
        {
            throw new ArgumentException("At least one cruise source is required.", nameof(sources));
        }

        var identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (!identifiers.Add(source.Identifier))
            {
                throw new ArgumentException(
                    $"Cruise source identifier '{source.Identifier}' is duplicated.",
                    nameof(sources));
            }
        }

        Sources = sources;
    }

    public IReadOnlyList<CruiseDiscoverySource> Sources { get; }
}
