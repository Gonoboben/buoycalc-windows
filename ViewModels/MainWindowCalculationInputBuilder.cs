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
            .Select(x => SanitizeCurrentProfilePoint(x.ToInput()))
            .OrderBy(x => x.DepthM)
            .ToList();
        var profileMaxHorizontalSpeedMS = currentProfile.Count == 0
            ? 0
            : currentProfile.Max(x => x.HorizontalSpeedMS);

        // CurrentSpeed and UseCurrentProfile remain on the UI source only for legacy project
        // compatibility during the v1 migration. The EnvironmentInput scalar slot receives only
        // a profile-derived compatibility summary; it is never an independent calculation input.
        var environment = new EnvironmentInput(
            Parse(source.Environment.WaterDensity),
            Parse(source.Environment.Depth),
            profileMaxHorizontalSpeedMS,
            Parse(source.Environment.WaveHeight),
            Parse(source.Environment.WavePeriod),
            source.Environment.SelectedSeabedPreset ?? SeabedCatalog.ById("unknown"),
            true,
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
            .Select(x => SanitizeAssemblyItem(x.ToInput()))
            .ToList();

        return new MainWindowCalculationInput(
            environment,
            buoy,
            anchor,
            assemblyItems,
            Parse(source.SafetyFactor));
    }

    private static CurrentProfilePointInput SanitizeCurrentProfilePoint(CurrentProfilePointInput point)
    {
        return point with
        {
            DepthM = FiniteOrZero(point.DepthM),
            EastCurrentMS = FiniteOrZero(point.EastCurrentMS),
            NorthCurrentMS = FiniteOrZero(point.NorthCurrentMS),
            VerticalCurrentMS = FiniteOrZero(point.VerticalCurrentMS),
            WaterDensityKgM3 = FiniteOrZero(point.WaterDensityKgM3)
        };
    }

    private static AssemblyItemInput SanitizeAssemblyItem(AssemblyItemInput item)
    {
        return item with
        {
            RopePreset = item.RopePreset is null ? null : SanitizeRopePreset(item.RopePreset),
            ConnectorPreset = item.ConnectorPreset is null ? null : SanitizeConnectorPreset(item.ConnectorPreset),
            LengthM = FiniteOrZero(item.LengthM),
            PayloadWeightAirKg = FiniteOrZero(item.PayloadWeightAirKg),
            PayloadVolumeM3 = FiniteOrZero(item.PayloadVolumeM3),
            PayloadProjectedAreaM2 = FiniteOrZero(item.PayloadProjectedAreaM2),
            PayloadDragCoefficient = FiniteOrZero(item.PayloadDragCoefficient)
        };
    }

    private static RopePreset SanitizeRopePreset(RopePreset preset)
    {
        return preset with
        {
            DiameterMm = FiniteOrZero(preset.DiameterMm),
            BreakingLoadKn = FiniteOrZero(preset.BreakingLoadKn),
            WeightWaterKgM = FiniteOrZero(preset.WeightWaterKgM),
            DragCoefficient = FiniteOrZero(preset.DragCoefficient)
        };
    }

    private static ConnectorPreset SanitizeConnectorPreset(ConnectorPreset preset)
    {
        return preset with
        {
            WeightAirKg = FiniteOrZero(preset.WeightAirKg),
            VolumeM3 = FiniteOrZero(preset.VolumeM3),
            BreakingLoadKn = FiniteOrZero(preset.BreakingLoadKn),
            ProjectedAreaM2 = FiniteOrZero(preset.ProjectedAreaM2),
            DragCoefficient = FiniteOrZero(preset.DragCoefficient)
        };
    }

    private static double Parse(string value)
    {
        value = (value ?? string.Empty).Replace(',', '.');
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? FiniteOrZero(result)
            : 0;
    }

    private static double FiniteOrZero(double value)
    {
        return double.IsFinite(value) ? value : 0;
    }
}
