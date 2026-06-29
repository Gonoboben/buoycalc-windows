using System;
using System.Collections.Generic;
using System.Linq;

namespace BuoyCalc.Windows.Services;

public sealed record MooringIterativeShapeComparisonRow(
    int Number,
    int SegmentNumber,
    string Label,
    double AlongLineM,
    double MainXOffsetM,
    double MainZDepthM,
    double CandidateXOffsetM,
    double CandidateZDepthM,
    double DeltaXM,
    double DeltaZM,
    double NodeDeltaM,
    double MainTensionKn,
    double CandidateTensionKn,
    double TensionDeltaKn,
    string Status);

public sealed record MooringIterativeShapeComparisonResult(
    IReadOnlyList<MooringIterativeShapeComparisonRow> Rows,
    double MainHorizontalOffsetM,
    double CandidateHorizontalOffsetM,
    double OffsetDifferenceM,
    double MaxNodeDeltaM,
    double AverageNodeDeltaM,
    double MaxTensionDeltaKn,
    bool WithinTolerance,
    string MethodNote);

public static class MooringIterativeShapeComparison
{
    private const double NodeDeltaToleranceM = 0.10;
    private const double OffsetToleranceM = 0.05;

    public static MooringIterativeShapeComparisonResult Build(
        MooringShapeResult mainShape,
        MooringIterativeSolverResult iterativeSolver)
    {
        var candidateShape = iterativeSolver.FinalShape;
        if (mainShape.Nodes.Count == 0 || candidateShape is null || candidateShape.Nodes.Count == 0)
        {
            return Empty("Нет основной формы или финальной кандидатной формы v0.39 для сравнения.");
        }

        var mainBySegment = mainShape.Nodes
            .GroupBy(x => x.SegmentNumber)
            .ToDictionary(x => x.Key, x => x.Last());

        var rows = new List<MooringIterativeShapeComparisonRow>();
        foreach (var candidate in candidateShape.Nodes.OrderBy(x => x.Number))
        {
            mainBySegment.TryGetValue(candidate.SegmentNumber, out var main);
            var mainX = main?.XOffsetM ?? 0;
            var mainZ = main?.ZDepthM ?? 0;
            var mainTension = main?.SegmentTensionKn ?? 0;
            var deltaX = candidate.XOffsetM - mainX;
            var deltaZ = candidate.ZDepthM - mainZ;
            var nodeDelta = Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
            var tensionDelta = candidate.SegmentTensionKn - mainTension;

            rows.Add(new MooringIterativeShapeComparisonRow(
                rows.Count,
                candidate.SegmentNumber,
                candidate.Label,
                candidate.AlongLineM,
                mainX,
                mainZ,
                candidate.XOffsetM,
                candidate.ZDepthM,
                deltaX,
                deltaZ,
                nodeDelta,
                mainTension,
                candidate.SegmentTensionKn,
                tensionDelta,
                nodeDelta <= NodeDeltaToleranceM ? "OK" : "INFO: кандидатная форма отличается от основной"));
        }

        var offsetDifference = candidateShape.HorizontalOffsetM - mainShape.HorizontalOffsetM;
        var maxNodeDelta = rows.Count > 0 ? rows.Max(x => x.NodeDeltaM) : 0;
        var averageNodeDelta = rows.Count > 0 ? rows.Average(x => x.NodeDeltaM) : 0;
        var maxTensionDelta = rows.Count > 0 ? rows.Max(x => Math.Abs(x.TensionDeltaKn)) : 0;
        var withinTolerance = Math.Abs(offsetDifference) <= OffsetToleranceM && maxNodeDelta <= NodeDeltaToleranceM;

        return new MooringIterativeShapeComparisonResult(
            rows,
            mainShape.HorizontalOffsetM,
            candidateShape.HorizontalOffsetM,
            offsetDifference,
            maxNodeDelta,
            averageNodeDelta,
            maxTensionDelta,
            withinTolerance,
            "v0.39.1: добавлен отдельный сравниватель основной формы и финальной кандидатной формы итерационного solver. Он нужен для отчётной проверки перед тем, как когда-либо использовать v0.39-форму в 2D/PDF или расчётных проверках.");
    }

    private static MooringIterativeShapeComparisonResult Empty(string note)
    {
        return new MooringIterativeShapeComparisonResult(
            Array.Empty<MooringIterativeShapeComparisonRow>(),
            0,
            0,
            0,
            0,
            0,
            0,
            false,
            note);
    }
}
