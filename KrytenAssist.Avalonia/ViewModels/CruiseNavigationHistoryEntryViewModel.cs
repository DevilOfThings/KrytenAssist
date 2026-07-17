using System;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseNavigationHistoryEntryViewModel
{
    public CruiseNavigationHistoryEntryViewModel(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        FullAddress = address.AbsoluteUri;
        DisplayAddress = BuildDisplayAddress(address);
    }

    public string FullAddress { get; }

    public string DisplayAddress { get; }

    private static string BuildDisplayAddress(Uri address)
    {
        var displayAddress = $"{address.Host}{address.AbsolutePath}";

        return string.IsNullOrEmpty(address.Query) && string.IsNullOrEmpty(address.Fragment)
            ? displayAddress
            : $"{displayAddress} …";
    }
}
