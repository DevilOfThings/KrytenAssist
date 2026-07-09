using System.Collections.Generic;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Models;

namespace KrytenAssist.Avalonia.Services;

public interface IPromptCardStore
{
    Task<IReadOnlyCollection<PromptCardModel>> GetAllAsync();

    Task SaveAllAsync(IReadOnlyCollection<PromptCardModel> promptCards);
}