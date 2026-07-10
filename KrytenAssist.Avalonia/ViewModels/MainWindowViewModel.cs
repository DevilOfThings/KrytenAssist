using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;
using KrytenAssist.Avalonia.Models;
using KrytenAssist.Avalonia.Services;
using System.Windows.Input;
using System.ComponentModel;
using System.Diagnostics;
using KrytenAssist.Avalonia.Options;
using Microsoft.Extensions.Options;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IPromptCardStore _promptCardStore;
    private string _newCategory = string.Empty;
    private string _searchText = string.Empty;
    private CancellationTokenSource? _searchDebounceCancellation;
    private static readonly TimeSpan SearchDebounceDelay = TimeSpan.FromMilliseconds(400);
    
    public ObservableCollection<string> Categories { get; } = [];
    private readonly Dictionary<string, EmbeddingVector> _embeddingCache = new();
    
    public bool HasNoSearchResults =>
        !string.IsNullOrWhiteSpace(SearchText) &&
        FilteredPromptCards.Count == 0;

    private readonly IEmbeddingService _embeddingService;
    private readonly CosineSimilarityService _cosineSimilarityService;
    private readonly IConversationService _conversationService;
    private readonly string _conversationSystemPrompt;
    private CancellationTokenSource? _conversationCancellationTokenSource;
    private string _conversationInput = string.Empty;
    private bool _isConversationBusy;
    private string? _conversationErrorMessage;
    private readonly IEmbeddingServiceStatus? _embeddingServiceStatus;
    private string? _embeddingStatusMessage;

    public MainWindowViewModel(
        IPromptCardStore promptCardStore,
        IEmbeddingService embeddingService,
        CosineSimilarityService cosineSimilarityService,
        IConversationService conversationService,
        IOptions<ConversationOptions> conversationOptions)
    {
        _promptCardStore = promptCardStore;
        _embeddingService = embeddingService;
        _embeddingServiceStatus = embeddingService as IEmbeddingServiceStatus;

        if (_embeddingServiceStatus is not null)
        {
            _embeddingServiceStatus.StatusChanged += OnEmbeddingServiceStatusChanged;
            _embeddingStatusMessage = _embeddingServiceStatus.StatusMessage;
        }
        _cosineSimilarityService = cosineSimilarityService;
        _conversationService = conversationService;
        _conversationSystemPrompt = conversationOptions.Value.SystemPrompt;
        
        SaveCommand = new AsyncCommand(SaveAsync);
        
        SelectCategoryCommand = new RelayCommand(parameter =>
        {
            if (parameter is string category)
            {
                NewCategory = category;
            }
        });
        
        SendMessageCommand = new AsyncCommand(SendMessageAsync);

        CancelConversationCommand = new RelayCommand(_ =>
            _conversationCancellationTokenSource?.Cancel());
    }

    public ICommand SaveCommand { get; }
    public ICommand SelectCategoryCommand { get; }
    public ICommand SendMessageCommand { get; }

    public ICommand CancelConversationCommand { get; }
    
    public string NewTitle { get; set; } = string.Empty;

    public string NewCategory
    {
        get => _newCategory;
        set
        {
            if (_newCategory == value)
            {
                return;
            }

            _newCategory = value;
            OnPropertyChanged(nameof(NewCategory));
        }
    }

    public string NewDescription { get; set; } = string.Empty;

    public string NewPromptText { get; set; } = string.Empty;

    public string NewTags { get; set; } = string.Empty;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value)
            {
                return;
            }

            _searchText = value;
            OnPropertyChanged(nameof(SearchText));
            _ = DebounceSearchAsync();
        }
    }

    public ObservableCollection<PromptCardModel> PromptCards { get; } = new();

    public ObservableCollection<PromptCardModel> FilteredPromptCards { get; } = new();

    public ObservableCollection<ConversationMessage> ConversationHistory { get; } = new();

    public string ConversationInput
    {
        get => _conversationInput;
        set
        {
            if (_conversationInput == value)
            {
                return;
            }

            _conversationInput = value;
            OnPropertyChanged(nameof(ConversationInput));
        }
    }

    public bool IsConversationBusy
    {
        get => _isConversationBusy;
        private set
        {
            if (_isConversationBusy == value)
            {
                return;
            }

            _isConversationBusy = value;
            OnPropertyChanged(nameof(IsConversationBusy));
        }
    }

    public string? ConversationErrorMessage
    {
        get => _conversationErrorMessage;
        private set
        {
            if (_conversationErrorMessage == value)
            {
                return;
            }

            _conversationErrorMessage = value;
            OnPropertyChanged(nameof(ConversationErrorMessage));
            OnPropertyChanged(nameof(HasConversationError));
        }
    }

    public bool HasConversationError =>
        !string.IsNullOrWhiteSpace(ConversationErrorMessage);

    public string? EmbeddingStatusMessage
    {
        get => _embeddingStatusMessage;
        private set
        {
            if (_embeddingStatusMessage == value)
            {
                return;
            }

            _embeddingStatusMessage = value;
            OnPropertyChanged(nameof(EmbeddingStatusMessage));
            OnPropertyChanged(nameof(HasEmbeddingStatusMessage));
        }
    }

    public bool HasEmbeddingStatusMessage =>
        !string.IsNullOrWhiteSpace(EmbeddingStatusMessage);
    
    private void OnEmbeddingServiceStatusChanged(object? sender, EventArgs e)
    {
        EmbeddingStatusMessage = _embeddingServiceStatus?.StatusMessage;
    }
   
    private async Task SendMessageAsync()
    {
        if (IsConversationBusy || string.IsNullOrWhiteSpace(ConversationInput))
        {
            return;
        }

        var userMessage = ConversationInput.Trim();

        // Before calling _conversationService.SendAsync(...), create a new CancellationTokenSource and assign it to _conversationCancellationTokenSource.
        _conversationCancellationTokenSource?.Dispose();
        _conversationCancellationTokenSource = new CancellationTokenSource();

        ConversationErrorMessage = null;
        IsConversationBusy = true;
        ConversationInput = string.Empty;

        ConversationHistory.Add(new ConversationMessage
        {
            Role = ConversationRole.User,
            Content = userMessage
        });

        try
        {
            // Pass _conversationCancellationTokenSource.Token into _conversationService.SendAsync(...).
            var response = await _conversationService.SendAsync(
                new ConversationRequest
                {
                    SystemPrompt = _conversationSystemPrompt,
                    UserMessage = userMessage
                },
                _conversationCancellationTokenSource.Token);

            ConversationHistory.Add(new ConversationMessage
            {
                Role = ConversationRole.Assistant,
                Content = response.Content
            });
        }
        catch (OperationCanceledException)
        {
            ConversationInput = userMessage;
        }
        catch (Exception exception)
        {
            ConversationInput = userMessage;
            ConversationErrorMessage = exception.Message;
        }
        finally
        {
            IsConversationBusy = false;
            // In the finally block, dispose _conversationCancellationTokenSource and set it to null.
            _conversationCancellationTokenSource.Dispose();
            _conversationCancellationTokenSource = null;
        }
    }
    public async Task LoadAsync()
    {
        var promptCards = await _promptCardStore.GetAllAsync();

        PromptCards.Clear();
        _embeddingCache.Clear();

        foreach (var promptCard in promptCards)
        {
            PromptCards.Add(promptCard);
        }
        
        RefreshCategories();
        await RefreshFilteredPromptCardsAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    internal sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;

        public RelayCommand(Action<object?> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }
    
    public async Task SaveAsync()
    {
        var promptCard = new PromptCardModel
        {
            Id = Guid.NewGuid(),
            Title = NewTitle,
            Category = NewCategory,
            Description = NewDescription,
            PromptText = NewPromptText,
            Tags = NewTags
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(tag => tag.Trim())
                .ToList(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var promptCards = (await _promptCardStore.GetAllAsync()).ToList();
        promptCards.Add(promptCard);

        await _promptCardStore.SaveAllAsync(promptCards);
        _embeddingCache.Clear();

        await LoadAsync();
    }
    
    internal sealed class AsyncCommand : ICommand
    {
        private readonly Func<Task> _execute;

        public AsyncCommand(Func<Task> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public async void Execute(object? parameter)
        {
            await _execute();
        }
    }
    
    private async Task DebounceSearchAsync()
    {
        _searchDebounceCancellation?.Cancel();
        _searchDebounceCancellation?.Dispose();

        _searchDebounceCancellation = new CancellationTokenSource();
        var cancellationToken = _searchDebounceCancellation.Token;

        try
        {
            await Task.Delay(SearchDebounceDelay, cancellationToken);
            await RefreshFilteredPromptCardsAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // A newer search superseded this one.
        }
    }
    
    private void RefreshCategories()
    {
        Categories.Clear();

        foreach (var category in PromptCards
                     .Select(p => p.Category)
                     .Where(c => !string.IsNullOrWhiteSpace(c))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(c => c))
        {
            Categories.Add(category);
        }
    }
    
    private async Task RefreshFilteredPromptCardsAsync(
        CancellationToken cancellationToken = default)
    {

    var search = SearchText.Trim();

    var prompts = PromptCards
        .Where(prompt => MatchesKeywordSearch(prompt, search))
        .ToList();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(
            search,
            cancellationToken);

        var rankedPrompts = new List<(PromptCardModel Prompt, double Similarity)>();

        foreach (var prompt in prompts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var promptEmbedding = await GetPromptEmbeddingAsync(
                prompt,
                cancellationToken);

            var similarity = _cosineSimilarityService.Calculate(
                queryEmbedding,
                promptEmbedding);

            rankedPrompts.Add((prompt, similarity));
        }

        prompts = rankedPrompts
            .OrderByDescending(result => result.Similarity)
            .Select(result => result.Prompt)
            .ToList();
    }

    cancellationToken.ThrowIfCancellationRequested();

    FilteredPromptCards.Clear();

    foreach (var prompt in prompts)
    {
        FilteredPromptCards.Add(prompt);
    }

    OnPropertyChanged(nameof(HasNoSearchResults));

    }
    
    private static string BuildSearchableText(PromptCardModel prompt)
    {
        return string.Join(' ',
            prompt.Title,
            prompt.Category,
            prompt.Description,
            prompt.PromptText,
            string.Join(' ', prompt.Tags));
    }
    
    private static bool MatchesKeywordSearch(PromptCardModel prompt, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        var searchableText = BuildSearchableText(prompt);

        return searchableText.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }
    private async Task<EmbeddingVector> GetPromptEmbeddingAsync(
        PromptCardModel prompt,
        CancellationToken cancellationToken)
    {
        var searchableText = BuildSearchableText(prompt);

        if (_embeddingCache.TryGetValue(searchableText, out var cachedEmbedding))
        {
            Debug.WriteLine($"Embedding cache hit: {prompt.Title}");
            return cachedEmbedding;
        }

        Debug.WriteLine($"Embedding cache miss: {prompt.Title}");

        var embedding = await _embeddingService.GenerateEmbeddingAsync(
            searchableText,
            cancellationToken);
        _embeddingCache[searchableText] = embedding;

        return embedding;
    }
}