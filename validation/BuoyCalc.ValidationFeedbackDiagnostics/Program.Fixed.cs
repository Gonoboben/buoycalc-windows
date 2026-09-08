using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal sealed record FixedDiagnosticScenario(
    string Id,
    double DepthM,
    double LineLengthM,
    double CurrentMS,
    bool HasPayload = false,
    double PayloadWeightAirKg = 0.0,
    double PayloadVolumeM3 = 0.0,
    double PayloadProjectedAreaM2 = 0.0,
    double PayloadDragCoefficient = 1.0);

internal sealed record FixedFeedbackGeometry(
    IReadOnlyList<(double X, double Z)> Nodes,
    double EndpointX,
    double EndpointZ);

internal sealed record FixedFeedbackState(
    CalculationResult Result,
    MooringSurfaceBoundaryInfoResult Boundary,
    MooringSurfaceBoundaryTensionTraceResult Trace,
    FixedFeedbackGeometry Geometry,
    double LineForceN,
    string Fingerprint);

internal sealed record FixedIterationRow(
    string Scenario,
    int Iteration,
    double DeltaQ0N,
    double DeltaEndpointXM,
    double DeltaEndpointZM,
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

internal static class FixedProgram
{
    private const int Budget = MooringSignedCandidateResult.ProductionFeedbackBudget;
    // Mirrors the inspected production integration-kernel identity only for validation diagnostics.
    // This is not a convergence tolerance and is never used to accept a candidate.
    private const double ProductionLengthIdentityToleranceM = 1e-9;

    public static int Main(string[] args)
    {
        var output = ResolveOutput(args);
        Directory.CreateDirectory(output);

        var allRows = new List<FixedIterationRow>();
        var summaries = new List<string>();

        foreach (var scenario in Scenarios())
        {
            var (environment, buoy, assembly, anchor) = BuildInputs(scenario);
            var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, 3.0);
            var candidate = run.Snapshot.SignedCandidate
                ?? throw new InvalidOperationException($"{scenario.Id}: production signed candidate missing.");

            if (candidate.Status != MooringSignedCandidateStatus.BudgetExhausted)
            {
                throw new InvalidOperationException(
                    $"{scenario.Id}: expected production BudgetExhausted, got {candidate.Status} ({candidate.DiagnosticCode}).");
            }

            var rows = Diagnose(
                scenario,
                environment,
                buoy,
                run.Result,
                run.Snapshot.TechnicalReportData.SequencePositions);
            allRows.AddRange(rows);

            var last = rows[^1];
            var firstRepeated = rows.FirstOrDefault(x => x.CycleLength.HasValue);
            summaries.Add(string.Join(" | ",
                scenario.Id,
                $"last dQ0={F(last.DeltaQ0N)} N",
                $"last dX={F(last.DeltaEndpointXM)} m",
                $"last dZ={F(last.DeltaEndpointZM)} m",
                $"last dLine={F(last.DeltaLineForceN)} N",
                $"last maxSeg={F(last.MaxSegmentForceDeltaN)} N",
                $"last maxNode={F(last.MaxNodeDeltaM)} m",
                $"last maxTrace={F(last.MaxTraceScalarDelta)}",
                $"differentFields={last.ExactDifferentFieldCount}",
                firstRepeated is null
                    ? "cycle=none"
                    : $"cycle=length {firstRepeated.CycleLength} at iteration {firstRepeated.Iteration}, repeats state {firstRepeated.RepeatedFromStateIteration}"));
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
            if (string.Equals(args[i], "--output", StringComparison.Ordinal))
                return args[i + 1];
        return Path.Combine("artifacts", "validation-feedback-diagnostics");
    }

    private static IReadOnlyList<FixedDiagnosticScenario> Scenarios() =>
        new[]
        {
            new FixedDiagnosticScenario("A06", 20.0, 22.0, 0.2),
            new FixedDiagnosticScenario("A11", 50.0, 55.0, 0.5),
            new FixedDiagnosticScenario("A15", 100.0, 110.0, 0.3, true, 20.0, 0.002, 0.05, 1.0)
        };

    private static (EnvironmentInput, BuoyInput, IReadOnlyList<AssemblyItemInput>, AnchorInput) BuildInputs(
        FixedDiagnosticScenario scenario)
    {
        var environment = new EnvironmentInput(
            1025.0,
            scenario.DepthM,
            0.0,
            0.0,
            0.0,
            new SeabedPreset("campaign:sand", "Validation sand", 1.2, "Validation diagnostic only."),
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
            "Validation diagnostic only.");
        var anchor = new AnchorInput("Series A 3000 kg block", "Concrete block", "Concrete", 3000.0, 1.2, 1.0);

        IReadOnlyList<AssemblyItemInput> assembly;
        if (!scenario.HasPayload)
        {
            assembly = new[] { Line("Main line", scenario.LineLengthM, rope) };
        }
        else
        {
            var upper = scenario.LineLengthM / 2.0;
            assembly = new[]
            {
                Line("Upper line", upper, rope),
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
                Line("Lower line", scenario.LineLengthM - upper, rope)
            };
        }

        return (environment, buoy, assembly, anchor);
    }

    private static AssemblyItemInput Line(string title, double lengthM, RopePreset rope) =>
        new(AssemblyItemKind.Line, title, true, rope, null, lengthM, 1, 0.0, 0.0, 0.0, 0.0);

    private static IReadOnlyList<FixedIterationRow> Diagnose(
        FixedDiagnosticScenario scenario,
        EnvironmentInput environment,
        BuoyInput buoy,
        CalculationResult baseResult,
        MooringSequencePositionResult sequence)
    {
        var boundary = MooringSurfaceBoundaryInfoAnalyzer.Build(environment, buoy, baseResult, sequence);
        if (!boundary.Solved || boundary.SolutionState is null || !boundary.BuoySteadyDragN.HasValue)
            throw new InvalidOperationException($"{scenario.Id}: initial boundary not solved: {boundary.Classification}.");

        var trace = MooringSurfaceBoundaryTensionTraceBuilder.Build(baseResult, sequence, boundary);
        if (!trace.Available)
            throw new InvalidOperationException($"{scenario.Id}: initial trace unavailable: {trace.UnavailableReason}.");

        var state = State(
            baseResult,
            boundary,
            trace,
            Geometry(trace),
            baseResult.SegmentRows.Sum(x => x.CurrentForceN));
        var buoySteadyDragN = boundary.BuoySteadyDragN.Value;
        var seen = new Dictionary<string, int>(StringComparer.Ordinal) { [state.Fingerprint] = 0 };
        var rows = new List<FixedIterationRow>(Budget);

        for (var iteration = 1; iteration <= Budget; iteration++)
        {
            var projection = Projection(state.Trace, state.Geometry);
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
                throw new InvalidOperationException($"{scenario.Id}: boundary failed at iteration {iteration}: {nextBoundary.Classification}.");

            var nextTrace = MooringSurfaceBoundaryTensionTraceBuilder.Build(nextResult, sequence, nextBoundary);
            if (!nextTrace.Available)
                throw new InvalidOperationException($"{scenario.Id}: trace failed at iteration {iteration}: {nextTrace.UnavailableReason}.");

            var next = State(nextResult, nextBoundary, nextTrace, Geometry(nextTrace), nextLineForceN);
            var differentFields = ExactDifferentFieldCount(state, next);

            int? repeatedFrom = null;
            int? cycleLength = null;
            if (seen.TryGetValue(next.Fingerprint, out var previousIteration))
            {
                repeatedFrom = previousIteration;
                cycleLength = iteration - previousIteration;
            }
            else
            {
                seen[next.Fingerprint] = iteration;
            }

            rows.Add(new FixedIterationRow(
                scenario.Id,
                iteration,
                next.Boundary.Q0N!.Value - state.Boundary.Q0N!.Value,
                next.Geometry.EndpointX - state.Geometry.EndpointX,
                next.Geometry.EndpointZ - state.Geometry.EndpointZ,
                next.LineForceN - state.LineForceN,
                MaxSegmentForceDelta(state.Result, next.Result),
                MaxNodeDelta(state.Geometry, next.Geometry),
                MaxTraceScalarDelta(state.Trace, next.Trace),
                differentFields,
                differentFields == 0,
                state.Fingerprint,
                next.Fingerprint,
                repeatedFrom,
                cycleLength));

            state = next;
        }

        return rows;
    }

    private static FixedFeedbackState State(
        CalculationResult result,
        MooringSurfaceBoundaryInfoResult boundary,
        MooringSurfaceBoundaryTensionTraceResult trace,
        FixedFeedbackGeometry geometry,
        double lineForceN)
    {
        var state = new FixedFeedbackState(result, boundary, trace, geometry, lineForceN, string.Empty);
        return state with { Fingerprint = Fingerprint(state) };
    }

    private static FixedFeedbackGeometry Geometry(MooringSurfaceBoundaryTensionTraceResult trace)
    {
        var nodes = new List<(double X, double Z)>(trace.Rows.Count + 1) { (0.0, 0.0) };
        var x = 0.0;
        var z = 0.0;
        foreach (var row in trace.Rows)
        {
            if (!row.TangentX.HasValue || !row.TangentZ.HasValue)
                throw new InvalidOperationException($"Missing tangent at segment {row.SegmentNumber}.");
            var ds = row.EndLengthM - row.StartLengthM;
            x += ds * row.TangentX.Value;
            z += ds * row.TangentZ.Value;
            nodes.Add((x, z));
        }
        return new FixedFeedbackGeometry(nodes, x, z);
    }

    private static MooringShapeProjectionResult Projection(
        MooringSurfaceBoundaryTensionTraceResult trace,
        FixedFeedbackGeometry geometry)
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

            if (!double.IsFinite(projected) || residual > ProductionLengthIdentityToleranceM)
                throw new InvalidOperationException($"Projection identity failed at segment {t.SegmentNumber}: {residual:R} m.");

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
                "INFO: validation-only exact-feedback mirror"));
        }

        var totalSegment = rows.Sum(x => x.SegmentLengthM);
        var totalProjected = rows.Sum(x => x.ProjectedLengthM);
        var totalResidual = Math.Abs(totalProjected - totalSegment);
        return new MooringShapeProjectionResult(
            rows,
            geometry.EndpointX,
            geometry.EndpointZ,
            totalSegment,
            totalProjected,
            totalResidual,
            geometry.EndpointX,
            geometry.EndpointZ,
            0.0,
            0.0,
            rows.Count == 0 ? 0.0 : rows.Max(x => x.AngleFromVerticalDeg),
            rows.Count == 0 ? 0.0 : rows.Average(x => x.AngleFromVerticalDeg),
            totalResidual <= ProductionLengthIdentityToleranceM,
            "Validation-only mirror of production signed projection; not reference truth.");
    }

    private static CalculationResult ApplyShapeForces(
        CalculationResult baseResult,
        CalculationResult currentResult,
        MooringSequencePositionResult sequence,
        MooringShapeForceResult shapeForces,
        double buoySteadyDragN,
        out double lineForceN)
    {
        var bySegment = shapeForces.Rows.ToDictionary(x => x.SegmentNumber);
        var segments = new List<SegmentCalculationRow>(currentResult.SegmentRows.Count);
        foreach (var segment in currentResult.SegmentRows.OrderBy(x => x.Number))
        {
            if (!bySegment.TryGetValue(segment.Number, out var force) || !double.IsFinite(force.ShapeForceN))
                throw new InvalidOperationException($"Missing/non-finite shape force at segment {segment.Number}.");
            segments.Add(segment with { CurrentForceN = Math.Max(0.0, force.ShapeForceN) });
        }

        lineForceN = segments.Sum(x => x.CurrentForceN);
        var totalCurrentForceN = buoySteadyDragN + lineForceN + sequence.DiscreteCurrentForceN;
        return currentResult with
        {
            SegmentRows = segments,
            CurrentForceN = totalCurrentForceN,
            HorizontalForceN = totalCurrentForceN + baseResult.WaveForceN
        };
    }

    private static int ExactDifferentFieldCount(FixedFeedbackState a, FixedFeedbackState b)
    {
        var count = 0;
        CountString(ref count, a.Boundary.Classification.ToString(), b.Boundary.Classification.ToString());
        CountNullableDouble(ref count, a.Boundary.Q0N, b.Boundary.Q0N);
        CountNullableDouble(ref count, a.Boundary.SolutionState?.EndpointXM, b.Boundary.SolutionState?.EndpointXM);
        CountNullableDouble(ref count, a.Boundary.SolutionState?.EndpointZM, b.Boundary.SolutionState?.EndpointZM);
        CountDouble(ref count, a.LineForceN, b.LineForceN);
        CountInt(ref count, a.Trace.PointLoadCrossings, b.Trace.PointLoadCrossings);
        CountInt(ref count, a.Geometry.Nodes.Count, b.Geometry.Nodes.Count);
        CountInt(ref count, a.Result.SegmentRows.Count, b.Result.SegmentRows.Count);
        CountInt(ref count, a.Trace.Rows.Count, b.Trace.Rows.Count);

        for (var i = 0; i < Math.Min(a.Geometry.Nodes.Count, b.Geometry.Nodes.Count); i++)
        {
            CountDouble(ref count, a.Geometry.Nodes[i].X, b.Geometry.Nodes[i].X);
            CountDouble(ref count, a.Geometry.Nodes[i].Z, b.Geometry.Nodes[i].Z);
        }

        var aSegments = a.Result.SegmentRows.OrderBy(x => x.Number).ToList();
        var bSegments = b.Result.SegmentRows.OrderBy(x => x.Number).ToList();
        for (var i = 0; i < Math.Min(aSegments.Count, bSegments.Count); i++)
        {
            CountInt(ref count, aSegments[i].Number, bSegments[i].Number);
            CountDouble(ref count, aSegments[i].CurrentForceN, bSegments[i].CurrentForceN);
        }

        for (var i = 0; i < Math.Min(a.Trace.Rows.Count, b.Trace.Rows.Count); i++)
        {
            var x = a.Trace.Rows[i];
            var y = b.Trace.Rows[i];
            CountInt(ref count, x.SegmentNumber, y.SegmentNumber);
            CountDouble(ref count, x.StartLengthM, y.StartLengthM);
            CountDouble(ref count, x.EndLengthM, y.EndLengthM);
            CountInt(ref count, x.PointLoadCrossingsAppliedBeforeSegment, y.PointLoadCrossingsAppliedBeforeSegment);
            CountDouble(ref count, x.StartHN, y.StartHN);
            CountDouble(ref count, x.StartVN, y.StartVN);
            CountDouble(ref count, x.MidHN, y.MidHN);
            CountDouble(ref count, x.MidVN, y.MidVN);
            CountDouble(ref count, x.EndHN, y.EndHN);
            CountDouble(ref count, x.EndVN, y.EndVN);
            CountDouble(ref count, x.MidTensionN, y.MidTensionN);
            CountNullableDouble(ref count, x.TangentX, y.TangentX);
            CountNullableDouble(ref count, x.TangentZ, y.TangentZ);
        }

        return count;
    }

    private static void CountDouble(ref int count, double a, double b)
    {
        if (a != b) count++;
    }

    private static void CountNullableDouble(ref int count, double? a, double? b)
    {
        if (a != b) count++;
    }

    private static void CountInt(ref int count, int a, int b)
    {
        if (a != b) count++;
    }

    private static void CountString(ref int count, string a, string b)
    {
        if (!string.Equals(a, b, StringComparison.Ordinal)) count++;
    }

    private static double MaxSegmentForceDelta(CalculationResult a, CalculationResult b)
    {
        var x = a.SegmentRows.OrderBy(r => r.Number).ToList();
        var y = b.SegmentRows.OrderBy(r => r.Number).ToList();
        return x.Zip(y, (left, right) => Math.Abs(right.CurrentForceN - left.CurrentForceN))
            .DefaultIfEmpty(0.0)
            .Max();
    }

    private static double MaxNodeDelta(FixedFeedbackGeometry a, FixedFeedbackGeometry b) =>
        a.Nodes.Zip(b.Nodes, (x, y) =>
                Math.Sqrt((y.X - x.X) * (y.X - x.X) + (y.Z - x.Z) * (y.Z - x.Z)))
            .DefaultIfEmpty(0.0)
            .Max();

    private static double MaxTraceScalarDelta(
        MooringSurfaceBoundaryTensionTraceResult a,
        MooringSurfaceBoundaryTensionTraceResult b)
    {
        var max = 0.0;
        foreach (var pair in a.Rows.Zip(b.Rows))
        {
            var x = pair.First;
            var y = pair.Second;
            Take(ref max, x.StartHN, y.StartHN);
            Take(ref max, x.StartVN, y.StartVN);
            Take(ref max, x.MidHN, y.MidHN);
            Take(ref max, x.MidVN, y.MidVN);
            Take(ref max, x.EndHN, y.EndHN);
            Take(ref max, x.EndVN, y.EndVN);
            Take(ref max, x.MidTensionN, y.MidTensionN);
            TakeNullable(ref max, x.TangentX, y.TangentX);
            TakeNullable(ref max, x.TangentZ, y.TangentZ);
        }
        return max;
    }

    private static void Take(ref double max, double a, double b) =>
        max = Math.Max(max, Math.Abs(b - a));

    private static void TakeNullable(ref double max, double? a, double? b)
    {
        if (a.HasValue && b.HasValue)
            Take(ref max, a.Value, b.Value);
        else if (a.HasValue != b.HasValue)
            max = double.PositiveInfinity;
    }

    private static string Fingerprint(FixedFeedbackState state)
    {
        var sb = new StringBuilder();
        sb.Append(state.Boundary.Classification).Append('|');
        AppendNullableDouble(sb, state.Boundary.Q0N);
        AppendNullableDouble(sb, state.Boundary.SolutionState?.EndpointXM);
        AppendNullableDouble(sb, state.Boundary.SolutionState?.EndpointZM);
        AppendDouble(sb, state.LineForceN);
        AppendInt(sb, state.Trace.PointLoadCrossings);

        foreach (var node in state.Geometry.Nodes)
        {
            AppendDouble(sb, node.X);
            AppendDouble(sb, node.Z);
        }
        foreach (var segment in state.Result.SegmentRows.OrderBy(x => x.Number))
        {
            AppendInt(sb, segment.Number);
            AppendDouble(sb, segment.CurrentForceN);
        }
        foreach (var row in state.Trace.Rows)
        {
            AppendInt(sb, row.SegmentNumber);
            AppendDouble(sb, row.StartLengthM);
            AppendDouble(sb, row.EndLengthM);
            AppendInt(sb, row.PointLoadCrossingsAppliedBeforeSegment);
            AppendDouble(sb, row.StartHN);
            AppendDouble(sb, row.StartVN);
            AppendDouble(sb, row.MidHN);
            AppendDouble(sb, row.MidVN);
            AppendDouble(sb, row.EndHN);
            AppendDouble(sb, row.EndVN);
            AppendDouble(sb, row.MidTensionN);
            AppendNullableDouble(sb, row.TangentX);
            AppendNullableDouble(sb, row.TangentZ);
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())))[..16];
    }

    private static void AppendDouble(StringBuilder sb, double value) =>
        sb.Append(BitConverter.DoubleToInt64Bits(value).ToString("X16", CultureInfo.InvariantCulture)).Append('|');

    private static void AppendNullableDouble(StringBuilder sb, double? value)
    {
        if (value.HasValue) AppendDouble(sb, value.Value);
        else sb.Append("null|");
    }

    private static void AppendInt(StringBuilder sb, int value) =>
        sb.Append(value.ToString(CultureInfo.InvariantCulture)).Append('|');

    private static void WriteCsv(string path, IReadOnlyList<FixedIterationRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Scenario,Iteration,DeltaQ0N,DeltaX,DeltaZ,DeltaLineForceN,MaxSegmentForceDeltaN,MaxNodeDeltaM,MaxTraceScalarDelta,ExactDifferentFieldCount,ExactFixedPoint,CurrentFingerprint,NextFingerprint,RepeatedFromStateIteration,CycleLength");
        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                r.Scenario,
                r.Iteration.ToString(CultureInfo.InvariantCulture),
                F(r.DeltaQ0N),
                F(r.DeltaEndpointXM),
                F(r.DeltaEndpointZM),
                F(r.DeltaLineForceN),
                F(r.MaxSegmentForceDeltaN),
                F(r.MaxNodeDeltaM),
                F(r.MaxTraceScalarDelta),
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

    private static void WriteMarkdown(
        string path,
        IReadOnlyList<FixedIterationRow> rows,
        IReadOnlyList<string> summaries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Series A exact-feedback budget diagnostics");
        sb.AppendLine();
        sb.AppendLine("Validation-only mirror of the production public feedback stages for A06/A11/A15. No solver/physics changes and no convergence epsilon are introduced.");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        foreach (var summary in summaries) sb.AppendLine("- " + summary);

        foreach (var group in rows.GroupBy(x => x.Scenario))
        {
            sb.AppendLine();
            sb.AppendLine($"## {group.Key} — last 12 iterations");
            sb.AppendLine();
            sb.AppendLine("| i | dQ0 N | dX m | dZ m | dLine N | max seg dN | max node dm | max trace delta | diff fields | repeated state |");
            sb.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
            foreach (var r in group.TakeLast(12))
            {
                var repeated = r.CycleLength.HasValue
                    ? $"state {r.RepeatedFromStateIteration}, len {r.CycleLength}"
                    : "—";
                sb.AppendLine($"| {r.Iteration} | {F(r.DeltaQ0N)} | {F(r.DeltaEndpointXM)} | {F(r.DeltaEndpointZM)} | {F(r.DeltaLineForceN)} | {F(r.MaxSegmentForceDeltaN)} | {F(r.MaxNodeDeltaM)} | {F(r.MaxTraceScalarDelta)} | {r.ExactDifferentFieldCount} | {repeated} |");
            }
        }

        sb.AppendLine();
        sb.AppendLine("A repeated exact-bit fingerprint proves a discrete state cycle. Small non-zero deltas without repetition prove continued floating-state evolution only; they do not authorize an epsilon-based production acceptance rule.");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
