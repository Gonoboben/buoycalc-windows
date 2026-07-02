using System.Text;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Markdown renderer boundary for the full technical report.
///
/// The top-level Markdown assembly now lives here. Existing section renderers are
/// gradually moving from ReportBuilder into this builder while the generated
/// output stays byte-for-byte stable.
/// </summary>
public static class TechnicalReportMarkdownBuilder
{
    public static string Build(string projectName, EnvironmentInput environment, BuoyInput buoy, AnchorInput anchor, CalculationResult result)
    {
        var sb = new StringBuilder();
        var data = TechnicalReportDataBuilder.Build(environment, result);
        var tensionRows = data.TensionRows;
        var shape = data.Shape;
        var shapeProjection = data.ShapeProjection;
        var shapeForces = data.ShapeForces;
        var shapeTensions = data.ShapeTensions;
        var sequencePositions = data.SequencePositions;
        var discreteLoadTensions = data.DiscreteLoadTensions;
        var discreteLoadShape = data.DiscreteLoadShape;
        var alternativeDiscreteNodes = data.AlternativeDiscreteNodes;
        var iterativeSolver = data.IterativeSolver;
        var diagnostics = data.Diagnostics;
        var vectorBalance = data.VectorBalance;
        TechnicalReportStorePublisher.Publish(data);

        sb.AppendLine("# BuoyCalc Windows — предварительный отчёт");
        sb.AppendLine();
        sb.AppendLine($"Проект: {projectName}");
        sb.AppendLine($"Вердикт: {result.Verdict}");
        sb.AppendLine($"Главный риск: {result.MainRisk}");
        sb.AppendLine($"Инженерная диагностика: {diagnostics.Summary}");
        sb.AppendLine();

        AppendEnvironment(sb, environment);
        AppendBuoy(sb, buoy, shape);
        AppendAnchor(sb, anchor, result);
        TechnicalReportMarkdownSectionBridge.Append("AppendTotals", sb, result, tensionRows, shape, shapeProjection, shapeForces, shapeTensions, sequencePositions, discreteLoadTensions, discreteLoadShape, alternativeDiscreteNodes, iterativeSolver, diagnostics);
        TechnicalReportMarkdownSectionBridge.Append("AppendDiagnostics", sb, diagnostics);
        TechnicalReportMarkdownSectionBridge.Append("AppendVectorBalanceRows", sb, vectorBalance);
        TechnicalReportMarkdownSectionBridge.Append("AppendElementRows", sb, result);
        TechnicalReportMarkdownSectionBridge.Append("AppendSequencePositionRows", sb, sequencePositions);
        TechnicalReportMarkdownSectionBridge.Append("AppendModelCoverageRows", sb, result);
        TechnicalReportMarkdownSectionBridge.Append("AppendSegmentRows", sb, result);
        TechnicalReportMarkdownSectionBridge.Append("AppendTensionRows", sb, tensionRows);
        TechnicalReportMarkdownSectionBridge.Append("AppendShapeRows", sb, shape);
        TechnicalReportMarkdownSectionBridge.Append("AppendShapeProjectionRows", sb, shapeProjection);
        TechnicalReportMarkdownSectionBridge.Append("AppendShapeForceRows", sb, shapeForces);
        TechnicalReportMarkdownSectionBridge.Append("AppendShapeTensionRows", sb, shapeTensions);
        TechnicalReportMarkdownSectionBridge.Append("AppendDiscreteLoadTensionRows", sb, discreteLoadTensions);
        TechnicalReportMarkdownSectionBridge.Append("AppendDiscreteLoadShapeRows", sb, discreteLoadShape);
        TechnicalReportMarkdownSectionBridge.Append("AppendAlternativeDiscreteNodeRows", sb, alternativeDiscreteNodes);
        TechnicalReportMarkdownSectionBridge.Append("AppendIterativeSolverRows", sb, iterativeSolver);
        TechnicalReportMarkdownSectionBridge.Append("AppendChecks", sb, result);

        sb.AppendLine("## Ограничения");
        sb.AppendLine(shape.MethodNote);
        sb.AppendLine(shapeProjection.MethodNote);
        sb.AppendLine(shapeForces.MethodNote);
        sb.AppendLine(shapeTensions.MethodNote);
        sb.AppendLine(sequencePositions.MethodNote);
        sb.AppendLine(discreteLoadTensions.MethodNote);
        sb.AppendLine(discreteLoadShape.MethodNote);
        sb.AppendLine(alternativeDiscreteNodes.MethodNote);
        sb.AppendLine(iterativeSolver.MethodNote);
        sb.AppendLine(vectorBalance.MethodNote);
        sb.AppendLine("v0.39 добавляет диагностический итерационный solver-слой. Он замыкает существующие блоки в цикл, но основной solver, 2D и PDF-схемы пока не заменяются.");

        return sb.ToString();
    }

    private static void AppendEnvironment(StringBuilder sb, EnvironmentInput environment)
    {
        sb.AppendLine("## Условия");
        sb.AppendLine($"- Плотность воды базовая: {environment.WaterDensityKgM3:0.####} кг/м³");
        sb.AppendLine($"- Плотность воды расчётная: {environment.EffectiveWaterDensityKgM3:0.####} кг/м³");
        sb.AppendLine($"- Глубина: {environment.DepthM:0.####} м");
        sb.AppendLine($"- Течение базовое: {environment.CurrentSpeedMS:0.####} м/с");
        sb.AppendLine($"- Течение расчётное: {environment.EffectiveCurrentSpeedMS:0.####} м/с");
        sb.AppendLine($"- Профиль течения: {(environment.UseCurrentProfile ? "используется" : "не используется")}");
        sb.AppendLine($"- Волна: {environment.WaveHeightM:0.####} м / {environment.WavePeriodS:0.####} с");
        sb.AppendLine($"- Грунт: {environment.Seabed.Name}");
        sb.AppendLine($"- Множитель грунта: {environment.Seabed.HoldingMultiplier:0.####}");
        sb.AppendLine($"- Примечание по грунту: {environment.Seabed.Note}");
        sb.AppendLine();

        if (environment.EffectiveCurrentProfile.Count == 0)
        {
            return;
        }

        sb.AppendLine("## Профиль течения по глубине");
        sb.AppendLine("| Глубина, м | U East, м/с | V North, м/с | W Vertical, м/с | |U|, м/с | ρ, кг/м³ |");
        sb.AppendLine("|---:|---:|---:|---:|---:|---:|");
        foreach (var p in environment.EffectiveCurrentProfile)
        {
            sb.AppendLine($"| {p.DepthM:0.####} | {p.EastCurrentMS:0.####} | {p.NorthCurrentMS:0.####} | {p.VerticalCurrentMS:0.####} | {p.SpeedMS:0.####} | {p.WaterDensityKgM3:0.####} |");
        }
        sb.AppendLine();
    }

    private static void AppendBuoy(StringBuilder sb, BuoyInput buoy, MooringShapeResult shape)
    {
        sb.AppendLine("## Буй");
        sb.AppendLine($"- Название: {buoy.Name}");
        sb.AppendLine($"- Объём: {buoy.VolumeM3:0.####} м³");
        sb.AppendLine($"- Масса: {buoy.WeightKg:0.####} кг");
        sb.AppendLine($"- Площадь: {buoy.ProjectedAreaM2:0.####} м²");
        sb.AppendLine($"- Cd: {buoy.DragCoefficient:0.####}");
        sb.AppendLine($"- Состояние по форме: {DisplayBuoyState(shape.BuoyState)}");
        sb.AppendLine($"- Глубина узла буя: {shape.BuoyPoint?.ZDepthM ?? 0:0.####} м");
        sb.AppendLine();
    }

    private static void AppendAnchor(StringBuilder sb, AnchorInput anchor, CalculationResult result)
    {
        sb.AppendLine("## Якорь");
        sb.AppendLine($"- Название: {anchor.Name}");
        sb.AppendLine($"- Тип: {anchor.Type}");
        sb.AppendLine($"- Материал: {anchor.Material}");
        sb.AppendLine($"- Масса: {anchor.WeightAirKg:0.####} кг");
        sb.AppendLine($"- Объём: {anchor.VolumeM3:0.####} м³");
        sb.AppendLine($"- Вес якоря в воде: {result.AnchorWeightWaterKg:0.####} кг");
        sb.AppendLine($"- Базовый коэф. удержания якоря: {result.AnchorBaseHoldingCoefficient:0.####}");
        sb.AppendLine($"- Множитель типа якоря: {result.AnchorTypeMultiplier:0.####}");
        sb.AppendLine($"- Множитель грунта: {result.SeabedHoldingMultiplier:0.####}");
        sb.AppendLine("- Формула удержания: вес в воде × K якоря × K типа × K грунта");
        sb.AppendLine();
    }

    private static string DisplayBuoyState(BuoyShapeState state)
    {
        return state switch
        {
            BuoyShapeState.Surface => "на поверхности",
            BuoyShapeState.Submerged => "под водой",
            BuoyShapeState.Overloaded => "перегружен / отрицательная плавучесть",
            _ => "не определено"
        };
    }
}
