using KrytenAssist.Avalonia.Models;
using KrytenAssist.Avalonia.Options;
using KrytenAssist.Avalonia.Services;
using KrytenAssist.Avalonia.ViewModels;
using Microsoft.Extensions.Options;

namespace KrytenAssist.Avalonia.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task CreatePrompt_AddsAndSelectsPromptAndRefreshesCategories()
    {
        var store = new TestPromptCardStore();
        var viewModel = CreateViewModel(store);
        await viewModel.LoadAsync();
        viewModel.OpenPromptEditorCommand.Execute(null);
        viewModel.NewTitle = "Created prompt";
        viewModel.NewCategory = "Created Category";
        viewModel.NewDescription = "Created description";
        viewModel.NewPromptText = "Created prompt text";
        viewModel.NewTags = "created, test";

        await viewModel.SaveAsync();

        var savedPrompt = Assert.Single(store.PromptCards);
        Assert.NotEqual(Guid.Empty, savedPrompt.Id);
        Assert.Equal(["created", "test"], savedPrompt.Tags);
        Assert.Contains("Created Category", viewModel.Categories);
        Assert.Equal(savedPrompt.Id, viewModel.SelectedPrompt?.Id);
    }

    [Fact]
    public async Task UsePrompt_CopiesSelectedPromptWithoutSendingOrChangingStoredPrompt()
    {
        var prompt = CreatePrompt(promptText: "Review this code for correctness.");
        var store = new TestPromptCardStore(prompt);
        var viewModel = CreateViewModel(store);
        await viewModel.LoadAsync();
        viewModel.SelectedPrompt = viewModel.PromptCards.Single();

        viewModel.UsePromptCommand.Execute(null);

        Assert.Equal(prompt.PromptText, viewModel.ConversationInput);
        Assert.Empty(viewModel.ConversationHistory);
        Assert.Equal(prompt.PromptText, store.PromptCards.Single().PromptText);
        Assert.Same(viewModel.PromptCards.Single(), viewModel.SelectedPrompt);
    }

    [Fact]
    public async Task UsePrompt_AppendsBehindSeparatorWhenComposerContainsText()
    {
        var prompt = CreatePrompt(promptText: "Stored prompt text");
        var viewModel = CreateViewModel(new TestPromptCardStore(prompt));
        await viewModel.LoadAsync();
        viewModel.SelectedPrompt = viewModel.PromptCards.Single();
        viewModel.ConversationInput = "Existing draft";

        viewModel.UsePromptCommand.Execute(null);

        Assert.Contains("Existing draft", viewModel.ConversationInput);
        Assert.Contains("---", viewModel.ConversationInput);
        Assert.EndsWith(prompt.PromptText, viewModel.ConversationInput);
    }

    [Fact]
    public async Task EditPrompt_UpdatesExistingPromptAndPreservesIdentityAndCreatedDate()
    {
        var createdAt = new DateTime(2026, 7, 1, 10, 30, 0, DateTimeKind.Utc);
        var prompt = CreatePrompt(category: "Old Category", createdAt: createdAt);
        var store = new TestPromptCardStore(prompt);
        var viewModel = CreateViewModel(store);
        await viewModel.LoadAsync();
        var selectedPrompt = viewModel.PromptCards.Single();
        viewModel.SelectedPrompt = selectedPrompt;
        viewModel.OpenEditPromptCommand.Execute(selectedPrompt);
        viewModel.NewTitle = "Updated title";
        viewModel.NewCategory = "New Category";
        viewModel.NewDescription = "Updated description";
        viewModel.NewPromptText = "Updated prompt text";
        viewModel.NewTags = "updated, review";

        await viewModel.SaveAsync();

        var savedPrompt = Assert.Single(store.PromptCards);
        Assert.Equal(prompt.Id, savedPrompt.Id);
        Assert.Equal(createdAt, savedPrompt.CreatedAt);
        Assert.True(savedPrompt.UpdatedAt > createdAt);
        Assert.Equal("Updated title", savedPrompt.Title);
        Assert.Equal("Updated prompt text", savedPrompt.PromptText);
        Assert.Equal(["updated", "review"], savedPrompt.Tags);
        Assert.DoesNotContain("Old Category", viewModel.Categories);
        Assert.Contains("New Category", viewModel.Categories);
        Assert.Equal(prompt.Id, viewModel.SelectedPrompt?.Id);
    }

    [Fact]
    public async Task InvalidEdit_DoesNotOverwriteStoredPrompt()
    {
        var prompt = CreatePrompt();
        var store = new TestPromptCardStore(prompt);
        var viewModel = CreateViewModel(store);
        await viewModel.LoadAsync();
        viewModel.OpenEditPromptCommand.Execute(viewModel.PromptCards.Single());
        viewModel.NewTitle = " ";

        await viewModel.SaveAsync();

        Assert.Equal(prompt.Title, store.PromptCards.Single().Title);
        Assert.True(viewModel.HasPromptEditorError);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task DeletePrompt_RefreshesPromptsCategoriesSearchAndSelection()
    {
        var deletedPrompt = CreatePrompt(
            title: "Delete me",
            category: "Temporary");
        var retainedPrompt = CreatePrompt(
            title: "Keep me",
            category: "Permanent");
        var store = new TestPromptCardStore(deletedPrompt, retainedPrompt);
        var viewModel = CreateViewModel(store);
        await viewModel.LoadAsync();
        viewModel.SelectedPrompt = viewModel.PromptCards.Single(prompt =>
            prompt.Id == deletedPrompt.Id);
        viewModel.SearchText = "Delete";
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        await viewModel.DeleteSelectedPromptAsync();

        Assert.Single(store.PromptCards);
        Assert.DoesNotContain(store.PromptCards, prompt => prompt.Id == deletedPrompt.Id);
        Assert.Single(viewModel.PromptCards);
        Assert.Empty(viewModel.FilteredPromptCards);
        Assert.True(viewModel.HasNoSearchResults);
        Assert.Null(viewModel.SelectedPrompt);
        Assert.DoesNotContain("Temporary", viewModel.Categories);
        Assert.Contains("Permanent", viewModel.Categories);
    }

    private static MainWindowViewModel CreateViewModel(IPromptCardStore store)
    {
        return new MainWindowViewModel(
            store,
            new TestEmbeddingService(),
            new CosineSimilarityService(),
            new TestConversationService(),
            new TestConversationMemory(),
            Microsoft.Extensions.Options.Options.Create(new ConversationOptions()));
    }

    private static PromptCardModel CreatePrompt(
        string title = "Test prompt",
        string category = "Testing",
        string promptText = "Test prompt text",
        DateTime? createdAt = null)
    {
        return new PromptCardModel
        {
            Id = Guid.NewGuid(),
            Title = title,
            Category = category,
            Description = "Description",
            PromptText = promptText,
            Tags = ["test"],
            CreatedAt = createdAt ?? DateTime.UtcNow.AddDays(-1),
            UpdatedAt = createdAt ?? DateTime.UtcNow.AddDays(-1)
        };
    }

    private sealed class TestPromptCardStore(params PromptCardModel[] promptCards)
        : IPromptCardStore
    {
        public List<PromptCardModel> PromptCards { get; private set; } = [.. promptCards];

        public int SaveCount { get; private set; }

        public Task<IReadOnlyCollection<PromptCardModel>> GetAllAsync()
        {
            return Task.FromResult<IReadOnlyCollection<PromptCardModel>>(PromptCards);
        }

        public Task SaveAllAsync(IReadOnlyCollection<PromptCardModel> promptCards)
        {
            PromptCards = [.. promptCards];
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class TestEmbeddingService : IEmbeddingService
    {
        public Task<EmbeddingVector> GenerateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EmbeddingVector([1.0]));
        }
    }

    private sealed class TestConversationService : IConversationService
    {
        public Task<ConversationResponse> SendAsync(
            ConversationRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ConversationResponse { Content = "Response" });
        }
    }

    private sealed class TestConversationMemory : IConversationMemory
    {
        public IReadOnlyList<ConversationMessage> GetRecentMessages() => [];

        public void AddTurn(
            ConversationMessage userMessage,
            ConversationMessage assistantMessage)
        {
        }

        public void Clear()
        {
        }
    }
}
