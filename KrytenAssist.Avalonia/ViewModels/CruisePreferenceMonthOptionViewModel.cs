using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruisePreferenceMonthOptionViewModel(
    int month,
    string label,
    Action changed) : INotifyPropertyChanged
{
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;
    public int Month { get; } = month is >= 1 and <= 12
        ? month
        : throw new ArgumentOutOfRangeException(nameof(month));
    public string Label { get; } = label ?? throw new ArgumentNullException(nameof(label));
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            changed();
        }
    }
}
