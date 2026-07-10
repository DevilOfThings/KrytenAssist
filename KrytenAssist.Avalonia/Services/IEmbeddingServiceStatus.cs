using System;

namespace KrytenAssist.Avalonia.Services;

public interface IEmbeddingServiceStatus
{
    string? StatusMessage { get; }

    event EventHandler? StatusChanged;
}