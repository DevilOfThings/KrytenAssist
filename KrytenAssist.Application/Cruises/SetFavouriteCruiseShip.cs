using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class SetFavouriteCruiseShip(IFavouriteCruiseShipRepository repository)
{
    public async Task<PersonalCruisePreferenceMutationResult> ExecuteAsync(CruiseShipKey key, bool isFavourite, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (cancellationToken.IsCancellationRequested) return PersonalCruisePreferenceMutationResult.Cancelled();
        try { return await repository.SetAsync(key, isFavourite, cancellationToken) ? PersonalCruisePreferenceMutationResult.Updated() : PersonalCruisePreferenceMutationResult.Unchanged(); }
        catch (OperationCanceledException) { return PersonalCruisePreferenceMutationResult.Cancelled(); }
        catch (Exception) { return PersonalCruisePreferenceMutationResult.Failed(); }
    }
}
