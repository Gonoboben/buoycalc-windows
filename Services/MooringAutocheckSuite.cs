using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuoyCalc.Windows.Models;

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

        Add(rows, "input-geometry", "Depth is set", "Depth > 0", $"Depth={deploymentMode.DepthM:0.####} m", deploymentMode.DepthM > 0, "Project depth is required.");
        Add(rows, "input-geometry", "Line length is set", "LineLength > 0", $"LineLength={deploymentMode.LineLengthM:0.####} m", deploymentMode.LineLengthM > 0, "Line length must be positive.");
        Add(rows, "shape-fallback", "Fallback shape has nodes", "Nodes >= 2", $"Nodes={fallbackShape.Nodes.Count}", fallbackShape.Nodes.Count >= 2, "MooringShapeSolver fallback must remain usable.");
        Add(rows, "shape-candidate", "Candidate shape has nodes", "Nodes >= 2", $"Nodes={candidateShape.Nodes.Count}", candidateShape.Nodes.Count >= 2, "Candidate shape is optional; fallback remains primary if gate rejects it.", MooringAutocheckSeverity.Info);
        Add(rows, "iterative-solver", "Stop reason is consistent", "Converged => StopReason=Converged; Diverged => not Converged", $"Converged={Bool(iterativeSolverConverged)}, Diverged={Bool(iterativeSolverDiverged)}, StopReason={stopReason}", (!iterativeSolverConverged || stopReason == MooringIterativeSolverStopReason.Converged) && (!iterativeSolverDiverged || !iterativeSolverConverged), "Solver convergence flags must not conflict.");
        Add(rows, "deployment-mode", "Deployment mode classified", "Mode != unknown", $"Mode={deploymentMode.ModeCode}", deploymentMode.Mode != MooringDeploymentMode.Unknown, "v0.41 should classify ordinary inputs.", MooringAutocheckSeverity.Warning);
        Add(rows, "deployment-mode", "Short line geometry", "short => LineLength < Depth", $"Mode={deploymentMode.ModeCode}, shortage={deploymentMode.ShortageM:0.####} m", deploymentMode.Mode != MooringDeploymentMode.ShortLine || deploymentMode.LineLengthM < deploymentMode.DepthM, "short mode must mean line is shorter than depth.");
        Add(rows, "deployment-mode", "Excess line geometry", "excess line => L/Depth >= 1.2", $"Mode={deploymentMode.ModeCode}, L/Depth={deploymentMode.LineToDepthRatio:0.####}", deploymentMode.Mode != MooringDeploymentMode.ExcessLine || deploymentMode.LineToDepthRatio >= 1.2, "excess line mode must mean meaningful line excess.");
        Add(rows, "loads", "Net buoyancy is finite", "NetBuoyancy is finite", $"NetBuoyancy={result.NetBuoyancyKg:0.####} kg", IsFinite(result.NetBuoyancyKg), "Net buoyancy must be finite.");
        Add(rows, "primary-selection", "Primary X offset is finite", "X is finite", $"fallbackX={fallbackShape.HorizontalOffsetM:0.####} m, candidateX={candidateShape.HorizontalOffsetM:0.####} m", IsFinite(fallbackShape.HorizontalOffsetM) && IsFinite(candidateShape.HorizontalOffsetM), "X coordinates must be finite for 2D/PDF.");

        AppendElementDatabaseChecks(rows, result);

        var passCount = rows.Count(x => x.Severity == MooringAutocheckSeverity.Pass);
        var infoCount = rows.Count(x => x.Severity == MooringAutocheckSeverity.Info);
        var warningCount = rows.Count(x => x.Severity == MooringAutocheckSeverity.Warning);
        var failCount = rows.Count(x => x.Severity == MooringAutocheckSeverity.Fail);
        var hasFailures = failCount > 0;
        var summary = hasFailures
            ? $"FAIL: {failCount} critical autochecks failed"
            : warningCount > 0
                ? $"WARNING: no critical failures, warnings={warningCount}"
                : "OK: core autochecks passed";

        return new MooringAutocheckResult(
            rows,
            passCount,
            infoCount,
            warningCount,
            failCount,
            hasFailures,
            summary,
            "v0.43: scenario autochecks now include element database quality checks. They do not change physics; they flag missing names, presets, invalid counts, impossible line lengths, Cd/area inconsistencies, WLL/MBL issues and non-finite reserves.");
    }

    public static string BuildReportTable(MooringAutocheckResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Scenario and element database autochecks v0.43");
        sb.AppendLine(result.MethodNote);
        sb.AppendLine();
        sb.AppendLine($"Result: {result.Summary}; pass={result.PassCount}, info={result.InfoCount}, warning={result.WarningCount}, fail={result.FailCount}.");
        sb.AppendLine();
        sb.AppendLine("| # | Scenario | Check | Expected | Actual | Status | Note |");
        sb.AppendLine("|---:|---|---|---|---|---|---|");
        foreach (var row in result.Rows)
        {
            sb.AppendLine($"| {row.Number} | {Escape(row.Scenario)} | {Escape(row.CheckName)} | {Escape(row.Expected)} | {Escape(row.Actual)} | {row.Severity} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        return sb.ToString();
    }

    private static void AppendElementDatabaseChecks(List<MooringAutocheckRow> rows, CalculationResult result)
    {
        var elements = result.ElementRows;
        var missingNames = elements.Count(x => string.IsNullOrWhiteSpace(x.Title));
        var missingKinds = elements.Count(x => string.IsNullOrWhiteSpace(x.Kind));
        var missingPresets = elements.Count(x => string.IsNullOrWhiteSpace(x.PresetName));
        var invalidCounts = elements.Count(x => x.Count <= 0);
        var negativeLengths = elements.Count(x => x.LengthM < 0);
        var distributedRows = elements.Count(x => x.LengthM > 0);
        var dragAreaWithoutCd = elements.Count(x => x.ProjectedAreaM2 > 0 && x.DragCoefficient <= 0);
        var cdWithoutArea = elements.Count(x => x.DragCoefficient > 0 && x.ProjectedAreaM2 <= 0 && x.CurrentForceN > 0);
        var wllAboveMbl = elements.Count(x => x.BreakingLoadKn > 0 && x.WorkingLoadKn > x.BreakingLoadKn);
        var nonFiniteReserve = elements.Count(x => !IsFinite(x.Reserve));
        var negativeBreakingLoad = elements.Count(x => x.BreakingLoadKn < 0 || x.WorkingLoadKn < 0);

        Add(rows, "element-database", "Element rows exist", "ElementRows.Count > 0", $"count={elements.Count}", elements.Count > 0, "The report must have at least buoy, line and anchor rows.");
        Add(rows, "element-database", "Element names are filled", "missing names = 0", $"missing={missingNames}", missingNames == 0, "Every database row should have a visible title.", MooringAutocheckSeverity.Warning);
        Add(rows, "element-database", "Element kinds are filled", "missing kinds = 0", $"missing={missingKinds}", missingKinds == 0, "Every database row should have a kind/type.");
        Add(rows, "element-database", "Preset names are filled", "missing presets = 0", $"missing={missingPresets}", missingPresets == 0, "Missing preset means the row is harder to trace back to the element database.", MooringAutocheckSeverity.Info);
        Add(rows, "element-database", "Element counts are positive", "invalid counts = 0", $"invalid={invalidCounts}", invalidCounts == 0, "Each element row should have count > 0.");
        Add(rows, "element-database", "No negative lengths", "negative lengths = 0", $"negative={negativeLengths}", negativeLengths == 0, "Length cannot be negative.");
        Add(rows, "element-database", "Distributed line rows exist", "rows with Length > 0 >= 1", $"distributed={distributedRows}", distributedRows >= 1, "A mooring should normally contain at least one distributed line row.", MooringAutocheckSeverity.Warning);
        Add(rows, "element-database", "Drag rows have Cd", "area > 0 => Cd > 0", $"bad={dragAreaWithoutCd}", dragAreaWithoutCd == 0, "Projected area without Cd makes current force suspicious.", MooringAutocheckSeverity.Warning);
        Add(rows, "element-database", "Cd rows have area when force exists", "Cd > 0 and force > 0 => area > 0", $"bad={cdWithoutArea}", cdWithoutArea == 0, "Cd without projected area cannot explain current force.", MooringAutocheckSeverity.Warning);
        Add(rows, "element-database", "WLL does not exceed MBL", "WLL <= MBL", $"bad={wllAboveMbl}", wllAboveMbl == 0, "Working load should not exceed breaking load.");
        Add(rows, "element-database", "Loads are non-negative", "MBL/WLL >= 0", $"bad={negativeBreakingLoad}", negativeBreakingLoad == 0, "Strength values must not be negative.");
        Add(rows, "element-database", "Reserve is finite", "Reserve finite", $"bad={nonFiniteReserve}", nonFiniteReserve == 0, "Reserve must be finite for report and checks.");
    }

    private static void Add(List<MooringAutocheckRow> rows, string scenario, string checkName, string expected, string actual, bool passed, string note, MooringAutocheckSeverity failedSeverity = MooringAutocheckSeverity.Fail)
    {
        rows.Add(new MooringAutocheckRow(rows.Count + 1, scenario, checkName, expected, actual, passed ? MooringAutocheckSeverity.Pass : failedSeverity, passed ? "OK" : note));
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
