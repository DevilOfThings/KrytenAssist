using KrytenAssist.Application.Cruises;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KrytenAssist.Infrastructure.Cruises.Marella;

public static class MarellaCruiseServiceCollectionExtensions
{
    private const string HttpClientName = "MarellaCruiseOfTheWeek";

    public static IServiceCollection AddMarellaCruiseOfTheWeek(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<MarellaCruiseOfTheWeekOptions>()
            .Bind(configuration)
            .Validate(
                options => Uri.TryCreate(
                               options.SourceUrl,
                               UriKind.Absolute,
                               out var sourceUri) &&
                           string.Equals(
                               sourceUri.Scheme,
                               Uri.UriSchemeHttps,
                               StringComparison.OrdinalIgnoreCase),
                "Marella Cruise of the Week SourceUrl must be a non-empty absolute HTTPS URL.")
            .Validate(
                options => options.TimeoutSeconds is >= 1 and <= 300,
                "Marella Cruise of the Week TimeoutSeconds must be between 1 and 300 seconds.")
            .ValidateOnStart();

        services.AddHttpClient(HttpClientName, (serviceProvider, httpClient) =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<MarellaCruiseOfTheWeekOptions>>()
                .Value;

            httpClient.BaseAddress = new Uri(options.SourceUrl, UriKind.Absolute);
            httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("KrytenAssist/0.1");
        });

        services.AddSingleton<MarellaCruiseOfTheWeekParser>();
        services.AddSingleton<ICruiseOfTheWeekProvider>(serviceProvider =>
            new MarellaCruiseOfTheWeekProvider(
                serviceProvider
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(HttpClientName),
                serviceProvider.GetRequiredService<MarellaCruiseOfTheWeekParser>()));

        return services;
    }
}
