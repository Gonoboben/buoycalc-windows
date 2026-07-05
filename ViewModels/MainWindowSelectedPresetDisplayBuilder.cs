using System.Globalization;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.ViewModels;

internal sealed record MainWindowBuoyPresetDisplay(
    string Name,
    string Volume,
    string Weight,
    string ProjectedArea,
    string DragCoefficient);

internal sealed record MainWindowAnchorPresetDisplay(
    string Name,
    string Type,
    string Material,
    string Weight,
    string Volume,
    string BaseHoldingCoefficient);

internal static class MainWindowSelectedPresetDisplayBuilder
{
    internal static MainWindowBuoyPresetDisplay BuildBuoy(BuoyLibraryItem preset)
    {
        return new MainWindowBuoyPresetDisplay(
            preset.Name,
            FormatDouble(preset.VolumeM3),
            FormatDouble(preset.WeightKg),
            FormatDouble(preset.ProjectedAreaM2),
            FormatDouble(preset.DragCoefficient));
    }

    internal static MainWindowAnchorPresetDisplay BuildAnchor(AnchorLibraryItem preset)
    {
        return new MainWindowAnchorPresetDisplay(
            preset.Name,
            preset.Type,
            preset.Material,
            FormatDouble(preset.WeightAirKg),
            FormatDouble(preset.VolumeM3),
            FormatDouble(preset.BaseHoldingCoefficient));
    }

    private static string FormatDouble(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
