using System.Collections.Generic;
using System.Linq;
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
        AppendTotals(sb, result, tensionRows, shape, shapeProjection, shapeForces, shapeTensions, sequencePositions, discreteLoadTensions, discreteLoadShape, alternativeDiscreteNodes, iterativeSolver, diagnostics);
        AppendDiagnostics(sb, diagnostics);
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
        sb.AppendLine("Расчёт формы остаётся предварительным: итерационный solver формирует кандидатную форму с дискретными нагрузками. Только кандидат, прошедший MooringPrimaryShapeGate, становится основной выбранной формой; иначе используется fallback MooringShapeSolver. 2D читает выбранную форму, а PDF сохраняет собственный порядок источников: альтернативная форма, выбранная форма, метрики отчёта и визуализационный fallback.");

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

    private static void AppendTotals(StringBuilder sb, CalculationResult result, IReadOnlyList<SegmentTensionRow> tensionRows, MooringShapeResult shape, MooringShapeProjectionResult shapeProjection, MooringShapeForceResult shapeForces, MooringShapeTensionResult shapeTensions, MooringSequencePositionResult sequencePositions, MooringDiscreteLoadTensionResult discreteLoadTensions, MooringDiscreteLoadShapeResult discreteLoadShape, MooringAlternativeDiscreteNodeResult alternativeDiscreteNodes, MooringIterativeSolverResult iterativeSolver, EngineeringDiagnosticsResult diagnostics)
    {
        sb.AppendLine("## Итоги");
        sb.AppendLine($"- Полная плавучесть буя: {result.BuoyancyKg:0.####} кг");
        sb.AppendLine($"- Вес постановки в воде: {result.TotalWeightWaterKg:0.####} кг");
        sb.AppendLine($"- Чистая плавучесть: {result.NetBuoyancyKg:0.####} кг");
        sb.AppendLine($"- Суммарная сила течения базовой модели (буй + линия + соединители + приборы): {result.CurrentForceN:0.####} Н");
        sb.AppendLine($"- Волновая составляющая: {result.WaveForceN:0.####} Н");
        sb.AppendLine($"- Суммарная горизонтальная нагрузка базовой модели (течение + волна): {result.HorizontalForceN:0.####} Н");
        sb.AppendLine($"- Расчётная нагрузка для проверки слабого звена: {result.TensionKn:0.####} кН");
        sb.AppendLine($"- Слабое звено: {result.WeakLinkName}");
        sb.AppendLine($"- MBL слабого звена: {result.WeakLinkBreakingLoadKn:0.####} кН");
        sb.AppendLine($"- WLL слабого звена: {result.WorkingLoadKn:0.####} кН");
        sb.AppendLine($"- Запас по слабому звену: {result.TensionReserve:0.####}");
        sb.AppendLine($"- Требуемое удержание якоря: {result.RequiredAnchorHoldingKg:0.####} кг");
        sb.AppendLine($"- Удержание якоря: {result.AnchorHoldingKg:0.####} кг");
        sb.AppendLine($"- Запас якоря: {result.AnchorReserve:0.####}");
        sb.AppendLine($"- Длина линии: {result.LineLengthM:0.####} м");
        sb.AppendLine($"- Приближённый снос базовой модели (Fгор / Fверт × глубина): {result.EstimatedOffsetM:0.####} м");
        sb.AppendLine($"- Дискретных элементов с координатой s: {sequencePositions.DiscreteElementCount}; вес в воде: {sequencePositions.DiscreteWeightWaterKg:0.####} кг; Fx: {sequencePositions.DiscreteCurrentForceN:0.####} Н");
        sb.AppendLine($"- Дискретных X/Z-узлов альтернативной формы: {alternativeDiscreteNodes.DiscreteNodeCount}; max Δузла={alternativeDiscreteNodes.MaxNodeDeltaM:0.####} м");
        sb.AppendLine($"- Итерационный solver: {(iterativeSolver.Converged ? "сошёлся" : "не сошёлся")}; итераций={iterativeSolver.IterationCount}; ΔXпосл={iterativeSolver.FinalOffsetChangeM:0.####} м; max Δузла={iterativeSolver.FinalMaxNodeDeltaM:0.####} м; невязка Z={iterativeSolver.FinalGeometryResidualM:0.####} м; причина остановки: {iterativeSolver.StopReasonText}");
        sb.AppendLine($"- Диагностика: {diagnostics.Summary}");

        if (tensionRows.Count > 0)
        {
            var maxTension = tensionRows.OrderByDescending(x => x.TensionKn).First();
            sb.AppendLine($"- Макс. натяжение линии по сегментной модели: {maxTension.TensionKn:0.####} кН, сегмент №{maxTension.Number}, z≈{maxTension.EstimatedDepthM:0.####} м");
        }

        if (shape.Nodes.Count > 0)
        {
            sb.AppendLine($"- Снос формы X/Z: {shape.HorizontalOffsetM:0.####} м");
            sb.AppendLine($"- Глубина якорного узла X/Z: {shape.AnchorPoint?.ZDepthM ?? 0:0.####} м");
            sb.AppendLine($"- Невязка якорной глубины: {shape.VerticalResidualM:0.####} м");
            sb.AppendLine($"- Невязка solver формы: {shape.ConvergenceResidualM:0.####} м");
            sb.AppendLine($"- Итераций solver формы: {shape.IterationCount}");
            sb.AppendLine($"- ΣdX формы: {shapeProjection.SumDeltaXM:0.####} м");
            sb.AppendLine($"- ΣdZ формы: {shapeProjection.SumDeltaZM:0.####} м");
            sb.AppendLine($"- Статус проекций формы: {(shapeProjection.GeometryClosed ? "OK" : "WARNING")}");
            sb.AppendLine($"- Shape-based сила линии: {shapeForces.ShapeLineForceN:0.####} Н");
            sb.AppendLine($"- Отличие shape-based силы линии: {shapeForces.DifferenceN:0.####} Н ({shapeForces.RelativeDifference:0.####})");
            sb.AppendLine($"- Статус shape-based сил: {(shapeForces.WithinTolerance ? "OK" : "INFO: отличается от старой оценки")}");
            sb.AppendLine($"- Top T старая / shape: {shapeTensions.TopOriginalTensionKn:0.####} / {shapeTensions.TopShapeTensionKn:0.####} кН");
            sb.AppendLine($"- Макс. отличие shape-based натяжения: {shapeTensions.MaxTensionDifferenceKn:0.####} кН; угол Δmax={shapeTensions.MaxAngleDifferenceDeg:0.####}°");
            sb.AppendLine($"- Статус shape-based натяжений: {(shapeTensions.WithinTolerance ? "OK" : "INFO: отличается от старой оценки")}");
            sb.AppendLine($"- Top T старая / с дискретными нагрузками: {discreteLoadTensions.TopOriginalTensionKn:0.####} / {discreteLoadTensions.TopDiscreteTensionKn:0.####} кН");
            sb.AppendLine($"- Макс. отличие натяжения от дискретных нагрузок: {discreteLoadTensions.MaxTensionDifferenceKn:0.####} кН; угол Δmax={discreteLoadTensions.MaxAngleDifferenceDeg:0.####}°");
            sb.AppendLine($"- Статус натяжений с дискретными нагрузками: {(discreteLoadTensions.WithinTolerance ? "OK" : "INFO: дискретные нагрузки заметно меняют натяжение")}");
            sb.AppendLine($"- Снос альтернативной формы с дискретными нагрузками: {discreteLoadShape.DiscreteHorizontalOffsetM:0.####} м");
            sb.AppendLine($"- Отличие альтернативной формы от основной: ΔXснос={discreteLoadShape.OffsetDifferenceM:0.####} м; max Δузла={discreteLoadShape.MaxNodeDeltaM:0.####} м");
            sb.AppendLine($"- Статус альтернативной формы: {(discreteLoadShape.Converged ? "OK" : "WARNING")}");
            sb.AppendLine($"- Узлов формы: {shape.Nodes.Count}");
        }
        sb.AppendLine();
    }

    private static void AppendDiagnostics(StringBuilder sb, EngineeringDiagnosticsResult diagnostics)
    {
        sb.AppendLine("## Контроль инженерной модели");
        sb.AppendLine($"Общий статус: {EngineeringDiagnostics.DisplaySeverity(diagnostics.OverallSeverity)} — {diagnostics.Summary}.");
        sb.AppendLine();
        sb.AppendLine("| Проверка | Значение | Допуск / критерий | Статус | Примечание |");
        sb.AppendLine("|---|---:|---|---|---|");
        foreach (var row in diagnostics.Rows)
        {
            sb.AppendLine($"| {Escape(row.CheckName)} | {Escape(row.Value)} | {Escape(row.Tolerance)} | {EngineeringDiagnostics.DisplaySeverity(row.Severity)} | {Escape(row.Note)} |");
        }
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

    private static string Escape(string value)
    {
        return (value ?? string.Empty).Replace("|", "/");
    }
}
