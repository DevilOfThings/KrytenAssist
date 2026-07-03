using KrytenAssist.Application.PromptCards;

namespace KrytenAssist.Api.Tests.TestData;

public static class PromptCardRequests
{
    public static CreatePromptCardRequest CreateValidRequest() => new(
        Title: "Test Prompt Card",
        Category: "Testing",
        Description: "A prompt card created during an integration test.",
        PromptText: "Write a test for this endpoint.",
        Tags: ["tests", "api"]);
}