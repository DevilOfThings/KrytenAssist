using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseHistoryGroupViewModel : INotifyPropertyChanged
{
    private readonly Action<CruiseHistoryItemViewModel?> _selectionChanged;
    private CruiseHistoryItemViewModel? _selectedHistory;

    public CruiseHistoryGroupViewModel(
        string? title,
        IReadOnlyList<CruiseHistoryItemViewModel> histories,
        CruiseHistoryItemViewModel? selectedHistory,
        Action<CruiseHistoryItemViewModel?> selectionChanged)
    {
        Title = title;
        Histories = histories;
        _selectedHistory = selectedHistory;
        _selectionChanged = selectionChanged;
    }

    public string? Title { get; }
    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
    public IReadOnlyList<CruiseHistoryItemViewModel> Histories { get; }

    public CruiseHistoryItemViewModel? SelectedHistory
    {
        get => _selectedHistory;
        set
        {
            if (ReferenceEquals(_selectedHistory, value))
            {
                return;
            }

            _selectedHistory = value;
            OnPropertyChanged();
            _selectionChanged(value);
        }
    }

    internal void SynchronizeSelection(CruiseHistoryItemViewModel? selectedHistory)
    {
        if (!ReferenceEquals(_selectedHistory, selectedHistory))
        {
            _selectedHistory = selectedHistory;
            OnPropertyChanged(nameof(SelectedHistory));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
