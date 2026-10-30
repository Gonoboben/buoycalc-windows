using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

/// <summary>Immutable observation data, not calculation result or engineering authority.</summary>
internal sealed record MooringSignedFeedbackObservedField(string Stage, int Segment, string Name, string Bits);
internal sealed record MooringSignedFeedbackObservedStep(int Iteration, IReadOnlyList<MooringSignedFeedbackObservedField> Fields);

internal sealed record MooringSignedFeedbackProbeGeometry(IReadOnlyList<(double X, double Z)> Nodes);
internal sealed record MooringSignedFeedbackProbePrior(
    CalculationResult Result, MooringSurfaceBoundaryInfoResult Boundary,
    MooringSurfaceBoundaryTensionTraceResult Trace, MooringSignedFeedbackProbeGeometry Geometry);
internal sealed record MooringSignedFeedbackProbeInput(
    string Scenario, int Iteration, MooringSignedFeedbackProbePrior Previous,
    MooringShapeProjectionResult Projection, MooringShapeForceResult ShapeForces,
    CalculationResult NextResult, MooringSurfaceBoundaryInfoResult NextBoundary,
    MooringSurfaceBoundaryTensionTraceResult NextTrace, MooringSequencePositionResult Sequence,
    double FixedBuoyDragN, double UpdatedLineForceN);

/// <summary>Only called by explicit diagnostic observer; normal production path bypasses it.</summary>
internal static class MooringSignedFeedbackParityProbe
{
    private static readonly int[] TargetSegments = { 105, 150, 166, 243 };
    private static string Bits(double value) =>
        BitConverter.DoubleToInt64Bits(value).ToString("X16", CultureInfo.InvariantCulture);

    internal static MooringSignedFeedbackObservedStep Capture(MooringSignedFeedbackProbeInput step)
    {
        var fields = new List<MooringSignedFeedbackObservedField>();
        void Record(string stage, int segment, string field, double value) =>
            fields.Add(new MooringSignedFeedbackObservedField(stage, segment, field, Bits(value)));

        var sequence = step.Sequence;
        var old = step.Previous;
        var oldLine = old.Result.SegmentRows.OrderBy(s => s.Number).Sum(s => s.CurrentForceN);
        var nextLine = step.NextResult.SegmentRows.OrderBy(s => s.Number).Sum(s => s.CurrentForceN);
        var ordered = sequence.Rows.OrderBy(x => x.Number).ToList();
        var pointSum = ordered
            .Where((x, i) => x.IsDiscrete && i != 0 && i != ordered.Count - 1)
            .Sum(x => x.CurrentForceN);
        var discrete = sequence.DiscreteCurrentForceN;

        Record("Before", 0, "FixedBuoyDragN", step.FixedBuoyDragN);
        Record("Before", 0, "ResultCurrentForceN", old.Result.CurrentForceN);
        Record("Before", 0, "SegmentSumN", oldLine);
        Record("Before", 0, "PointSumN", pointSum);
        Record("Before", 0, "DiscreteCurrentForceN", discrete);
        Record("Before", 0, "RecoveredBuoyDragN", old.Result.CurrentForceN - oldLine - pointSum);
        Record("Before", 0, "BoundaryBuoyDragN", old.Boundary.BuoySteadyDragN ?? double.NaN);
        Record("Before", 0, "TraceInitialHN", old.Trace.Rows[0].StartHN);
        Record("Before", 0, "BoundaryQ0N", old.Boundary.Q0N ?? double.NaN);

        var firstAdd = step.FixedBuoyDragN + nextLine;
        var recomposed = firstAdd + discrete;
        var recovered = step.NextResult.CurrentForceN - nextLine - pointSum;
        var boundaryDrag = step.NextBoundary.BuoySteadyDragN
            ?? throw new InvalidOperationException($"{step.Scenario}: missing next buoy drag.");
        var traceStart = step.NextTrace.Rows[0].StartHN;
        Record("After", 0, "FixedBuoyDragN", step.FixedBuoyDragN);
        Record("After", 0, "UpdatedLineForceN", step.UpdatedLineForceN);
        Record("After", 0, "SegmentSumN", nextLine);
        Record("After", 0, "PointSumN", pointSum);
        Record("After", 0, "DiscreteCurrentForceN", discrete);
        Record("After", 0, "FixedPlusLineN", firstAdd);
        Record("After", 0, "RecomposedTotalN", recomposed);
        Record("After", 0, "ResultCurrentForceN", step.NextResult.CurrentForceN);
        Record("After", 0, "RecoveredBuoyDragN", recovered);
        Record("After", 0, "BoundaryBuoyDragN", boundaryDrag);
        Record("After", 0, "TraceInitialHN", traceStart);
        Record("After", 0, "BoundaryQ0N", step.NextBoundary.Q0N ?? double.NaN);

        // These are exact arithmetic identities for this validation mirror.
        // No tolerance is used to accept or correct any floating-point state.
        if (Bits(step.UpdatedLineForceN) != Bits(nextLine) ||
            Bits(recomposed) != Bits(step.NextResult.CurrentForceN) ||
            Bits(boundaryDrag) != Bits(Math.Max(0.0, recovered)) ||
            Bits(traceStart) != Bits(boundaryDrag))
            throw new InvalidOperationException(
                $"{step.Scenario} iteration {step.Iteration}: failed observed force-recomposition identity.");

        var segments = old.Result.SegmentRows.ToDictionary(s => s.Number);
        var forceRows = step.ShapeForces.Rows.ToDictionary(x => x.SegmentNumber);
        for (var i = 0; i < step.Projection.Rows.Count; i++)
        {
            var projected = step.Projection.Rows[i];
            var segmentNumber = projected.SegmentNumber;
            if (!TargetSegments.Contains(segmentNumber)) continue;
            if (!segments.TryGetValue(segmentNumber, out var segment) ||
                !forceRows.TryGetValue(segmentNumber, out var force))
                throw new InvalidOperationException($"{step.Scenario}: missing target segment {segmentNumber}.");

            var start = old.Geometry.Nodes[i];
            var end = old.Geometry.Nodes[i + 1];
            var ds = projected.SegmentLengthM;
            var dx = projected.DeltaXM;
            var dz = projected.DeltaZM;
            var tx = dx / Math.Max(0.0001, ds);
            var tz = dz / Math.Max(0.0001, ds);
            var ux = segment.LocalSpeedMS;
            var uz = segment.VerticalCurrentMS;
            var speedSquared = ux * ux + uz * uz;
            var speed = Math.Sqrt(speedSquared);
            var dot = ux * tx + uz * tz;
            var normalSquared = Math.Max(0.0, speed * speed - dot * dot);
            var normalSpeed = Math.Sqrt(normalSquared);

            Record("Projection", segmentNumber, "StartX", start.X);
            Record("Projection", segmentNumber, "EndX", end.X);
            Record("Projection", segmentNumber, "StartZ", start.Z);
            Record("Projection", segmentNumber, "EndZ", end.Z);
            Record("Projection", segmentNumber, "SegmentLengthM", ds);
            Record("Projection", segmentNumber, "DeltaX", dx);
            Record("Projection", segmentNumber, "DeltaZ", dz);
            Record("Projection", segmentNumber, "Tx", tx);
            Record("Projection", segmentNumber, "Tz", tz);
            Record("Projection", segmentNumber, "LocalCurrentX", ux);
            Record("Projection", segmentNumber, "LocalCurrentZ", uz);
            Record("Projection", segmentNumber, "SpeedSquared", speedSquared);
            Record("Projection", segmentNumber, "Speed", speed);
            Record("Projection", segmentNumber, "Dot", dot);
            Record("Projection", segmentNumber, "NormalSquared", normalSquared);
            Record("Projection", segmentNumber, "NormalSpeed", normalSpeed);
            Record("Projection", segmentNumber, "ObservedShapeForceN", force.ShapeForceN);
            Record("Projection", segmentNumber, "PreviousSegmentForceN", segment.CurrentForceN);
            Record("Projection", segmentNumber, "NextSegmentForceN",
                step.NextResult.SegmentRows[i].CurrentForceN);
        }

        if (fields.GroupBy(x => (x.Stage, x.Segment, x.Name)).Any(g => g.Count() != 1))
            throw new InvalidOperationException("BC-AUD-006-PARITY: duplicate field identity.");
        return new MooringSignedFeedbackObservedStep(step.Iteration, fields.AsReadOnly());
    }
}
