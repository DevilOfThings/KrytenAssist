using System.Net;
using System.Net.Http.Json;
using KrytenAssist.Api.Tests.Infrastructure;
using KrytenAssist.Api.Tests.TestData;
using KrytenAssist.Application.PromptCards;
using KrytenAssist.Core.Entities;

namespace KrytenAssist.Api.Tests.PromptCards;

public sealed class UpdatePromptCardTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task UpdatePromptCard_WithExistingId_ReturnsOk()
    {
        // Arrange
        var createRequest = PromptCardRequests.CreateValidRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/promptcards", createRequest);
        var createResponseBody = await createResponse.Content.ReadFromJsonAsync<CreatePromptCardResponse>();

        Assert.NotNull(createResponseBody);

        var updateRequest = new UpdatePromptCardRequest
        {
            Title = "Updated Test Prompt Card",
            Category = "Updated Testing",
            Description = "An updated prompt card from an integration test.",
            PromptText = "Update this endpoint test.",
            Tags = ["updated", "tests", "api"]
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/promptcards/{createResponseBody.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/promptcards/{createResponseBody.Id}");
        var getResponseBody = await getResponse.Content.ReadFromJsonAsync<PromptCard>();

        Assert.NotNull(getResponseBody);
        Assert.Equal(createResponseBody.Id, getResponseBody.Id);
        Assert.Equal(updateRequest.Title, getResponseBody.Title);
        Assert.Equal(updateRequest.Category, getResponseBody.Category);
    }
    
    [Fact]
    public async Task UpdatePromptCard_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        var updateRequest = new UpdatePromptCardRequest
        {
            Title = "Updated Test Prompt Card",
            Category = "Updated Testing",
            Description = "An updated prompt card from an integration test.",
            PromptText = "Update this endpoint test.",
            Tags = ["updated", "tests", "api"]
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/promptcards/{id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}