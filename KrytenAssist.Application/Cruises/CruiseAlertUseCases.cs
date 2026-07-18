using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class ListCruiseAlerts(ICruiseAlertRepository repository)
{
    public async Task<CruiseAlertQueryResult> ExecuteAsync(CruiseAlertQuery query, CancellationToken token = default)
    { if (token.IsCancellationRequested) return CruiseAlertQueryResult.Cancelled(); try { var values = await repository.ListAsync(query, token); return CruiseAlertQueryResult.Success(values.OrderByDescending(x => x.EventTime).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.EventKey, StringComparer.Ordinal)); } catch (OperationCanceledException) { return CruiseAlertQueryResult.Cancelled(); } catch { return CruiseAlertQueryResult.Failed(); } }
}

public sealed class GetCruiseAlert(ICruiseAlertRepository repository)
{
    public async Task<CruiseAlertItemResult> ExecuteAsync(Guid id, CancellationToken token = default)
    { if (token.IsCancellationRequested) return new(CruiseAlertOperationStatus.Cancelled, null, "Loading the alert was cancelled."); try { var value = await repository.GetAsync(id, token); return value is null ? new(CruiseAlertOperationStatus.NotFound, null, null) : new(CruiseAlertOperationStatus.Found, value, null); } catch (OperationCanceledException) { return new(CruiseAlertOperationStatus.Cancelled, null, "Loading the alert was cancelled."); } catch { return new(CruiseAlertOperationStatus.Failed, null, "The alert could not be loaded locally."); } }
}

public sealed class CountUnreadCruiseAlerts(ICruiseAlertRepository repository)
{
    public async Task<CruiseAlertCountResult> ExecuteAsync(CancellationToken token = default)
    { if (token.IsCancellationRequested) return new(CruiseAlertOperationStatus.Cancelled, 0, "Counting alerts was cancelled."); try { return new(CruiseAlertOperationStatus.Success, await repository.CountUnreadAsync(token), null); } catch (OperationCanceledException) { return new(CruiseAlertOperationStatus.Cancelled, 0, "Counting alerts was cancelled."); } catch { return new(CruiseAlertOperationStatus.Failed, 0, "Unread alerts could not be counted locally."); } }
}

public sealed class ChangeCruiseAlertStatus(ICruiseAlertRepository repository)
{
    public async Task<CruiseAlertMutationResult> ExecuteAsync(Guid id, CruiseAlertStatus status, CancellationToken token = default)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (token.IsCancellationRequested) return new(CruiseAlertOperationStatus.Cancelled, null, "The alert change was cancelled.");
        try { var existing = await repository.GetAsync(id, token); if (existing is null) return new(CruiseAlertOperationStatus.NotFound, null, null); if (existing.Status == status) return new(CruiseAlertOperationStatus.Unchanged, existing, null); await repository.UpdateStatusAsync(id, status, token); return new(CruiseAlertOperationStatus.Updated, existing.WithStatus(status), null); }
        catch (OperationCanceledException) { return new(CruiseAlertOperationStatus.Cancelled, null, "The alert change was cancelled."); } catch { return new(CruiseAlertOperationStatus.Failed, null, "The alert could not be changed locally."); }
    }
}

public sealed class GetCruiseAlertSettings(ICruiseAlertSettingsRepository repository)
{
    public async Task<CruiseAlertSettingsResult> ExecuteAsync(CancellationToken token = default)
    { if (token.IsCancellationRequested) return new(CruiseAlertOperationStatus.Cancelled, null, "Loading alert settings was cancelled."); try { return new(CruiseAlertOperationStatus.Success, await repository.GetAsync(token), null); } catch (OperationCanceledException) { return new(CruiseAlertOperationStatus.Cancelled, null, "Loading alert settings was cancelled."); } catch { return new(CruiseAlertOperationStatus.Failed, null, "Alert settings could not be loaded locally."); } }
}

public sealed class SaveCruiseAlertSettings(ICruiseAlertSettingsRepository repository)
{
    public async Task<CruiseAlertSettingsResult> ExecuteAsync(CruiseAlertSettings settings, CancellationToken token = default)
    { ArgumentNullException.ThrowIfNull(settings); if (token.IsCancellationRequested) return new(CruiseAlertOperationStatus.Cancelled, null, "Saving alert settings was cancelled."); try { var old = await repository.GetAsync(token); if (old == settings) return new(CruiseAlertOperationStatus.Unchanged, old, null); await repository.SaveAsync(settings, token); return new(CruiseAlertOperationStatus.Updated, settings, null); } catch (OperationCanceledException) { return new(CruiseAlertOperationStatus.Cancelled, null, "Saving alert settings was cancelled."); } catch { return new(CruiseAlertOperationStatus.Failed, null, "Alert settings could not be saved locally."); } }
}
