extern alias KrytenApplication;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CountUnread = KrytenApplication::KrytenAssist.Application.Cruises.CountUnreadCruiseAlerts;
using OperationStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertOperationStatus;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseAlertCoordinator(CountUnread countUnread) : INotifyPropertyChanged
{
    private CancellationTokenSource? _cancellation;
    private int _generation;
    private int _unreadCount;
    private bool _hasCountError;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? AlertsChanged;

    public int UnreadCount { get => _unreadCount; private set { if (_unreadCount == value) return; _unreadCount = value; Changed(); Changed(nameof(BadgeText)); } }
    public string BadgeText => UnreadCount == 0 ? "Alerts" : $"Alerts · {UnreadCount}";
    public bool HasCountError { get => _hasCountError; private set { if (_hasCountError == value) return; _hasCountError = value; Changed(); } }

    public async Task RefreshCountAsync()
    {
        var generation = ++_generation;
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();
        var result = await countUnread.ExecuteAsync(_cancellation.Token);
        if (generation != _generation) return;
        if (result.Status == OperationStatus.Success)
        {
            UnreadCount = result.Count;
            HasCountError = false;
        }
        else if (result.Status == OperationStatus.Failed)
        {
            HasCountError = true;
        }
    }

    public async Task NotifyAlertsCreatedAsync(int createdCount)
    {
        if (createdCount <= 0) return;
        await RefreshCountAsync();
        AlertsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Cancel()
    {
        ++_generation;
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
    }

    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
