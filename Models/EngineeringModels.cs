using System;
using System.Collections.Generic;
using System.Linq;

namespace BuoyCalc.Windows.Models;

public record RopePreset(string Id, string Name, string Material, double DiameterMm, double BreakingLoadKn, double WeightWaterKgM, double DragCoefficient, string Note);
public record ConnectorPreset(string Id, string Name, string Type, double WeightAirKg, double VolumeM3, double BreakingLoadKn, double ProjectedAreaM2, double DragCoefficient, string Note);
public record AnchorPreset(string Id, string Name, string Type, string Material, double WeightAirKg, double VolumeM3, double BaseHoldingCoefficient, string Note);
public record SeabedPreset(string Id, string Name, double HoldingMultiplier, string Note)
{
    public string DisplayName => Name;
}

public enum AssemblyItemKind
{
    Connector,
    Line,
    Payload
}

public record AssemblyItemInput(
    AssemblyItemKind Kind,
    string Title,
    bool IsEnabled,
    RopePreset? RopePreset,
    ConnectorPreset? ConnectorPreset,
    double LengthM,
    int Count,
    double PayloadWeightAirKg,
    double PayloadVolumeM3,
    double PayloadProjectedAreaM2,
    double PayloadDragCoefficient);

public record EnvironmentInput(
    double WaterDensityKgM3,
    double DepthM,
    double CurrentSpeedMS,
    double WaveHeightM,
    double WavePeriodS,
    SeabedPreset Seabed);

public record BuoyInput(
    string Name,
    double VolumeM3,
    double WeightKg,
    double ProjectedAreaM2,
    double DragCoefficient);

public record AnchorInput(
    string Name,
    string Type,
    string Material,
    double WeightAirKg,
    double VolumeM3,
    double BaseHoldingCoefficient);

public record CalculationResult(
    string Verdict,
    string MainRisk,
    double BuoyancyKg,
    double TotalWeightWaterKg,
    double NetBuoyancyKg,
    double HorizontalForceN,
    double TensionKn,
    double WorkingLoadKn,
    double TensionReserve,
    double AnchorHoldingKg,
    double AnchorReserve,
    double LineLengthM,
    double EstimatedOffsetM,
    IReadOnlyList<string> Checks);

public static class BuoyCalculator
{
    public static CalculationResult Calculate(
        EnvironmentInput environment,
        BuoyInput buoy,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        AnchorInput anchor,
        double safetyFactor)
    {
        var enabledItems = assemblyItems.Where(x => x.IsEnabled).ToList();
        var lineItems = enabledItems.Where(x => x.Kind == AssemblyItemKind.Line && x.RopePreset is not null).ToList();
        var connectorItems = enabledItems.Where(x => x.Kind == AssemblyItemKind.Connector && x.ConnectorPreset is not null).ToList();
        var payloadItems = enabledItems.Where(x => x.Kind == AssemblyItemKind.Payload).ToList();

        var lineLength = lineItems.Sum(x => Math.Max(0, x.LengthM));
        var lineWeightWater = lineItems.Sum(x => Math.Max(0, x.LengthM) * x.RopePreset!.WeightWaterKgM);
        var lineArea = lineItems.Sum(x => Math.Max(0, x.LengthM) * x.RopePreset!.DiameterMm / 1000.0);
        var lineMinMbl = lineItems.Count > 0 ? lineItems.Min(x => x.RopePreset!.BreakingLoadKn) : double.PositiveInfinity;

        var connectorWeightWater = connectorItems.Sum(x => Math.Max(0, x.Count) * WeightInWaterKg(x.ConnectorPreset!.WeightAirKg, x.ConnectorPreset.VolumeM3, environment.WaterDensityKgM3));
        var connectorArea = connectorItems.Sum(x => Math.Max(0, x.Count) * x.ConnectorPreset!.ProjectedAreaM2);
        var connectorMinMbl = connectorItems.Count > 0 ? connectorItems.Min(x => x.ConnectorPreset!.BreakingLoadKn) : double.PositiveInfinity;

        var payloadWeightWater = payloadItems.Sum(x => WeightInWaterKg(x.PayloadWeightAirKg, x.PayloadVolumeM3, environment.WaterDensityKgM3));
        var payloadArea = payloadItems.Sum(x => x.PayloadProjectedAreaM2);
        var payloadCdArea = payloadItems.Sum(x => x.PayloadProjectedAreaM2 * x.PayloadDragCoefficient);
        var payloadAverageCd = payloadArea > 0 ? payloadCdArea / payloadArea : 1.0;

        var buoyancyKg = buoy.VolumeM3 * environment.WaterDensityKgM3;
        var buoyWeightWater = WeightInWaterKg(buoy.WeightKg, 0, environment.WaterDensityKgM3);
        var totalWeightWater = buoyWeightWater + lineWeightWater + connectorWeightWater + payloadWeightWater;
        var netBuoyancyKg = buoyancyKg - totalWeightWater;

        var currentForce = DragForce(environment.WaterDensityKgM3, environment.CurrentSpeedMS, buoy.ProjectedAreaM2, buoy.DragCoefficient)
            + DragForce(environment.WaterDensityKgM3, environment.CurrentSpeedMS, lineArea, 1.2)
            + DragForce(environment.WaterDensityKgM3, environment.CurrentSpeedMS, connectorArea, 1.2)
            + DragForce(environment.WaterDensityKgM3, environment.CurrentSpeedMS, payloadArea, payloadAverageCd);

        var waveVelocity = environment.WavePeriodS > 0 ? Math.PI * environment.WaveHeightM / environment.WavePeriodS : 0;
        var waveForce = DragForce(environment.WaterDensityKgM3, waveVelocity, buoy.ProjectedAreaM2, buoy.DragCoefficient);
        var horizontalForce = currentForce + waveForce;

        var verticalForceN = Math.Max(0, netBuoyancyKg) * 9.80665;
        var tensionN = Math.Sqrt(horizontalForce * horizontalForce + verticalForceN * verticalForceN);
        var tensionKn = tensionN / 1000.0;

        var weakLinkKn = Math.Min(lineMinMbl, connectorMinMbl);
        if (double.IsInfinity(weakLinkKn))
        {
            weakLinkKn = 0;
        }

        var workingLoad = safetyFactor > 0 ? weakLinkKn / safetyFactor : 0;
        var tensionReserve = tensionKn > 0 ? workingLoad / tensionKn : 0;

        var anchorWeightWater = WeightInWaterKg(anchor.WeightAirKg, anchor.VolumeM3, environment.WaterDensityKgM3);
        var anchorHoldingKg = anchorWeightWater * anchor.BaseHoldingCoefficient * environment.Seabed.HoldingMultiplier;
        var requiredHoldingKg = horizontalForce / 9.80665;
        var anchorReserve = requiredHoldingKg > 0 ? anchorHoldingKg / requiredHoldingKg : 0;

        var estimatedOffset = verticalForceN > 0 ? horizontalForce / verticalForceN * environment.DepthM : 0;

        var checks = new List<string>
        {
            netBuoyancyKg > 0 ? "OK: положительная плавучесть" : "FAILED: отрицательная плавучесть",
            lineLength >= environment.DepthM ? "OK: длина линии не меньше глубины" : "FAILED: линия короче глубины",
            tensionReserve >= 1 ? "OK: запас по натяжению" : "WARNING: малый запас по натяжению",
            anchorReserve >= 1 ? "OK: запас якоря" : "WARNING: малый запас якоря"
        };

        var verdict = checks.Any(x => x.StartsWith("FAILED")) ? "Не подходит" : checks.Any(x => x.StartsWith("WARNING")) ? "Требуется проверка" : "Подходит";
        var mainRisk = checks.FirstOrDefault(x => x.StartsWith("FAILED")) ?? checks.FirstOrDefault(x => x.StartsWith("WARNING")) ?? "Критичных рисков не найдено";

        return new CalculationResult(verdict, mainRisk, buoyancyKg, totalWeightWater, netBuoyancyKg, horizontalForce, tensionKn, workingLoad, tensionReserve, anchorHoldingKg, anchorReserve, lineLength, estimatedOffset, checks);
    }

    private static double WeightInWaterKg(double weightAirKg, double volumeM3, double waterDensityKgM3)
    {
        return weightAirKg - volumeM3 * waterDensityKgM3;
    }

    private static double DragForce(double rho, double velocity, double area, double cd)
    {
        return 0.5 * rho * velocity * velocity * area * cd;
    }
}
