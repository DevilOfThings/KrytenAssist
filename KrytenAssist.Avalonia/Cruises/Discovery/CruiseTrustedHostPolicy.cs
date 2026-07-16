using System;

namespace KrytenAssist.Avalonia.Cruises.Discovery;

public enum CruiseAddressTrust
{
    Trusted,
    BrowserInternal,
    Untrusted
}

public sealed class CruiseTrustedHostPolicy
{
    public CruiseAddressTrust Classify(Uri? address, CruiseDiscoverySource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (address is null || !address.IsAbsoluteUri)
        {
            return CruiseAddressTrust.Untrusted;
        }

        if (string.Equals(address.Scheme, "about", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(address.OriginalString, "about:blank", StringComparison.OrdinalIgnoreCase))
        {
            return CruiseAddressTrust.BrowserInternal;
        }

        return address.Scheme == Uri.UriSchemeHttps &&
               string.Equals(address.Host, source.TrustedHost, StringComparison.OrdinalIgnoreCase)
            ? CruiseAddressTrust.Trusted
            : CruiseAddressTrust.Untrusted;
    }
}
