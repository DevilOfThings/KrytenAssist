extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using System.Net;
using FluentAssertions;
using KrytenAssist.Avalonia.Tests.Cruises;
using CruiseOfTheWeekException =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseOfTheWeekException;
using MarellaCruiseOfTheWeekParser =
    KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Marella.MarellaCruiseOfTheWeekParser;
using MarellaCruiseOfTheWeekProvider =
    KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Marella.MarellaCruiseOfTheWeekProvider;

namespace KrytenAssist.Avalonia.Tests.Cruises.Marella;

public sealed class MarellaCruiseOfTheWeekProviderTests
{
    [Fact]
    public async Task GetCurrentAsync_ShouldRequestConfiguredUrlAndReturnParsedObservation()
    {
        // Arrange
        var handler = new StubHttpMessageHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CruiseTestData.CreateHtml())
            }));
        using var client = CreateClient(handler);
        var provider = new MarellaCruiseOfTheWeekProvider(
            client,
            new MarellaCruiseOfTheWeekParser());

        // Act
        var observation = await provider.GetCurrentAsync(CruiseTestData.ObservedAt);

        // Assert
        handler.InvocationCount.Should().Be(1);
        handler.RequestUri.Should().Be(new Uri(CruiseTestData.SourceUrl));
        handler.Method.Should().Be(HttpMethod.Get);
        observation.ObservedAt.Should().Be(CruiseTestData.ObservedAt);
        observation.SourceReference.Should().Be(CruiseTestData.SourceUrl);
        observation.Snapshot.Offer.Title.Should().Be("Mediterranean Medley");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetCurrentAsync_ShouldWrapNonSuccessResponses(HttpStatusCode statusCode)
    {
        // Arrange
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(statusCode)));
        using var client = CreateClient(handler);
        var provider = new MarellaCruiseOfTheWeekProvider(
            client,
            new MarellaCruiseOfTheWeekParser());

        // Act
        Func<Task> act = () => provider.GetCurrentAsync(CruiseTestData.ObservedAt);

        // Assert
        await act.Should().ThrowAsync<CruiseOfTheWeekException>()
            .WithMessage("*could not be retrieved*");
    }

    [Fact]
    public async Task GetCurrentAsync_ShouldWrapTransportFailure()
    {
        // Arrange
        var transportException = new HttpRequestException("Offline");
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromException<HttpResponseMessage>(transportException));
        using var client = CreateClient(handler);
        var provider = new MarellaCruiseOfTheWeekProvider(
            client,
            new MarellaCruiseOfTheWeekParser());

        // Act
        Func<Task> act = () => provider.GetCurrentAsync(CruiseTestData.ObservedAt);

        // Assert
        var exception = await act.Should().ThrowAsync<CruiseOfTheWeekException>();
        exception.Which.InnerException.Should().BeSameAs(transportException);
    }

    [Fact]
    public async Task GetCurrentAsync_ShouldWrapTimeoutCancellation()
    {
        // Arrange
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromException<HttpResponseMessage>(new OperationCanceledException("Timed out")));
        using var client = CreateClient(handler);
        var provider = new MarellaCruiseOfTheWeekProvider(
            client,
            new MarellaCruiseOfTheWeekParser());

        // Act
        Func<Task> act = () => provider.GetCurrentAsync(CruiseTestData.ObservedAt);

        // Assert
        await act.Should().ThrowAsync<CruiseOfTheWeekException>()
            .WithMessage("*timed out*");
    }

    [Fact]
    public async Task GetCurrentAsync_ShouldPropagateCallerCancellation()
    {
        // Arrange
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var client = CreateClient(handler);
        var provider = new MarellaCruiseOfTheWeekProvider(
            client,
            new MarellaCruiseOfTheWeekParser());
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        // Act
        Func<Task> act = () => provider.GetCurrentAsync(
            CruiseTestData.ObservedAt,
            cancellationSource.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        handler.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCurrentAsync_ShouldRejectEmptyResponse()
    {
        // Arrange
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("   ")
            }));
        using var client = CreateClient(handler);
        var provider = new MarellaCruiseOfTheWeekProvider(
            client,
            new MarellaCruiseOfTheWeekParser());

        // Act
        Func<Task> act = () => provider.GetCurrentAsync(CruiseTestData.ObservedAt);

        // Assert
        await act.Should().ThrowAsync<CruiseOfTheWeekException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void Constructor_ShouldRejectInvalidDependenciesAndBaseAddresses()
    {
        // Arrange
        using var missingAddressClient = new HttpClient(new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://example.test/cruise-of-the-week")
        };
        var parser = new MarellaCruiseOfTheWeekParser();

        // Act
        Action nullClient = () => new MarellaCruiseOfTheWeekProvider(null!, parser);
        Action nullParser = () => new MarellaCruiseOfTheWeekProvider(missingAddressClient, null!);
        Action missingAddress = () => new MarellaCruiseOfTheWeekProvider(
            missingAddressClient,
            parser);
        Action insecureAddress = () => new MarellaCruiseOfTheWeekProvider(httpClient, parser);

        // Assert
        nullClient.Should().Throw<ArgumentNullException>();
        nullParser.Should().Throw<ArgumentNullException>();
        missingAddress.Should().Throw<ArgumentException>();
        insecureAddress.Should().Throw<ArgumentException>().WithMessage("*HTTPS*");
    }

    private static HttpClient CreateClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(CruiseTestData.SourceUrl)
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        public int InvocationCount { get; private set; }

        public Uri? RequestUri { get; private set; }

        public HttpMethod? Method { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            RequestUri = request.RequestUri;
            Method = request.Method;
            return responseFactory(request, cancellationToken);
        }
    }
}
