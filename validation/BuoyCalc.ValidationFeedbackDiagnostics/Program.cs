using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal sealed record DiagnosticScenario(
    string Id,
    double DepthM,
    double LineLengthM,
    double CurrentMS,
    bool SplitLineWithPayload = false,
    double PayloadWeightAirKg = 0.0,
    double PayloadVolumeM3 = 0.0,
    double PayloadProjectedAreaM2 = 0.0,
    double PayloadDragCoefficient = 1.0);

internal sealed record FeedbackGeometry(
    IReadOnlyList<(double X, double Z)> Nodes,
    double EndpointX,
    double EndpointZ);

internal sealed record FeedbackState(
    CalculationResult Result,
    MooringSurfaceBoundaryInfoResult Boundary,
    MooringSurfaceBoundaryTensionTraceResult Trace,
    FeedbackGeometry Geometry,
    double LineForceN,
    string Fingerprint);

internal sealed record IterationRow(
    string Scenario,
    int Iteration,
    double CurrentQ0N,
    double NextQ0N,
    double DeltaQ0N,
    double CurrentEndpointXM,
    double NextEndpointXM,
    double DeltaEndpointXM,
    double CurrentEndpointZM,
    double NextEndpointZM,
    double DeltaEndpointZM,
    double CurrentLineForceN,
    double NextLineForceN,
    double DeltaLineForceN,
    double MaxSegmentForceDeltaN,
    double MaxNodeDeltaM,
    double MaxTraceScalarDelta,
    int ExactDifferentFieldCount,
    bool ExactFixedPoint,
    string CurrentFingerprint,
    string NextFingerprint,
    int? RepeatedFromStateIteration,
    int? CycleLength);

internal static class Program
{
    private const int Budget = MooringSignedCandidateResult.ProductionFeedbackBudget;

    public static int Main(string[] args)
    {
        var output = ResolveOutput(args);
        Directory.CreateDirectory(output);

        var allRows = new List<IterationRow>();
        var summaries = new List<string>();

        foreach (var scenario in Scenarios())
        {
            var (environment, buoy, assembly, anchor) = BuildInputs(scenario);
            var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, 3.0);
            var productionCandidate = run.Snapshot.SignedCandidate
                ?? throw new InvalidOperationException($"{scenario.Id}: production signed candidate missing.");

            if (productionCandidate.Status != MooringSignedCandidateStatus.BudgetExhausted)
            {
                throw new InvalidOperationException(
                    $"{scenario.Id}: expected diagnostic target BudgetExhausted, got {productionCandidate.Status}.");
            }

            var rows = Diagnose(
                scenario,
                environment,
                buoy,
                run.Result,
                run.Snapshot.TechnicalReportData.SequencePositions);
            allRows.AddRange(rows);

            var last = rows[^1];
            var cycleRows = rows.Where(x => x.CycleLength.HasValue).ToList();
            var firstCycle = cycleRows.FirstOrDefault();
            summaries.Add(string.Join(" | ",
                scenario.Id,
                $"iterations={rows.Count}",
                $"last dQ0={F(last.DeltaQ0N)} N",
                $"last dX={F(last.DeltaEndpointXM)} m",
                $"last dZ={F(last.DeltaEndpointZM)} m",
                $"last dLineForce={F(last.DeltaLineForceN)} N",
                $"last maxSegmentDelta={F(last.MaxSegmentForceDeltaN)} N",
                $"last maxNodeDelta={F(last.MaxNodeDeltaM)} m",
                $"last maxTraceDelta={F(last.MaxTraceScalarDelta)}",
                $"last exact-different-fields={last.ExactDifferentFieldCount}",
                firstCycle is null
                    ? "cycle=none detected"
                    : $"cycle first seen at iteration {firstCycle.Iteration}, length={firstCycle.CycleLength}, repeats state {firstCycle.RepeatedFromStateIteration}"));
        }

        WriteCsv(Path.Combine(output, "feedback-budget-diagnostics.csv"), allRows);
        WriteMarkdown(Path.Combine(output, "feedback-budget-diagnostics.md"), allRows, summaries);

        Console.WriteLine("FEEDBACK_DIAGNOSTICS_COMPLETE");
        foreach (var summary in summaries)
            Console.WriteLine(summary);
        return 0;
    }

    private static string ResolveOutput(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--output")
                return args[i + 1];
        return Path.Combine("artifacts", "validation-feedback-diagnostics");
    }

    private static IReadOnlyList<DiagnosticScenario> Scenarios() =>
        new[]
        {
            new DiagnosticScenario("A06", 20.0, 22.0, 0.2),
            new DiagnosticScenario("A11", 50.0, 55.0, 0.5),
            new DiagnosticScenario("A15", 100.0, 110.0, 0.3, true, 20.0, 0.002, 0.05, 1.0)
        };

    private static (EnvironmentInput Environment, BuoyInput Buoy, IReadOnlyList<AssemblyItemInput> Assembly, AnchorInput Anchor)
        BuildInputs(DiagnosticScenario scenario)
    {
        var environment = new EnvironmentInput(
            1025.0,
            scenario.DepthM,
            0.0,
            0.0,
            0.0,
            new SeabedPreset("campaign:sand", "Validation sand", 1.2, "Diagnostic fixture only."),
            true,
            new[]
            {
                new CurrentProfilePointInput(0.0, scenario.CurrentMS, 0.0, 0.0, 1025.0),
                new CurrentProfilePointInput(scenario.DepthM, scenario.CurrentMS, 0.0, 0.0, 1025.0)
            });

        var buoy = new BuoyInput("Series A reference buoy", 1.0, 100.0, 0.10, 0.8);
        var rope = new RopePreset(
            "campaign:rope-14",
            "Series A 14 mm line",
            "Synthetic reference",
            14.0,
            70.0,
            0.15,
            1.0,
            "Diagnostic fixture only.");
        var anchor = new AnchorInput("Series A 3000 kg block", "Concrete block", "Concrete", 3000.0, 1.2, 1.0);

        IReadOnlyList<AssemblyItemInput> assembly;
        if (!scenario.SplitLineWithPayload)
        {
            assembly = new[] { LineItem("Main line", scenario.LineLengthM, rope) };
        }
        else
        {
            var upper = scenario.LineLengthM / 2.0;
            assembly = new[]
            {
                LineItem("Upper line", upper, rope),
                new AssemblyItemInput(
                    AssemblyItemKind.Payload,
                    "Internal instrument",
                    true,
                    null,
                    null,
                    0.0,
                    1,
                    scenario.PayloadWeightAirKg,
                    scenario.PayloadVolumeM3,
                    scenario.PayloadProjectedAreaM2,
                    scenario.PayloadDragCoefficient),
                LineItem("Lower line", scenario.LineLengthM - upper, rope)
            };
        }

        return (environment, buoy, assembly, anchor);
    }

    private static AssemblyItemInput LineItem(string title, double lengthM, RopePreset rope) =>
        new(AssemblyItemKind.Line, title, true, rope, null, lengthM, 1, 0.0, 0.0, 0.0, 0.0);

    private static IReadOnlyList<IterationRow> Diagnose(
        DiagnosticScenario scenario,
        EnvironmentInput environment,
        BuoyInput buoy,
        CalculationResult baseResult,
        MooringSequencePositionResult sequence)
    {
        var initialBoundary = MooringSurfaceBoundaryInfoAnalyzer.Build(environment, buoy, baseResult, sequence);
        if (!initialBoundary.Solved || initialBoundary.SolutionState is null || !initialBoundary.BuoySteadyDragN.HasValue)
            throw new InvalidOperationException($"{scenario.Id}: initial boundary is not uniquely solved: {initialBoundary.Classification}.");

        var initialTrace = MooringSurfaceBoundaryTensionTraceBuilder.Build(baseResult, sequence, initialBoundary);
        if (!initialTrace.Available)
            throw new InvalidOperationException($"{scenario.Id}: initial trace unavailable: {initialTrace.UnavailableReason}.");

        var initialGeometry = BuildGeometry(initialTrace);
        var initialLineForce = baseResult.SegmentRows.Sum(x => x.CurrentForceN);
        var state = BuildState(baseResult, initialBoundary, initialTrace, initialGeometry, initialLineForce);
        var buoySteadyDragN = initialBoundary.BuoySteadyDragN.Value;

        var seen = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [state.Fingerprint] = 0
        };
        var rows = new List<IterationRow>(Budget);

        for (var iteration = 1; iteration <= Budget; iteration++)
        {
            var projection = BuildProjection(state.Trace, state.Geometry);
            var shapeForces = MooringShapeForceAnalyzer.Build(state.Result, projection);
            var nextResult = ApplyShapeForces(
                baseResult,
                state.Result,
                sequence,
                shapeForces,
                buoySteadyDragN,
                out var nextLineForceN);
            var nextBoundary = MooringSurfaceBoundaryInfoAnalyzer.Build(environment, buoy, nextResult, sequence);
            if (!nextBoundary.Solved || nextBoundary.SolutionState is null)
                throw new InvalidOperationException($"{scenario.Id}: next boundary failed at {iteration}: {nextBoundary.Classification}.");
            var nextTrace = MooringSurfaceBoundaryTensionTraceBuilder.Build(nextResult, sequence, nextBoundary);
            if (!nextTrace.Available)
                throw new InvalidOperationException($"{scenario.Id}: next trace unavailable at {iteration}: {nextTrace.UnavailableReason}.");
            var nextGeometry = BuildGeometry(nextTrace);
            var next = BuildState(nextResult, nextBoundary, nextTrace, nextGeometry, nextLineForceN);

            var exact = ExactFixedPoint(state, next);
            var differentCount = CountExactDifferences(state, next);
            var maxSegmentDelta = MaxSegmentForceDelta(state.Result, next.Result);
            var maxNodeDelta = MaxNodeDelta(state.Geometry, next.Geometry);
            var maxTraceDelta = MaxTraceScalarDelta(state.Trace, next.Trace);
            int? repeatedFrom = null;
            int? cycleLength = null;
            if (seen.TryGetValue(next.Fingerprint, out var previousStateIteration))
            {
                repeatedFrom = previousStateIteration;
                cycleLength = iteration - previousStateIteration;
            }
            else
            {
                seen[next.Fingerprint] = iteration;
            }

            rows.Add(new IterationRow(
                scenario.Id,
                iteration,
                state.Boundary.Q0N!.Value,
                next.Boundary.Q0N!.Value,
                next.Boundary.Q0N.Value - state.Boundary.Q0N.Value,
                state.Geometry.EndpointX,
                next.Geometry.EndpointX,
                next.Geometry.EndpointX - state.Geometry.EndpointX,
                state.Geometry.EndpointZ,
                next.Geometry.EndpointZ,
                next.Geometry.EndpointZ - state.Geometry.EndpointZ,
                state.LineForceN,
                next.LineForceN,
                next.LineForceN - state.LineForceN,
                maxSegmentDelta,
                maxNodeDelta,
                maxTraceDelta,
                differentCount,
                exact,
                state.Fingerprint,
                next.Fingerprint,
                repeatedFrom,
                cycleLength));

            state = next;
        }

        return rows;
    }

    private static FeedbackState BuildState(
        CalculationResult result,
        MooringSurfaceBoundaryInfoResult boundary,
        MooringSurfaceBoundaryTensionTraceResult trace,
        FeedbackGeometry geometry,
        double lineForceN)
    {
        var state = new FeedbackState(result, boundary, trace, geometry, lineForceN, string.Empty);
        return state with { Fingerprint = Fingerprint(state) };
    }

    private static FeedbackGeometry BuildGeometry(MooringSurfaceBoundaryTensionTraceResult trace)
    {
        var nodes = new List<(double X, double Z)>(trace.Rows.Count + 1) { (0.0, 0.0) };
        var x = 0.0;
        var z = 0.0;
        foreach (var row in trace.Rows)
        {
            if (!row.TangentX.HasValue || !row.TangentZ.HasValue)
                throw new InvalidOperationException($"Trace tangent missing at segment {row.SegmentNumber}.");
            var ds = row.EndLengthM - row.StartLengthM;
            x += ds * row.TangentX.Value;
            z += ds * row.TangentZ.Value;
            nodes.Add((x, z));
        }
        return new FeedbackGeometry(nodes, x, z);
    }

    private static MooringShapeProjectionResult BuildProjection(
        MooringSurfaceBoundaryTensionTraceResult trace,
        FeedbackGeometry geometry)
    {
        var rows = new List<MooringShapeProjectionRow>(trace.Rows.Count);
        for (var i = 0; i < trace.Rows.Count; i++)
        {
            var t = trace.Rows[i];
            var start = geometry.Nodes[i];
            var end = geometry.Nodes[i + 1];
            var ds = t.EndLengthM - t.StartLengthM;
            var dx = end.X - start.X;
            var dz = end.Z - start.Z;
            var projected = Math.Sqrt(dx * dx + dz * dz);
            var residual = Math.Abs(projected - ds);
            var angle = Math.Atan2(Math.Abs(dx), Math.Max(1e-12, Math.Abs(dz))) * 180.0 / Math.PI;
            rows.Add(new MooringShapeProjectionRow(
                i + 1,
                t.SegmentNumber,
                t.SourceElement,
                ds,
                dx,
                dz,
                projected,
                residual,
                angle,
                t.MidTensionN / 1000.0,
                "INFO: validation feedback diagnostic mirror"));
        }

        var totalSegmentLength = rows.Sum(x => x.SegmentLengthM);
        var totalProjectedLength = rows.Sum(x => x.ProjectedLengthM);
        var totalResidual = Math.Abs(totalProjectedLength - totalSegmentLength);
        return new MooringShapeProjectionResult(
            rows,
            geometry.EndpointX,
            geometry.EndpointZ,
            totalSegmentLength,
            totalProjectedLength,
            totalResidual,
            geometry.EndpointX,
            geometry.EndpointZ,
            0.0,
            0.0,
            rows.Count == 0 ? 0.0 : rows.Max(x => x.AngleFromVerticalDeg),
            rows.Count == 0 ? 0.0 : rows.Average(x => x.AngleFromVerticalDeg),
            totalResidual <= MooringSurfaceBoundaryIntegrationKernel.LengthToleranceM,
            "Validation-only mirror of production signed feedback projection; not reference truth.");
    }

    private static CalculationResult ApplyShapeForces(
        CalculationResult baseResult,
        CalculationResult currentResult,
        MooringSequencePositionResult sequence,
        MooringShapeForceResult shapeForces,
        double buoySteadyDragN,
        out double updatedLineForceN)
    {
        var forces = shapeForces.Rows.ToDictionary(x => x.SegmentNumber);
        var updatedSegments = new List<SegmentCalculationRow>(currentResult.SegmentRows.Count);
        foreach (var segment in currentResult.SegmentRows.OrderBy(x => x.Number))
        {
            if (!forces.TryGetValue(segment.Number, out var force))
                throw new InvalidOperationException($"Missing diagnostic shape force at segment {segment.Number}.");
            updatedSegments.Add(segment with { CurrentForceN = Math.Max(0.0, force.ShapeForceN) });
        }

        updatedLineForceN = updatedSegments.Sum(x => x.CurrentForceN);
        var totalCurrent = buoySteadyDragN + updatedLineForceN + sequence.DiscreteCurrentForceN;
        return currentResult with
        {
            SegmentRows = updatedSegments,
            CurrentForceN = totalCurrent,
            HorizontalForceN = totalCurrent + baseResult.WaveForceN
        };
    }

    private static bool ExactFixedPoint(FeedbackState current, FeedbackState next) =>
        CountExactDifferences(current, next) == 0;

    private static int CountExactDifferences(FeedbackState current, FeedbackState next)
    {
        var count = 0;
        void Compare(double? a, double? b) { if (a != b) count++; }
        void Compare(double a, double b) { if (a != b) count++; }
        void Compare(int a, int b) { if (a != b) count++; }
        void Compare(string a, string b) { if (!string.Equals(a, b, StringComparison.Ordinal)) count++; }

        Compare(current.Boundary.Classification.ToString(), next.Boundary.Classification.ToString());
        Compare(current.Boundary.Q0N, next.Boundary.Q0N);
        Compare(current.Boundary.SolutionState?.EndpointXM, next.Boundary.SolutionState?.EndpointXM);
        Compare(current.Boundary.SolutionState?.EndpointZM, next.Boundary.SolutionState?.EndpointZM);
        Compare(current.LineForceN, next.LineForceN);
        Compare(current.Trace.PointLoadCrossings, next.Trace.PointLoadCrossings);
        Compare(current.Geometry.Nodes.Count, next.Geometry.Nodes.Count);
        Compare(current.Result.SegmentRows.Count, next.Result.SegmentRows.Count);
        Compare(current.Trace.Rows.Count, next.Trace.Rows.Count);

        var nodeCount = Math.Min(current.Geometry.Nodes.Count, next.Geometry.Nodes.Count);
        for (var i = 0; i < nodeCount; i++)
        {
            Compare(current.Geometry.Nodes[i].X, next.Geometry.Nodes[i].X);
            Compare(current.Geometry.Nodes[i].Z, next.Geometry.Nodes[i].Z);
        }

        var currentSegments = current.Result.SegmentRows.OrderBy(x => x.Number).ToList();
        var nextSegments = next.Result.SegmentRows.OrderBy(x => x.Number).ToList();
        for (var i = 0; i < Math.Min(currentSegments.Count, nextSegments.Count); i++)
        {
            Compare(currentSegments[i].Number, nextSegments[i].Number);
            Compare(currentSegments[i].CurrentForceN, nextSegments[i].CurrentForceN);
        }

        for (var i = 0; i < Math.Min(current.Trace.Rows.Count, next.Trace.Rows.Count); i++)
        {
            var a = current.Trace.Rows[i];
            var b = next.Trace.Rows[i];
            Compare(a.SegmentNumber, b.SegmentNumber);
            Compare(a.StartLengthM, b.StartLengthM);
            Compare(a.EndLengthM, b.EndLengthM);
            Compare(a.PointLoadCrossingsAppliedBeforeSegment, b.PointLoadCrossingsAppliedBeforeSegment);
            Compare(a.StartHN, b.StartHN);
            Compare(a.StartVN, b.StartVN);
            Compare(a.MidHN, b.MidHN);
            Compare(a.MidVN, b.MidVN);
            Compare(a.EndHN, b.EndHN);
            Compare(a.EndVN, b.EndVN);
            Compare(a.MidTensionN, b.MidTensionN);
            Compare(a.TangentX, b.TangentX);
            Compare(a.TangentZ, b.TangentZ);
        }

        return count;
    }

    private static double MaxSegmentForceDelta(CalculationResult current, CalculationResult next)
    {
        var a = current.SegmentRows.OrderBy(x => x.Number).ToList();
        var b = next.SegmentRows.OrderBy(x => x.Number).ToList();
        return a.Zip(b, (x, y) => Math.Abs(y.CurrentForceN - x.CurrentForceN)).DefaultIfEmpty(0.0).Max();
    }

    private static double MaxNodeDelta(FeedbackGeometry current, FeedbackGeometry next) =>
        current.Nodes.Zip(next.Nodes, (a, b) => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Z - a.Z) * (b.Z - a.Z)))
            .DefaultIfEmpty(0.0)
            .Max();

    private static double MaxTraceScalarDelta(
        MooringSurfaceBoundaryTensionTraceResult current,
        MooringSurfaceBoundaryTensionTraceResult next)
    {
        var max = 0.0;
        void Take(double a, double b) => max = Math.Max(max, Math.Abs(b - a));
        void Take(double? a, double? b)
        {
            if (a.HasValue && b.HasValue)
                Take(a.Value, b.Value);
            else if (a.HasValue != b.HasValue)
                max = double.PositiveInfinity;
        }

        foreach (var pair in current.Rows.Zip(next.Rows))
        {
            var a = pair.First;
            var b = pair.Second;
            Take(a.StartHN, b.StartHN);
            Take(a.StartVN, b.StartVN);
            Take(a.MidHN, b.MidHN);
            Take(a.MidVN, b.MidVN);
            Take(a.EndHN, b.EndHN);
            Take(a.EndVN, b.EndVN);
            Take(a.MidTensionN, b.MidTensionN);
            Take(a.TangentX, b.TangentX);
            Take(a.TangentZ, b.TangentZ);
        }
        return max;
    }

    private static string Fingerprint(FeedbackState state)
    {
        var sb = new StringBuilder();
        void D(double? v) => sb.Append(v.HasValue ? BitConverter.DoubleToInt64Bits(v.Value).ToString("X16", CultureInfo.InvariantCulture) : "null").Append('|');
        void D(double v) => sb.Append(BitConverter.DoubleToInt64Bits(v).ToString("X16", CultureInfo.InvariantCulture)).Append('|');
        void I(int v) => sb.Append(v.ToString(CultureInfo.InvariantCulture)).Append('|');

        sb.Append(state.Boundary.Classification).Append('|');
        D(state.Boundary.Q0N);
        D(state.Boundary.SolutionState?.EndpointXM);
        D(state.Boundary.SolutionState?.EndpointZM);
        D(state.LineForceN);
        I(state.Trace.PointLoadCrossings);
        foreach (var node in state.Geometry.Nodes) { D(node.X); D(node.Z); }
        foreach (var segment in state.Result.SegmentRows.OrderBy(x => x.Number)) { I(segment.Number); D(segment.CurrentForceN); }
        foreach (var row in state.Trace.Rows)
        {
            I(row.SegmentNumber);
            D(row.StartLengthM); D(row.EndLengthM);
            I(row.PointLoadCrossingsAppliedBeforeSegment);
            D(row.StartHN); D(row.StartVN); D(row.MidHN); D(row.MidVN); D(row.EndHN); D(row.EndVN); D(row.MidTensionN); D(row.TangentX); D(row.TangentZ);
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())))[..16];
    }

    private static void WriteCsv(string path, IReadOnlyList<IterationRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Scenario,Iteration,CurrentQ0N,NextQ0N,DeltaQ0N,CurrentX,NextX,DeltaX,CurrentZ,NextZ,DeltaZ,CurrentLineForceN,NextLineForceN,DeltaLineForceN,MaxSegmentForceDeltaN,MaxNodeDeltaM,MaxTraceScalarDelta,ExactDifferentFieldCount,ExactFixedPoint,CurrentFingerprint,NextFingerprint,RepeatedFromStateIteration,CycleLength");
        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                r.Scenario,
                r.Iteration.ToString(CultureInfo.InvariantCulture),
                F(r.CurrentQ0N), F(r.NextQ0N), F(r.DeltaQ0N),
                F(r.CurrentEndpointXM), F(r.NextEndpointXM), F(r.DeltaEndpointXM),
                F(r.CurrentEndpointZM), F(r.NextEndpointZM), F(r.DeltaEndpointZM),
                F(r.CurrentLineForceN), F(r.NextLineForceN), F(r.DeltaLineForceN),
                F(r.MaxSegmentForceDeltaN), F(r.MaxNodeDeltaM), F(r.MaxTraceScalarDelta),
                r.ExactDifferentFieldCount.ToString(CultureInfo.InvariantCulture),
                r.ExactFixedPoint.ToString(CultureInfo.InvariantCulture),
                r.CurrentFingerprint,
                r.NextFingerprint,
                r.RepeatedFromStateIteration?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                r.CycleLength?.ToString(CultureInfo.InvariantCulture) ?? string.Empty
            }));
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void WriteMarkdown(string path, IReadOnlyList<IterationRow> rows, IReadOnlyList<string> summaries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Series A exact-feedback budget diagnostics");
        sb.AppendLine();
        sb.AppendLine("Validation-only mirror of the production public feedback stages for A06/A11/A15. It does not change or authorize production physics and does not introduce a convergence epsilon.");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        foreach (var summary in summaries) sb.AppendLine("- " + summary);
        sb.AppendLine();

        foreach (var group in rows.GroupBy(x => x.Scenario))
        {
            sb.AppendLine($"## {group.Key} — last 12 iterations");
            sb.AppendLine();
            sb.AppendLine("| i | dQ0 N | dX m | dZ m | dLine N | max seg dN | max node dm | max trace delta | diff fields | cycle |");
            sb.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
            foreach (var r in group.TakeLast(12))
            {
                sb.AppendLine($"| {r.Iteration} | {F(r.DeltaQ0N)} | {F(r.DeltaEndpointXM)} | {F(r.DeltaEndpointZM)} | {F(r.DeltaLineForceN)} | {F(r.MaxSegmentForceDeltaN)} | {F(r.MaxNodeDeltaM)} | {F(r.MaxTraceScalarDelta)} | {r.ExactDifferentFieldCount} | {(r.CycleLength.HasValue ? $"len {r.CycleLength} -> state {r.RepeatedFromStateIteration}" : "—")} |");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Interpretation: non-zero deltas show that the exact production state has not become bit-identical. A repeated fingerprint reveals an exact cycle. Small deltas without a repeated fingerprint show continued floating-state evolution, not permission to substitute an epsilon.");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
