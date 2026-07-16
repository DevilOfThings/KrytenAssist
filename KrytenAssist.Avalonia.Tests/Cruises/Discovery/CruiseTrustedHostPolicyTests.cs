using KrytenAssist.Avalonia.Cruises.Discovery;

namespace KrytenAssist.Avalonia.Tests.Cruises.Discovery;

public sealed class CruiseTrustedHostPolicyTests
{
    private readonly CruiseTrustedHostPolicy _policy = new();
    private readonly CruiseDiscoverySource _source = new CruiseDiscoverySourceCatalog().Sources[0];

    [Theory]
    [InlineData("https://www.tui.co.uk/cruise", CruiseAddressTrust.Trusted)]
    [InlineData("HTTPS://WWW.TUI.CO.UK/cruise", CruiseAddressTrust.Trusted)]
    [InlineData("http://www.tui.co.uk/cruise", CruiseAddressTrust.Untrusted)]
    [InlineData("https://www.tui.co.uk.evil.example/cruise", CruiseAddressTrust.Untrusted)]
    [InlineData("https://notwww.tui.co.uk/cruise", CruiseAddressTrust.Untrusted)]
    [InlineData("https://tui.co.uk/cruise", CruiseAddressTrust.Untrusted)]
    [InlineData("https://example.com/cruise", CruiseAddressTrust.Untrusted)]
    [InlineData("about:blank", CruiseAddressTrust.BrowserInternal)]
    public void Classify_UsesExactHttpsHostRules(string value, CruiseAddressTrust expected)
    {
        Assert.Equal(expected, _policy.Classify(new Uri(value), _source));
    }

    [Fact]
    public void Classify_RejectsMissingAddress()
    {
        Assert.Equal(CruiseAddressTrust.Untrusted, _policy.Classify(null, _source));
        Assert.Equal(
            CruiseAddressTrust.Untrusted,
            _policy.Classify(new Uri("/cruise", UriKind.Relative), _source));
    }
}
