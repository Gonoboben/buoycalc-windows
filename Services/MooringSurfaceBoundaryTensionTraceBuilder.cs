using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record MooringSurfaceBoundaryTensionTraceRow(
    int SegmentNumber,
    string SourceElement,
    double StartLengthM,
    double EndLengthM,
    double MidLengthM,
    double EstimatedDepthM,
    int PointLoadCrossingsAppliedBeforeSegment,
    double StartHN,
    double StartVN,
    double MidHN,
    double MidVN,
    double EndHN,
    double EndVN,
    double MidTensionN,
    double? TangentX,
    double? TangentZ,
    double? SignedAngleFromDownwardVerticalDeg);

public sealed record MooringSurfaceBoundaryTensionTraceResult(
    bool Available,
    MooringSurfaceBoundaryInfoClassification ParentClassification,
    IReadOnlyList<MooringSurfaceBoundaryTensionTraceRow> Rows,
    double? StartHN,
    double? StartVN,
    double? EndHN,
    double? EndVN,
    int PointLoadCrossings,
    int IndeterminateSegmentCount,
    string ParentMethodNote,
    string MethodNote,
    string UnavailableReason);

public static class MooringSurfaceBoundaryTensionTraceBuilder
{
    private const string Method =
        "INFO only: boundary-conditioned frozen-load tension trace from solved (D_b,Q0); " +
        "shared midpoint integration kernel; signed submerged weights; existing sequence point-load ownership; " +
        "steady current; wave excluded; diagnostic only, not selected-shape authority.";

    public static MooringSurfaceBoundaryTensionTraceResult Build(
        CalculationResult result,
        MooringSequencePositionResult sequence,
        MooringSurfaceBoundaryInfoResult surfaceBoundary)
    {
        if (!surfaceBoundary.Solved ||
            !surfaceBoundary.BuoySteadyDragN.HasValue ||
            !surfaceBoundary.Q0N.HasValue ||
            surfaceBoundary.SolutionState is null)
        {
            return Unavailable(surfaceBoundary, "Parent surface-boundary state is not a solved bounded Q0 state.");
        }

        var orderedSequence = sequence.Rows.OrderBy(x => x.Number).ToList();
        if (orderedSequence.Count < 2 || !orderedSequence[0].IsDiscrete || !orderedSequence[^1].IsDiscrete)
        {
            return Unavailable(surfaceBoundary, "Required top/bottom sequence boundary rows are unavailable.");
        }

        var topNumber = orderedSequence[0].Number;
        var bottomNumber = orderedSequence[^1].Number;
        var points = orderedSequence
            .Where(x => x.IsDiscrete && x.Number != topNumber && x.Number != bottomNumber)
            .OrderBy(x => x.PositionAlongLineM)
            .ThenBy(x => x.Number)
            .ToList();
        var segments = result.SegmentRows.OrderBy(x => x.Number).ToList();
        var segmentByNumber = segments.ToDictionary(x => x.Number);
        var rows = new List<MooringSurfaceBoundaryTensionTraceRow>(segments.Count);

        var state = MooringSurfaceBoundaryIntegrationKernel.Integrate(
            segments,
            points,
            surfaceBoundary.Q0N.Value,
            surfaceBoundary.BuoySteadyDragN.Value,
            step =>
            {
                var segment = segmentByNumber[step.SegmentNumber];
                double? tangentX = null;
                double? tangentZ = null;
                double? signedAngle = null;
                if (double.IsFinite(step.MidTensionN) &&
                    step.MidTensionN > MooringSurfaceBoundaryIntegrationKernel.ForceEpsilonN)
                {
                    tangentX = step.MidHN / step.MidTensionN;
                    tangentZ = step.MidVN / step.MidTensionN;
                    signedAngle = Math.Atan2(step.MidHN, step.MidVN) * 180.0 / Math.PI;
                }

                rows.Add(new MooringSurfaceBoundaryTensionTraceRow(
                    step.SegmentNumber,
                    segment.SourceElement,
                    step.StartLengthM,
                    step.EndLengthM,
                    (step.StartLengthM + step.EndLengthM) / 2.0,
                    segment.EstimatedDepthM,
                    step.PointLoadCrossingsAppliedBeforeSegment,
                    step.StartHN,
                    step.StartVN,
                    step.MidHN,
                    step.MidVN,
                    step.EndHN,
                    step.EndVN,
                    step.MidTensionN,
                    tangentX,
                    tangentZ,
                    signedAngle));
            });

        return new MooringSurfaceBoundaryTensionTraceResult(
            true,
            surfaceBoundary.Classification,
            rows,
            surfaceBoundary.BuoySteadyDragN,
            surfaceBoundary.Q0N,
            state.EndHN,
            state.EndVN,
            state.PointLoadCrossings,
            state.IndeterminateSegmentCount,
            surfaceBoundary.MethodNote,
            Method,
            string.Empty);
    }

    private static MooringSurfaceBoundaryTensionTraceResult Unavailable(
        MooringSurfaceBoundaryInfoResult surfaceBoundary,
        string reason)
    {
        return new MooringSurfaceBoundaryTensionTraceResult(
            false,
            surfaceBoundary.Classification,
            Array.Empty<MooringSurfaceBoundaryTensionTraceRow>(),
            surfaceBoundary.BuoySteadyDragN,
            surfaceBoundary.Q0N,
            null,
            null,
            0,
            0,
            surfaceBoundary.MethodNote,
            Method,
            reason);
    }
}
