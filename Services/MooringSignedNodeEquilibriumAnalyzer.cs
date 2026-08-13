using System;
using System.Collections.Generic;
using System.Linq;

namespace BuoyCalc.Windows.Services;

public sealed record MooringSignedNodeEquilibriumRow(
    int Number,
    double PositionAlongLineM,
    string SourceElements,
    int SourceElementCount,
    double NodeWeightWaterKg,
    double NodeCurrentForceN,
    double NodeForceXN,
    double NodeForceZN,
    int? UpperSegmentNumber,
    int? LowerSegmentNumber,
    double? UpperTangentX,
    double? UpperTangentZ,
    double? LowerTangentX,
    double? LowerTangentZ,
    double? InclusiveHorizontalForceN,
    double? InclusiveVerticalForceN,
    double? BelowHorizontalForceN,
    double? BelowVerticalForceN,
    double? UpperTensionN,
    double? LowerTensionN,
    double? ResidualXN,
    double? ResidualZN,
    double? ResidualN,
    double? RelativeResidual,
    bool IsAvailable,
    string Status);

public sealed record MooringSignedNodeEquilibriumResult(
    IReadOnlyList<MooringSignedNodeEquilibriumRow> Rows,
    int NodeCount,
    int AvailableNodeCount,
    int IndeterminateNodeCount,
    double? MaxResidualN,
    double? MaxRelativeResidual,
    int? WorstNodeNumber,
    string MethodNote);

/// <summary>
/// INFO-only signed internal-node free-body diagnostic for the pre-iterative
/// discrete-load candidate state.
///
/// Source boundary: docs/CONTROL_MARK_BERTEAUX_SIGNED_NODE_SOURCE_2026-08-13.md.
/// This analyzer never feeds solver convergence, primary-shape selection,
/// engineering verdicts, anchor checks, weak-link checks, 2D or PDF geometry.
/// </summary>
public static class MooringSignedNodeEquilibriumAnalyzer
{
    private const double G = 9.80665;
    private const double GeometryLengthToleranceM = 1e-12;

    public static MooringSignedNodeEquilibriumResult Build(
        MooringSequencePositionResult sequencePositions,
        MooringDiscreteLoadTensionResult discreteTensions,
        MooringDiscreteLoadShapeResult discreteShape)
    {
        if (sequencePositions.Rows.Count == 0 ||
            discreteTensions.DiscreteLoads.Count == 0 ||
            discreteTensions.Rows.Count == 0 ||
            discreteShape.Rows.Count == 0)
        {
            return Empty("Нет полной pre-iterative sequence/discrete-tension/discrete-shape family для signed internal-node diagnostic.");
        }

        var mappingToleranceM = MappingToleranceM(sequencePositions.TotalLineLengthM);
        var loadGroups = GroupLoads(discreteTensions.DiscreteLoads, mappingToleranceM);
        var shapeRows = discreteShape.Rows
            .OrderBy(x => x.AlongLineM)
            .ThenBy(x => x.Number)
            .ToList();
        var tensionRows = discreteTensions.Rows
            .OrderBy(x => x.StartAlongLineM)
            .ThenBy(x => x.Number)
            .ToList();

        var rows = new List<MooringSignedNodeEquilibriumRow>();
        for (var groupIndex = 0; groupIndex < loadGroups.Count; groupIndex++)
        {
            var group = loadGroups[groupIndex];
            var positionM = group.PositionAlongLineM;
            var sourceElements = string.Join(" + ", group.Items
                .OrderBy(x => x.Number)
                .Select(x => $"{x.Title} [#{x.Number}]"));
            var nodeWeightWaterKg = group.Items.Sum(x => x.WeightWaterKg);
            var nodeCurrentForceN = group.Items.Sum(x => x.CurrentForceN);
            var nodeForceXN = nodeCurrentForceN;
            var nodeForceZN = nodeWeightWaterKg * G;

            if (!IsFinite(positionM) ||
                !IsFinite(nodeWeightWaterKg) ||
                !IsFinite(nodeCurrentForceN) ||
                !IsFinite(nodeForceZN))
            {
                rows.Add(Indeterminate(
                    groupIndex + 1,
                    positionM,
                    sourceElements,
                    group.Items.Count,
                    nodeWeightWaterKg,
                    nodeCurrentForceN,
                    nodeForceXN,
                    nodeForceZN,
                    "INDETERMINATE: non-finite node position/load."));
                continue;
            }

            if (positionM <= mappingToleranceM ||
                positionM >= sequencePositions.TotalLineLengthM - mappingToleranceM)
            {
                rows.Add(Indeterminate(
                    groupIndex + 1,
                    positionM,
                    sourceElements,
                    group.Items.Count,
                    nodeWeightWaterKg,
                    nodeCurrentForceN,
                    nodeForceXN,
                    nodeForceZN,
                    "INDETERMINATE: boundary node; buoy/anchor reaction is not solved by Candidate B."));
                continue;
            }

            var matchingShapeIndices = Enumerable.Range(0, shapeRows.Count)
                .Where(i => Math.Abs(shapeRows[i].AlongLineM - positionM) <= mappingToleranceM)
                .ToList();

            if (matchingShapeIndices.Count != 1)
            {
                rows.Add(Indeterminate(
                    groupIndex + 1,
                    positionM,
                    sourceElements,
                    group.Items.Count,
                    nodeWeightWaterKg,
                    nodeCurrentForceN,
                    nodeForceXN,
                    nodeForceZN,
                    $"INDETERMINATE: expected one candidate-shape junction at s; found {matchingShapeIndices.Count}."));
                continue;
            }

            var nodeIndex = matchingShapeIndices[0];
            if (nodeIndex <= 0 || nodeIndex >= shapeRows.Count - 1)
            {
                rows.Add(Indeterminate(
                    groupIndex + 1,
                    positionM,
                    sourceElements,
                    group.Items.Count,
                    nodeWeightWaterKg,
                    nodeCurrentForceN,
                    nodeForceXN,
                    nodeForceZN,
                    "INDETERMINATE: candidate-shape junction has no two adjacent internal segments."));
                continue;
            }

            var upperStart = shapeRows[nodeIndex - 1];
            var nodePoint = shapeRows[nodeIndex];
            var lowerEnd = shapeRows[nodeIndex + 1];
            var upperSegmentNumber = nodePoint.SegmentNumber;
            var lowerSegmentNumber = lowerEnd.SegmentNumber;

            if (!TryUnitTangent(
                    nodePoint.XOffsetM - upperStart.XOffsetM,
                    nodePoint.ZDepthM - upperStart.ZDepthM,
                    out var upperTangentX,
                    out var upperTangentZ) ||
                !TryUnitTangent(
                    lowerEnd.XOffsetM - nodePoint.XOffsetM,
                    lowerEnd.ZDepthM - nodePoint.ZDepthM,
                    out var lowerTangentX,
                    out var lowerTangentZ))
            {
                rows.Add(Indeterminate(
                    groupIndex + 1,
                    positionM,
                    sourceElements,
                    group.Items.Count,
                    nodeWeightWaterKg,
                    nodeCurrentForceN,
                    nodeForceXN,
                    nodeForceZN,
                    "INDETERMINATE: degenerate or non-finite adjacent candidate-shape tangent.",
                    upperSegmentNumber,
                    lowerSegmentNumber));
                continue;
            }

            var matchingLowerTensions = tensionRows
                .Where(x => Math.Abs(x.StartAlongLineM - positionM) <= mappingToleranceM)
                .ToList();

            if (matchingLowerTensions.Count != 1)
            {
                rows.Add(Indeterminate(
                    groupIndex + 1,
                    positionM,
                    sourceElements,
                    group.Items.Count,
                    nodeWeightWaterKg,
                    nodeCurrentForceN,
                    nodeForceXN,
                    nodeForceZN,
                    $"INDETERMINATE: expected one inclusive lower-segment cut state; found {matchingLowerTensions.Count}.",
                    upperSegmentNumber,
                    lowerSegmentNumber,
                    upperTangentX,
                    upperTangentZ,
                    lowerTangentX,
                    lowerTangentZ));
                continue;
            }

            var inclusive = matchingLowerTensions[0];
            if (inclusive.SegmentNumber != lowerSegmentNumber)
            {
                rows.Add(Indeterminate(
                    groupIndex + 1,
                    positionM,
                    sourceElements,
                    group.Items.Count,
                    nodeWeightWaterKg,
                    nodeCurrentForceN,
                    nodeForceXN,
                    nodeForceZN,
                    $"INDETERMINATE: tension/geometry lower-segment mapping mismatch ({inclusive.SegmentNumber} != {lowerSegmentNumber}).",
                    upperSegmentNumber,
                    lowerSegmentNumber,
                    upperTangentX,
                    upperTangentZ,
                    lowerTangentX,
                    lowerTangentZ));
                continue;
            }

            var inclusiveH = inclusive.CumulativeHorizontalForceN;
            var inclusiveV = inclusive.CumulativeVerticalForceN;
            var belowH = inclusiveH - nodeForceXN;
            var belowV = inclusiveV - nodeForceZN;

            if (!AllFinite(
                    inclusiveH,
                    inclusiveV,
                    belowH,
                    belowV,
                    upperTangentX,
                    upperTangentZ,
                    lowerTangentX,
                    lowerTangentZ))
            {
                rows.Add(Indeterminate(
                    groupIndex + 1,
                    positionM,
                    sourceElements,
                    group.Items.Count,
                    nodeWeightWaterKg,
                    nodeCurrentForceN,
                    nodeForceXN,
                    nodeForceZN,
                    "INDETERMINATE: non-finite cumulative force/tangent state.",
                    upperSegmentNumber,
                    lowerSegmentNumber,
                    upperTangentX,
                    upperTangentZ,
                    lowerTangentX,
                    lowerTangentZ));
                continue;
            }

            var upperTensionN = Hypot(inclusiveH, inclusiveV);
            var lowerTensionN = Hypot(belowH, belowV);
            var residualXN = -upperTensionN * upperTangentX +
                lowerTensionN * lowerTangentX +
                nodeForceXN;
            var residualZN = -upperTensionN * upperTangentZ +
                lowerTensionN * lowerTangentZ +
                nodeForceZN;
            var residualN = Hypot(residualXN, residualZN);
            var nodeForceN = Hypot(nodeForceXN, nodeForceZN);
            var referenceForceN = Math.Max(
                1.0,
                Math.Max(upperTensionN, Math.Max(lowerTensionN, nodeForceN)));
            var relativeResidual = residualN / referenceForceN;

            if (!AllFinite(upperTensionN, lowerTensionN, residualXN, residualZN, residualN, relativeResidual))
            {
                rows.Add(Indeterminate(
                    groupIndex + 1,
                    positionM,
                    sourceElements,
                    group.Items.Count,
                    nodeWeightWaterKg,
                    nodeCurrentForceN,
                    nodeForceXN,
                    nodeForceZN,
                    "INDETERMINATE: non-finite reconstructed tension/residual.",
                    upperSegmentNumber,
                    lowerSegmentNumber,
                    upperTangentX,
                    upperTangentZ,
                    lowerTangentX,
                    lowerTangentZ));
                continue;
            }

            rows.Add(new MooringSignedNodeEquilibriumRow(
                groupIndex + 1,
                positionM,
                sourceElements,
                group.Items.Count,
                nodeWeightWaterKg,
                nodeCurrentForceN,
                nodeForceXN,
                nodeForceZN,
                upperSegmentNumber,
                lowerSegmentNumber,
                upperTangentX,
                upperTangentZ,
                lowerTangentX,
                lowerTangentZ,
                inclusiveH,
                inclusiveV,
                belowH,
                belowV,
                upperTensionN,
                lowerTensionN,
                residualXN,
                residualZN,
                residualN,
                relativeResidual,
                true,
                "INFO: pre-iterative discrete-load candidate signed internal-node residual; no engineering acceptance threshold."));
        }

        var availableRows = rows
            .Where(x => x.IsAvailable && x.ResidualN.HasValue && x.RelativeResidual.HasValue)
            .ToList();
        var worst = availableRows
            .OrderByDescending(x => x.RelativeResidual!.Value)
            .ThenByDescending(x => x.ResidualN!.Value)
            .FirstOrDefault();

        return new MooringSignedNodeEquilibriumResult(
            rows,
            rows.Count,
            availableRows.Count,
            rows.Count - availableRows.Count,
            availableRows.Count > 0 ? availableRows.Max(x => x.ResidualN!.Value) : null,
            availableRows.Count > 0 ? availableRows.Max(x => x.RelativeResidual!.Value) : null,
            worst?.Number,
            "INFO-only signed free-body check for grouped internal point-load nodes of the pre-iterative discrete-load candidate. Uses sequence positions, inclusive discrete cumulative cut states and the X/Z tangents of the corresponding discrete-load candidate shape. It is not selected-shape equilibrium and does not feed solver/gate/verdict decisions.");
    }

    private static MooringSignedNodeEquilibriumResult Empty(string note)
    {
        return new MooringSignedNodeEquilibriumResult(
            Array.Empty<MooringSignedNodeEquilibriumRow>(),
            0,
            0,
            0,
            null,
            null,
            null,
            note);
    }

    private static IReadOnlyList<LoadGroup> GroupLoads(
        IReadOnlyList<MooringDiscreteLoadEntry> loads,
        double toleranceM)
    {
        var ordered = loads
            .OrderBy(x => x.PositionAlongLineM)
            .ThenBy(x => x.Number)
            .ToList();
        var groups = new List<LoadGroup>();

        foreach (var load in ordered)
        {
            if (groups.Count == 0 ||
                Math.Abs(load.PositionAlongLineM - groups[^1].PositionAlongLineM) > toleranceM)
            {
                groups.Add(new LoadGroup(
                    load.PositionAlongLineM,
                    new List<MooringDiscreteLoadEntry> { load }));
                continue;
            }

            groups[^1].Items.Add(load);
        }

        return groups;
    }

    private static double MappingToleranceM(double totalLineLengthM)
    {
        return Math.Max(1e-9, Math.Max(1.0, Math.Abs(totalLineLengthM)) * 1e-12);
    }

    private static bool TryUnitTangent(
        double dx,
        double dz,
        out double tangentX,
        out double tangentZ)
    {
        tangentX = 0;
        tangentZ = 0;

        if (!IsFinite(dx) || !IsFinite(dz))
        {
            return false;
        }

        var lengthM = Hypot(dx, dz);
        if (!IsFinite(lengthM) || lengthM <= GeometryLengthToleranceM)
        {
            return false;
        }

        tangentX = dx / lengthM;
        tangentZ = dz / lengthM;
        return IsFinite(tangentX) && IsFinite(tangentZ);
    }

    private static MooringSignedNodeEquilibriumRow Indeterminate(
        int number,
        double positionAlongLineM,
        string sourceElements,
        int sourceElementCount,
        double nodeWeightWaterKg,
        double nodeCurrentForceN,
        double nodeForceXN,
        double nodeForceZN,
        string status,
        int? upperSegmentNumber = null,
        int? lowerSegmentNumber = null,
        double? upperTangentX = null,
        double? upperTangentZ = null,
        double? lowerTangentX = null,
        double? lowerTangentZ = null)
    {
        return new MooringSignedNodeEquilibriumRow(
            number,
            positionAlongLineM,
            sourceElements,
            sourceElementCount,
            nodeWeightWaterKg,
            nodeCurrentForceN,
            nodeForceXN,
            nodeForceZN,
            upperSegmentNumber,
            lowerSegmentNumber,
            upperTangentX,
            upperTangentZ,
            lowerTangentX,
            lowerTangentZ,
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
            false,
            status);
    }

    private static double Hypot(double x, double y)
    {
        return Math.Sqrt(x * x + y * y);
    }

    private static bool AllFinite(params double[] values)
    {
        return values.All(IsFinite);
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private sealed record LoadGroup(
        double PositionAlongLineM,
        List<MooringDiscreteLoadEntry> Items);
}
