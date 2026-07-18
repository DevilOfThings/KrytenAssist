using KrytenAssist.Application.Abstractions.Persistence;

namespace KrytenAssist.Application.Cruises;

public sealed class GetCruisePreferences(ICruisePreferencesRepository repository)
{
    public async Task<CruisePreferencesQueryResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return CruisePreferencesQueryResult.Cancelled();
        try { return CruisePreferencesQueryResult.Success(await repository.GetAsync(cancellationToken)); }
        catch (OperationCanceledException) { return CruisePreferencesQueryResult.Cancelled(); }
        catch (Exception) { return CruisePreferencesQueryResult.Failed(); }
    }
}
