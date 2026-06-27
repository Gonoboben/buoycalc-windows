using System;
using System.Collections.Generic;
using System.Linq;

namespace BuoyCalc.Windows.Models;

public record RopePreset(string Id, string Name, string Material, double DiameterMm, double BreakingLoadKn, double WeightWaterKgM, double DragCoefficient, string Note);
public record ConnectorPreset(string Id, string Name, string Type, double WeightAirKg, double VolumeM3, double BreakingLoadKn, double ProjectedAreaM2, double DragCoefficient, string Note);
public record AnchorPreset(string Id, string Name, string Type, string Material, double WeightAirKg, double VolumeM3, double BaseHoldingCoefficient, string Note);
public record SeabedPreset(string Id, string Name, double HoldingMultiplier, string Note)
{
    public string DisplayName => $"{Name} · K={HoldingMultiplier:0.##}";
    public override string ToString() => DisplayName;
}

public record CurrentProfilePointInput(
    double DepthM,
    double EastCurrentMS,
    double NorthCurrentMS,
    double VerticalCurrentMS,
    double WaterDensityKgM3)
{
    public double HorizontalSpeedMS => Math.Sqrt(EastCurrentMS * EastCurrentMS + NorthCurrentMS * NorthCurrentMS);
    public double SpeedMS => Math.Sqrt(EastCurrentMS * EastCurrentMS + NorthCurrentMS * NorthCurrentMS + VerticalCurrentMS * VerticalCurrentMS);
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
    SeabedPreset Seabed,
    bool UseCurrentProfile = false,
    IReadOnlyList<CurrentProfilePointInput>? CurrentProfile = null)
{
    public IReadOnlyList<CurrentProfilePointInput> EffectiveCurrentProfile => CurrentProfile ?? Array.Empty<CurrentProfilePointInput>();

    public double EffectiveCurrentSpeedMS => UseCurrentProfile && EffectiveCurrentProfile.Count > 0
        ? EffectiveCurrentProfile.Max(x => x.HorizontalSpeedMS)
        : CurrentSpeedMS;

    public double EffectiveWaterDensityKgM3 => UseCurrentProfile && EffectiveCurrentProfile.Count > 0
        ? EffectiveCurrentProfile.Average(x => x.WaterDensityKgM3 > 0 ? x.WaterDensityKgM3 : WaterDensityKgM3)
        : WaterDensityKgM3;
}

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

public record ElementCalculationRow(
    int Number,
    string Kind,
    string Title,
    string PresetName,
    double LengthM,
    int Count,
    double WeightWaterKg,
    double ProjectedAreaM2,
    double DragCoefficient,
    double CurrentForceN,
    double BreakingLoadKn,
    double WorkingLoadKn,
    double Reserve,
    string Status);

public record CalculationResult(
    string Verdict,
    string MainRisk,
    double BuoyancyKg,
    double TotalWeightWaterKg,
    double NetBuoyancyKg,
    double CurrentForceN,
    double WaveForceN,
    double HorizontalForceN,
    double TensionKn,
    double WeakLinkBreakingLoadKn,
    string WeakLinkName,
    double WorkingLoadKn,
    double TensionReserve,
    double AnchorWeightWaterKg,
    double AnchorBaseHoldingCoefficient,
    double AnchorTypeMultiplier,
    double SeabedHoldingMultiplier,
    double AnchorHoldingKg,
    double RequiredAnchorHoldingKg,
    double AnchorReserve,
    double LineLengthM,
    double EstimatedOffsetM,
    IReadOnlyList<ElementCalculationRow> ElementRows,
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
        var currentSpeedMS = environment.EffectiveCurrentSpeedMS;
        var waterDensityKgM3 = environment.EffectiveWaterDensityKgM3;

        var lineLength = lineItems.Sum(x => Math.Max(0, x.LengthM));
        var lineWeightWater = lineItems.Sum(x => Math.Max(0, x.LengthM) * x.RopePreset!.WeightWaterKgM);
        var lineCurrentForce = lineItems.Sum(x => DragForce(waterDensityKgM3, currentSpeedMS, Math.Max(0, x.LengthM) * x.RopePreset!.DiameterMm / 1000.0, x.RopePreset!.DragCoefficient));

        var connectorWeightWater = connectorItems.Sum(x => Math.Max(1, x.Count) * WeightInWaterKg(x.ConnectorPreset!.WeightAirKg, x.ConnectorPreset.VolumeM3, waterDensityKgM3));
        var connectorCurrentForce = connectorItems.Sum(x => Math.Max(1, x.Count) * DragForce(waterDensityKgM3, currentSpeedMS, x.ConnectorPreset!.ProjectedAreaM2, x.ConnectorPreset.DragCoefficient));

        var payloadWeightWater = payloadItems.Sum(x => WeightInWaterKg(x.PayloadWeightAirKg, x.PayloadVolumeM3, waterDensityKgM3));
        var payloadCurrentForce = payloadItems.Sum(x => DragForce(waterDensityKgM3, currentSpeedMS, x.PayloadProjectedAreaM2, x.PayloadDragCoefficient));

        var buoyancyKg = buoy.VolumeM3 * waterDensityKgM3;
        var buoyWeightWater = WeightInWaterKg(buoy.WeightKg, 0, waterDensityKgM3);
        var totalWeightWater = buoyWeightWater + lineWeightWater + connectorWeightWater + payloadWeightWater;
        var netBuoyancyKg = buoyancyKg - totalWeightWater;

        var buoyCurrentForce = DragForce(waterDensityKgM3, currentSpeedMS, buoy.ProjectedAreaM2, buoy.DragCoefficient);
        var currentForce = buoyCurrentForce + lineCurrentForce + connectorCurrentForce + payloadCurrentForce;

        var waveVelocity = environment.WavePeriodS > 0 ? Math.PI * environment.WaveHeightM / environment.WavePeriodS : 0;
        var waveForce = DragForce(waterDensityKgM3, waveVelocity, buoy.ProjectedAreaM2, buoy.DragCoefficient);
        var horizontalForce = currentForce + waveForce;

        var verticalForceN = Math.Max(0, netBuoyancyKg) * 9.80665;
        var tensionN = Math.Sqrt(horizontalForce * horizontalForce + verticalForceN * verticalForceN);
        var tensionKn = tensionN / 1000.0;

        var assemblyRows = BuildAssemblyRows(enabledItems, environment, safetyFactor, tensionKn);
        var structuralRows = assemblyRows.Where(x => x.BreakingLoadKn > 0).ToList();
        var weakRow = structuralRows.OrderBy(x => x.BreakingLoadKn).FirstOrDefault();
        var weakLinkKn = weakRow?.BreakingLoadKn ?? 0;
        var weakLinkName = weakRow is null ? "Не определено" : $"{weakRow.Title} / {weakRow.PresetName}";

        var workingLoad = safetyFactor > 0 && weakLinkKn > 0 ? weakLinkKn / safetyFactor : 0;
        var tensionReserve = tensionKn > 0 && workingLoad > 0 ? workingLoad / tensionKn : 0;

        var anchorWeightWater = WeightInWaterKg(anchor.WeightAirKg, anchor.VolumeM3, waterDensityKgM3);
        var anchorTypeMultiplier = AnchorTypeMultiplier(anchor.Type);
        var seabedMultiplier = environment.Seabed.HoldingMultiplier;
        var anchorHoldingKg = anchorWeightWater * anchor.BaseHoldingCoefficient * anchorTypeMultiplier * seabedMultiplier;
        var requiredHoldingKg = horizontalForce / 9.80665;
        var anchorReserve = requiredHoldingKg > 0 ? anchorHoldingKg / requiredHoldingKg : 0;

        var elementRows = BuildSystemRows(
            buoy,
            anchor,
            environment,
            assemblyRows,
            buoyancyKg,
            buoyCurrentForce,
            anchorWeightWater,
            anchorTypeMultiplier,
            seabedMultiplier,
            anchorReserve);

        var estimatedOffset = verticalForceN > 0 ? horizontalForce / verticalForceN * environment.DepthM : 0;

        var checks = new List<string>
        {
            netBuoyancyKg > 0 ? "OK: положительная плавучесть" : "FAILED: отрицательная плавучесть",
            lineLength >= environment.DepthM ? "OK: длина линии не меньше глубины" : "FAILED: линия короче глубины",
            structuralRows.Count > 0 ? "OK: найдено слабое звено цепочки" : "WARNING: нет элементов с MBL для проверки слабого звена",
            tensionReserve >= 1 ? "OK: запас по слабому звену" : "WARNING: малый запас по слабому звену",
            anchorReserve >= 1 ? "OK: запас якоря" : "WARNING: малый запас якоря",
            environment.Seabed.Id == "unknown" ? "WARNING: грунт не задан точно" : $"OK: грунт учтён: {environment.Seabed.Name}",
            environment.UseCurrentProfile ? $"INFO: используется профиль течения, расчётная скорость пока принята как максимум |Uгор| = {currentSpeedMS:0.####} м/с" : $"INFO: используется одно значение скорости течения = {currentSpeedMS:0.####} м/с",
            $"INFO: удержание якоря = вес в воде × K якоря × K типа × K грунта = {anchorWeightWater:0.####} × {anchor.BaseHoldingCoefficient:0.####} × {anchorTypeMultiplier:0.####} × {seabedMultiplier:0.####}"
        };

        if (assemblyRows.Any(x => x.Status.StartsWith("WARNING")))
        {
            checks.Add("WARNING: один или несколько элементов имеют малый индивидуальный запас");
        }

        if (environment.Seabed.Id == "rock" && IsDeadweightAnchor(anchor.Type))
        {
            checks.Add("WARNING: каменистый грунт может ухудшать работу грузового якоря");
        }

        var verdict = checks.Any(x => x.StartsWith("FAILED")) ? "Не подходит" : checks.Any(x => x.StartsWith("WARNING")) ? "Требуется проверка" : "Подходит";
        var mainRisk = checks.FirstOrDefault(x => x.StartsWith("FAILED")) ?? checks.FirstOrDefault(x => x.StartsWith("WARNING")) ?? "Критичных рисков не найдено";

        return new CalculationResult(
            verdict,
            mainRisk,
            buoyancyKg,
            totalWeightWater,
            netBuoyancyKg,
            currentForce,
            waveForce,
            horizontalForce,
            tensionKn,
            weakLinkKn,
            weakLinkName,
            workingLoad,
            tensionReserve,
            anchorWeightWater,
            anchor.BaseHoldingCoefficient,
            anchorTypeMultiplier,
            seabedMultiplier,
            anchorHoldingKg,
            requiredHoldingKg,
            anchorReserve,
            lineLength,
            estimatedOffset,
            elementRows,
            checks);
    }

    private static IReadOnlyList<ElementCalculationRow> BuildAssemblyRows(IReadOnlyList<AssemblyItemInput> enabledItems, EnvironmentInput environment, double safetyFactor, double tensionKn)
    {
        var rows = new List<ElementCalculationRow>();
        var number = 1;
        var currentSpeedMS = environment.EffectiveCurrentSpeedMS;
        var waterDensityKgM3 = environment.EffectiveWaterDensityKgM3;

        foreach (var item in enabledItems)
        {
            var count = item.Kind == AssemblyItemKind.Connector ? Math.Max(1, item.Count) : Math.Max(0, item.Count);
            var kind = DisplayKind(item.Kind);
            var presetName = "Ручной ввод";
            var lengthM = 0.0;
            var weightWaterKg = 0.0;
            var areaM2 = 0.0;
            var cd = 1.0;
            var breakingLoadKn = 0.0;

            if (item.Kind == AssemblyItemKind.Line && item.RopePreset is not null)
            {
                presetName = item.RopePreset.Name;
                lengthM = Math.Max(0, item.LengthM);
                weightWaterKg = lengthM * item.RopePreset.WeightWaterKgM;
                areaM2 = lengthM * item.RopePreset.DiameterMm / 1000.0;
                cd = item.RopePreset.DragCoefficient;
                breakingLoadKn = item.RopePreset.BreakingLoadKn;
                count = 1;
            }
            else if (item.Kind == AssemblyItemKind.Connector && item.ConnectorPreset is not null)
            {
                presetName = item.ConnectorPreset.Name;
                weightWaterKg = count * WeightInWaterKg(item.ConnectorPreset.WeightAirKg, item.ConnectorPreset.VolumeM3, waterDensityKgM3);
                areaM2 = count * item.ConnectorPreset.ProjectedAreaM2;
                cd = item.ConnectorPreset.DragCoefficient;
                breakingLoadKn = item.ConnectorPreset.BreakingLoadKn;
            }
            else if (item.Kind == AssemblyItemKind.Payload)
            {
                presetName = item.Title;
                weightWaterKg = WeightInWaterKg(item.PayloadWeightAirKg, item.PayloadVolumeM3, waterDensityKgM3);
                areaM2 = item.PayloadProjectedAreaM2;
                cd = item.PayloadDragCoefficient;
                count = 1;
            }

            var currentForceN = DragForce(waterDensityKgM3, currentSpeedMS, areaM2, cd);
            var workingLoadKn = safetyFactor > 0 && breakingLoadKn > 0 ? breakingLoadKn / safetyFactor : 0;
            var reserve = tensionKn > 0 && workingLoadKn > 0 ? workingLoadKn / tensionKn : 0;
            var status = breakingLoadKn <= 0 ? "INFO: MBL не задан" : reserve >= 1 ? "OK" : "WARNING: малый запас";

            rows.Add(new ElementCalculationRow(number++, kind, item.Title, presetName, lengthM, count, weightWaterKg, areaM2, cd, currentForceN, breakingLoadKn, workingLoadKn, reserve, status));
        }

        return rows;
    }

    private static IReadOnlyList<ElementCalculationRow> BuildSystemRows(
        BuoyInput buoy,
        AnchorInput anchor,
        EnvironmentInput environment,
        IReadOnlyList<ElementCalculationRow> assemblyRows,
        double buoyancyKg,
        double buoyCurrentForceN,
        double anchorWeightWaterKg,
        double anchorTypeMultiplier,
        double seabedMultiplier,
        double anchorReserve)
    {
        var rows = new List<ElementCalculationRow>();
        var number = 1;

        rows.Add(new ElementCalculationRow(
            number++,
            "Буй",
            buoy.Name,
            "Выбранный буй",
            0,
            1,
            buoy.WeightKg - buoyancyKg,
            buoy.ProjectedAreaM2,
            buoy.DragCoefficient,
            buoyCurrentForceN,
            0,
            0,
            0,
            "INFO: источник плавучести"));

        foreach (var row in assemblyRows)
        {
            rows.Add(row with { Number = number++ });
        }

        rows.Add(new ElementCalculationRow(
            number,
            "Якорь",
            anchor.Name,
            $"{anchor.Type}; грунт: {environment.Seabed.Name}; Kтип={anchorTypeMultiplier:0.####}; Kгр={seabedMultiplier:0.####}",
            0,
            1,
            anchorWeightWaterKg,
            0,
            0,
            0,
            0,
            0,
            anchorReserve,
            anchorReserve >= 1 ? "OK: запас якоря" : "WARNING: малый запас якоря"));

        return rows;
    }

    private static string DisplayKind(AssemblyItemKind kind)
    {
        return kind switch
        {
            AssemblyItemKind.Connector => "Соединитель",
            AssemblyItemKind.Payload => "Прибор",
            _ => "Линия"
        };
    }

    private static double AnchorTypeMultiplier(string type)
    {
        var value = (type ?? string.Empty).ToLowerInvariant();

        if (value.Contains("plate") || value.Contains("плит")) return 1.25;
        if (value.Contains("mushroom") || value.Contains("гриб")) return 1.15;
        if (value.Contains("deadweight") || value.Contains("груз") || value.Contains("concrete") || value.Contains("бетон")) return 1.0;

        return 1.0;
    }

    private static bool IsDeadweightAnchor(string type)
    {
        var value = (type ?? string.Empty).ToLowerInvariant();
        return value.Contains("deadweight") || value.Contains("груз") || value.Contains("concrete") || value.Contains("бетон");
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
