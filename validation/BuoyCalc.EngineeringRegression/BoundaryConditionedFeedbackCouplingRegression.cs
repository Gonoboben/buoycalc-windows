using System.Globalization;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class BoundaryConditionedFeedbackCouplingRegression
{
    private const double GravityMS2 = 9.80665;
    private const double UnitVectorTolerance = 1e-10;
    private const double GeometryToleranceM = 1e-9;
    private const double ForceToleranceN = 1e-6;
    private static readonly int[] Budgets = { 4, 8, 16, 32, 64 };

    public static void Validate()
    {
        foreach (var scenario in SurfaceBoundaryCanonicalMeasurementRegression.BuildCanonicalScenarios())
        {
            var run = ApplicationCalculationRunner.Run(
                scenario.Environment,
                scenario.Buoy,
                scenario.Assembly,
                scenario.Anchor,
                scenario.SafetyFactor);

            MeasureScenario(
                scenario.Label,
                scenario.Environment,
                scenario.Buoy,
                run.Result,
                run.Snapshot,
                requireInitialSolved: true);
        }

        MeasureControlledBuoyantScenario();
    }

    private static void MeasureControlledBuoyantScenario()
    {
        var seabed = new SeabedPreset(
            "feedback-buoyant:sand",
            "Feedback buoyant sand",
            1.2,
            "Deterministic validation-only seabed.");
        var buoy = new BuoyInput(
            "Feedback buoyant buoy",
            1.0,
            100.0,
            0.8,
            0.8);
        var rope = new RopePreset(
            "feedback-buoyant:line",
            "Feedback buoyant line",
            "Synthetic buoyant",
            20.0,
            100.0,
            -0.05,
            1.2,
            "Negative signed water weight is intentional for validation.");
        var anchor = new AnchorInput(
            "Feedback buoyant anchor",
            "Concrete block",
            "Concrete",
            1000.0,
            0.4,
            1.0);
        var environment = new EnvironmentInput(
            1025.0,
            30.0,
            0.0,
            0.0,
            0.0,
            seabed,
            true,
            new[]
            {
                new CurrentProfilePointInput(0.0, 0.3, 0.0, 0.0, 1025.0),
                new CurrentProfilePointInput(30.0, 0.3, 0.0, 0.0, 1025.0)
            });
        var assembly = new[]
        {
            new AssemblyItemInput(
                AssemblyItemKind.Line,
                "Buoyant line",
                true,
                rope,
                null,
                30.0,
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

        MeasureScenario(
            "buoyant-line",
            environment,
            buoy,
            run.Result,
            run.Snapshot,
            requireInitialSolved: false);
    }

    private static void MeasureScenario(
        string label,
        EnvironmentInput environment,
        BuoyInput buoy,
        CalculationResult baseResult,
        CalculationSnapshot snapshot,
        bool requireInitialSolved)
    {
        var data = snapshot.TechnicalReportData;
        var sequence = data.SequencePositions;
        var initialBoundary = data.SurfaceBoundaryInfo;
        var initialTrace = data.SurfaceBoundaryTensionTrace;
        var selectedX = snapshot.SelectedShape?.Shape.HorizontalOffsetM;
        var candidateB = data.SignedNodeEquilibrium;

        Console.WriteLine(string.Join("|",
            "BOUNDARY_FEEDBACK_SCENARIO",
            label,
            $"InitialClass={initialBoundary.Classification}",
            $"InitialSolved={initialBoundary.Solved}",
            $"InitialX={Format(initialBoundary.SolutionState?.EndpointXM)}",
            $"InitialZ={Format(initialBoundary.SolutionState?.EndpointZM)}",
            $"InitialQ0N={Format(initialBoundary.Q0N)}",
            $"SelectedX={Format(selectedX)}",
            $"HistoricalCandidateBMaxResidualN={Format(candidateB.MaxResidualN)}",
            $"HistoricalCandidateBMaxRelativeResidual={Format(candidateB.MaxRelativeResidual)}"));

        if (!initialBoundary.Solved ||
            initialBoundary.SolutionState is null ||
            !initialBoundary.Q0N.HasValue ||
            !initialBoundary.BuoySteadyDragN.HasValue ||
            !initialTrace.Available)
        {
            if (requireInitialSolved)
            {
                throw new InvalidOperationException(
                    $"Boundary feedback {label}: canonical initial boundary must be solved; got {initialBoundary.Classification}, trace available={initialTrace.Available}.");
            }

            Console.WriteLine(string.Join("|",
                "BOUNDARY_FEEDBACK_TERMINAL",
                label,
                "Budget=0",
                "Iteration=0",
                $"Reason=InitialBoundary:{initialBoundary.Classification}"));
            return;
        }

        ValidateTraceContract(label, "initial", initialBoundary, initialTrace, sequence, baseResult.SegmentRows.Count);

        foreach (var budget in Budgets)
        {
            var outcome = RunBudget(
                label,
                budget,
                environment,
                buoy,
                baseResult,
                sequence,
                initialBoundary,
                initialTrace,
                emitIterations: budget == Budgets[^1]);

            Console.WriteLine(string.Join("|",
                "BOUNDARY_FEEDBACK_BUDGET",
                label,
                $"Budget={budget}",
                $"Iterations={outcome.Iterations}",
                $"Stop={outcome.StopReason}",
                $"X={Format(outcome.EndpointXM)}",
                $"Z={Format(outcome.EndpointZM)}",
                $"Q0N={Format(outcome.Q0N)}",
                $"DeltaX={Format(outcome.LastDeltaXM)}",
                $"DeltaZ={Format(outcome.LastDeltaZM)}",
                $"DeltaQ0N={Format(outcome.LastDeltaQ0N)}",
                $"MaxNodeDeltaM={Format(outcome.LastMaxNodeDeltaM)}",
                $"LineForceN={Format(outcome.LineForceN)}",
                $"DeltaLineForceN={Format(outcome.LastDeltaLineForceN)}",
                $"MaxSegmentForceDeltaN={Format(outcome.LastMaxSegmentForceDeltaN)}",
                $"DepthResidualM={Format(outcome.DepthResidualM)}",
                $"NegativeDz={outcome.NegativeDzSegmentCount}",
                $"PointLoads={outcome.PointLoadCrossings}",
                $"MaxPointJumpResidualN={Format(outcome.MaxPointJumpResidualN)}"));
        }
    }

    private static BudgetOutcome RunBudget(
        string label,
        int budget,
        EnvironmentInput environment,
        BuoyInput buoy,
        CalculationResult baseResult,
        MooringSequencePositionResult sequence,
        MooringSurfaceBoundaryInfoResult initialBoundary,
        MooringSurfaceBoundaryTensionTraceResult initialTrace,
        bool emitIterations)
    {
        var currentResult = baseResult;
        var currentBoundary = initialBoundary;
        var currentTrace = initialTrace;
        var currentGeometry = BuildGeometry(currentTrace, label + $" budget {budget} initial");
        var currentLineForceN = currentResult.SegmentRows.Sum(x => x.CurrentForceN);
        var buoySteadyDragN = initialBoundary.BuoySteadyDragN!.Value;
        var stopReason = "BudgetReached";

        double? lastDeltaX = null;
        double? lastDeltaZ = null;
        double? lastDeltaQ0 = null;
        double? lastMaxNodeDelta = null;
        double? lastDeltaLineForce = null;
        double? lastMaxSegmentForceDelta = null;
        double? lastDepthResidual = currentBoundary.TargetDepthM.HasValue
            ? currentGeometry.EndpointZM - currentBoundary.TargetDepthM.Value
            : null;
        var lastJumpResidual = ValidatePointJumpClosure(currentTrace, sequence, label + $" budget {budget} initial");
        var iterations = 0;

        for (var iteration = 1; iteration <= budget; iteration++)
        {
            var projection = BuildProjection(currentTrace, currentGeometry, label + $" budget {budget} iteration {iteration}");
            var shapeForces = MooringShapeForceAnalyzer.Build(currentResult, projection);
            if (shapeForces.Rows.Count != currentResult.SegmentRows.Count)
            {
                throw new InvalidOperationException(
                    $"Boundary feedback {label}: shape-force row count {shapeForces.Rows.Count} != segment count {currentResult.SegmentRows.Count} at budget {budget}, iteration {iteration}.");
            }

            var forceBySegment = shapeForces.Rows.ToDictionary(x => x.SegmentNumber);
            var updatedSegments = new List<SegmentCalculationRow>(currentResult.SegmentRows.Count);
            var maxSegmentForceDeltaN = 0.0;

            foreach (var segment in currentResult.SegmentRows.OrderBy(x => x.Number))
            {
                if (!forceBySegment.TryGetValue(segment.Number, out var force))
                {
                    throw new InvalidOperationException(
                        $"Boundary feedback {label}: missing shape-force mapping for segment {segment.Number} at budget {budget}, iteration {iteration}.");
                }

                if (!double.IsFinite(force.ShapeForceN) || force.ShapeForceN < -ForceToleranceN)
                {
                    throw new InvalidOperationException(
                        $"Boundary feedback {label}: invalid ShapeForceN={force.ShapeForceN:R} on segment {segment.Number} at budget {budget}, iteration {iteration}.");
                }

                maxSegmentForceDeltaN = Math.Max(
                    maxSegmentForceDeltaN,
                    Math.Abs(force.ShapeForceN - segment.CurrentForceN));
                updatedSegments.Add(segment with { CurrentForceN = force.ShapeForceN });
            }

            var updatedLineForceN = updatedSegments.Sum(x => x.CurrentForceN);
            var updatedTotalCurrentForceN =
                buoySteadyDragN +
                updatedLineForceN +
                sequence.DiscreteCurrentForceN;
            var nextResult = currentResult with
            {
                SegmentRows = updatedSegments,
                CurrentForceN = updatedTotalCurrentForceN,
                HorizontalForceN = updatedTotalCurrentForceN + baseResult.WaveForceN
            };

            var nextBoundary = MooringSurfaceBoundaryInfoAnalyzer.Build(
                environment,
                buoy,
                nextResult,
                sequence);

            iterations = iteration;
            lastDeltaLineForce = updatedLineForceN - currentLineForceN;
            lastMaxSegmentForceDelta = maxSegmentForceDeltaN;

            if (!nextBoundary.Solved ||
                nextBoundary.SolutionState is null ||
                !nextBoundary.Q0N.HasValue ||
                !nextBoundary.BuoySteadyDragN.HasValue)
            {
                stopReason = "Boundary:" + nextBoundary.Classification;
                if (emitIterations)
                {
                    Console.WriteLine(string.Join("|",
                        "BOUNDARY_FEEDBACK_ITER",
                        label,
                        $"Budget={budget}",
                        $"i={iteration}",
                        $"Stop={stopReason}",
                        $"LineForceN={Format(updatedLineForceN)}",
                        $"DeltaLineForceN={Format(lastDeltaLineForce)}",
                        $"MaxSegmentForceDeltaN={Format(lastMaxSegmentForceDelta)}"));
                }
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

            ValidateTraceContract(
                label,
                $"budget {budget} iteration {iteration}",
                nextBoundary,
                nextTrace,
                sequence,
                nextResult.SegmentRows.Count);

            var nextGeometry = BuildGeometry(
                nextTrace,
                label + $" budget {budget} iteration {iteration}");
            var maxNodeDeltaM = MaxNodeDelta(currentGeometry, nextGeometry, label, budget, iteration);
            var deltaX = nextGeometry.EndpointXM - currentGeometry.EndpointXM;
            var deltaZ = nextGeometry.EndpointZM - currentGeometry.EndpointZM;
            var deltaQ0 = nextBoundary.Q0N.Value - currentBoundary.Q0N!.Value;
            var depthResidual = nextBoundary.TargetDepthM.HasValue
                ? nextGeometry.EndpointZM - nextBoundary.TargetDepthM.Value
                : (double?)null;
            var jumpResidual = ValidatePointJumpClosure(
                nextTrace,
                sequence,
                label + $" budget {budget} iteration {iteration}");

            lastDeltaX = deltaX;
            lastDeltaZ = deltaZ;
            lastDeltaQ0 = deltaQ0;
            lastMaxNodeDelta = maxNodeDeltaM;
            lastDepthResidual = depthResidual;
            lastJumpResidual = jumpResidual;

            if (emitIterations)
            {
                var top = nextTrace.Rows[0];
                var middle = nextTrace.Rows[nextTrace.Rows.Count / 2];
                var bottom = nextTrace.Rows[^1];
                Console.WriteLine(string.Join("|",
                    "BOUNDARY_FEEDBACK_ITER",
                    label,
                    $"Budget={budget}",
                    $"i={iteration}",
                    $"Class={nextBoundary.Classification}",
                    $"Q0N={Format(nextBoundary.Q0N)}",
                    $"X={Format(nextGeometry.EndpointXM)}",
                    $"Z={Format(nextGeometry.EndpointZM)}",
                    $"DeltaX={Format(deltaX)}",
                    $"DeltaZ={Format(deltaZ)}",
                    $"DeltaQ0N={Format(deltaQ0)}",
                    $"LineForceN={Format(updatedLineForceN)}",
                    $"DeltaLineForceN={Format(lastDeltaLineForce)}",
                    $"MaxSegmentForceDeltaN={Format(maxSegmentForceDeltaN)}",
                    $"MaxNodeDeltaM={Format(maxNodeDeltaM)}",
                    $"DepthResidualM={Format(depthResidual)}",
                    $"NegativeDz={nextGeometry.NegativeDzSegmentCount}",
                    $"PointLoads={nextTrace.PointLoadCrossings}",
                    $"MaxPointJumpResidualN={Format(jumpResidual)}",
                    $"Top={FormatCut(top)}",
                    $"Mid={FormatCut(middle)}",
                    $"Bottom={FormatCut(bottom)}",
                    "Stop=Continue"));
            }

            currentResult = nextResult;
            currentBoundary = nextBoundary;
            currentTrace = nextTrace;
            currentGeometry = nextGeometry;
            currentLineForceN = updatedLineForceN;
        }

        return new BudgetOutcome(
            iterations,
            stopReason,
            currentGeometry.EndpointXM,
            currentGeometry.EndpointZM,
            currentBoundary.Q0N,
            lastDeltaX,
            lastDeltaZ,
            lastDeltaQ0,
            lastMaxNodeDelta,
            currentLineForceN,
            lastDeltaLineForce,
            lastMaxSegmentForceDelta,
            lastDepthResidual,
            currentGeometry.NegativeDzSegmentCount,
            currentTrace.PointLoadCrossings,
            lastJumpResidual);
    }

    private static void ValidateTraceContract(
        string label,
        string stage,
        MooringSurfaceBoundaryInfoResult boundary,
        MooringSurfaceBoundaryTensionTraceResult trace,
        MooringSequencePositionResult sequence,
        int segmentCount)
    {
        if (!boundary.Solved || boundary.SolutionState is null)
            throw new InvalidOperationException($"Boundary feedback {label} {stage}: solved boundary state missing.");
        if (!trace.Available)
            throw new InvalidOperationException($"Boundary feedback {label} {stage}: trace unavailable: {trace.UnavailableReason}");
        if (trace.Rows.Count != segmentCount)
            throw new InvalidOperationException($"Boundary feedback {label} {stage}: trace rows {trace.Rows.Count} != segments {segmentCount}.");
        if (trace.PointLoadCrossings != boundary.SolutionState.PointLoadCrossings)
            throw new InvalidOperationException($"Boundary feedback {label} {stage}: trace/boundary point-load crossing count mismatch.");

        Near(boundary.SolutionState.EndHN, Require(trace.EndHN, label + " terminal H"), ForceToleranceN, label + " " + stage + " terminal H");
        Near(boundary.SolutionState.EndVN, Require(trace.EndVN, label + " terminal V"), ForceToleranceN, label + " " + stage + " terminal V");

        var previousCrossings = 0;
        foreach (var row in trace.Rows)
        {
            if (row.PointLoadCrossingsAppliedBeforeSegment < previousCrossings ||
                row.PointLoadCrossingsAppliedBeforeSegment > trace.PointLoadCrossings)
            {
                throw new InvalidOperationException(
                    $"Boundary feedback {label} {stage}: non-monotone point-load crossing count at segment {row.SegmentNumber}.");
            }
            previousCrossings = row.PointLoadCrossingsAppliedBeforeSegment;

            if (!row.TangentX.HasValue || !row.TangentZ.HasValue)
            {
                throw new InvalidOperationException(
                    $"Boundary feedback {label} {stage}: indeterminate tangent at segment {row.SegmentNumber}.");
            }

            var tx = row.TangentX.Value;
            var tz = row.TangentZ.Value;
            if (!double.IsFinite(tx) || !double.IsFinite(tz))
            {
                throw new InvalidOperationException(
                    $"Boundary feedback {label} {stage}: non-finite tangent at segment {row.SegmentNumber}.");
            }
            Near(1.0, tx * tx + tz * tz, UnitVectorTolerance, label + " " + stage + $" tangent norm segment {row.SegmentNumber}");
        }

        var expectedPointCount = InternalPoints(sequence).Count;
        if (trace.PointLoadCrossings != expectedPointCount)
        {
            throw new InvalidOperationException(
                $"Boundary feedback {label} {stage}: trace crossed {trace.PointLoadCrossings} points, expected {expectedPointCount}.");
        }
    }
    private static Geometry BuildGeometry(
        MooringSurfaceBoundaryTensionTraceResult trace,
        string label)
    {
        var nodes = new List<GeometryNode>(trace.Rows.Count + 1)
        {
            new(0.0, 0.0)
        };
        var x = 0.0;
        var z = 0.0;
        var negativeDz = 0;

        foreach (var row in trace.Rows)
        {
            var ds = row.EndLengthM - row.StartLengthM;
            if (!double.IsFinite(ds) || ds < -GeometryToleranceM)
            {
                throw new InvalidOperationException(
                    $"Boundary feedback {label}: invalid ds={ds:R} at segment {row.SegmentNumber}.");
            }

            var tx = Require(row.TangentX, label + $" tangent X segment {row.SegmentNumber}");
            var tz = Require(row.TangentZ, label + $" tangent Z segment {row.SegmentNumber}");
            var dx = ds * tx;
            var dz = ds * tz;
            if (!double.IsFinite(dx) || !double.IsFinite(dz))
            {
                throw new InvalidOperationException(
                    $"Boundary feedback {label}: non-finite geometry increment at segment {row.SegmentNumber}.");
            }

            x += dx;
            z += dz;
            if (dz < -GeometryToleranceM)
                negativeDz++;
            nodes.Add(new GeometryNode(x, z));
        }

        return new Geometry(nodes, x, z, negativeDz);
    }

    private static MooringShapeProjectionResult BuildProjection(
        MooringSurfaceBoundaryTensionTraceResult trace,
        Geometry geometry,
        string label)
    {
        if (geometry.Nodes.Count != trace.Rows.Count + 1)
        {
            throw new InvalidOperationException(
                $"Boundary feedback {label}: geometry node count does not match trace rows.");
        }

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
            var lengthResidual = Math.Abs(projectedLength - ds);
            if (lengthResidual > GeometryToleranceM)
            {
                throw new InvalidOperationException(
                    $"Boundary feedback {label}: projection length residual {lengthResidual:R} m at segment {traceRow.SegmentNumber}.");
            }

            var displayAngleDeg = Math.Atan2(
                Math.Abs(dx),
                Math.Max(1e-12, Math.Abs(dz))) * 180.0 / Math.PI;

            rows.Add(new MooringShapeProjectionRow(
                i + 1,
                traceRow.SegmentNumber,
                traceRow.SourceElement,
                ds,
                dx,
                dz,
                projectedLength,
                lengthResidual,
                displayAngleDeg,
                traceRow.MidTensionN / 1000.0,
                "INFO: validation-only boundary-conditioned signed projection"));
        }

        var totalSegmentLength = rows.Sum(x => x.SegmentLengthM);
        var totalProjectedLength = rows.Sum(x => x.ProjectedLengthM);
        var totalLengthResidual = Math.Abs(totalProjectedLength - totalSegmentLength);
        var maxAngle = rows.Count > 0 ? rows.Max(x => x.AngleFromVerticalDeg) : 0.0;
        var averageAngle = rows.Count > 0 ? rows.Average(x => x.AngleFromVerticalDeg) : 0.0;

        return new MooringShapeProjectionResult(
            rows,
            geometry.EndpointXM,
            geometry.EndpointZM,
            totalSegmentLength,
            totalProjectedLength,
            totalLengthResidual,
            geometry.EndpointXM,
            geometry.EndpointZM,
            0.0,
            0.0,
            maxAngle,
            averageAngle,
            totalLengthResidual <= GeometryToleranceM,
            "Validation-only boundary-conditioned signed projection. Signed dx/dz are authoritative; scalar angle is display-only.");
    }

    private static double MaxNodeDelta(
        Geometry previous,
        Geometry next,
        string label,
        int budget,
        int iteration)
    {
        if (previous.Nodes.Count != next.Nodes.Count)
        {
            throw new InvalidOperationException(
                $"Boundary feedback {label}: node count changed at budget {budget}, iteration {iteration}.");
        }

        var max = 0.0;
        for (var i = 0; i < previous.Nodes.Count; i++)
        {
            var dx = next.Nodes[i].XM - previous.Nodes[i].XM;
            var dz = next.Nodes[i].ZM - previous.Nodes[i].ZM;
            var delta = Math.Sqrt(dx * dx + dz * dz);
            if (!double.IsFinite(delta))
            {
                throw new InvalidOperationException(
                    $"Boundary feedback {label}: non-finite node delta at node {i}, budget {budget}, iteration {iteration}.");
            }
            max = Math.Max(max, delta);
        }
        return max;
    }

    private static double ValidatePointJumpClosure(
        MooringSurfaceBoundaryTensionTraceResult trace,
        MooringSequencePositionResult sequence,
        string label)
    {
        var points = InternalPoints(sequence);
        var pointIndex = 0;
        var previousH = Require(trace.StartHN, label + " trace start H");
        var previousV = Require(trace.StartVN, label + " trace start V");
        var previousCrossings = 0;
        var maxResidualN = 0.0;

        foreach (var row in trace.Rows)
        {
            var targetCrossings = row.PointLoadCrossingsAppliedBeforeSegment;
            if (targetCrossings < previousCrossings || targetCrossings > points.Count)
            {
                throw new InvalidOperationException(
                    $"Boundary feedback {label}: invalid target crossing count {targetCrossings} at segment {row.SegmentNumber}.");
            }

            var expectedDeltaH = 0.0;
            var expectedDeltaV = 0.0;
            while (pointIndex < targetCrossings)
            {
                expectedDeltaH += points[pointIndex].CurrentForceN;
                expectedDeltaV -= points[pointIndex].WeightWaterKg * GravityMS2;
                pointIndex++;
            }

            var actualDeltaH = row.StartHN - previousH;
            var actualDeltaV = row.StartVN - previousV;
            var residualN = Math.Sqrt(
                (actualDeltaH - expectedDeltaH) * (actualDeltaH - expectedDeltaH) +
                (actualDeltaV - expectedDeltaV) * (actualDeltaV - expectedDeltaV));
            maxResidualN = Math.Max(maxResidualN, residualN);

            previousH = row.EndHN;
            previousV = row.EndVN;
            previousCrossings = targetCrossings;
        }

        var terminalDeltaH = 0.0;
        var terminalDeltaV = 0.0;
        while (pointIndex < points.Count)
        {
            terminalDeltaH += points[pointIndex].CurrentForceN;
            terminalDeltaV -= points[pointIndex].WeightWaterKg * GravityMS2;
            pointIndex++;
        }

        var terminalH = Require(trace.EndHN, label + " trace end H");
        var terminalV = Require(trace.EndVN, label + " trace end V");
        var terminalResidualN = Math.Sqrt(
            ((terminalH - previousH) - terminalDeltaH) * ((terminalH - previousH) - terminalDeltaH) +
            ((terminalV - previousV) - terminalDeltaV) * ((terminalV - previousV) - terminalDeltaV));
        maxResidualN = Math.Max(maxResidualN, terminalResidualN);

        if (pointIndex != trace.PointLoadCrossings || pointIndex != points.Count)
        {
            throw new InvalidOperationException(
                $"Boundary feedback {label}: point-load closure consumed {pointIndex} points, trace reports {trace.PointLoadCrossings}, expected {points.Count}.");
        }
        if (!double.IsFinite(maxResidualN) || maxResidualN > ForceToleranceN)
        {
            throw new InvalidOperationException(
                $"Boundary feedback {label}: point-load jump residual {maxResidualN:R} N exceeds {ForceToleranceN:R} N.");
        }

        return maxResidualN;
    }

    private static IReadOnlyList<MooringSequencePositionRow> InternalPoints(
        MooringSequencePositionResult sequence)
    {
        var ordered = sequence.Rows.OrderBy(x => x.Number).ToList();
        if (ordered.Count < 2)
            return Array.Empty<MooringSequencePositionRow>();

        var topNumber = ordered[0].Number;
        var bottomNumber = ordered[^1].Number;
        return ordered
            .Where(x => x.IsDiscrete && x.Number != topNumber && x.Number != bottomNumber)
            .OrderBy(x => x.PositionAlongLineM)
            .ThenBy(x => x.Number)
            .ToList();
    }

    private static string FormatCut(MooringSurfaceBoundaryTensionTraceRow row)
    {
        return string.Join(",",
            $"n={row.SegmentNumber}",
            $"H={Format(row.MidHN)}",
            $"V={Format(row.MidVN)}",
            $"tx={Format(row.TangentX)}",
            $"tz={Format(row.TangentZ)}");
    }

    private static double Require(double? value, string label)
    {
        return value ?? throw new InvalidOperationException($"Boundary feedback: missing {label}.");
    }

    private static void Near(double expected, double actual, double tolerance, string label)
    {
        if (!double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"Boundary feedback {label}: expected {expected:R}, got {actual:R}, tolerance {tolerance:R}.");
        }
    }

    private static string Format(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);

    private static string Format(double? value) =>
        value.HasValue ? Format(value.Value) : "n/a";

    private sealed record GeometryNode(double XM, double ZM);

    private sealed record Geometry(
        IReadOnlyList<GeometryNode> Nodes,
        double EndpointXM,
        double EndpointZM,
        int NegativeDzSegmentCount);

    private sealed record BudgetOutcome(
        int Iterations,
        string StopReason,
        double EndpointXM,
        double EndpointZM,
        double? Q0N,
        double? LastDeltaXM,
        double? LastDeltaZM,
        double? LastDeltaQ0N,
        double? LastMaxNodeDeltaM,
        double LineForceN,
        double? LastDeltaLineForceN,
        double? LastMaxSegmentForceDeltaN,
        double? DepthResidualM,
        int NegativeDzSegmentCount,
        int PointLoadCrossings,
        double MaxPointJumpResidualN);
}