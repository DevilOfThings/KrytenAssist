

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Models;

namespace KrytenAssist.Avalonia.Services;

public sealed class JsonPromptCardStore : IPromptCardStore
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public JsonPromptCardStore()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var krytenFolderPath = Path.Combine(appDataPath, "KrytenAssist");

        Directory.CreateDirectory(krytenFolderPath);

        _filePath = Path.Combine(krytenFolderPath, "prompt-cards.json");
    }

    public async Task<IReadOnlyCollection<PromptCardModel>> GetAllAsync()
    {
        if (!File.Exists(_filePath))
        {
            var sampleCards = new List<PromptCardModel>
            {
                new()
                {
                    Title = "Example Prompt",
                    Category = "Getting Started",
                    Description = "This prompt confirms offline storage is working.",
                    PromptText = "Explain the Repository pattern with a simple C# example.",
                    Tags = new List<string> { "example", "offline" }
                }
            };

            await SaveAllAsync(sampleCards);

            return sampleCards;
        }

        await using var stream = File.OpenRead(_filePath);

        var promptCards = await JsonSerializer.DeserializeAsync<List<PromptCardModel>>(stream, _jsonOptions);

        return promptCards is null
            ? Array.Empty<PromptCardModel>()
            : promptCards;
    }

    public async Task SaveAllAsync(IReadOnlyCollection<PromptCardModel> promptCards)
    {
        await using var stream = File.Create(_filePath);

        await JsonSerializer.SerializeAsync(stream, promptCards, _jsonOptions);
    }
}