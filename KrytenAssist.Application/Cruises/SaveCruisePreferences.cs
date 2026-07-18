using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class SaveCruisePreferences(ICruisePreferencesRepository repository)
{
    public async Task<PersonalCruisePreferenceMutationResult> ExecuteAsync(CruisePreferences preferences, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        if (cancellationToken.IsCancellationRequested) return PersonalCruisePreferenceMutationResult.Cancelled();
        try
        {
            if ((await repository.GetAsync(cancellationToken)).Equals(preferences)) return PersonalCruisePreferenceMutationResult.Unchanged();
            await repository.SaveAsync(preferences, cancellationToken);
            return PersonalCruisePreferenceMutationResult.Updated();
        }
        catch (OperationCanceledException) { return PersonalCruisePreferenceMutationResult.Cancelled(); }
        catch (Exception) { return PersonalCruisePreferenceMutationResult.Failed(); }
    }
}
