using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.ViewModels;

internal sealed record MainWindowProjectEnvironmentSaveSource(
    string ProjectName,
    string WaterDensity,
    string Depth,
    string CurrentSpeed,
    bool UseCurrentProfile,
    string WaveHeight,
    string WavePeriod,
    string? SelectedSeabedPresetId);

internal sealed record MainWindowProjectBuoySaveSource(
    string Name,
    string? SelectedPresetId,
    string Volume,
    string Weight,
    string Area,
    string DragCoefficient);

internal sealed record MainWindowProjectAnchorSaveSource(
    string? SelectedPresetId,
    string Name,
    string Type,
    string Material,
    string Weight,
    string Volume,
    string BaseHoldingCoefficient);

internal sealed record MainWindowProjectSaveSource(
    MainWindowProjectEnvironmentSaveSource Environment,
    MainWindowProjectBuoySaveSource Buoy,
    MainWindowProjectAnchorSaveSource Anchor,
    string SafetyFactor,
    IReadOnlyList<CurrentProfilePointViewModel> CurrentProfilePoints,
    IReadOnlyList<AssemblyItemViewModel> AssemblyItems);

internal sealed record MainWindowProjectEnvironmentRestoreModel(
    string ProjectName,
    string WaterDensity,
    string Depth,
    string CurrentSpeed,
    bool UseCurrentProfile,
    string WaveHeight,
    string WavePeriod,
    string SelectedSeabedPresetId);

internal sealed record MainWindowProjectBuoyRestoreModel(
    string Name,
    string SelectedPresetId);

internal sealed record MainWindowProjectAnchorRestoreModel(
    string SelectedPresetId,
    string Name,
    string Type,
    string Material,
    string Weight,
    string Volume,
    string BaseHoldingCoefficient);

internal sealed record MainWindowProjectRestoreModel(
    MainWindowProjectEnvironmentRestoreModel Environment,
    MainWindowProjectBuoyRestoreModel Buoy,
    MainWindowProjectAnchorRestoreModel Anchor,
    string SafetyFactor,
    IReadOnlyList<CurrentProfilePointDto> CurrentProfilePoints,
    IReadOnlyList<AssemblyItemDto> AssemblyItems);

internal static class MainWindowProjectDtoMapper
{
    internal static BuoyProjectDto ToDto(MainWindowProjectSaveSource source)
    {
        return new BuoyProjectDto
        {
            ProjectName = source.Environment.ProjectName,
            WaterDensity = source.Environment.WaterDensity,
            Depth = source.Environment.Depth,
            CurrentSpeed = source.Environment.CurrentSpeed,
            UseCurrentProfile = source.Environment.UseCurrentProfile ? "true" : "false",
            WaveHeight = source.Environment.WaveHeight,
            WavePeriod = source.Environment.WavePeriod,
            SelectedSeabedPresetId = source.Environment.SelectedSeabedPresetId ?? "unknown",
            BuoyName = source.Buoy.Name,
            SelectedBuoyPresetId = source.Buoy.SelectedPresetId ?? string.Empty,
            BuoyVolume = source.Buoy.Volume,
            BuoyWeight = source.Buoy.Weight,
            BuoyArea = source.Buoy.Area,
            BuoyCd = source.Buoy.DragCoefficient,
            SelectedAnchorPresetId = source.Anchor.SelectedPresetId ?? string.Empty,
            AnchorName = source.Anchor.Name,
            AnchorType = source.Anchor.Type,
            AnchorMaterial = source.Anchor.Material,
            AnchorWeight = source.Anchor.Weight,
            AnchorVolume = source.Anchor.Volume,
            AnchorCoefficient = source.Anchor.BaseHoldingCoefficient,
            SafetyFactor = source.SafetyFactor,
            CurrentProfilePoints = source.CurrentProfilePoints
                .Select(x => x.ToDto())
                .ToList(),
            AssemblyItems = source.AssemblyItems
                .Select(x => new AssemblyItemDto
                {
                    IsEnabled = x.IsEnabled,
                    Kind = x.Kind,
                    Title = x.Title,
                    RopePresetId = x.RopePresetStorageId,
                    ConnectorPresetId = x.ConnectorPresetStorageId,
                    PayloadPresetId = x.PayloadPresetStorageId,
                    LengthM = x.LengthM,
                    Count = x.IsConnector ? "1" : x.Count,
                    PayloadWeightAirKg = x.PayloadWeightAirKg,
                    PayloadVolumeM3 = x.PayloadVolumeM3,
                    PayloadProjectedAreaM2 = x.PayloadProjectedAreaM2,
                    PayloadDragCoefficient = x.PayloadDragCoefficient
                })
                .ToList()
        };
    }

    internal static MainWindowProjectRestoreModel FromDto(BuoyProjectDto dto)
    {
        return new MainWindowProjectRestoreModel(
            new MainWindowProjectEnvironmentRestoreModel(
                dto.ProjectName,
                dto.WaterDensity,
                dto.Depth,
                dto.CurrentSpeed,
                string.Equals(dto.UseCurrentProfile, "true", StringComparison.OrdinalIgnoreCase),
                dto.WaveHeight,
                dto.WavePeriod,
                dto.SelectedSeabedPresetId),
            new MainWindowProjectBuoyRestoreModel(
                string.IsNullOrWhiteSpace(dto.BuoyName) ? "Буй" : dto.BuoyName,
                dto.SelectedBuoyPresetId),
            new MainWindowProjectAnchorRestoreModel(
                dto.SelectedAnchorPresetId,
                dto.AnchorName,
                dto.AnchorType,
                dto.AnchorMaterial,
                dto.AnchorWeight,
                dto.AnchorVolume,
                dto.AnchorCoefficient),
            dto.SafetyFactor,
            dto.CurrentProfilePoints,
            dto.AssemblyItems);
    }
}
