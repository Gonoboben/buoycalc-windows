using System.Collections.Generic;

namespace BuoyCalc.Windows.Models;

public sealed class BuoyProjectDto
{
    public string ProjectName { get; set; } = string.Empty;

    public string WaterDensity { get; set; } = string.Empty;
    public string Depth { get; set; } = string.Empty;
    public string CurrentSpeed { get; set; } = string.Empty;
    public string UseCurrentProfile { get; set; } = string.Empty;
    public string PlanarXAxisAzimuthDeg { get; set; } = string.Empty;
    public string WaveHeight { get; set; } = string.Empty;
    public string WavePeriod { get; set; } = string.Empty;
    public string SelectedSeabedPresetId { get; set; } = string.Empty;

    public string BuoyName { get; set; } = string.Empty;
    public string SelectedBuoyPresetId { get; set; } = string.Empty;
    public string BuoyVolume { get; set; } = string.Empty;
    public string BuoyWeight { get; set; } = string.Empty;
    public string BuoyArea { get; set; } = string.Empty;
    public string BuoyCd { get; set; } = string.Empty;

    public string SelectedAnchorPresetId { get; set; } = string.Empty;
    public string AnchorName { get; set; } = string.Empty;
    public string AnchorType { get; set; } = string.Empty;
    public string AnchorMaterial { get; set; } = string.Empty;
    public string AnchorWeight { get; set; } = string.Empty;
    public string AnchorVolume { get; set; } = string.Empty;
    public string AnchorCoefficient { get; set; } = string.Empty;
    public string SafetyFactor { get; set; } = string.Empty;

    public List<AssemblyItemDto> AssemblyItems { get; set; } = new();
    public List<CurrentProfilePointDto> CurrentProfilePoints { get; set; } = new();
}

public sealed class AssemblyItemDto
{
    public bool IsEnabled { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string RopePresetId { get; set; } = string.Empty;
    public string ConnectorPresetId { get; set; } = string.Empty;
    public string PayloadPresetId { get; set; } = string.Empty;
    public string LengthM { get; set; } = string.Empty;
    public string Count { get; set; } = string.Empty;
    public string PayloadWeightAirKg { get; set; } = string.Empty;
    public string PayloadVolumeM3 { get; set; } = string.Empty;
    public string PayloadProjectedAreaM2 { get; set; } = string.Empty;
    public string PayloadDragCoefficient { get; set; } = string.Empty;
    public ResolvedRopePresetDto? ResolvedRopePreset { get; set; }
    public ResolvedConnectorPresetDto? ResolvedConnectorPreset { get; set; }
}

/// <summary>
/// Resolved rope engineering input retained by the project. The library ID remains
/// a source hint; these values are the replay authority after the project is saved.
/// Presentation-only library notes are deliberately excluded.
/// </summary>
public sealed class ResolvedRopePresetDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public double DiameterMm { get; set; }
    public double BreakingLoadKn { get; set; }
    public double WeightWaterKgM { get; set; }
    public double DragCoefficient { get; set; }
}

/// <summary>
/// Resolved connector engineering input retained by the project. The library ID
/// remains a source hint; these values are the replay authority after save.
/// </summary>
public sealed class ResolvedConnectorPresetDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double WeightAirKg { get; set; }
    public double VolumeM3 { get; set; }
    public double BreakingLoadKn { get; set; }
    public double ProjectedAreaM2 { get; set; }
    public double DragCoefficient { get; set; }
}

public sealed class CurrentProfilePointDto
{
    public string DepthM { get; set; } = string.Empty;
    public string EastCurrentMS { get; set; } = string.Empty;
    public string NorthCurrentMS { get; set; } = string.Empty;
    public string VerticalCurrentMS { get; set; } = string.Empty;
    public string WaterDensityKgM3 { get; set; } = string.Empty;
}
