using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record MooringShapeForceRow(
    int Number,
    int SegmentNumber,
    string Label,
    double SegmentLengthM,
    double LocalSpeedMS,
    double NormalSpeedMS,
    double AngleFromVerticalDeg,
    double OriginalForceN,
    double ShapeForceN,
    double DifferenceN,
    double Ratio,
    string Status);

public sealed record MooringShapeForceResult(
    IReadOnlyList<MooringShapeForceRow> Rows,
    double OriginalLineForceN,
    double ShapeLineForceN,
    double DifferenceN,
    double RelativeDifference,
    double MaxRowDifferenceN,
    bool WithinTolerance,
    string MethodNote);

public static class MooringShapeForceAnalyzer
{
    private const double RelativeTolerance = 0.25;

    public static MooringShapeForceResult Build(CalculationResult result, MooringShapeProjectionResult projection)
    {
        if (result.SegmentRows.Count == 0 || projection.Rows.Count == 0)
        {
            return new MooringShapeForceResult(
                Array.Empty<MooringShapeForceRow>(),
                0,
                0,
                0,
                0,
                0,
                false,
                "Нет сегментов линии или проекций формы для shape-based оценки сил.");
        }

        var segmentsByNumber = result.SegmentRows.ToDictionary(x => x.Number);
        var rows = new List<MooringShapeForceRow>();

        foreach (var projectionRow in projection.Rows)
        {
            if (!segmentsByNumber.TryGetValue(projectionRow.SegmentNumber, out var segment))
            {
                continue;
            }

            var segmentLengthM = Math.Max(0.0001, projectionRow.SegmentLengthM);
            var tx = projectionRow.DeltaXM / segmentLengthM;
            var tz = projectionRow.DeltaZM / segmentLengthM;

            var currentX = segment.LocalSpeedMS;
            var currentZ = segment.VerticalCurrentMS;
            var speed = Math.Sqrt(currentX * currentX + currentZ * currentZ);
            var dot = currentX * tx + currentZ * tz;
            var normalSpeed = Math.Sqrt(Math.Max(0, speed * speed - dot * dot));

            var shapeForceN = 0.5 * segment.WaterDensityKgM3 * segment.DragCoefficient * segment.ProjectedAreaM2 * normalSpeed * normalSpeed;
            var originalForceN = segment.CurrentForceN;
            var differenceN = shapeForceN - originalForceN;
            var ratio = Math.Abs(originalForceN) > 0.0001 ? shapeForceN / originalForceN : 0;
            var relativeDifference = Math.Abs(differenceN) / Math.Max(1.0, Math.Abs(originalForceN));
            var status = relativeDifference <= RelativeTolerance ? "OK" : "INFO: ориентация заметно меняет силу";

            rows.Add(new MooringShapeForceRow(
                rows.Count + 1,
                projectionRow.SegmentNumber,
                projectionRow.Label,
                projectionRow.SegmentLengthM,
                speed,
                normalSpeed,
                projectionRow.AngleFromVerticalDeg,
                originalForceN,
                shapeForceN,
                differenceN,
                ratio,
                status));
        }

        var originalLineForceN = rows.Sum(x => x.OriginalForceN);
        var shapeLineForceN = rows.Sum(x => x.ShapeForceN);
        var difference = shapeLineForceN - originalLineForceN;
        var relative = Math.Abs(difference) / Math.Max(1.0, Math.Abs(originalLineForceN));
        var maxRowDifference = rows.Count > 0 ? rows.Max(x => Math.Abs(x.DifferenceN)) : 0;
        var withinTolerance = relative <= RelativeTolerance;

        return new MooringShapeForceResult(
            rows,
            originalLineForceN,
            shapeLineForceN,
            difference,
            relative,
            maxRowDifference,
            withinTolerance,
            "v0.31: добавлена shape-based оценка сил линии. Для каждого сегмента используется нормальная составляющая скорости к фактической X/Z-ориентации сегмента. Эта ведомость пока сравнивает силы, но ещё не подставляет их обратно в основной solver.");
    }
}
