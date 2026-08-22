using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Local midpoint force state for the v1 quasi-static design envelope.
/// The steady magnitude/direction comes from the already-selected Accepted signed shape.
/// WaveForceN is applied once as the existing horizontal buoy-boundary design proxy.
/// This is not a dynamic or time-domain result and never feeds back into X/Z geometry.
/// </summary>
public sealed record MooringDesignEnvelopeMidpointRow(
    int SegmentNumber,
    string SourceElement,
    double StartLengthM,
    double EndLengthM,
    double MidLengthM,
    double SteadyMidHN,
    double SteadyMidVN,
    double SteadyMidTensionN,
    double DesignMidHN,
    double DesignMidVN,
    double DesignMidTensionN);

/// <summary>
/// Read-only design-demand state associated with one actually selected
/// SignedBoundaryFeedback candidate. Scalar CalculationResult authority is unchanged.
/// </summary>
public sealed record MooringSelectedDesignEnvelopeState(
    MooringShapeSourceIdentity SourceIdentity,
    double WaveHorizontalIncrementN,
    double SurfaceSteadyHN,
    double SurfaceSteadyVN,
    double SurfaceSteadyTensionN,
    double SurfaceDesignHN,
    double SurfaceDesignVN,
    double SurfaceDesignTensionN,
    double AnchorSteadyHN,
    double AnchorSteadyVN,
    double AnchorSteadyTensionN,
    double AnchorDesignHN,
    double AnchorDesignVN,
    double AnchorDesignTensionN,
    IReadOnlyList<MooringDesignEnvelopeMidpointRow> MidpointRows,
    int MaxDesignMidpointSegmentNumber,
    double MaxDesignMidpointTensionN,
    int PointLoadCrossings,
    bool ContainsDiscreteLoads,
    string MethodNote);

public static class MooringSelectedDesignEnvelopeStateProjector
{
    private const double GeometryToleranceM = 1e-8;
    private const double ForceEpsilonN = 1e-9;

    public static MooringSelectedDesignEnvelopeState? Project(
        CalculationResult result,
        MooringSelectedShapeResult? selectedCore,
        MooringSignedCandidateResult signedCandidate)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(signedCandidate);

        var steadyState = MooringSelectedSignedBoundaryStateProjector.Project(selectedCore, signedCandidate);
        if (steadyState is null)
            return null;

        var waveN = result.WaveForceN;
        if (!double.IsFinite(waveN) || waveN < 0.0)
        {
            throw new InvalidOperationException(
                "Selected design envelope requires one finite non-negative legacy WaveForceN proxy.");
        }

        var shape = selectedCore!.Shape;
        var nodes = shape.Nodes.OrderBy(x => x.Number).ToList();
        if (nodes.Count < 2 || nodes.Count != result.SegmentRows.Count + 1)
        {
            throw new InvalidOperationException(
                "Selected design envelope requires exactly one Accepted signed midpoint tension node per production segment.");
        }

        var rows = new List<MooringDesignEnvelopeMidpointRow>(nodes.Count - 1);
        for (var i = 1; i < nodes.Count; i++)
        {
            var previous = nodes[i - 1];
            var node = nodes[i];
            var dxM = node.XOffsetM - previous.XOffsetM;
            var dzM = node.ZDepthM - previous.ZDepthM;
            var projectedLengthM = Math.Sqrt(dxM * dxM + dzM * dzM);
            var segmentLengthM = node.SegmentLengthM;
            var steadyTensionN = node.SegmentTensionKn * 1000.0;

            if (!double.IsFinite(dxM) ||
                !double.IsFinite(dzM) ||
                !double.IsFinite(projectedLengthM) ||
                !double.IsFinite(segmentLengthM) ||
                segmentLengthM <= 0.0 ||
                projectedLengthM <= GeometryToleranceM ||
                Math.Abs(projectedLengthM - segmentLengthM) > GeometryToleranceM ||
                !double.IsFinite(steadyTensionN) ||
                steadyTensionN <= ForceEpsilonN)
            {
                throw new InvalidOperationException(
                    $"Selected design envelope cannot reconstruct finite signed midpoint direction/tension at segment {node.SegmentNumber}.");
            }

            var tangentX = dxM / projectedLengthM;
            var tangentZ = dzM / projectedLengthM;
            var steadyHN = steadyTensionN * tangentX;
            var steadyVN = steadyTensionN * tangentZ;
            var designHN = steadyHN + waveN;
            var designVN = steadyVN;
            var designTensionN = waveN == 0.0
                ? steadyTensionN
                : Magnitude(designHN, designVN);

            if (!Finite(steadyHN, steadyVN, designHN, designVN, designTensionN) ||
                designTensionN <= ForceEpsilonN)
            {
                throw new InvalidOperationException(
                    $"Selected design envelope produced a non-finite/non-positive midpoint state at segment {node.SegmentNumber}.");
            }

            var startLengthM = node.AlongLineM - segmentLengthM;
            var endLengthM = node.AlongLineM;
            if (!double.IsFinite(startLengthM) ||
                !double.IsFinite(endLengthM) ||
                startLengthM < -GeometryToleranceM ||
                endLengthM + GeometryToleranceM < startLengthM)
            {
                throw new InvalidOperationException(
                    $"Selected design envelope has invalid along-line coordinates at segment {node.SegmentNumber}.");
            }

            rows.Add(new MooringDesignEnvelopeMidpointRow(
                node.SegmentNumber,
                node.Label,
                Math.Max(0.0, startLengthM),
                endLengthM,
                (startLengthM + endLengthM) / 2.0,
                steadyHN,
                steadyVN,
                steadyTensionN,
                designHN,
                designVN,
                designTensionN));
        }

        var maxRow = rows
            .OrderByDescending(x => x.DesignMidTensionN)
            .ThenBy(x => x.SegmentNumber)
            .First();

        var surfaceSteadyTensionN = Magnitude(steadyState.BuoySteadyDragN, steadyState.Q0N);
        var surfaceDesignHN = steadyState.BuoySteadyDragN + waveN;
        var surfaceDesignTensionN = waveN == 0.0
            ? surfaceSteadyTensionN
            : Magnitude(surfaceDesignHN, steadyState.Q0N);
        var anchorSteadyTensionN = Magnitude(steadyState.EndHN, steadyState.EndVN);
        var anchorDesignHN = steadyState.EndHN + waveN;
        var anchorDesignTensionN = waveN == 0.0
            ? anchorSteadyTensionN
            : Magnitude(anchorDesignHN, steadyState.EndVN);

        if (!Finite(
                surfaceSteadyTensionN,
                surfaceDesignHN,
                surfaceDesignTensionN,
                anchorSteadyTensionN,
                anchorDesignHN,
                anchorDesignTensionN))
        {
            throw new InvalidOperationException("Selected design envelope boundary resultants are non-finite.");
        }

        return new MooringSelectedDesignEnvelopeState(
            steadyState.SourceIdentity,
            waveN,
            steadyState.BuoySteadyDragN,
            steadyState.Q0N,
            surfaceSteadyTensionN,
            surfaceDesignHN,
            steadyState.Q0N,
            surfaceDesignTensionN,
            steadyState.EndHN,
            steadyState.EndVN,
            anchorSteadyTensionN,
            anchorDesignHN,
            steadyState.EndVN,
            anchorDesignTensionN,
            rows,
            maxRow.SegmentNumber,
            maxRow.DesignMidTensionN,
            steadyState.PointLoadCrossings,
            steadyState.ContainsDiscreteLoads,
            "Quasi-static v1 design envelope: source-backed Accepted signed steady midpoint tension/direction plus the existing horizontal buoy WaveForceN proxy exactly once; selected X/Z is unchanged; no vertical wave, inertia/added mass, distributed wave or time-domain dynamics; CalculationResult scalar authority unchanged.");
    }

    private static double Magnitude(double hN, double vN) =>
        Math.Sqrt(hN * hN + vN * vN);

    private static bool Finite(params double[] values) =>
        values.All(double.IsFinite);
}
