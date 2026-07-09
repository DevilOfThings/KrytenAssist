using System.Collections.ObjectModel;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Models;
using KrytenAssist.Avalonia.Services;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class MainWindowViewModel
{
    private readonly IPromptCardStore _promptCardStore;

    public MainWindowViewModel(IPromptCardStore promptCardStore)
    {
        _promptCardStore = promptCardStore;
    }

    public ObservableCollection<PromptCardModel> PromptCards { get; } = new();

    public async Task LoadAsync()
    {
        var promptCards = await _promptCardStore.GetAllAsync();

        PromptCards.Clear();

        foreach (var promptCard in promptCards)
        {
            PromptCards.Add(promptCard);
        }
    }
}