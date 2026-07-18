using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Abstractions.Persistence;

public interface ICruisePreferencesRepository
{
    Task<CruisePreferences> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CruisePreferences preferences, CancellationToken cancellationToken = default);
}
