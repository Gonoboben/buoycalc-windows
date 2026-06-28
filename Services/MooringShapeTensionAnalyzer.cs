using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record MooringShapeTensionRow(
    int Number,
    int SegmentNumber,
    string SourceElement,
    double EstimatedDepthM,
    double SegmentLengthM,
    double WeightWaterKg,
    double OriginalSegmentForceN,
    double ShapeSegmentForceN,
    double OriginalTensionKn,
    double ShapeTensionKn,
    double TensionDifferenceKn,
    double OriginalAngleFromVerticalDeg,
    double ShapeAngleFromVerticalDeg,
    double AngleDifferenceDeg,
    double CumulativeShapeHorizontalForceN,
    double CumulativeVerticalForceN,
    string Status);

public sealed record MooringShapeTensionResult(
    IReadOnlyList<MooringShapeTensionRow> Rows,
    double MaxOriginalTensionKn,
    double MaxShapeTensionKn,
    double MaxTensionDifferenceKn,
    double MaxAngleDifferenceDeg,
    double TopOriginalTensionKn,
    double TopShapeTensionKn,
    double RelativeTopTensionDifference,
    bool WithinTolerance,
    string MethodNote);

public static class MooringShapeTensionAnalyzer
{
    private const double G = 9.80665;
    private const double RelativeTolerance = 0.25;

    public static MooringShapeTensionResult Build(
        CalculationResult result,
        IReadOnlyList<SegmentTensionRow> originalTensionRows,
        MooringShapeForceResult shapeForces)
    {
        if (result.SegmentRows.Count == 0 || shapeForces.Rows.Count == 0)
        {
            return new MooringShapeTensionResult(
                Array.Empty<MooringShapeTensionRow>(),
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                false,
                "Нет сегментов линии или shape-based сил для расчёта альтернативных натяжений.");
        }

        var weightPerMeterByElement = result.ElementRows
            .Where(x => x.Kind == "Линия" && x.LengthM > 0)
            .GroupBy(x => x.Title)
            .ToDictionary(
                x => x.Key,
                x => x.Sum(v => v.WeightWaterKg) / Math.Max(0.0001, x.Sum(v => v.LengthM)));

        var originalByNumber = originalTensionRows.ToDictionary(x => x.Number);
        var shapeForceBySegment = shapeForces.Rows.ToDictionary(x => x.SegmentNumber);
        var workRows = result.SegmentRows
            .OrderBy(x => x.Number)
            .Select(segment =>
            {
                weightPerMeterByElement.TryGetValue(segment.SourceElement, out var weightPerMeterKgM);
                var weightWaterKg = weightPerMeterKgM * segment.SegmentLengthM;
                shapeForceBySegment.TryGetValue(segment.Number, out var shapeForce);
                return new WorkRow(segment, weightWaterKg, shapeForce?.ShapeForceN ?? segment.CurrentForceN);
            })
            .ToList();

        var byNumber = new Dictionary<int, MooringShapeTensionRow>();
        var cumulativeShapeHorizontalForceN = 0.0;
        var cumulativeVerticalForceN = 0.0;

        foreach (var row in workRows.OrderByDescending(x => x.Segment.Number))
        {
            cumulativeShapeHorizontalForceN += row.ShapeSegmentForceN;
            cumulativeVerticalForceN += row.WeightWaterKg * G;

            var shapeTensionN = Math.Sqrt(
                cumulativeShapeHorizontalForceN * cumulativeShapeHorizontalForceN +
                cumulativeVerticalForceN * cumulativeVerticalForceN);
            var shapeTensionKn = shapeTensionN / 1000.0;
            var shapeAngleDeg = Math.Atan2(
                Math.Abs(cumulativeShapeHorizontalForceN),
                Math.Max(0.0001, Math.Abs(cumulativeVerticalForceN))) * 180.0 / Math.PI;

            originalByNumber.TryGetValue(row.Segment.Number, out var original);
            var originalTensionKn = original?.TensionKn ?? 0;
            var originalAngleDeg = original?.AngleFromVerticalDeg ?? 0;
            var tensionDiffKn = shapeTensionKn - originalTensionKn;
            var angleDiffDeg = shapeAngleDeg - originalAngleDeg;
            var relativeDiff = Math.Abs(tensionDiffKn) / Math.Max(0.001, Math.Abs(originalTensionKn));

            byNumber[row.Segment.Number] = new MooringShapeTensionRow(
                row.Segment.Number,
                row.Segment.Number,
                row.Segment.SourceElement,
                row.Segment.EstimatedDepthM,
                row.Segment.SegmentLengthM,
                row.WeightWaterKg,
                row.Segment.CurrentForceN,
                row.ShapeSegmentForceN,
                originalTensionKn,
                shapeTensionKn,
                tensionDiffKn,
                originalAngleDeg,
                shapeAngleDeg,
                angleDiffDeg,
                cumulativeShapeHorizontalForceN,
                cumulativeVerticalForceN,
                relativeDiff <= RelativeTolerance ? "OK" : "INFO: shape-based натяжение заметно отличается");
        }

        var rows = workRows.Select(x => byNumber[x.Segment.Number]).OrderBy(x => x.Number).ToList();
        var maxOriginal = rows.Count > 0 ? rows.Max(x => x.OriginalTensionKn) : 0;
        var maxShape = rows.Count > 0 ? rows.Max(x => x.ShapeTensionKn) : 0;
        var maxDiff = rows.Count > 0 ? rows.Max(x => Math.Abs(x.TensionDifferenceKn)) : 0;
        var maxAngleDiff = rows.Count > 0 ? rows.Max(x => Math.Abs(x.AngleDifferenceDeg)) : 0;
        var topRow = rows.OrderBy(x => x.Number).FirstOrDefault();
        var topOriginal = topRow?.OriginalTensionKn ?? 0;
        var topShape = topRow?.ShapeTensionKn ?? 0;
        var relativeTopDiff = Math.Abs(topShape - topOriginal) / Math.Max(0.001, Math.Abs(topOriginal));
        var withinTolerance = relativeTopDiff <= RelativeTolerance;

        return new MooringShapeTensionResult(
            rows,
            maxOriginal,
            maxShape,
            maxDiff,
            maxAngleDiff,
            topOriginal,
            topShape,
            relativeTopDiff,
            withinTolerance,
            "v0.32: добавлена альтернативная ведомость натяжений от shape-based сил. Она сравнивает старые и новые натяжения, но пока не заменяет основной расчёт и не перестраивает форму.");
    }

    private sealed record WorkRow(SegmentCalculationRow Segment, double WeightWaterKg, double ShapeSegmentForceN);
}
