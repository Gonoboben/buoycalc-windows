using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Calculation-core evaluator for the signed boundary/shape-force feedback candidate.
/// It produces candidate truth only. It does not select a production shape source.
/// </summary>
public static class MooringSignedCandidateEvaluator
{
    // Existing Package E validation identity contract. This is not a feedback-convergence tolerance.
    private const double PointLoadClosureToleranceN = 1e-6;
    private const double GeometryToleranceM = MooringSurfaceBoundaryIntegrationKernel.LengthToleranceM;
    private const double GravityMS2 = MooringSurfaceBoundaryIntegrationKernel.GravityMS2;

    public static MooringSignedCandidateResult Build(
        EnvironmentInput? environment,
        BuoyInput? buoy,
        CalculationResult? baseResult,
        MooringSequencePositionResult? sequence)
    {
        if (environment is null || baseResult is null || sequence is null)
        {
            return NonAccepted(
                MooringSignedCandidateStatus.Unavailable,
                boundary: null,
                feedbackIterations: 0,
                pointLoadCrossings: 0,
                "SignedCandidateInputUnavailable",
                "Signed candidate requires environment, calculation result and sequence-position inputs.");
        }

        var currentResult = baseResult;
        var currentBoundary = MooringSurfaceBoundaryInfoAnalyzer.Build(
            environment,
            buoy,
            currentResult,
            sequence);

        if (!IsUniqueSolvedBoundary(currentBoundary))
            return FromBoundary(currentBoundary, 0);

        var currentTrace = MooringSurfaceBoundaryTensionTraceBuilder.Build(
            currentResult,
            sequence,
            currentBoundary);
        if (!TryValidateTrace(
                currentResult,
                sequence,
                currentBoundary,
                currentTrace,
                out var initialTraceDiagnostic))
        {
            return NonAccepted(
                MooringSignedCandidateStatus.RejectedNumerical,
                currentBoundary,
                0,
                SafePointLoadCrossings(currentTrace),
                "SignedCandidateInitialTraceInvalid",
                initialTraceDiagnostic);
        }

        if (!TryBuildFeedbackGeometry(currentTrace, out var currentGeometry, out var initialGeometryDiagnostic))
        {
            return NonAccepted(
                MooringSignedCandidateStatus.RejectedNumerical,
                currentBoundary,
                0,
                currentTrace.PointLoadCrossings,
                "SignedCandidateInitialGeometryInvalid",
                initialGeometryDiagnostic);
        }

        var currentLineForceN = currentResult.SegmentRows.Sum(x => x.CurrentForceN);
        var buoySteadyDragN = currentBoundary.BuoySteadyDragN!.Value;

        for (var iteration = 1; iteration <= MooringSignedCandidateResult.ProductionFeedbackBudget; iteration++)
        {
            if (!TryBuildProjection(currentTrace, currentGeometry, out var projection, out var projectionDiagnostic))
            {
                return NonAccepted(
                    MooringSignedCandidateStatus.RejectedNumerical,
                    currentBoundary,
                    iteration - 1,
                    currentTrace.PointLoadCrossings,
                    "SignedCandidateProjectionInvalid",
                    projectionDiagnostic);
            }

            var shapeForces = MooringShapeForceAnalyzer.Build(currentResult, projection);
            if (!TryApplyShapeForces(
                    baseResult,
                    currentResult,
                    sequence,
                    shapeForces,
                    buoySteadyDragN,
                    out var nextResult,
                    out var updatedLineForceN,
                    out var forceDiagnostic))
            {
                return NonAccepted(
                    MooringSignedCandidateStatus.RejectedNumerical,
                    currentBoundary,
                    iteration - 1,
                    currentTrace.PointLoadCrossings,
                    "SignedCandidateShapeForceInvalid",
                    forceDiagnostic);
            }

            var nextBoundary = MooringSurfaceBoundaryInfoAnalyzer.Build(
                environment,
                buoy,
                nextResult,
                sequence);
            if (!IsUniqueSolvedBoundary(nextBoundary))
                return FromBoundary(nextBoundary, iteration);

            var nextTrace = MooringSurfaceBoundaryTensionTraceBuilder.Build(
                nextResult,
                sequence,
                nextBoundary);
            if (!TryValidateTrace(
                    nextResult,
                    sequence,
                    nextBoundary,
                    nextTrace,
                    out var traceDiagnostic))
            {
                return NonAccepted(
                    MooringSignedCandidateStatus.RejectedNumerical,
                    nextBoundary,
                    iteration,
                    SafePointLoadCrossings(nextTrace),
                    "SignedCandidateTraceInvalid",
                    traceDiagnostic);
            }

            if (!TryBuildFeedbackGeometry(nextTrace, out var nextGeometry, out var geometryDiagnostic))
            {
                return NonAccepted(
                    MooringSignedCandidateStatus.RejectedNumerical,
                    nextBoundary,
                    iteration,
                    nextTrace.PointLoadCrossings,
                    "SignedCandidateGeometryInvalid",
                    geometryDiagnostic);
            }

            if (IsExactFixedPoint(
                    currentResult,
                    currentBoundary,
                    currentTrace,
                    currentGeometry,
                    currentLineForceN,
                    nextResult,
                    nextBoundary,
                    nextTrace,
                    nextGeometry,
                    updatedLineForceN))
            {
                if (!TryBuildAcceptedShape(
                        nextResult,
                        nextBoundary,
                        nextTrace,
                        iteration,
                        out var acceptedShape,
                        out var shapeDiagnostic))
                {
                    return NonAccepted(
                        MooringSignedCandidateStatus.RejectedNumerical,
                        nextBoundary,
                        iteration,
                        nextTrace.PointLoadCrossings,
                        "SignedCandidateAcceptedShapeInvalid",
                        shapeDiagnostic);
                }

                return MooringSignedCandidateResult.CreateAccepted(
                    acceptedShape,
                    nextBoundary,
                    iteration,
                    nextTrace.PointLoadCrossings > 0,
                    nextTrace.PointLoadCrossings,
                    "SignedCandidateExactFixedPoint",
                    $"Signed boundary/shape-force feedback reached an exact deterministic fixed point at iteration {iteration} within the fixed production budget {MooringSignedCandidateResult.ProductionFeedbackBudget}.",
                    nextTrace);
            }

            currentResult = nextResult;
            currentBoundary = nextBoundary;
            currentTrace = nextTrace;
            currentGeometry = nextGeometry;
            currentLineForceN = updatedLineForceN;
        }

        return NonAccepted(
            MooringSignedCandidateStatus.BudgetExhausted,
            currentBoundary,
            MooringSignedCandidateResult.ProductionFeedbackBudget,
            currentTrace.PointLoadCrossings,
            "SignedCandidateExactFixedPointNotReached",
            $"Signed candidate remained physically/numerically valid but did not reach an exact deterministic fixed point within {MooringSignedCandidateResult.ProductionFeedbackBudget} feedback iterations.");
    }

    private static bool IsUniqueSolvedBoundary(MooringSurfaceBoundaryInfoResult boundary) =>
        boundary.Solved &&
        boundary.SolutionState is not null &&
        boundary.Q0N.HasValue &&
        double.IsFinite(boundary.Q0N.Value) &&
        boundary.BuoySteadyDragN.HasValue &&
        double.IsFinite(boundary.BuoySteadyDragN.Value);

    private static MooringSignedCandidateResult FromBoundary(
        MooringSurfaceBoundaryInfoResult boundary,
        int feedbackIterations)
    {
        var status = boundary.Classification switch
        {
            MooringSurfaceBoundaryInfoClassification.UnavailableMissingBuoyInput or
            MooringSurfaceBoundaryInfoClassification.UnavailableMissingBoundaryRows or
            MooringSurfaceBoundaryInfoClassification.InvalidInput
                => MooringSignedCandidateStatus.Unavailable,

            MooringSurfaceBoundaryInfoClassification.LineShorterThanDepth or
            MooringSurfaceBoundaryInfoClassification.TautNonZeroHorizontalLoadNoFiniteRoot or
            MooringSurfaceBoundaryInfoClassification.VerticalGeometryCapacityInsufficient or
            MooringSurfaceBoundaryInfoClassification.NoRootRequiresNegativeQ0 or
            MooringSurfaceBoundaryInfoClassification.InsufficientBuoyancyCapacity
                => MooringSignedCandidateStatus.RejectedPhysical,

            MooringSurfaceBoundaryInfoClassification.VerticalGeometryBoundaryNonUnique or
            MooringSurfaceBoundaryInfoClassification.VerticalGeometryUniqueForceStateFamily
                => MooringSignedCandidateStatus.Indeterminate,

            MooringSurfaceBoundaryInfoClassification.IndeterminateEndpointState or
            MooringSurfaceBoundaryInfoClassification.NonMonotoneDepthResponse or
            MooringSurfaceBoundaryInfoClassification.NoRootUnclassified or
            MooringSurfaceBoundaryInfoClassification.IndeterminateDuringRootSearch or
            MooringSurfaceBoundaryInfoClassification.BracketedButDepthToleranceNotReached
                => MooringSignedCandidateStatus.RejectedNumerical,

            _ => MooringSignedCandidateStatus.RejectedNumerical
        };

        return NonAccepted(
            status,
            boundary,
            feedbackIterations,
            0,
            "SignedCandidateBoundary_" + boundary.Classification,
            $"Signed candidate boundary gate terminated with classification {boundary.Classification}; solved={boundary.Solved}, available={boundary.Available}.");
    }

    private static MooringSignedCandidateResult NonAccepted(
        MooringSignedCandidateStatus status,
        MooringSurfaceBoundaryInfoResult? boundary,
        int feedbackIterations,
        int pointLoadCrossings,
        string diagnosticCode,
        string diagnosticText)
    {
        var crossings = Math.Max(0, pointLoadCrossings);
        return MooringSignedCandidateResult.CreateNonAccepted(
            status,
            shape: null,
            boundary,
            feedbackIterations,
            crossings > 0,
            crossings,
            diagnosticCode,
            diagnosticText);
    }

    private static int SafePointLoadCrossings(MooringSurfaceBoundaryTensionTraceResult trace) =>
        Math.Max(0, trace.PointLoadCrossings);

    private static bool TryValidateTrace(
        CalculationResult result,
        MooringSequencePositionResult sequence,
        MooringSurfaceBoundaryInfoResult boundary,
        MooringSurfaceBoundaryTensionTraceResult trace,
        out string diagnostic)
    {
        diagnostic = string.Empty;

        if (!boundary.Solved || boundary.SolutionState is null)
        {
            diagnostic = "Trace validation requires a unique solved boundary state.";
            return false;
        }
        if (!trace.Available)
        {
            diagnostic = "Boundary-conditioned tension trace is unavailable: " + trace.UnavailableReason;
            return false;
        }
        if (trace.Rows.Count != result.SegmentRows.Count)
        {
            diagnostic = $"Trace row count {trace.Rows.Count} does not match segment count {result.SegmentRows.Count}.";
            return false;
        }
        if (trace.PointLoadCrossings != boundary.SolutionState.PointLoadCrossings)
        {
            diagnostic = $"Trace point-load crossings {trace.PointLoadCrossings} do not match boundary crossings {boundary.SolutionState.PointLoadCrossings}.";
            return false;
        }

        var internalPoints = InternalPoints(sequence);
        if (trace.PointLoadCrossings != internalPoints.Count)
        {
            diagnostic = $"Trace point-load crossings {trace.PointLoadCrossings} do not match internal discrete point count {internalPoints.Count}.";
            return false;
        }

        var segmentByNumber = result.SegmentRows.ToDictionary(x => x.Number);
        var previousCrossings = 0;
        foreach (var row in trace.Rows)
        {
            if (!segmentByNumber.TryGetValue(row.SegmentNumber, out var segment))
            {
                diagnostic = $"Trace segment {row.SegmentNumber} is absent from CalculationResult.";
                return false;
            }
            if (row.StartLengthM != segment.StartLengthM ||
                row.EndLengthM != segment.EndLengthM ||
                row.PointLoadCrossingsAppliedBeforeSegment < previousCrossings ||
                row.PointLoadCrossingsAppliedBeforeSegment > trace.PointLoadCrossings)
            {
                diagnostic = $"Trace segment {row.SegmentNumber} has inconsistent segment/crossing identity.";
                return false;
            }
            if (!row.TangentX.HasValue || !row.TangentZ.HasValue ||
                !double.IsFinite(row.TangentX.Value) || !double.IsFinite(row.TangentZ.Value) ||
                !double.IsFinite(row.MidTensionN) || row.MidTensionN <= MooringSurfaceBoundaryIntegrationKernel.ForceEpsilonN)
            {
                diagnostic = $"Trace segment {row.SegmentNumber} has an invalid tangent/tension state.";
                return false;
            }
            previousCrossings = row.PointLoadCrossingsAppliedBeforeSegment;
        }

        if (!TryPointLoadClosure(trace, internalPoints, out var maxResidualN, out diagnostic))
            return false;
        if (maxResidualN > PointLoadClosureToleranceN)
        {
            diagnostic = $"Point-load jump closure residual {maxResidualN:R} N exceeds the existing {PointLoadClosureToleranceN:R} N identity contract.";
            return false;
        }

        return true;
    }

    private static bool TryPointLoadClosure(
        MooringSurfaceBoundaryTensionTraceResult trace,
        IReadOnlyList<MooringSequencePositionRow> points,
        out double maxResidualN,
        out string diagnostic)
    {
        maxResidualN = 0.0;
        diagnostic = string.Empty;

        if (!trace.StartHN.HasValue || !trace.StartVN.HasValue ||
            !trace.EndHN.HasValue || !trace.EndVN.HasValue)
        {
            diagnostic = "Trace terminal force state is unavailable for point-load closure.";
            return false;
        }

        var pointIndex = 0;
        var previousH = trace.StartHN.Value;
        var previousV = trace.StartVN.Value;
        var previousCrossings = 0;

        foreach (var row in trace.Rows)
        {
            var targetCrossings = row.PointLoadCrossingsAppliedBeforeSegment;
            if (targetCrossings < previousCrossings || targetCrossings > points.Count)
            {
                diagnostic = $"Invalid point-load crossing target {targetCrossings} at segment {row.SegmentNumber}.";
                return false;
            }

            var expectedDeltaH = 0.0;
            var expectedDeltaV = 0.0;
            while (pointIndex < targetCrossings)
            {
                expectedDeltaH += points[pointIndex].CurrentForceN;
                expectedDeltaV -= points[pointIndex].WeightWaterKg * GravityMS2;
                pointIndex++;
            }

            var residualH = (row.StartHN - previousH) - expectedDeltaH;
            var residualV = (row.StartVN - previousV) - expectedDeltaV;
            var residualN = Math.Sqrt(residualH * residualH + residualV * residualV);
            if (!double.IsFinite(residualN))
            {
                diagnostic = $"Non-finite point-load closure residual at segment {row.SegmentNumber}.";
                return false;
            }
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

        var terminalResidualH = (trace.EndHN.Value - previousH) - terminalDeltaH;
        var terminalResidualV = (trace.EndVN.Value - previousV) - terminalDeltaV;
        var terminalResidualN = Math.Sqrt(
            terminalResidualH * terminalResidualH +
            terminalResidualV * terminalResidualV);
        if (!double.IsFinite(terminalResidualN))
        {
            diagnostic = "Non-finite terminal point-load closure residual.";
            return false;
        }
        maxResidualN = Math.Max(maxResidualN, terminalResidualN);

        if (pointIndex != points.Count || pointIndex != trace.PointLoadCrossings)
        {
            diagnostic = $"Point-load closure consumed {pointIndex} loads; expected {points.Count}, trace reports {trace.PointLoadCrossings}.";
            return false;
        }

        return true;
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

    private static bool TryBuildFeedbackGeometry(
        MooringSurfaceBoundaryTensionTraceResult trace,
        out FeedbackGeometry geometry,
        out string diagnostic)
    {
        var nodes = new List<FeedbackNode>(trace.Rows.Count + 1)
        {
            new(0.0, 0.0)
        };
        var x = 0.0;
        var z = 0.0;
        diagnostic = string.Empty;

        foreach (var row in trace.Rows)
        {
            var ds = row.EndLengthM - row.StartLengthM;
            if (!double.IsFinite(ds) || ds < -GeometryToleranceM ||
                !row.TangentX.HasValue || !row.TangentZ.HasValue)
            {
                geometry = default!;
                diagnostic = $"Invalid signed geometry input at segment {row.SegmentNumber}.";
                return false;
            }

            var dx = ds * row.TangentX.Value;
            var dz = ds * row.TangentZ.Value;
            if (!double.IsFinite(dx) || !double.IsFinite(dz) || dz < -GeometryToleranceM)
            {
                geometry = default!;
                diagnostic = $"Invalid signed geometry increment at segment {row.SegmentNumber}: dx={dx:R}, dz={dz:R}.";
                return false;
            }

            x += dx;
            z += dz;
            if (!double.IsFinite(x) || !double.IsFinite(z))
            {
                geometry = default!;
                diagnostic = $"Non-finite signed geometry accumulation at segment {row.SegmentNumber}.";
                return false;
            }
            nodes.Add(new FeedbackNode(x, z));
        }

        geometry = new FeedbackGeometry(nodes, x, z);
        return true;
    }

    private static bool TryBuildProjection(
        MooringSurfaceBoundaryTensionTraceResult trace,
        FeedbackGeometry geometry,
        out MooringShapeProjectionResult projection,
        out string diagnostic)
    {
        diagnostic = string.Empty;
        if (geometry.Nodes.Count != trace.Rows.Count + 1)
        {
            projection = EmptyProjection();
            diagnostic = "Signed feedback geometry node count does not match trace rows.";
            return false;
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
            if (!double.IsFinite(projectedLength) || !double.IsFinite(lengthResidual) || lengthResidual > GeometryToleranceM)
            {
                projection = EmptyProjection();
                diagnostic = $"Signed projection length residual {lengthResidual:R} m is invalid at segment {traceRow.SegmentNumber}.";
                return false;
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
                "INFO: signed boundary-feedback candidate projection"));
        }

        var totalSegmentLength = rows.Sum(x => x.SegmentLengthM);
        var totalProjectedLength = rows.Sum(x => x.ProjectedLengthM);
        var totalLengthResidual = Math.Abs(totalProjectedLength - totalSegmentLength);
        var maxAngle = rows.Count > 0 ? rows.Max(x => x.AngleFromVerticalDeg) : 0.0;
        var averageAngle = rows.Count > 0 ? rows.Average(x => x.AngleFromVerticalDeg) : 0.0;

        projection = new MooringShapeProjectionResult(
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
            "Calculation-core signed boundary-feedback projection; signed dx/dz drive MooringShapeForceAnalyzer.");
        return true;
    }

    private static MooringShapeProjectionResult EmptyProjection() =>
        new(
            Array.Empty<MooringShapeProjectionRow>(),
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            false,
            "Unavailable signed candidate projection.");

    private static bool TryApplyShapeForces(
        CalculationResult baseResult,
        CalculationResult currentResult,
        MooringSequencePositionResult sequence,
        MooringShapeForceResult shapeForces,
        double buoySteadyDragN,
        out CalculationResult nextResult,
        out double updatedLineForceN,
        out string diagnostic)
    {
        diagnostic = string.Empty;
        updatedLineForceN = 0.0;

        if (shapeForces.Rows.Count != currentResult.SegmentRows.Count)
        {
            nextResult = currentResult;
            diagnostic = $"Shape-force row count {shapeForces.Rows.Count} does not match segment count {currentResult.SegmentRows.Count}.";
            return false;
        }

        var forceBySegment = shapeForces.Rows.ToDictionary(x => x.SegmentNumber);
        var updatedSegments = new List<SegmentCalculationRow>(currentResult.SegmentRows.Count);
        foreach (var segment in currentResult.SegmentRows.OrderBy(x => x.Number))
        {
            if (!forceBySegment.TryGetValue(segment.Number, out var force) ||
                !double.IsFinite(force.ShapeForceN) || force.ShapeForceN < -PointLoadClosureToleranceN)
            {
                nextResult = currentResult;
                diagnostic = $"Invalid/missing shape force for segment {segment.Number}.";
                return false;
            }

            updatedSegments.Add(segment with { CurrentForceN = Math.Max(0.0, force.ShapeForceN) });
        }

        updatedLineForceN = updatedSegments.Sum(x => x.CurrentForceN);
        var updatedTotalCurrentForceN =
            buoySteadyDragN +
            updatedLineForceN +
            sequence.DiscreteCurrentForceN;
        if (!double.IsFinite(updatedTotalCurrentForceN))
        {
            nextResult = currentResult;
            diagnostic = "Updated signed candidate total current force is non-finite.";
            return false;
        }

        nextResult = currentResult with
        {
            SegmentRows = updatedSegments,
            CurrentForceN = updatedTotalCurrentForceN,
            HorizontalForceN = updatedTotalCurrentForceN + baseResult.WaveForceN
        };
        return true;
    }

    private static bool IsExactFixedPoint(
        CalculationResult currentResult,
        MooringSurfaceBoundaryInfoResult currentBoundary,
        MooringSurfaceBoundaryTensionTraceResult currentTrace,
        FeedbackGeometry currentGeometry,
        double currentLineForceN,
        CalculationResult nextResult,
        MooringSurfaceBoundaryInfoResult nextBoundary,
        MooringSurfaceBoundaryTensionTraceResult nextTrace,
        FeedbackGeometry nextGeometry,
        double nextLineForceN)
    {
        if (currentBoundary.Classification != nextBoundary.Classification ||
            currentBoundary.Q0N != nextBoundary.Q0N ||
            currentBoundary.SolutionState?.EndpointXM != nextBoundary.SolutionState?.EndpointXM ||
            currentBoundary.SolutionState?.EndpointZM != nextBoundary.SolutionState?.EndpointZM ||
            currentLineForceN != nextLineForceN ||
            currentTrace.PointLoadCrossings != nextTrace.PointLoadCrossings ||
            currentGeometry.Nodes.Count != nextGeometry.Nodes.Count ||
            currentResult.SegmentRows.Count != nextResult.SegmentRows.Count ||
            currentTrace.Rows.Count != nextTrace.Rows.Count)
        {
            return false;
        }

        for (var i = 0; i < currentGeometry.Nodes.Count; i++)
        {
            if (currentGeometry.Nodes[i].XM != nextGeometry.Nodes[i].XM ||
                currentGeometry.Nodes[i].ZM != nextGeometry.Nodes[i].ZM)
                return false;
        }

        var currentSegments = currentResult.SegmentRows.OrderBy(x => x.Number).ToList();
        var nextSegments = nextResult.SegmentRows.OrderBy(x => x.Number).ToList();
        for (var i = 0; i < currentSegments.Count; i++)
        {
            if (currentSegments[i].Number != nextSegments[i].Number ||
                currentSegments[i].CurrentForceN != nextSegments[i].CurrentForceN)
                return false;
        }

        for (var i = 0; i < currentTrace.Rows.Count; i++)
        {
            var current = currentTrace.Rows[i];
            var next = nextTrace.Rows[i];
            if (current.SegmentNumber != next.SegmentNumber ||
                current.StartLengthM != next.StartLengthM ||
                current.EndLengthM != next.EndLengthM ||
                current.PointLoadCrossingsAppliedBeforeSegment != next.PointLoadCrossingsAppliedBeforeSegment ||
                current.StartHN != next.StartHN ||
                current.StartVN != next.StartVN ||
                current.MidHN != next.MidHN ||
                current.MidVN != next.MidVN ||
                current.EndHN != next.EndHN ||
                current.EndVN != next.EndVN ||
                current.MidTensionN != next.MidTensionN ||
                current.TangentX != next.TangentX ||
                current.TangentZ != next.TangentZ)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryBuildAcceptedShape(
        CalculationResult result,
        MooringSurfaceBoundaryInfoResult boundary,
        MooringSurfaceBoundaryTensionTraceResult trace,
        int iteration,
        out MooringShapeResult shape,
        out string diagnostic)
    {
        diagnostic = string.Empty;
        var segmentByNumber = result.SegmentRows.ToDictionary(x => x.Number);
        var nodes = new List<MooringShapePoint>(trace.Rows.Count + 1)
        {
            new(
                1,
                0,
                "Signed boundary top",
                0.0,
                0.0,
                0.0,
                0.0,
                0.0,
                0.0,
                "INFO: signed boundary-feedback candidate")
        };

        var x = 0.0;
        var z = 0.0;
        foreach (var row in trace.Rows)
        {
            if (!segmentByNumber.TryGetValue(row.SegmentNumber, out var segment) ||
                !row.TangentX.HasValue || !row.TangentZ.HasValue ||
                !double.IsFinite(row.MidTensionN) || row.MidTensionN <= MooringSurfaceBoundaryIntegrationKernel.ForceEpsilonN)
            {
                shape = EmptyShape();
                diagnostic = $"Cannot build accepted signed shape at segment {row.SegmentNumber}.";
                return false;
            }

            // Preserve MooringSurfaceBoundaryIntegrationKernel operation order exactly.
            x += segment.SegmentLengthM * row.MidHN / row.MidTensionN;
            z += segment.SegmentLengthM * row.MidVN / row.MidTensionN;
            var angle = Math.Atan2(
                Math.Abs(row.TangentX.Value),
                Math.Max(1e-12, Math.Abs(row.TangentZ.Value))) * 180.0 / Math.PI;

            nodes.Add(new MooringShapePoint(
                nodes.Count + 1,
                row.SegmentNumber,
                row.SourceElement,
                row.EndLengthM,
                x,
                z,
                segment.SegmentLengthM,
                angle,
                row.MidTensionN / 1000.0,
                "INFO: signed boundary-feedback candidate"));
        }

        if (boundary.SolutionState is null ||
            x != boundary.SolutionState.EndpointXM ||
            z != boundary.SolutionState.EndpointZM)
        {
            shape = EmptyShape();
            diagnostic = "Accepted signed shape endpoint is not bit-identical to the solved boundary endpoint.";
            return false;
        }

        var targetDepth = boundary.TargetDepthM ?? z;
        var lineLength = boundary.LineLengthM ?? result.LineLengthM;
        shape = new MooringShapeResult(
            nodes,
            nodes[0],
            nodes[^1],
            BuoyShapeState.Surface,
            targetDepth,
            lineLength,
            x,
            Math.Abs(z - targetDepth),
            true,
            "Calculation-core signed boundary/shape-force feedback candidate. Candidate only; not selected authority.",
            iteration,
            0.0,
            1.0,
            "Exact deterministic feedback fixed point; no convergence epsilon; fixed production budget 64.");
        return true;
    }

    private static MooringShapeResult EmptyShape() =>
        new(
            Array.Empty<MooringShapePoint>(),
            null,
            null,
            BuoyShapeState.Unknown,
            0.0,
            0.0,
            0.0,
            0.0,
            false,
            "Unavailable signed candidate shape.",
            0,
            0.0,
            1.0,
            "Unavailable.");

    private sealed record FeedbackNode(double XM, double ZM);

    private sealed record FeedbackGeometry(
        IReadOnlyList<FeedbackNode> Nodes,
        double EndpointXM,
        double EndpointZM);
}
