using System.Collections.Generic;

namespace BuoyCalc.Windows.Models;

public sealed class BuoyProjectDto
{
    public string ProjectName { get; set; } = string.Empty;

    public string WaterDensity { get; set; } = string.Empty;
    public string Depth { get; set; } = string.Empty;
    public string CurrentSpeed { get; set; } = string.Empty;
    public string WaveHeight { get; set; } = string.Empty;
    public string WavePeriod { get; set; } = string.Empty;

    public string BuoyName { get; set; } = string.Empty;
    public string SelectedBuoyPresetId { get; set; } = string.Empty;
    public string BuoyVolume { get; set; } = string.Empty;
    public string BuoyWeight { get; set; } = string.Empty;
    public string BuoyArea { get; set; } = string.Empty;
    public string BuoyCd { get; set; } = string.Empty;

    public string AnchorWeight { get; set; } = string.Empty;
    public string AnchorVolume { get; set; } = string.Empty;
    public string AnchorCoefficient { get; set; } = string.Empty;
    public string SafetyFactor { get; set; } = string.Empty;

    public List<AssemblyItemDto> AssemblyItems { get; set; } = new();
}

public sealed class AssemblyItemDto
{
    public bool IsEnabled { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string RopePresetId { get; set; } = string.Empty;
    public string ConnectorPresetId { get; set; } = string.Empty;
    public string LengthM { get; set; } = string.Empty;
    public string Count { get; set; } = string.Empty;
    public string PayloadWeightAirKg { get; set; } = string.Empty;
    public string PayloadVolumeM3 { get; set; } = string.Empty;
    public string PayloadProjectedAreaM2 { get; set; } = string.Empty;
    public string PayloadDragCoefficient { get; set; } = string.Empty;
}
