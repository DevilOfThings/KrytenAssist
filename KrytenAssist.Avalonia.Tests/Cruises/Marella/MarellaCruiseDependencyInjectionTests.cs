extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ICruiseOfTheWeekProvider =
    KrytenApplication::KrytenAssist.Application.Cruises.ICruiseOfTheWeekProvider;
using MarellaCruiseOfTheWeekOptions =
    KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Marella.MarellaCruiseOfTheWeekOptions;
using MarellaCruiseOfTheWeekParser =
    KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Marella.MarellaCruiseOfTheWeekParser;
using MarellaCruiseOfTheWeekProvider =
    KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Marella.MarellaCruiseOfTheWeekProvider;
using MarellaCruiseServiceCollectionExtensions =
    KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Marella.MarellaCruiseServiceCollectionExtensions;

namespace KrytenAssist.Avalonia.Tests.Cruises.Marella;

public sealed class MarellaCruiseDependencyInjectionTests
{
    [Fact]
    public void AddMarellaCruiseOfTheWeek_ShouldRegisterConfiguredSingletonGraph()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        var returnedServices = MarellaCruiseServiceCollectionExtensions
            .AddMarellaCruiseOfTheWeek(services, configuration);
        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider
            .GetRequiredService<IOptions<MarellaCruiseOfTheWeekOptions>>()
            .Value;
        var firstParser = serviceProvider.GetRequiredService<MarellaCruiseOfTheWeekParser>();
        var secondParser = serviceProvider.GetRequiredService<MarellaCruiseOfTheWeekParser>();
        var firstProvider = serviceProvider.GetRequiredService<ICruiseOfTheWeekProvider>();
        var secondProvider = serviceProvider.GetRequiredService<ICruiseOfTheWeekProvider>();

        // Assert
        returnedServices.Should().BeSameAs(services);
        options.SourceUrl.Should().Be(CruiseTestData.SourceUrl);
        options.TimeoutSeconds.Should().Be(30);
        firstParser.Should().BeSameAs(secondParser);
        firstProvider.Should().BeOfType<MarellaCruiseOfTheWeekProvider>();
        secondProvider.Should().BeSameAs(firstProvider);
    }

    [Fact]
    public void AddMarellaCruiseOfTheWeek_ShouldConfigureNamedHttpClientWithoutRequestingIt()
    {
        // Arrange
        var services = new ServiceCollection();
        MarellaCruiseServiceCollectionExtensions.AddMarellaCruiseOfTheWeek(
            services,
            CreateConfiguration());
        using var serviceProvider = services.BuildServiceProvider();

        // Act
        var client = serviceProvider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient("MarellaCruiseOfTheWeek");

        // Assert
        client.BaseAddress.Should().Be(new Uri(CruiseTestData.SourceUrl));
        client.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        client.DefaultRequestHeaders.UserAgent.ToString().Should().Contain("KrytenAssist/0.1");
    }

    [Fact]
    public void AddMarellaCruiseOfTheWeek_ShouldGuardArguments()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        Action nullServices = () => MarellaCruiseServiceCollectionExtensions
            .AddMarellaCruiseOfTheWeek(null!, configuration);
        Action nullConfiguration = () => MarellaCruiseServiceCollectionExtensions
            .AddMarellaCruiseOfTheWeek(services, null!);

        // Assert
        nullServices.Should().Throw<ArgumentNullException>();
        nullConfiguration.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null, "30")]
    [InlineData("", "30")]
    [InlineData("   ", "30")]
    [InlineData("relative/path", "30")]
    [InlineData("http://example.test/cruise", "30")]
    [InlineData("not a url", "30")]
    [InlineData("https://example.test/cruise", "0")]
    [InlineData("https://example.test/cruise", "301")]
    public void Options_ShouldRejectInvalidConfiguration(string? sourceUrl, string timeout)
    {
        // Arrange
        var services = new ServiceCollection();
        MarellaCruiseServiceCollectionExtensions.AddMarellaCruiseOfTheWeek(
            services,
            CreateConfiguration(sourceUrl, timeout));
        using var serviceProvider = services.BuildServiceProvider();

        // Act
        Action act = () => _ = serviceProvider
            .GetRequiredService<IOptions<MarellaCruiseOfTheWeekOptions>>()
            .Value;

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("300")]
    public void Options_ShouldAcceptTimeoutBoundaries(string timeout)
    {
        // Arrange
        var services = new ServiceCollection();
        MarellaCruiseServiceCollectionExtensions.AddMarellaCruiseOfTheWeek(
            services,
            CreateConfiguration(CruiseTestData.SourceUrl, timeout));
        using var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider
            .GetRequiredService<IOptions<MarellaCruiseOfTheWeekOptions>>()
            .Value;

        // Assert
        options.TimeoutSeconds.Should().Be(int.Parse(timeout));
    }

    private static IConfiguration CreateConfiguration(
        string? sourceUrl = CruiseTestData.SourceUrl,
        string timeout = "30")
    {
        var values = new Dictionary<string, string?>
        {
            ["SourceUrl"] = sourceUrl,
            ["TimeoutSeconds"] = timeout
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
