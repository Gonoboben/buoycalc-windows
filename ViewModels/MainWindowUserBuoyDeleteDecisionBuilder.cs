using System;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.ViewModels;

internal sealed record MainWindowUserBuoyDeleteDecision(
    bool CanDelete,
    string BlockedStatusText,
    string SelectedId,
    string CapturedName);

internal static class MainWindowUserBuoyDeleteDecisionBuilder
{
    internal static MainWindowUserBuoyDeleteDecision Build(BuoyLibraryItem? selectedPreset)
    {
        if (selectedPreset is null)
        {
            return new MainWindowUserBuoyDeleteDecision(
                false,
                "Выберите пользовательский буй для удаления.",
                string.Empty,
                string.Empty);
        }

        if (selectedPreset.Source != "User"
            || selectedPreset.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            return new MainWindowUserBuoyDeleteDecision(
                false,
                "Встроенный буй удалить нельзя. Удалять можно только пользовательские буи.",
                string.Empty,
                string.Empty);
        }

        return new MainWindowUserBuoyDeleteDecision(
            true,
            string.Empty,
            selectedPreset.Id,
            selectedPreset.Name);
    }
}
