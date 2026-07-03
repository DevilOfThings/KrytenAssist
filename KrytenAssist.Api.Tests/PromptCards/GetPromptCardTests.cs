using System.Net;
using System.Net.Http.Json;
using KrytenAssist.Api.Tests;
using KrytenAssist.Api.Tests.Infrastructure;
using KrytenAssist.Api.Tests.TestData;
using KrytenAssist.Application.PromptCards;
using KrytenAssist.Core.Entities;

public class GetPromptCardTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    
    [Fact]
    public async Task GetPromptCards_ReturnsOk()
    {
        // Arrange

        // Act
        var response = await _client.GetAsync("/api/promptcards");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public async Task GetPromptCardById_WithExistingId_ReturnsOk()
    {
        // Arrange
        var request = PromptCardRequests.CreateValidRequest();

        var createResponse = await _client.PostAsJsonAsync("/api/promptcards", request);
        var createResponseBody = await createResponse.Content.ReadFromJsonAsync<CreatePromptCardResponse>();

        Assert.NotNull(createResponseBody);

        // Act
        var response = await _client.GetAsync($"/api/promptcards/{createResponseBody.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadFromJsonAsync<PromptCard>();

        Assert.NotNull(responseBody);
        Assert.Equal(createResponseBody.Id, responseBody.Id);
        Assert.Equal(request.Title, responseBody.Title);
        Assert.Equal(request.Category, responseBody.Category);
    }
    
    [Fact]
    public async Task GetPromptCardById_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/promptcards/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}