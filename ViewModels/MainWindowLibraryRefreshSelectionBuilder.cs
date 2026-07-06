using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.ViewModels;

internal static class MainWindowLibraryRefreshSelectionBuilder
{
    internal static BuoyLibraryItem? SelectBuoy(
        IReadOnlyList<BuoyLibraryItem> items,
        string? selectedId)
    {
        return items.FirstOrDefault(x => x.Id == selectedId)
            ?? items.FirstOrDefault();
    }

    internal static AnchorLibraryItem? SelectAnchor(
        IReadOnlyList<AnchorLibraryItem> items,
        string? selectedId)
    {
        return items.FirstOrDefault(x => x.Id == selectedId)
            ?? items.FirstOrDefault();
    }
}
