using System.Globalization;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class BoundaryFeedbackIndependentReferenceRegression
{
    private const double RhoKgM3 = 1025.0;
    private const double LineLengthM = 100.0;
    private const double TargetDepthM = 80.0;
    private const double CurrentMS = 0.5;
    private const double DiameterM = 0.020;
    private const double DragCoefficient = 1.2;
    private const int FeedbackBudget = 64;
    private const double FixtureTolerance = 1e-9;

    public static void Validate()
    {
        var reference = SolveAnalyticalReference();
        ValidateReference(reference);

        var candidate = RunCandidate();
        ValidateCandidateFixture(candidate);

        var deltaQ0N = candidate.Q0N - reference.Q0N;
        var deltaXM = candidate.XM - reference.XM;
        var deltaZM = candidate.ZM - reference.ZM;
        var deltaEndHN = candidate.EndHN - reference.EndHN;
        var deltaLineForceN = candidate.LineForceN - reference.LineForceN;

        Console.WriteLine(string.Join("|",
            "BOUNDARY_REFERENCE_ANALYTICAL",
            $"kNPerM={Format(reference.KNPerM)}",
            $"Q0N={Format(reference.Q0N)}",
            $"X={Format(reference.XM)}",
            $"Z={Format(reference.ZM)}",
            $"EndHN={Format(reference.EndHN)}",
            $"LineForceN={Format(reference.LineForceN)}",
            $"LengthResidualM={Format(reference.LengthResidualM)}",
            $"DepthResidualM={Format(reference.DepthResidualM)}",
            $"QRootIterations={reference.QRootIterations}",
            $"HRootIterations={reference.HRootIterations}"));

        Console.WriteLine(string.Join("|",
            "BOUNDARY_REFERENCE_CANDIDATE",
            $"Budget={FeedbackBudget}",
            $"Iterations={candidate.Iterations}",
            $"Stop={candidate.StopReason}",
            $"Segments={candidate.SegmentCount}",
            $"Q0N={Format(candidate.Q0N)}",
            $"X={Format(candidate.XM)}",
            $"Z={Format(candidate.ZM)}",
            $"EndHN={Format(candidate.EndHN)}",
            $"LineForceN={Format(candidate.LineForceN)}",
            $"DepthResidualM={Format(candidate.DepthResidualM)}",
            $"LastDeltaX={Format(candidate.LastDeltaXM)}",
            $"LastDeltaZ={Format(candidate.LastDeltaZM)}",
            $"LastDeltaQ0N={Format(candidate.LastDeltaQ0N)}",
            $"LastMaxNodeDeltaM={Format(candidate.LastMaxNodeDeltaM)}",
            $"LastDeltaLineForceN={Format(candidate.LastDeltaLineForceN)}",
            $"NegativeDz={candidate.NegativeDzSegmentCount}",
            $"PointLoads={candidate.PointLoadCrossings}"));

        Console.WriteLine(string.Join("|",
            "BOUNDARY_REFERENCE_COMPARE",
            $"ReferenceQ0N={Format(reference.Q0N)}",
            $"CandidateQ0N={Format(candidate.Q0N)}",
            $"DeltaQ0N={Format(deltaQ0N)}",
            $"RelativeQ0={Format(Relative(deltaQ0N, reference.Q0N))}",
            $"ReferenceX={Format(reference.XM)}",
            $"CandidateX={Format(candidate.XM)}",
            $"DeltaX={Format(deltaXM)}",
            $"RelativeX={Format(Relative(deltaXM, reference.XM))}",
            $"ReferenceZ={Format(reference.ZM)}",
            $"CandidateZ={Format(candidate.ZM)}",
            $"DeltaZ={Format(deltaZM)}",
            $"ReferenceEndHN={Format(reference.EndHN)}",
            $"CandidateEndHN={Format(candidate.EndHN)}",
            $"DeltaEndHN={Format(deltaEndHN)}",
            $"ReferenceLineForceN={Format(reference.LineForceN)}",
            $"CandidateLineForceN={Format(candidate.LineForceN)}",
            $"DeltaLineForceN={Format(deltaLineForceN)}",
            $"CandidateDepthResidualM={Format(candidate.DepthResidualM)}",
            $"CandidateNegativeDz={candidate.NegativeDzSegmentCount}",
            $"CandidatePointLoads={candidate.PointLoadCrossings}"));
    }

    private static AnalyticalReference SolveAnalyticalReference()
    {
        var k = 0.5 * RhoKgM3 * DragCoefficient * DiameterM * CurrentMS * CurrentMS;
        if (!double.IsFinite(k) || k <= 0.0)
            throw new InvalidOperationException("Boundary reference: invalid analytical drag coefficient per unit length.");

        var qLow = 1e-9;
        var qHigh = 1.0;
        var highState = EvaluateForQ(k, qHigh);
        var highExpandCount = 0;
        while (highState.ZM < TargetDepthM && highExpandCount < 80)
        {
            qHigh *= 2.0;
            highState = EvaluateForQ(k, qHigh);
            highExpandCount++;
        }

        var lowState = EvaluateForQ(k, qLow);
        if (!double.IsFinite(lowState.ZM) || lowState.ZM >= TargetDepthM)
            throw new InvalidOperationException("Boundary reference: analytical Q lower bracket does not lie below target depth.");
        if (!double.IsFinite(highState.ZM) || highState.ZM <= TargetDepthM)
            throw new InvalidOperationException("Boundary reference: analytical Q upper bracket does not lie above target depth.");

        const int qIterations = 160;
        AnalyticalAtQ state = highState;
        for (var i = 0; i < qIterations; i++)
        {
            var qMid = 0.5 * (qLow + qHigh);
            state = EvaluateForQ(k, qMid);
            if (state.ZM < TargetDepthM)
                qLow = qMid;
            else
                qHigh = qMid;
        }

        var q0N = 0.5 * (qLow + qHigh);
        state = EvaluateForQ(k, q0N);
        return new AnalyticalReference(
            k,
            q0N,
            state.XM,
            state.ZM,
            state.EndHN,
            state.EndHN,
            state.LengthResidualM,
            state.ZM - TargetDepthM,
            qIterations,
            state.HRootIterations);
    }

    private static AnalyticalAtQ EvaluateForQ(double k, double q0N)
    {
        if (!double.IsFinite(q0N) || q0N <= 0.0)
            throw new InvalidOperationException("Boundary reference: analytical Q0 must be positive and finite.");

        var hLow = 0.0;
        var hHigh = k * LineLengthM;
        const int hIterations = 160;
        for (var i = 0; i < hIterations; i++)
        {
            var hMid = 0.5 * (hLow + hHigh);
            var length = AnalyticalLength(k, q0N, hMid);
            if (length < LineLengthM)
                hLow = hMid;
            else
                hHigh = hMid;
        }

        var h1 = 0.5 * (hLow + hHigh);
        var tension = Math.Sqrt(h1 * h1 + q0N * q0N);
        var xM = (tension * tension * tension - q0N * q0N * q0N) /
                 (3.0 * k * q0N * q0N);
        var zM = (h1 * tension + q0N * q0N * Math.Asinh(h1 / q0N)) /
                 (2.0 * k * q0N);
        var lengthResidual = AnalyticalLength(k, q0N, h1) - LineLengthM;

        return new AnalyticalAtQ(h1, xM, zM, lengthResidual, hIterations);
    }

    private static double AnalyticalLength(double k, double q0N, double h1N)
    {
        return h1N / k + h1N * h1N * h1N / (3.0 * k * q0N * q0N);
    }

    private static CandidateOutcome RunCandidate()
    {
        var seabed = new SeabedPreset(
            "reference-neutral:sand",
            "Reference neutral sand",
            1.0,
            "Validation-only reference fixture.");
        var buoy = new BuoyInput(
            "Reference zero-drag buoy",
            0.1,
            10.0,
            0.0,
            0.8);
        var rope = new RopePreset(
            "reference-neutral:line",
            "Reference neutral line",
            "Synthetic neutral",
            DiameterM * 1000.0,
            100.0,
            0.0,
            DragCoefficient,
            "Validation-only neutral line for independent analytical comparison.");
        var anchor = new AnchorInput(
            "Reference anchor",
            "Concrete block",
            "Concrete",
            1000.0,
            0.4,
            1.0);
        var environment = new EnvironmentInput(
            RhoKgM3,
            TargetDepthM,
            CurrentMS,
            0.0,
            0.0,
            seabed);
        var assembly = new[]
        {
            new AssemblyItemInput(
                AssemblyItemKind.Line,
                "Reference neutral line",
                true,
                rope,
                null,
                LineLengthM,
                1,
                0.0,
                0.0,
                0.0,
                0.0)
        };

        var run = ApplicationCalculationRunner.Run(
            environment,
            buoy,
            assembly,
            anchor,
            3.0);

        var currentResult = run.Result;
        var data = run.Snapshot.TechnicalReportData;
        var sequence = data.SequencePositions;
        var currentBoundary = data.SurfaceBoundaryInfo;
        var currentTrace = data.SurfaceBoundaryTensionTrace;

        if (!currentBoundary.Solved || currentBoundary.SolutionState is null || !currentBoundary.Q0N.HasValue)
        {
            throw new InvalidOperationException(
                $"Boundary reference candidate: initial boundary must solve, got {currentBoundary.Classification}.");
        }
        if (!currentTrace.Available)
            throw new InvalidOperationException("Boundary reference candidate: initial tension trace unavailable.");
        if (currentResult.SegmentRows.Count != 500)
            throw new InvalidOperationException($"Boundary reference candidate: expected 500 production 0.20 m segments, got {currentResult.SegmentRows.Count}.");
        if (currentResult.SegmentRows.Any(x => Math.Abs(x.WeightWaterKg) > FixtureTolerance))
            throw new InvalidOperationException("Boundary reference candidate: neutral-line fixture acquired non-zero signed water weight.");
        if (currentResult.SegmentRows.Any(x => Math.Abs(x.VerticalCurrentMS) > FixtureTolerance))
            throw new InvalidOperationException("Boundary reference candidate: fixture acquired non-zero vertical current.");
        if (Math.Abs(sequence.DiscreteCurrentForceN) > FixtureTolerance)
            throw new InvalidOperationException($"Boundary reference candidate: expected zero internal discrete current force, got {sequence.DiscreteCurrentForceN:R} N.");
        if (!currentBoundary.BuoySteadyDragN.HasValue || Math.Abs(currentBoundary.BuoySteadyDragN.Value) > FixtureTolerance)
            throw new InvalidOperationException($"Boundary reference candidate: expected zero buoy steady drag, got {currentBoundary.BuoySteadyDragN:R} N.");
        if (Math.Abs(currentResult.WaveForceN) > FixtureTolerance)
            throw new InvalidOperationException("Boundary reference candidate: fixture acquired non-zero wave force.");
        if (currentTrace.PointLoadCrossings != 0)
            throw new InvalidOperationException($"Boundary reference candidate: expected no internal point loads, got {currentTrace.PointLoadCrossings}.");

        var currentGeometry = BuildGeometry(currentTrace);
        var currentLineForceN = currentResult.SegmentRows.Sum(x => x.CurrentForceN);
        var buoySteadyDragN = currentBoundary.BuoySteadyDragN.Value;
        double lastDeltaXM = 0.0;
        double lastDeltaZM = 0.0;
        double lastDeltaQ0N = 0.0;
        double lastMaxNodeDeltaM = 0.0;
        double lastDeltaLineForceN = 0.0;
        var stopReason = "BudgetReached";
        var iterations = 0;

        for (var iteration = 1; iteration <= FeedbackBudget; iteration++)
        {
            var projection = BuildProjection(currentTrace, currentGeometry);
            var shapeForces = MooringShapeForceAnalyzer.Build(currentResult, projection);
            if (shapeForces.Rows.Count != currentResult.SegmentRows.Count)
                throw new InvalidOperationException("Boundary reference candidate: shape-force/segment row mismatch.");

            var forceBySegment = shapeForces.Rows.ToDictionary(x => x.SegmentNumber);
            var updatedSegments = new List<SegmentCalculationRow>(currentResult.SegmentRows.Count);
            foreach (var segment in currentResult.SegmentRows.OrderBy(x => x.Number))
            {
                if (!forceBySegment.TryGetValue(segment.Number, out var force) ||
                    !double.IsFinite(force.ShapeForceN) || force.ShapeForceN < -1e-9)
                {
                    throw new InvalidOperationException($"Boundary reference candidate: invalid shape force at segment {segment.Number}.");
                }
                updatedSegments.Add(segment with { CurrentForceN = Math.Max(0.0, force.ShapeForceN) });
            }

            var updatedLineForceN = updatedSegments.Sum(x => x.CurrentForceN);
            var updatedTotalCurrentForceN = buoySteadyDragN + updatedLineForceN + sequence.DiscreteCurrentForceN;
            var nextResult = currentResult with
            {
                SegmentRows = updatedSegments,
                CurrentForceN = updatedTotalCurrentForceN,
                HorizontalForceN = updatedTotalCurrentForceN + run.Result.WaveForceN
            };

            var nextBoundary = MooringSurfaceBoundaryInfoAnalyzer.Build(
                environment,
                buoy,
                nextResult,
                sequence);
            iterations = iteration;
            lastDeltaLineForceN = updatedLineForceN - currentLineForceN;

            if (!nextBoundary.Solved || nextBoundary.SolutionState is null || !nextBoundary.Q0N.HasValue)
            {
                stopReason = "Boundary:" + nextBoundary.Classification;
                break;
            }

            var nextTrace = MooringSurfaceBoundaryTensionTraceBuilder.Build(
                nextResult,
                sequence,
                nextBoundary);
            if (!nextTrace.Available)
            {
                stopReason = "TraceUnavailable:" + nextTrace.UnavailableReason;
                break;
            }
            if (nextTrace.PointLoadCrossings != 0)
                throw new InvalidOperationException("Boundary reference candidate: point-load ownership changed during feedback.");

            var nextGeometry = BuildGeometry(nextTrace);
            lastDeltaXM = nextGeometry.EndpointXM - currentGeometry.EndpointXM;
            lastDeltaZM = nextGeometry.EndpointZM - currentGeometry.EndpointZM;
            lastDeltaQ0N = nextBoundary.Q0N.Value - currentBoundary.Q0N!.Value;
            lastMaxNodeDeltaM = MaxNodeDelta(currentGeometry.Nodes, nextGeometry.Nodes);

            currentResult = nextResult;
            currentBoundary = nextBoundary;
            currentTrace = nextTrace;
            currentGeometry = nextGeometry;
            currentLineForceN = updatedLineForceN;
        }

        if (stopReason != "BudgetReached")
            throw new InvalidOperationException($"Boundary reference candidate: feedback terminated early: {stopReason}.");

        var q0N = Require(currentBoundary.Q0N, "candidate Q0");
        var endHN = Require(currentTrace.EndHN, "candidate end H");
        var depthResidualM = currentGeometry.EndpointZM - TargetDepthM;
        return new CandidateOutcome(
            iterations,
            stopReason,
            currentResult.SegmentRows.Count,
            q0N,
            currentGeometry.EndpointXM,
            currentGeometry.EndpointZM,
            endHN,
            currentLineForceN,
            depthResidualM,
            lastDeltaXM,
            lastDeltaZM,
            lastDeltaQ0N,
            lastMaxNodeDeltaM,
            lastDeltaLineForceN,
            currentGeometry.NegativeDzSegmentCount,
            currentTrace.PointLoadCrossings);
    }

    private static Geometry BuildGeometry(MooringSurfaceBoundaryTensionTraceResult trace)
    {
        var nodes = new List<Node>(trace.Rows.Count + 1) { new(0.0, 0.0) };
        var x = 0.0;
        var z = 0.0;
        var negativeDz = 0;
        foreach (var row in trace.Rows)
        {
            var ds = row.EndLengthM - row.StartLengthM;
            var tx = Require(row.TangentX, $"tangent X segment {row.SegmentNumber}");
            var tz = Require(row.TangentZ, $"tangent Z segment {row.SegmentNumber}");
            if (!double.IsFinite(ds) || ds <= 0.0 || !double.IsFinite(tx) || !double.IsFinite(tz))
                throw new InvalidOperationException($"Boundary reference candidate: invalid trace geometry at segment {row.SegmentNumber}.");

            var norm = tx * tx + tz * tz;
            if (Math.Abs(norm - 1.0) > 1e-10)
                throw new InvalidOperationException($"Boundary reference candidate: non-unit tangent at segment {row.SegmentNumber}.");

            var dx = ds * tx;
            var dz = ds * tz;
            x += dx;
            z += dz;
            if (dz < -FixtureTolerance)
                negativeDz++;
            nodes.Add(new Node(x, z));
        }
        return new Geometry(nodes, x, z, negativeDz);
    }

    private static MooringShapeProjectionResult BuildProjection(
        MooringSurfaceBoundaryTensionTraceResult trace,
        Geometry geometry)
    {
        if (geometry.Nodes.Count != trace.Rows.Count + 1)
            throw new InvalidOperationException("Boundary reference candidate: geometry/trace node mismatch.");

        var rows = new List<MooringShapeProjectionRow>(trace.Rows.Count);
        for (var i = 0; i < trace.Rows.Count; i++)
        {
            var traceRow = trace.Rows[i];
            var start = geometry.Nodes[i];
            var end = geometry.Nodes[i + 1];
            var ds = traceRow.EndLengthM - traceRow.StartLengthM;
            var dx = end.XM - start.XM;
            var dz = end.ZM - start.ZM;
            var projectedLength = Math.Sqrt(dx * dx + dz * dz);
            var residual = Math.Abs(projectedLength - ds);
            if (residual > FixtureTolerance)
                throw new InvalidOperationException($"Boundary reference candidate: projection length residual at segment {traceRow.SegmentNumber}.");

            var angle = Math.Atan2(Math.Abs(dx), Math.Max(1e-12, Math.Abs(dz))) * 180.0 / Math.PI;
            rows.Add(new MooringShapeProjectionRow(
                i + 1,
                traceRow.SegmentNumber,
                traceRow.SourceElement,
                ds,
                dx,
                dz,
                projectedLength,
                residual,
                angle,
                traceRow.MidTensionN / 1000.0,
                "INFO: validation-only independent-reference candidate projection"));
        }

        var totalSegmentLength = rows.Sum(x => x.SegmentLengthM);
        var totalProjectedLength = rows.Sum(x => x.ProjectedLengthM);
        var totalResidual = Math.Abs(totalProjectedLength - totalSegmentLength);
        var maxAngle = rows.Count == 0 ? 0.0 : rows.Max(x => x.AngleFromVerticalDeg);
        var averageAngle = rows.Count == 0 ? 0.0 : rows.Average(x => x.AngleFromVerticalDeg);
        return new MooringShapeProjectionResult(
            rows,
            geometry.EndpointXM,
            geometry.EndpointZM,
            totalSegmentLength,
            totalProjectedLength,
            totalResidual,
            geometry.EndpointXM,
            geometry.EndpointZM,
            0.0,
            0.0,
            maxAngle,
            averageAngle,
            totalResidual <= FixtureTolerance,
            "Validation-only candidate projection for independent analytical reference comparison.");
    }

    private static double MaxNodeDelta(IReadOnlyList<Node> previous, IReadOnlyList<Node> next)
    {
        if (previous.Count != next.Count)
            throw new InvalidOperationException("Boundary reference candidate: node count changed during feedback.");
        var max = 0.0;
        for (var i = 0; i < previous.Count; i++)
        {
            var dx = next[i].XM - previous[i].XM;
            var dz = next[i].ZM - previous[i].ZM;
            max = Math.Max(max, Math.Sqrt(dx * dx + dz * dz));
        }
        return max;
    }

    private static void ValidateReference(AnalyticalReference reference)
    {
        if (!FinitePositive(reference.KNPerM) ||
            !FinitePositive(reference.Q0N) ||
            !FinitePositive(reference.EndHN) ||
            !double.IsFinite(reference.XM) || reference.XM <= 0.0 ||
            !double.IsFinite(reference.ZM) || reference.ZM <= 0.0)
            throw new InvalidOperationException("Boundary reference: analytical result is not finite/positive.");
        if (Math.Abs(reference.LengthResidualM) > 1e-10)
            throw new InvalidOperationException($"Boundary reference: analytical length closure residual {reference.LengthResidualM:R} m.");
        if (Math.Abs(reference.DepthResidualM) > 1e-10)
            throw new InvalidOperationException($"Boundary reference: analytical depth closure residual {reference.DepthResidualM:R} m.");
    }

    private static void ValidateCandidateFixture(CandidateOutcome candidate)
    {
        if (candidate.Iterations != FeedbackBudget || candidate.StopReason != "BudgetReached")
            throw new InvalidOperationException("Boundary reference candidate: fixed validation budget did not complete.");
        if (!FinitePositive(candidate.Q0N) ||
            !FinitePositive(candidate.EndHN) ||
            !FinitePositive(candidate.LineForceN) ||
            !double.IsFinite(candidate.XM) || candidate.XM <= 0.0 ||
            !double.IsFinite(candidate.ZM) || candidate.ZM <= 0.0 ||
            !double.IsFinite(candidate.DepthResidualM))
            throw new InvalidOperationException("Boundary reference candidate: final state is non-finite or non-positive.");
        if (candidate.NegativeDzSegmentCount != 0)
            throw new InvalidOperationException($"Boundary reference candidate: neutral downward fixture has {candidate.NegativeDzSegmentCount} negative-dz segments.");
        if (candidate.PointLoadCrossings != 0)
            throw new InvalidOperationException($"Boundary reference candidate: fixture unexpectedly crossed {candidate.PointLoadCrossings} point loads.");
    }

    private static double Require(double? value, string label) =>
        value ?? throw new InvalidOperationException($"Boundary reference candidate: missing {label}.");

    private static bool FinitePositive(double value) => double.IsFinite(value) && value > 0.0;

    private static double Relative(double delta, double reference) =>
        Math.Abs(reference) > 1e-12 ? delta / reference : 0.0;

    private static string Format(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);

    private sealed record AnalyticalAtQ(
        double EndHN,
        double XM,
        double ZM,
        double LengthResidualM,
        int HRootIterations);

    private sealed record AnalyticalReference(
        double KNPerM,
        double Q0N,
        double XM,
        double ZM,
        double EndHN,
        double LineForceN,
        double LengthResidualM,
        double DepthResidualM,
        int QRootIterations,
        int HRootIterations);

    private sealed record Node(double XM, double ZM);

    private sealed record Geometry(
        IReadOnlyList<Node> Nodes,
        double EndpointXM,
        double EndpointZM,
        int NegativeDzSegmentCount);

    private sealed record CandidateOutcome(
        int Iterations,
        string StopReason,
        int SegmentCount,
        double Q0N,
        double XM,
        double ZM,
        double EndHN,
        double LineForceN,
        double DepthResidualM,
        double LastDeltaXM,
        double LastDeltaZM,
        double LastDeltaQ0N,
        double LastMaxNodeDeltaM,
        double LastDeltaLineForceN,
        int NegativeDzSegmentCount,
        int PointLoadCrossings);
}
