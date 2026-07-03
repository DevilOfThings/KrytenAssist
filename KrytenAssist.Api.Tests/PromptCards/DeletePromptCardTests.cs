

using System.Net;
using System.Net.Http.Json;
using KrytenAssist.Api.Tests.Infrastructure;
using KrytenAssist.Api.Tests.TestData;
using KrytenAssist.Application.PromptCards;

namespace KrytenAssist.Api.Tests.PromptCards;

public sealed class DeletePromptCardTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task DeletePromptCard_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        var request = PromptCardRequests.CreateValidRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/promptcards", request);
        var createResponseBody = await createResponse.Content.ReadFromJsonAsync<CreatePromptCardResponse>();

        Assert.NotNull(createResponseBody);

        // Act
        var response = await _client.DeleteAsync($"/api/promptcards/{createResponseBody.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
    
    [Fact]
    public async Task DeletePromptCard_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/promptcards/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}