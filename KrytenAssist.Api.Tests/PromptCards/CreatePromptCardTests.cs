using System.Net;
using System.Net.Http.Json;
using KrytenAssist.Api.Tests.Infrastructure;
using KrytenAssist.Api.Tests.TestData;
using KrytenAssist.Application.PromptCards;

namespace KrytenAssist.Api.Tests.PromptCards;

public class CreatePromptCardTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    
    
    [Fact]
    public async Task PostPromptCard_WithValidRequest_ReturnsCreated()
    {
        // Arrange

        var request = PromptCardRequests.CreateValidRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/promptcards", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var responseBody = await response.Content.ReadFromJsonAsync<CreatePromptCardResponse>();

        Assert.NotNull(responseBody);
        Assert.NotEqual(Guid.Empty, responseBody.Id);
    }

    [Fact]
    public async Task PostPromptCard_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = PromptCardRequests.CreateValidRequest() with
        {
            Title = string.Empty
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/promptcards", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}