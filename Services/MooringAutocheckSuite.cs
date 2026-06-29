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
            "v0.42: built-in scenario autocheck suite. It does not change physics; it checks consistency of geometry, fallback shape, candidate shape, deployment mode and numeric state.");
    }

    public static string BuildReportTable(MooringAutocheckResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Scenario autochecks v0.42");
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
