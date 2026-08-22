using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public enum MooringLocalElementDemandLocationKind
{
    LineStart,
    LineMidpoint,
    LineEnd,
    PointBefore,
    PointAfter
}

/// <summary>
/// Selected wave-aware local demand for one internal sequence element.
/// This is a demand/provenance state only; it does not own WLL, reserve or weak-link policy.
/// </summary>
public sealed record MooringSelectedLocalElementDemandRow(
    int ElementNumber,
    string Kind,
    string Title,
    string PresetName,
    bool IsDistributed,
    bool IsDiscrete,
    double StartAlongLineM,
    double EndAlongLineM,
    double PositionAlongLineM,
    bool Available,
    double? DesignDemandN,
    MooringLocalElementDemandLocationKind? GoverningLocation,
    int? GoverningSegmentNumber,
    double? GoverningAlongLineM,
    double? GoverningSteadyHN,
    double? GoverningSteadyVN,
    double? GoverningDesignHN,
    double? GoverningDesignVN,
    double? PointBeforeSteadyHN,
    double? PointBeforeSteadyVN,
    double? PointBeforeDesignTensionN,
    double? PointAfterSteadyHN,
    double? PointAfterSteadyVN,
    double? PointAfterDesignTensionN,
    string AvailabilityReason);

/// <summary>
/// Local element-demand map associated only with an actually selected Accepted
/// SignedBoundaryFeedback candidate and its retained exact-fixed-point trace.
/// </summary>
public sealed record MooringSelectedLocalElementDemandState(
    MooringShapeSourceIdentity SourceIdentity,
    double WaveHorizontalIncrementN,
    IReadOnlyList<MooringSelectedLocalElementDemandRow> Rows,
    int DistributedElementCount,
    int DiscreteElementCount,
    int MappedTraceSegmentCount,
    int ResolvedPointLoadCount,
    string MethodNote);

public static class MooringSelectedLocalElementDemandStateProjector
{
    private const double GravityMS2 = 9.80665;
    private const double LengthIdentityToleranceM = 1e-9;
    private const double PointClosureToleranceN = 1e-6;

    private sealed record DemandCandidate(
        double TensionN,
        MooringLocalElementDemandLocationKind Location,
        int SegmentNumber,
        double AlongLineM,
        double SteadyHN,
        double SteadyVN,
        double DesignHN,
        double DesignVN);

    private sealed record PointSides(
        double BeforeHN,
        double BeforeVN,
        double BeforeDesignN,
        double AfterHN,
        double AfterVN,
        double AfterDesignN);

    public static MooringSelectedLocalElementDemandState? Project(
        CalculationResult result,
        MooringSequencePositionResult sequence,
        MooringSelectedShapeResult? selectedCore,
        MooringSignedCandidateResult signedCandidate)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(sequence);
        ArgumentNullException.ThrowIfNull(signedCandidate);

        var selectedBoundary = MooringSelectedSignedBoundaryStateProjector.Project(selectedCore, signedCandidate);
        if (selectedBoundary is null)
            return null;

        var trace = signedCandidate.FinalTensionTrace
            ?? throw new InvalidOperationException(
                "Selected local element demand requires the retained exact-fixed-point Accepted tension trace.");

        if (!trace.Available ||
            trace.Rows.Count != result.SegmentRows.Count ||
            trace.PointLoadCrossings != signedCandidate.PointLoadCrossings ||
            !trace.StartHN.HasValue || !trace.StartVN.HasValue ||
            !trace.EndHN.HasValue || !trace.EndVN.HasValue)
        {
            throw new InvalidOperationException(
                "Selected local element demand requires one complete retained trace matching production segment/point identity.");
        }

        var waveN = result.WaveForceN;
        if (!double.IsFinite(waveN) || waveN < 0.0)
        {
            throw new InvalidOperationException(
                "Selected local element demand requires one finite non-negative legacy WaveForceN proxy.");
        }

        var orderedSequence = sequence.Rows.OrderBy(x => x.Number).ToList();
        if (orderedSequence.Count < 2 ||
            !orderedSequence[0].IsDiscrete ||
            !orderedSequence[^1].IsDiscrete)
        {
            throw new InvalidOperationException(
                "Selected local element demand requires explicit top and bottom sequence boundary rows.");
        }

        var topNumber = orderedSequence[0].Number;
        var bottomNumber = orderedSequence[^1].Number;
        var internalRows = orderedSequence
            .Where(x => x.Number != topNumber && x.Number != bottomNumber)
            .OrderBy(x => x.Number)
            .ToList();
        var distributedRows = internalRows.Where(x => x.IsDistributed).ToList();
        var points = internalRows
            .Where(x => x.IsDiscrete)
            .OrderBy(x => x.PositionAlongLineM)
            .ThenBy(x => x.Number)
            .ToList();

        if (trace.PointLoadCrossings != points.Count)
        {
            throw new InvalidOperationException(
                $"Selected local element demand point count {points.Count} differs from retained trace crossings {trace.PointLoadCrossings}.");
        }

        ValidateTraceSegmentOwnership(trace, distributedRows);
        var pointSides = ResolvePointSides(trace, points, waveN);

        var rows = new List<MooringSelectedLocalElementDemandRow>(internalRows.Count);
        foreach (var element in internalRows)
        {
            if (element.IsDistributed)
                rows.Add(BuildDistributedRow(element, trace, waveN));
            else if (element.IsDiscrete)
                rows.Add(BuildDiscreteRow(element, pointSides));
            else
                throw new InvalidOperationException($"Selected local element demand has unsupported sequence role at element {element.Number}.");
        }

        return new MooringSelectedLocalElementDemandState(
            selectedBoundary.SourceIdentity,
            waveN,
            rows,
            distributedRows.Count,
            points.Count,
            trace.Rows.Count,
            pointSides.Count,
            "Selected v1 quasi-static local element demand: exact retained Accepted fixed-point steady H/V trace + existing sequence point-load ownership + the validated existing horizontal WaveForceN proxy exactly once. Lines govern over retained start/mid/end states in their s-range; discrete items govern over both sides of their own explicit point jump. No WLL/reserve/weak-link authority is assigned here.");
    }

    private static MooringSelectedLocalElementDemandRow BuildDistributedRow(
        MooringSequencePositionRow element,
        MooringSurfaceBoundaryTensionTraceResult trace,
        double waveN)
    {
        var segmentRows = trace.Rows
            .Where(row => SegmentBelongsToLine(row, element))
            .OrderBy(row => row.SegmentNumber)
            .ToList();

        if (segmentRows.Count == 0)
        {
            return new MooringSelectedLocalElementDemandRow(
                element.Number,
                element.Kind,
                element.Title,
                element.PresetName,
                true,
                false,
                element.StartAlongLineM,
                element.EndAlongLineM,
                element.PositionAlongLineM,
                false,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "NoProductionSegmentsInLineRange");
        }

        var candidates = new List<DemandCandidate>(segmentRows.Count * 3);
        foreach (var row in segmentRows)
        {
            AddCandidate(
                row.StartHN,
                row.StartVN,
                waveN,
                MooringLocalElementDemandLocationKind.LineStart,
                row.SegmentNumber,
                row.StartLengthM);
            AddCandidate(
                row.MidHN,
                row.MidVN,
                waveN,
                MooringLocalElementDemandLocationKind.LineMidpoint,
                row.SegmentNumber,
                row.MidLengthM);
            AddCandidate(
                row.EndHN,
                row.EndVN,
                waveN,
                MooringLocalElementDemandLocationKind.LineEnd,
                row.SegmentNumber,
                row.EndLengthM);
        }

        var governing = candidates
            .OrderByDescending(x => x.TensionN)
            .ThenBy(x => x.AlongLineM)
            .ThenBy(x => LocationRank(x.Location))
            .ThenBy(x => x.SegmentNumber)
            .First();

        return new MooringSelectedLocalElementDemandRow(
            element.Number,
            element.Kind,
            element.Title,
            element.PresetName,
            true,
            false,
            element.StartAlongLineM,
            element.EndAlongLineM,
            element.PositionAlongLineM,
            true,
            governing.TensionN,
            governing.Location,
            governing.SegmentNumber,
            governing.AlongLineM,
            governing.SteadyHN,
            governing.SteadyVN,
            governing.DesignHN,
            governing.DesignVN,
            null,
            null,
            null,
            null,
            null,
            null,
            string.Empty);

        void AddCandidate(
            double steadyHN,
            double steadyVN,
            double waveIncrementN,
            MooringLocalElementDemandLocationKind location,
            int segmentNumber,
            double alongLineM)
        {
            var designHN = steadyHN + waveIncrementN;
            var tensionN = Magnitude(designHN, steadyVN);
            if (!Finite(steadyHN, steadyVN, designHN, tensionN, alongLineM) || tensionN < 0.0)
            {
                throw new InvalidOperationException(
                    $"Selected local line demand produced invalid state at segment {segmentNumber}, {location}.");
            }

            candidates.Add(new DemandCandidate(
                tensionN,
                location,
                segmentNumber,
                alongLineM,
                steadyHN,
                steadyVN,
                designHN,
                steadyVN));
        }
    }

    private static MooringSelectedLocalElementDemandRow BuildDiscreteRow(
        MooringSequencePositionRow element,
        IReadOnlyDictionary<int, PointSides> pointSides)
    {
        if (!pointSides.TryGetValue(element.Number, out var sides))
        {
            throw new InvalidOperationException(
                $"Selected local element demand is missing point-side state for discrete element {element.Number}.");
        }

        var afterGoverns = sides.AfterDesignN > sides.BeforeDesignN;
        var demandN = afterGoverns ? sides.AfterDesignN : sides.BeforeDesignN;
        var location = afterGoverns
            ? MooringLocalElementDemandLocationKind.PointAfter
            : MooringLocalElementDemandLocationKind.PointBefore;
        var governingH = afterGoverns ? sides.AfterHN : sides.BeforeHN;
        var governingV = afterGoverns ? sides.AfterVN : sides.BeforeVN;

        return new MooringSelectedLocalElementDemandRow(
            element.Number,
            element.Kind,
            element.Title,
            element.PresetName,
            false,
            true,
            element.StartAlongLineM,
            element.EndAlongLineM,
            element.PositionAlongLineM,
            true,
            demandN,
            location,
            null,
            element.PositionAlongLineM,
            governingH,
            governingV,
            governingH,
            governingV,
            sides.BeforeHN,
            sides.BeforeVN,
            sides.BeforeDesignN,
            sides.AfterHN,
            sides.AfterVN,
            sides.AfterDesignN,
            string.Empty);
    }

    private static void ValidateTraceSegmentOwnership(
        MooringSurfaceBoundaryTensionTraceResult trace,
        IReadOnlyList<MooringSequencePositionRow> distributedRows)
    {
        foreach (var traceRow in trace.Rows)
        {
            var owners = distributedRows.Count(element => SegmentBelongsToLine(traceRow, element));
            if (owners != 1)
            {
                throw new InvalidOperationException(
                    $"Retained trace segment {traceRow.SegmentNumber} maps to {owners} distributed sequence ranges; expected exactly one.");
            }
        }
    }

    private static Dictionary<int, PointSides> ResolvePointSides(
        MooringSurfaceBoundaryTensionTraceResult trace,
        IReadOnlyList<MooringSequencePositionRow> points,
        double waveN)
    {
        var resolved = new Dictionary<int, PointSides>();
        var pointIndex = 0;
        var currentH = trace.StartHN!.Value;
        var currentV = trace.StartVN!.Value;

        foreach (var row in trace.Rows)
        {
            var targetCrossings = row.PointLoadCrossingsAppliedBeforeSegment;
            if (targetCrossings < pointIndex || targetCrossings > points.Count)
            {
                throw new InvalidOperationException(
                    $"Invalid retained point-crossing target {targetCrossings} before segment {row.SegmentNumber}.");
            }

            while (pointIndex < targetCrossings)
                Resolve(points[pointIndex++]);

            RequireClosure(currentH, currentV, row.StartHN, row.StartVN, $"segment {row.SegmentNumber} start");
            currentH = row.EndHN;
            currentV = row.EndVN;
        }

        while (pointIndex < points.Count)
            Resolve(points[pointIndex++]);

        RequireClosure(currentH, currentV, trace.EndHN!.Value, trace.EndVN!.Value, "retained terminal state");

        if (resolved.Count != points.Count || pointIndex != trace.PointLoadCrossings)
        {
            throw new InvalidOperationException(
                $"Selected local point resolution consumed {pointIndex} points, resolved {resolved.Count}; expected {points.Count}/{trace.PointLoadCrossings}.");
        }

        return resolved;

        void Resolve(MooringSequencePositionRow point)
        {
            if (resolved.ContainsKey(point.Number))
                throw new InvalidOperationException($"Duplicate discrete sequence element number {point.Number} in local-demand mapping.");

            var beforeH = currentH;
            var beforeV = currentV;
            var beforeDesignN = Magnitude(beforeH + waveN, beforeV);

            currentH += point.CurrentForceN;
            currentV -= point.WeightWaterKg * GravityMS2;

            var afterH = currentH;
            var afterV = currentV;
            var afterDesignN = Magnitude(afterH + waveN, afterV);
            if (!Finite(beforeH, beforeV, beforeDesignN, afterH, afterV, afterDesignN))
                throw new InvalidOperationException($"Non-finite local point state at sequence element {point.Number}.");

            resolved.Add(point.Number, new PointSides(
                beforeH,
                beforeV,
                beforeDesignN,
                afterH,
                afterV,
                afterDesignN));
        }
    }

    private static bool SegmentBelongsToLine(
        MooringSurfaceBoundaryTensionTraceRow segment,
        MooringSequencePositionRow line)
    {
        if (!line.IsDistributed)
            return false;

        return segment.StartLengthM + LengthIdentityToleranceM >= line.StartAlongLineM &&
               segment.EndLengthM <= line.EndAlongLineM + LengthIdentityToleranceM &&
               segment.EndLengthM > segment.StartLengthM;
    }

    private static void RequireClosure(
        double actualH,
        double actualV,
        double expectedH,
        double expectedV,
        string label)
    {
        var dh = actualH - expectedH;
        var dv = actualV - expectedV;
        var residualN = Math.Sqrt(dh * dh + dv * dv);
        if (!double.IsFinite(residualN) || residualN > PointClosureToleranceN)
        {
            throw new InvalidOperationException(
                $"Selected local point-load closure residual {residualN:R} N at {label} exceeds existing identity contract {PointClosureToleranceN:R} N.");
        }
    }

    private static int LocationRank(MooringLocalElementDemandLocationKind location) => location switch
    {
        MooringLocalElementDemandLocationKind.LineStart => 0,
        MooringLocalElementDemandLocationKind.LineMidpoint => 1,
        MooringLocalElementDemandLocationKind.LineEnd => 2,
        MooringLocalElementDemandLocationKind.PointBefore => 3,
        MooringLocalElementDemandLocationKind.PointAfter => 4,
        _ => 5
    };

    private static double Magnitude(double hN, double vN) =>
        Math.Sqrt(hN * hN + vN * vN);

    private static bool Finite(params double[] values) =>
        values.All(double.IsFinite);
}
