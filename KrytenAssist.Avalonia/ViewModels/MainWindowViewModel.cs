using System;
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
    
    public ObservableCollection<string> Categories { get; } = [];

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

    public ObservableCollection<PromptCardModel> PromptCards { get; } = new();

    public async Task LoadAsync()
    {
        var promptCards = await _promptCardStore.GetAllAsync();

        PromptCards.Clear();

        foreach (var promptCard in promptCards)
        {
            PromptCards.Add(promptCard);
        }
        
        RefreshCategories();
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
}