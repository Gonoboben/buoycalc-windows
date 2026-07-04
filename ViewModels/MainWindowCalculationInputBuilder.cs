using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.ViewModels;

internal sealed record MainWindowEnvironmentInputSource(
    string WaterDensity,
    string Depth,
    string CurrentSpeed,
    string WaveHeight,
    string WavePeriod,
    SeabedPreset? SelectedSeabedPreset,
    bool UseCurrentProfile,
    IReadOnlyList<CurrentProfilePointViewModel> CurrentProfilePoints);

internal sealed record MainWindowBuoyInputSource(
    string Name,
    string Volume,
    string Weight,
    string Area,
    string DragCoefficient);

internal sealed record MainWindowAnchorInputSource(
    string Name,
    string Type,
    string Material,
    string Weight,
    string Volume,
    string BaseHoldingCoefficient);

internal sealed record MainWindowCalculationInputSource(
    MainWindowEnvironmentInputSource Environment,
    MainWindowBuoyInputSource Buoy,
    MainWindowAnchorInputSource Anchor,
    IReadOnlyList<AssemblyItemViewModel> AssemblyItems,
    string SafetyFactor);

internal sealed record MainWindowCalculationInput(
    EnvironmentInput Environment,
    BuoyInput Buoy,
    AnchorInput Anchor,
    IReadOnlyList<AssemblyItemInput> AssemblyItems,
    double SafetyFactor);

internal static class MainWindowCalculationInputBuilder
{
    internal static MainWindowCalculationInput Build(MainWindowCalculationInputSource source)
    {
        var currentProfile = source.Environment.CurrentProfilePoints
            .Select(x => x.ToInput())
            .OrderBy(x => x.DepthM)
            .ToList();

        var environment = new EnvironmentInput(
            Parse(source.Environment.WaterDensity),
            Parse(source.Environment.Depth),
            Parse(source.Environment.CurrentSpeed),
            Parse(source.Environment.WaveHeight),
            Parse(source.Environment.WavePeriod),
            source.Environment.SelectedSeabedPreset ?? SeabedCatalog.ById("unknown"),
            source.Environment.UseCurrentProfile,
            currentProfile);

        var buoy = new BuoyInput(
            source.Buoy.Name,
            Parse(source.Buoy.Volume),
            Parse(source.Buoy.Weight),
            Parse(source.Buoy.Area),
            Parse(source.Buoy.DragCoefficient));

        var anchor = new AnchorInput(
            source.Anchor.Name,
            source.Anchor.Type,
            source.Anchor.Material,
            Parse(source.Anchor.Weight),
            Parse(source.Anchor.Volume),
            Parse(source.Anchor.BaseHoldingCoefficient));

        var assemblyItems = source.AssemblyItems
            .Select(x => x.ToInput())
            .ToList();

        return new MainWindowCalculationInput(
            environment,
            buoy,
            anchor,
            assemblyItems,
            Parse(source.SafetyFactor));
    }

    private static double Parse(string value)
    {
        value = (value ?? string.Empty).Replace(',', '.');
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }
}
