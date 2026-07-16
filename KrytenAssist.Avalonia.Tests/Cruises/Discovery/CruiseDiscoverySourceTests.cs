using KrytenAssist.Avalonia.Cruises.Discovery;

namespace KrytenAssist.Avalonia.Tests.Cruises.Discovery;

public sealed class CruiseDiscoverySourceTests
{
    [Fact]
    public void Catalogue_ContainsOnlyProvenMarellaSource()
    {
        var source = Assert.Single(new CruiseDiscoverySourceCatalog().Sources);

        Assert.Equal("marella-cruise-of-the-week", source.Identifier);
        Assert.Equal("Marella Cruise of the Week", source.DisplayName);
        Assert.Equal("www.tui.co.uk", source.TrustedHost);
        Assert.Equal(Uri.UriSchemeHttps, source.StartingAddress.Scheme);
        Assert.True(source.SupportsLinkDiagnostics);
    }

    [Theory]
    [InlineData(null, "Marella", "www.tui.co.uk")]
    [InlineData("marella", null, "www.tui.co.uk")]
    [InlineData("marella", "Marella", null)]
    [InlineData("", "Marella", "www.tui.co.uk")]
    public void Constructor_RejectsMissingRequiredValues(
        string? identifier,
        string? displayName,
        string? trustedHost)
    {
        Assert.Throws<ArgumentException>(() => new CruiseDiscoverySource(
            identifier!,
            displayName!,
            "Description",
            trustedHost!,
            new Uri("https://www.tui.co.uk/cruise"),
            true));
    }

    [Fact]
    public void Constructor_RejectsNonHttpsAndMismatchedHosts()
    {
        Assert.Throws<ArgumentException>(() => Create(
            "www.tui.co.uk",
            new Uri("http://www.tui.co.uk/cruise")));
        Assert.Throws<ArgumentException>(() => Create(
            "www.tui.co.uk",
            new Uri("https://tui.co.uk/cruise")));
    }

    private static CruiseDiscoverySource Create(string host, Uri address) => new(
        "source",
        "Source",
        "Description",
        host,
        address,
        false);
}
