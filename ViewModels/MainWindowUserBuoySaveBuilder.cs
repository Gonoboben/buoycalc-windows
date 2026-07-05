using System.Globalization;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.ViewModels;

internal sealed record MainWindowUserBuoySaveSource(
    string? BuoyName,
    BuoyLibraryItem? SelectedBuoyPreset,
    string? BuoyVolume,
    string? BuoyWeight,
    string? BuoyArea,
    string? BuoyCd);

internal sealed record MainWindowUserBuoySaveRequest(
    string NormalizedName,
    BuoyLibraryItem Buoy);

internal static class MainWindowUserBuoySaveBuilder
{
    internal static MainWindowUserBuoySaveRequest Build(MainWindowUserBuoySaveSource source)
    {
        var normalizedName = string.IsNullOrWhiteSpace(source.BuoyName)
            ? "Пользовательский буй"
            : source.BuoyName.Trim();
        var selectedUserId = source.SelectedBuoyPreset is { Source: "User" }
            ? source.SelectedBuoyPreset.Id
            : string.Empty;

        var buoy = new BuoyLibraryItem
        {
            Id = selectedUserId,
            Source = "User",
            Name = normalizedName,
            VolumeM3 = Parse(source.BuoyVolume),
            WeightKg = Parse(source.BuoyWeight),
            ProjectedAreaM2 = Parse(source.BuoyArea),
            DragCoefficient = Parse(source.BuoyCd),
            Note = "Сохранено пользователем из формы буя."
        };

        return new MainWindowUserBuoySaveRequest(normalizedName, buoy);
    }

    private static double Parse(string? value)
    {
        value = (value ?? string.Empty).Replace(',', '.');
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
    }
}
