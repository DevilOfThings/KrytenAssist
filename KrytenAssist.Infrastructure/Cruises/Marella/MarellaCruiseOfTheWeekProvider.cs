using KrytenAssist.Application.Cruises;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Infrastructure.Cruises.Marella;

public sealed class MarellaCruiseOfTheWeekProvider : ICruiseOfTheWeekProvider
{
    private readonly HttpClient _httpClient;
    private readonly MarellaCruiseOfTheWeekParser _parser;
    private readonly Uri _sourceAddress;

    public MarellaCruiseOfTheWeekProvider(
        HttpClient httpClient,
        MarellaCruiseOfTheWeekParser parser)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(parser);

        if (httpClient.BaseAddress is null || !httpClient.BaseAddress.IsAbsoluteUri)
        {
            throw new ArgumentException(
                "The HTTP client must have an absolute base address.",
                nameof(httpClient));
        }

        if (!string.Equals(
                httpClient.BaseAddress.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The HTTP client base address must use HTTPS.",
                nameof(httpClient));
        }

        _httpClient = httpClient;
        _parser = parser;
        _sourceAddress = httpClient.BaseAddress;
    }

    public async Task<CruiseObservation> GetCurrentAsync(
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var response = await _httpClient.GetAsync(
                _sourceAddress,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(html))
            {
                throw new CruiseOfTheWeekException(
                    "The Cruise of the Week response was empty.");
            }

            return _parser.Parse(
                html,
                observedAt,
                _sourceAddress.AbsoluteUri);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            throw new CruiseOfTheWeekException(
                "The Cruise of the Week request timed out.",
                exception);
        }
        catch (HttpRequestException exception)
        {
            throw new CruiseOfTheWeekException(
                "The Cruise of the Week could not be retrieved.",
                exception);
        }
    }
}
