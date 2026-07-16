using System;

namespace KrytenAssist.Avalonia.Cruises.Discovery;

public sealed record CruiseDiscoverySource
{
    public CruiseDiscoverySource(
        string identifier,
        string displayName,
        string description,
        string trustedHost,
        Uri startingAddress,
        bool supportsLinkDiagnostics)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("A source identifier is required.", nameof(identifier));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A source display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(trustedHost))
        {
            throw new ArgumentException("A trusted host is required.", nameof(trustedHost));
        }

        ArgumentNullException.ThrowIfNull(startingAddress);
        if (!startingAddress.IsAbsoluteUri || startingAddress.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException(
                "A source starting address must be an absolute HTTPS address.",
                nameof(startingAddress));
        }

        var normalizedHost = trustedHost.Trim();
        if (!string.Equals(startingAddress.Host, normalizedHost, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The starting address host must match the trusted host.",
                nameof(startingAddress));
        }

        Identifier = identifier.Trim();
        DisplayName = displayName.Trim();
        Description = description?.Trim() ?? string.Empty;
        TrustedHost = normalizedHost;
        StartingAddress = startingAddress;
        SupportsLinkDiagnostics = supportsLinkDiagnostics;
    }

    public string Identifier { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public string TrustedHost { get; }

    public Uri StartingAddress { get; }

    public bool SupportsLinkDiagnostics { get; }
}
