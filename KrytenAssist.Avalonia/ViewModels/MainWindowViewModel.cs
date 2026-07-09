using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Models;
using KrytenAssist.Avalonia.Services;
using System.Windows.Input;
using System.ComponentModel;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IPromptCardStore _promptCardStore;
    private string _newCategory = string.Empty;
    private string _searchText = string.Empty;
    
    public ObservableCollection<string> Categories { get; } = [];
    
    public bool HasNoSearchResults =>
        !string.IsNullOrWhiteSpace(SearchText) &&
        FilteredPromptCards.Count == 0;

    public MainWindowViewModel(IPromptCardStore promptCardStore)
    {
        _promptCardStore = promptCardStore;
        SaveCommand = new AsyncCommand(SaveAsync);
        
        SelectCategoryCommand = new RelayCommand(parameter =>
        {
            if (parameter is string category)
            {
                NewCategory = category;
            }
        });
    }

    public ICommand SaveCommand { get; }
    public ICommand SelectCategoryCommand { get; }
    
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
            RefreshFilteredPromptCards();
        }
    }

    public ObservableCollection<PromptCardModel> PromptCards { get; } = new();

    public ObservableCollection<PromptCardModel> FilteredPromptCards { get; } = new();

    public async Task LoadAsync()
    {
        var promptCards = await _promptCardStore.GetAllAsync();

        PromptCards.Clear();

        foreach (var promptCard in promptCards)
        {
            PromptCards.Add(promptCard);
        }
        
        RefreshCategories();
        RefreshFilteredPromptCards();
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
    
    private void RefreshFilteredPromptCards()
    {
        FilteredPromptCards.Clear();

        IEnumerable<PromptCardModel> prompts = PromptCards;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();

            prompts = PromptCards.Where(prompt =>
                prompt.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                prompt.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(prompt.Description) &&
                 prompt.Description.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                prompt.PromptText.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                prompt.Tags.Any(tag => tag.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var prompt in prompts)
        {
            FilteredPromptCards.Add(prompt);
        }
        
        OnPropertyChanged(nameof(HasNoSearchResults));
    }
}