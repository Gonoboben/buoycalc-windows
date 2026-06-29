using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuoyCalc.Windows.Services;

public enum MooringAutocheckSeverity
{
    Pass,
    Info,
    Warning,
    Fail
}

public sealed record MooringAutocheckRow(
    int Number,
    string Scenario,
    string CheckName,
    string Expected,
    string Actual,
    MooringAutocheckSeverity Severity,
    string Status);

public sealed record MooringAutocheckResult(
    IReadOnlyList<MooringAutocheckRow> Rows,
    int PassCount,
    int InfoCount,
    int WarningCount,
    int FailCount,
    bool HasFailures,
    string Summary,
    string MethodNote);

public static class MooringAutocheckSuite
{
    public static MooringAutocheckResult Build(
        CalculationResult result,
        MooringShapeResult fallbackShape,
        MooringShapeResult candidateShape,
        bool iterativeSolverConverged,
        bool iterativeSolverDiverged,
        MooringIterativeSolverStopReason stopReason,
        MooringDeploymentModeResult deploymentMode)
    {
        var rows = new List<MooringAutocheckRow>();

        Add(rows,
            "input-geometry",
            "Глубина задана",
            "Depth > 0",
            $"Depth={deploymentMode.DepthM:0.####} м",
            deploymentMode.DepthM > 0,
            "Проектная глубина нужна для любой постановки.");

        Add(rows,
            "input-geometry",
            "Длина линии задана",
            "LineLength > 0",
            $"LineLength={deploymentMode.LineLengthM:0.####} м",
            deploymentMode.LineLengthM > 0,
            "Суммарная длина линии должна быть положительной.");

        Add(rows,
            "shape-fallback",
            "Fallback-форма имеет узлы",
            "Nodes >= 2",
            $"Nodes={fallbackShape.Nodes.Count}",
            fallbackShape.Nodes.Count >= 2,
            "Старая форма MooringShapeSolver должна оставаться рабочим fallback.");

        Add(rows,
            "shape-candidate",
            "Кандидатная форма имеет узлы",
            "Nodes >= 2",
            $"Nodes={candidateShape.Nodes.Count}",
            candidateShape.Nodes.Count >= 2,
            "Кандидатная форма нужна для v0.40 gate; если она не готова, fallback остаётся основной.",
            MooringAutocheckSeverity.Info);

        Add(rows,
            "iterative-solver",
            "StopReason согласован со сходимостью",
            "Converged => StopReason=Converged; Diverged => not Converged",
            $"Converged={Bool(iterativeSolverConverged)}, Diverged={Bool(iterativeSolverDiverged)}, StopReason={stopReason}",
            (!iterativeSolverConverged || stopReason == MooringIterativeSolverStopReason.Converged) &&
                (!iterativeSolverDiverged || !iterativeSolverConverged),
            "Состояния сходимости и защитной остановки не должны противоречить друг другу.");

        Add(rows,
            "deployment-mode",
            "Режим постановки классифицирован",
            "Mode != unknown",
            $"Mode={deploymentMode.ModeCode}",
            deploymentMode.Mode != MooringDeploymentMode.Unknown,
            "v0.41 должен давать явный режим для обычных входных данных.",
            MooringAutocheckSeverity.Warning);

        Add(rows,
            "deployment-mode",
            "Short line согласован с геометрией",
            "short => LineLength < Depth",
            $"Mode={deploymentMode.ModeCode}, shortage={deploymentMode.ShortageM:0.####} м",
            deploymentMode.Mode != MooringDeploymentMode.ShortLine || deploymentMode.LineLengthM < deploymentMode.DepthM,
            "Режим short должен означать, что линия короче проектной глубины.");

        Add(rows,
            "deployment-mode",
            "Excess line согласован с геометрией",
            "excess line => L/Depth >= 1.2",
            $"Mode={deploymentMode.ModeCode}, L/Depth={deploymentMode.LineToDepthRatio:0.####}",
            deploymentMode.Mode != MooringDeploymentMode.ExcessLine || deploymentMode.LineToDepthRatio >= 1.2,
            "Режим excess line должен означать заметный избыток длины линии.");

        Add(rows,
            "loads",
            "Чистая плавучесть конечна",
            "NetBuoyancy is finite",
            $"NetBuoyancy={result.NetBuoyancyKg:0.####} кг",
            IsFinite(result.NetBuoyancyKg),
            "Непредставимое значение плавучести ломает режимы постановки.");

        Add(rows,
            "primary-selection",
            "Основной X-снос конечен",
            "X is finite",
            $"fallbackX={fallbackShape.HorizontalOffsetM:0.####} м, candidateX={candidateShape.HorizontalOffsetM:0.####} м",
            IsFinite(fallbackShape.HorizontalOffsetM) && IsFinite(candidateShape.HorizontalOffsetM),
            "X-координаты должны быть конечными для 2D/PDF.");

        var passCount = rows.Count(x => x.Severity == MooringAutocheckSeverity.Pass);
        var infoCount = rows.Count(x => x.Severity == MooringAutocheckSeverity.Info);
        var warningCount = rows.Count(x => x.Severity == MooringAutocheckSeverity.Warning);
        var failCount = rows.Count(x => x.Severity == MooringAutocheckSeverity.Fail);
        var hasFailures = failCount > 0;
        var summary = hasFailures
            ? $"FAIL: {failCount} критических автопроверок не пройдено"
            : warningCount > 0
                ? $"WARNING: критических ошибок нет, предупреждений={warningCount}"
                : "OK: автопроверки расчётного ядра пройдены";

        return new MooringAutocheckResult(
            rows,
            passCount,
            infoCount,
            warningCount,
            failCount,
            hasFailures,
            summary,
            "v0.42: встроенный набор автопроверок сценариев. Он не меняет расчёт, а проверяет согласованность входной геометрии, fallback-формы, кандидатной формы, режима постановки и численных признаков перед будущими регрессионными тестами.");
    }

    public static string BuildReportTable(MooringAutocheckResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Автопроверки сценариев v0.42");
        sb.AppendLine(result.MethodNote);
        sb.AppendLine();
        sb.AppendLine($"Итог: {result.Summary}; pass={result.PassCount}, info={result.InfoCount}, warning={result.WarningCount}, fail={result.FailCount}.");
        sb.AppendLine();
        sb.AppendLine("| № | Сценарий | Проверка | Ожидается | Фактически | Статус | Примечание |");
        sb.AppendLine("|---:|---|---|---|---|---|---|");
        foreach (var row in result.Rows)
        {
            sb.AppendLine($"| {row.Number} | {Escape(row.Scenario)} | {Escape(row.CheckName)} | {Escape(row.Expected)} | {Escape(row.Actual)} | {row.Severity} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        return sb.ToString();
    }

    private static void Add(
        List<MooringAutocheckRow> rows,
        string scenario,
        string checkName,
        string expected,
        string actual,
        bool passed,
        string note,
        MooringAutocheckSeverity failedSeverity = MooringAutocheckSeverity.Fail)
    {
        rows.Add(new MooringAutocheckRow(
            rows.Count + 1,
            scenario,
            checkName,
            expected,
            actual,
            passed ? MooringAutocheckSeverity.Pass : failedSeverity,
            passed ? "OK" : note));
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static string Bool(bool value)
    {
        return value ? "YES" : "NO";
    }

    private static string Escape(string value)
    {
        return (value ?? string.Empty).Replace("|", "/");
    }
}
