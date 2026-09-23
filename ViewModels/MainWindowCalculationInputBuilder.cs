using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.ApplicationModel;
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
        var profileMaxHorizontalSpeedMS = currentProfile.Count == 0
            ? 0
            : currentProfile.Max(x => x.HorizontalSpeedMS);

        // CurrentSpeed and UseCurrentProfile remain on the UI source only for legacy project
        // compatibility during the v1 migration. The EnvironmentInput scalar slot receives only
        // a profile-derived compatibility summary; it is never an independent calculation input.
        var environment = new EnvironmentInput(
            Parse("Environment.WaterDensityKgM3", source.Environment.WaterDensity),
            Parse("Environment.DepthM", source.Environment.Depth),
            profileMaxHorizontalSpeedMS,
            Parse("Environment.WaveHeightM", source.Environment.WaveHeight),
            Parse("Environment.WavePeriodS", source.Environment.WavePeriod),
            source.Environment.SelectedSeabedPreset ?? SeabedCatalog.ById("unknown"),
            true,
            currentProfile);

        var buoy = new BuoyInput(
            source.Buoy.Name,
            Parse("Buoy.VolumeM3", source.Buoy.Volume),
            Parse("Buoy.WeightKg", source.Buoy.Weight),
            Parse("Buoy.ProjectedAreaM2", source.Buoy.Area),
            Parse("Buoy.DragCoefficient", source.Buoy.DragCoefficient));

        var anchor = new AnchorInput(
            source.Anchor.Name,
            source.Anchor.Type,
            source.Anchor.Material,
            Parse("Anchor.WeightAirKg", source.Anchor.Weight),
            Parse("Anchor.VolumeM3", source.Anchor.Volume),
            Parse("Anchor.BaseHoldingCoefficient", source.Anchor.BaseHoldingCoefficient));

        var assemblyItems = source.AssemblyItems
            .Select(x => x.ToInput())
            .ToList();

        return new MainWindowCalculationInput(
            environment,
            buoy,
            anchor,
            assemblyItems,
            Parse("SafetyFactor", source.SafetyFactor));
    }

    private static double Parse(string field, string value) =>
        EngineeringNumberParser.ParseFinite(field, value);
}
