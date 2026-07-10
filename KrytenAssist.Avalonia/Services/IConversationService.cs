using System.Threading;
using System.Threading.Tasks;
using KrytenAssist.Avalonia.Models;

namespace KrytenAssist.Avalonia.Services;

public interface IConversationService
{
    Task<ConversationResponse> SendAsync(
        ConversationRequest request,
        CancellationToken cancellationToken = default);
}