using System;
using System.Collections.Generic;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

internal sealed record MooringSurfaceBoundaryIntegrationStep(
    int SegmentNumber,
    double StartLengthM,
    double EndLengthM,
    double StartHN,
    double StartVN,
    double MidHN,
    double MidVN,
    double EndHN,
    double EndVN,
    double MidTensionN,
    int PointLoadCrossingsAppliedBeforeSegment);

internal static class MooringSurfaceBoundaryIntegrationKernel
{
    internal const double GravityMS2 = 9.80665;
    internal const double LengthToleranceM = 1e-9;
    internal const double ForceEpsilonN = 1e-9;

    internal static MooringSurfaceBoundaryIntegrationState Integrate(
        IReadOnlyList<SegmentCalculationRow> segments,
        IReadOnlyList<MooringSequencePositionRow> points,
        double q0N,
        double initialHN,
        Action<MooringSurfaceBoundaryIntegrationStep>? onSegment = null)
    {
        var hN = initialHN;
        var vN = q0N;
        var xM = 0.0;
        var zM = 0.0;
        var minHN = hN;
        var maxHN = hN;
        var minVN = vN;
        var maxVN = vN;
        var sawPositiveV = vN > ForceEpsilonN;
        var sawNegativeV = vN < -ForceEpsilonN;
        var pointIndex = 0;
        var pointCrossings = 0;
        var indeterminateSegments = 0;

        foreach (var segment in segments)
        {
            while (pointIndex < points.Count &&
                   points[pointIndex].PositionAlongLineM <= segment.StartLengthM + LengthToleranceM)
            {
                ApplyPoint(points[pointIndex++]);
            }

            var startHN = hN;
            var startVN = vN;
            var pointCrossingsBeforeSegment = pointCrossings;
            var hMidN = hN + 0.5 * segment.CurrentForceN;
            var vMidN = vN - 0.5 * segment.WeightWaterKg * GravityMS2;
            var tensionMidN = Math.Sqrt(hMidN * hMidN + vMidN * vMidN);

            Track(hMidN, vMidN);
            if (!double.IsFinite(tensionMidN) || tensionMidN <= ForceEpsilonN)
            {
                indeterminateSegments++;
            }
            else
            {
                xM += segment.SegmentLengthM * hMidN / tensionMidN;
                zM += segment.SegmentLengthM * vMidN / tensionMidN;
            }

            hN += segment.CurrentForceN;
            vN -= segment.WeightWaterKg * GravityMS2;
            Track(hN, vN);

            onSegment?.Invoke(new MooringSurfaceBoundaryIntegrationStep(
                segment.Number,
                segment.StartLengthM,
                segment.EndLengthM,
                startHN,
                startVN,
                hMidN,
                vMidN,
                hN,
                vN,
                tensionMidN,
                pointCrossingsBeforeSegment));
        }

        while (pointIndex < points.Count)
            ApplyPoint(points[pointIndex++]);

        return new MooringSurfaceBoundaryIntegrationState(
            xM,
            zM,
            hN,
            vN,
            minHN,
            maxHN,
            minVN,
            maxVN,
            sawPositiveV && sawNegativeV,
            pointCrossings,
            indeterminateSegments);

        void ApplyPoint(MooringSequencePositionRow point)
        {
            hN += point.CurrentForceN;
            vN -= point.WeightWaterKg * GravityMS2;
            pointCrossings++;
            Track(hN, vN);
        }

        void Track(double h, double v)
        {
            minHN = Math.Min(minHN, h);
            maxHN = Math.Max(maxHN, h);
            minVN = Math.Min(minVN, v);
            maxVN = Math.Max(maxVN, v);
            sawPositiveV |= v > ForceEpsilonN;
            sawNegativeV |= v < -ForceEpsilonN;
        }
    }
}
