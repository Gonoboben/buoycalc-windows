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
    string? SelectedSeabedPresetId)
{
    public string PlanarXAxisAzimuthDeg { get; init; } = string.Empty;
}

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
    string SelectedSeabedPresetId)
{
    public string PlanarXAxisAzimuthDeg { get; init; } = string.Empty;
}

internal sealed record MainWindowProjectBuoyRestoreModel(
    string Name,
    string SelectedPresetId,
    string Volume,
    string Weight,
    string Area,
    string DragCoefficient);

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
            // Retained fields keep old JSON readers/deserializers compatible, but no scalar
            // current value is persisted as engineering authority after the profile-only switch.
            CurrentSpeed = string.Empty,
            UseCurrentProfile = "true",
            PlanarXAxisAzimuthDeg = source.Environment.PlanarXAxisAzimuthDeg,
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
                .Select(ToAssemblyItemDto)
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
                string.Empty,
                true,
                dto.WaveHeight,
                dto.WavePeriod,
                dto.SelectedSeabedPresetId)
            {
                PlanarXAxisAzimuthDeg = dto.PlanarXAxisAzimuthDeg
            },
            new MainWindowProjectBuoyRestoreModel(
                dto.BuoyName,
                dto.SelectedBuoyPresetId,
                dto.BuoyVolume,
                dto.BuoyWeight,
                dto.BuoyArea,
                dto.BuoyCd),
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

    private static AssemblyItemDto ToAssemblyItemDto(AssemblyItemViewModel item)
    {
        var input = item.ToInput();
        return new AssemblyItemDto
        {
            IsEnabled = item.IsEnabled,
            Kind = item.Kind,
            Title = item.Title,
            RopePresetId = item.RopePresetStorageId,
            ConnectorPresetId = item.ConnectorPresetStorageId,
            PayloadPresetId = item.PayloadPresetStorageId,
            LengthM = item.LengthM,
            Count = item.IsConnector ? "1" : item.Count,
            PayloadWeightAirKg = item.PayloadWeightAirKg,
            PayloadVolumeM3 = item.PayloadVolumeM3,
            PayloadProjectedAreaM2 = item.PayloadProjectedAreaM2,
            PayloadDragCoefficient = item.PayloadDragCoefficient,
            ResolvedRopePreset = input.RopePreset is null
                ? null
                : new ResolvedRopePresetDto
                {
                    Id = input.RopePreset.Id,
                    Name = input.RopePreset.Name,
                    Material = input.RopePreset.Material,
                    DiameterMm = input.RopePreset.DiameterMm,
                    BreakingLoadKn = input.RopePreset.BreakingLoadKn,
                    WeightWaterKgM = input.RopePreset.WeightWaterKgM,
                    DragCoefficient = input.RopePreset.DragCoefficient
                },
            ResolvedConnectorPreset = input.ConnectorPreset is null
                ? null
                : new ResolvedConnectorPresetDto
                {
                    Id = input.ConnectorPreset.Id,
                    Name = input.ConnectorPreset.Name,
                    Type = input.ConnectorPreset.Type,
                    WeightAirKg = input.ConnectorPreset.WeightAirKg,
                    VolumeM3 = input.ConnectorPreset.VolumeM3,
                    BreakingLoadKn = input.ConnectorPreset.BreakingLoadKn,
                    ProjectedAreaM2 = input.ConnectorPreset.ProjectedAreaM2,
                    DragCoefficient = input.ConnectorPreset.DragCoefficient
                }
        };
    }
}
