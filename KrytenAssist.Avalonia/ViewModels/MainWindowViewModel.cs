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
    private readonly IConversationMemory _conversationMemory;
    private readonly string _conversationSystemPrompt;
    private CancellationTokenSource? _conversationCancellationTokenSource;
    private string _conversationInput = string.Empty;
    private bool _isConversationBusy;
    private string? _conversationErrorMessage;
    private readonly IEmbeddingServiceStatus? _embeddingServiceStatus;
    private string? _embeddingStatusMessage;
    private bool _isPromptEditorOpen;
    private bool _isDeleteConfirmationOpen;
    private PromptCardModel? _selectedPrompt;
    private Guid? _editingPromptId;
    private DateTime _editingPromptCreatedAt;
    private string _newTitle = string.Empty;
    private string _newDescription = string.Empty;
    private string _newPromptText = string.Empty;
    private string _newTags = string.Empty;
    private string? _promptEditorErrorMessage;

    public MainWindowViewModel(
        IPromptCardStore promptCardStore,
        IEmbeddingService embeddingService,
        CosineSimilarityService cosineSimilarityService,
        IConversationService conversationService,
        IConversationMemory conversationMemory,
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
        _conversationMemory = conversationMemory;
        _conversationSystemPrompt = conversationOptions.Value.SystemPrompt;
        
        SaveCommand = new AsyncCommand(SaveAndClosePromptEditorAsync);
        OpenPromptEditorCommand = new RelayCommand(_ => OpenCreatePromptEditor());
        OpenEditPromptCommand = new RelayCommand(OpenEditPrompt);
        ClosePromptEditorCommand = new RelayCommand(_ => ClosePromptEditor());
        UsePromptCommand = new RelayCommand(_ => UseSelectedPrompt());
        RequestDeletePromptCommand = new RelayCommand(_ => RequestDeleteSelectedPrompt());
        CancelDeletePromptCommand = new RelayCommand(_ => IsDeleteConfirmationOpen = false);
        ConfirmDeletePromptCommand = new AsyncCommand(DeleteSelectedPromptAsync);
        
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
        
        ClearConversationCommand = new RelayCommand(_ => ClearConversation());
    }

    public ICommand SaveCommand { get; }
    public ICommand OpenPromptEditorCommand { get; }
    public ICommand OpenEditPromptCommand { get; }
    public ICommand ClosePromptEditorCommand { get; }
    public ICommand UsePromptCommand { get; }
    public ICommand RequestDeletePromptCommand { get; }
    public ICommand CancelDeletePromptCommand { get; }
    public ICommand ConfirmDeletePromptCommand { get; }
    public ICommand SelectCategoryCommand { get; }
    public ICommand SendMessageCommand { get; }
    public ICommand ClearConversationCommand { get; }
    public ICommand CancelConversationCommand { get; }
    
    public string NewTitle
    {
        get => _newTitle;
        set => SetEditorField(ref _newTitle, value, nameof(NewTitle));
    }

    public string NewCategory
    {
        get => _newCategory;
        set => SetEditorField(ref _newCategory, value, nameof(NewCategory));
    }

    public string NewDescription
    {
        get => _newDescription;
        set => SetEditorField(ref _newDescription, value, nameof(NewDescription));
    }

    public string NewPromptText
    {
        get => _newPromptText;
        set => SetEditorField(ref _newPromptText, value, nameof(NewPromptText));
    }

    public string NewTags
    {
        get => _newTags;
        set => SetEditorField(ref _newTags, value, nameof(NewTags));
    }

    public PromptCardModel? SelectedPrompt
    {
        get => _selectedPrompt;
        set
        {
            if (ReferenceEquals(_selectedPrompt, value))
            {
                return;
            }

            _selectedPrompt = value;
            OnPropertyChanged(nameof(SelectedPrompt));
            OnPropertyChanged(nameof(HasSelectedPrompt));
            OnPropertyChanged(nameof(DeleteConfirmationMessage));
        }
    }

    public bool HasSelectedPrompt => SelectedPrompt is not null;

    public string PromptEditorTitle =>
        _editingPromptId.HasValue ? "Edit Prompt" : "Create Prompt";

    public string PromptEditorSaveButtonText =>
        _editingPromptId.HasValue ? "Save Changes" : "Save";

    public string? PromptEditorErrorMessage
    {
        get => _promptEditorErrorMessage;
        private set
        {
            if (_promptEditorErrorMessage == value)
            {
                return;
            }

            _promptEditorErrorMessage = value;
            OnPropertyChanged(nameof(PromptEditorErrorMessage));
            OnPropertyChanged(nameof(HasPromptEditorError));
        }
    }

    public bool HasPromptEditorError =>
        !string.IsNullOrWhiteSpace(PromptEditorErrorMessage);

    public bool IsDeleteConfirmationOpen
    {
        get => _isDeleteConfirmationOpen;
        private set
        {
            if (_isDeleteConfirmationOpen == value)
            {
                return;
            }

            _isDeleteConfirmationOpen = value;
            OnPropertyChanged(nameof(IsDeleteConfirmationOpen));
        }
    }

    public string DeleteConfirmationMessage => SelectedPrompt is null
        ? string.Empty
        : $"Delete \"{SelectedPrompt.Title}\"?";

    public bool IsPromptEditorOpen
    {
        get => _isPromptEditorOpen;
        private set
        {
            if (_isPromptEditorOpen == value)
            {
                return;
            }

            _isPromptEditorOpen = value;
            OnPropertyChanged(nameof(IsPromptEditorOpen));
        }
    }

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
            // Build conversation history with memory
            var requestMessages = _conversationMemory
                .GetRecentMessages()
                .ToList();

            requestMessages.Add(new ConversationMessage
            {
                Role = ConversationRole.User,
                Content = userMessage
            });

            var response = await _conversationService.SendAsync(
                new ConversationRequest
                {
                    SystemPrompt = _conversationSystemPrompt,
                    Messages = requestMessages
                },
                _conversationCancellationTokenSource.Token);

            ConversationHistory.Add(new ConversationMessage
            {
                Role = ConversationRole.Assistant,
                Content = response.Content
            });

            _conversationMemory.AddTurn(
                new ConversationMessage
                {
                    Role = ConversationRole.User,
                    Content = userMessage
                },
                new ConversationMessage
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
        await LoadAsync(SelectedPrompt?.Id);
    }

    private async Task LoadAsync(Guid? selectedPromptId)
    {
        var promptCards = await _promptCardStore.GetAllAsync();

        PromptCards.Clear();
        _embeddingCache.Clear();

        foreach (var promptCard in promptCards)
        {
            PromptCards.Add(promptCard);
        }
        
        RefreshCategories();
        await RefreshFilteredPromptCardsAsync(selectedPromptId: selectedPromptId);
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
        PromptEditorErrorMessage = null;

        if (string.IsNullOrWhiteSpace(NewTitle) ||
            string.IsNullOrWhiteSpace(NewPromptText))
        {
            PromptEditorErrorMessage = "Title and prompt text are required.";
            return;
        }

        var promptCards = (await _promptCardStore.GetAllAsync()).ToList();
        var now = DateTime.UtcNow;
        PromptCardModel savedPrompt;

        if (_editingPromptId.HasValue)
        {
            var existingIndex = promptCards.FindIndex(prompt =>
                prompt.Id == _editingPromptId.Value);

            if (existingIndex < 0)
            {
                PromptEditorErrorMessage = "The prompt could not be found.";
                return;
            }

            savedPrompt = BuildPromptCard(
                _editingPromptId.Value,
                _editingPromptCreatedAt,
                now);
            promptCards[existingIndex] = savedPrompt;
        }
        else
        {
            savedPrompt = BuildPromptCard(Guid.NewGuid(), now, now);
            promptCards.Add(savedPrompt);
        }

        await _promptCardStore.SaveAllAsync(promptCards);
        _embeddingCache.Clear();

        await LoadAsync(savedPrompt.Id);
    }

    private async Task SaveAndClosePromptEditorAsync()
    {
        await SaveAsync();

        if (!HasPromptEditorError)
        {
            ClosePromptEditor();
        }
    }

    public async Task DeleteSelectedPromptAsync()
    {
        if (SelectedPrompt is null)
        {
            IsDeleteConfirmationOpen = false;
            return;
        }

        var selectedPromptId = SelectedPrompt.Id;
        var promptCards = (await _promptCardStore.GetAllAsync())
            .Where(prompt => prompt.Id != selectedPromptId)
            .ToList();

        await _promptCardStore.SaveAllAsync(promptCards);
        _embeddingCache.Clear();
        IsDeleteConfirmationOpen = false;
        SelectedPrompt = null;

        await LoadAsync(selectedPromptId: null);
    }

    private void OpenCreatePromptEditor()
    {
        _editingPromptId = null;
        _editingPromptCreatedAt = default;
        ClearEditorFields();
        NotifyEditorModeChanged();
        IsPromptEditorOpen = true;
    }

    private void OpenEditPrompt(object? parameter)
    {
        var prompt = parameter as PromptCardModel ?? SelectedPrompt;

        if (prompt is null)
        {
            return;
        }

        SelectedPrompt = prompt;
        _editingPromptId = prompt.Id;
        _editingPromptCreatedAt = prompt.CreatedAt;
        NewTitle = prompt.Title;
        NewCategory = prompt.Category;
        NewDescription = prompt.Description ?? string.Empty;
        NewPromptText = prompt.PromptText;
        NewTags = string.Join(", ", prompt.Tags);
        PromptEditorErrorMessage = null;
        NotifyEditorModeChanged();
        IsPromptEditorOpen = true;
    }

    private void ClosePromptEditor()
    {
        IsPromptEditorOpen = false;
        PromptEditorErrorMessage = null;
    }

    private void UseSelectedPrompt()
    {
        if (SelectedPrompt is null)
        {
            return;
        }

        var promptText = SelectedPrompt.PromptText;

        ConversationInput = string.IsNullOrWhiteSpace(ConversationInput)
            ? promptText
            : $"{ConversationInput.TrimEnd()}{Environment.NewLine}{Environment.NewLine}---{Environment.NewLine}{Environment.NewLine}{promptText}";
    }

    private void RequestDeleteSelectedPrompt()
    {
        if (SelectedPrompt is not null)
        {
            IsDeleteConfirmationOpen = true;
        }
    }

    private PromptCardModel BuildPromptCard(
        Guid id,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new PromptCardModel
        {
            Id = id,
            Title = NewTitle.Trim(),
            Category = NewCategory.Trim(),
            Description = NewDescription.Trim(),
            PromptText = NewPromptText,
            Tags = NewTags
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(tag => tag.Trim())
                .Where(tag => tag.Length > 0)
                .ToList(),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    private void ClearEditorFields()
    {
        NewTitle = string.Empty;
        NewCategory = string.Empty;
        NewDescription = string.Empty;
        NewPromptText = string.Empty;
        NewTags = string.Empty;
        PromptEditorErrorMessage = null;
    }

    private void NotifyEditorModeChanged()
    {
        OnPropertyChanged(nameof(PromptEditorTitle));
        OnPropertyChanged(nameof(PromptEditorSaveButtonText));
    }

    private void SetEditorField(
        ref string field,
        string value,
        string propertyName)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);

        if (HasPromptEditorError)
        {
            PromptEditorErrorMessage = null;
        }
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
        CancellationToken cancellationToken = default,
        Guid? selectedPromptId = null)
    {

    selectedPromptId ??= SelectedPrompt?.Id;
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

    SelectedPrompt = selectedPromptId.HasValue
        ? FilteredPromptCards.FirstOrDefault(prompt => prompt.Id == selectedPromptId.Value)
        : null;

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
    
    private void ClearConversation()
    {
        if (IsConversationBusy)
        {
            _conversationCancellationTokenSource?.Cancel();
        }

        ConversationHistory.Clear();

        _conversationMemory.Clear();

        ConversationErrorMessage = null;

        ConversationInput = string.Empty;
    }
}
