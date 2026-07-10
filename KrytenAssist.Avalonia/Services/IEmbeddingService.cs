using System.Threading;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Models;

namespace KrytenAssist.Avalonia.Services;

public interface IEmbeddingService
{
    Task<EmbeddingVector> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}