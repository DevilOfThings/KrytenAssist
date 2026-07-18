extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using FavouriteRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.IFavouriteCruiseShipRepository;
using PreferencesRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruisePreferencesRepository;
using SavedRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ISavedCruiseRepository;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

internal sealed class FakeSavedCruiseRepository : SavedRepository
{
    internal Dictionary<CruiseSailingKey, SavedCruise> Items { get; } = [];
    internal Exception? Exception { get; set; }
    internal int UpsertCalls { get; private set; }
    internal int RemoveCalls { get; private set; }

    public Task<SavedCruise?> GetAsync(CruiseSailingKey key, CancellationToken token = default) =>
        Exception is null ? Task.FromResult(Items.GetValueOrDefault(key)) : Task.FromException<SavedCruise?>(Exception);

    public Task<IReadOnlyList<SavedCruise>> ListAsync(CancellationToken token = default) =>
        Exception is null ? Task.FromResult<IReadOnlyList<SavedCruise>>(Items.Values.ToArray()) : Task.FromException<IReadOnlyList<SavedCruise>>(Exception);

    public Task UpsertAsync(SavedCruise value, CancellationToken token = default)
    {
        if (Exception is not null) return Task.FromException(Exception);
        UpsertCalls++; Items[value.SailingKey] = value; return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(CruiseSailingKey key, CancellationToken token = default)
    {
        if (Exception is not null) return Task.FromException<bool>(Exception);
        RemoveCalls++; return Task.FromResult(Items.Remove(key));
    }
}

internal sealed class FakeFavouriteCruiseShipRepository : FavouriteRepository
{
    internal HashSet<CruiseShipKey> Items { get; } = [];
    internal Exception? Exception { get; set; }
    public Task<IReadOnlyList<CruiseShipKey>> ListAsync(CancellationToken token = default) => Exception is null ? Task.FromResult<IReadOnlyList<CruiseShipKey>>(Items.ToArray()) : Task.FromException<IReadOnlyList<CruiseShipKey>>(Exception);
    public Task<bool> SetAsync(CruiseShipKey key, bool value, CancellationToken token = default)
    {
        if (Exception is not null) return Task.FromException<bool>(Exception);
        return Task.FromResult(value ? Items.Add(key) : Items.Remove(key));
    }
}

internal sealed class FakeCruisePreferencesRepository : PreferencesRepository
{
    internal CruisePreferences Value { get; set; } = new();
    internal Exception? Exception { get; set; }
    internal int SaveCalls { get; private set; }
    public Task<CruisePreferences> GetAsync(CancellationToken token = default) => Exception is null ? Task.FromResult(Value) : Task.FromException<CruisePreferences>(Exception);
    public Task SaveAsync(CruisePreferences value, CancellationToken token = default)
    {
        if (Exception is not null) return Task.FromException(Exception);
        SaveCalls++; Value = value; return Task.CompletedTask;
    }
}
