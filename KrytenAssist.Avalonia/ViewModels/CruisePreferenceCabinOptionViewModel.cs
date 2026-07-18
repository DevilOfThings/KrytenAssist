using System;
using System.ComponentModel;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruisePreferenceCabinOptionViewModel(
    CruiseCabinType cabin,
    string label,
    Action changed) : INotifyPropertyChanged
{
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;
    public CruiseCabinType Cabin { get; } = Enum.IsDefined(cabin)
        ? cabin
        : throw new ArgumentOutOfRangeException(nameof(cabin));
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
