using System;
using System.Collections.Generic;
using System.Linq;

namespace BuoyCalc.Windows.Services;

public sealed record MooringDiscreteLoadShapeRow(
    int Number,
    int SegmentNumber,
    string SourceElement,
    double AlongLineM,
    double XOffsetM,
    double ZDepthM,
    double SegmentLengthM,
    double OriginalAngleFromVerticalDeg,
    double DiscreteAngleFromVerticalDeg,
    double UsedAngleFromVerticalDeg,
    double DiscreteTensionKn,
    double OriginalXOffsetM,
    double OriginalZDepthM,
    double DeltaXM,
    double DeltaZM,
    string Status);

public sealed record MooringDiscreteLoadShapeResult(
    IReadOnlyList<MooringDiscreteLoadShapeRow> Rows,
    double OriginalHorizontalOffsetM,
    double DiscreteHorizontalOffsetM,
    double OffsetDifferenceM,
    double AnchorDepthM,
    double VerticalResidualM,
    double MaxNodeDeltaM,
    double AngleScale,
    int IterationCount,
    bool Converged,
    string MethodNote);

public static class MooringDiscreteLoadShapeBuilder
{
    private const double DepthToleranceM = 0.01;
    private const int MaxIterations = 60;

    public static MooringDiscreteLoadShapeResult Build(
        MooringShapeResult originalShape,
        MooringDiscreteLoadTensionResult discreteTensions)
    {
        if (originalShape.Nodes.Count == 0 || discreteTensions.Rows.Count == 0)
        {
            return Empty("Нет исходной формы или натяжений с дискретными нагрузками для построения альтернативной формы.");
        }

        var lineLengthM = Math.Max(0, originalShape.LineLengthM);
        var targetDepthM = Math.Max(0, originalShape.DepthM);
        if (lineLengthM <= 0 || targetDepthM <= 0)
        {
            return Empty("Нет длины линии или проектной глубины для построения альтернативной формы.");
        }

        var rows = discreteTensions.Rows.OrderBy(x => x.Number).ToList();
        var scaleResult = SolveAngleScale(rows, lineLengthM, targetDepthM);
        var verticalSpanM = VerticalSpan(rows, lineLengthM, targetDepthM, scaleResult.AngleScale);
        var topNodeDepthM = Math.Max(0, targetDepthM - verticalSpanM);
        var originalBySegment = originalShape.Nodes
            .GroupBy(x => x.SegmentNumber)
            .ToDictionary(x => x.Key, x => x.Last());

        var output = new List<MooringDiscreteLoadShapeRow>();
        var alongLineM = 0.0;
        var xOffsetM = 0.0;
        var zDepthM = topNodeDepthM;

        output.Add(new MooringDiscreteLoadShapeRow(
            0,
            rows.First().SegmentNumber,
            "Буй / верхний конец линии",
            0,
            0,
            topNodeDepthM,
            0,
            originalShape.BuoyPoint?.SegmentAngleFromVerticalDeg ?? 0,
            0,
            0,
            0,
            originalShape.BuoyPoint?.XOffsetM ?? 0,
            originalShape.BuoyPoint?.ZDepthM ?? 0,
            0,
            topNodeDepthM - (originalShape.BuoyPoint?.ZDepthM ?? 0),
            "INFO: верхний граничный узел"));

        foreach (var row in rows)
        {
            var segmentLengthM = Math.Max(0, row.SegmentLengthM);
            var usedAngleDeg = ScaleAngle(row.DiscreteAngleFromVerticalDeg, scaleResult.AngleScale, lineLengthM, targetDepthM);
            var angleRad = usedAngleDeg * Math.PI / 180.0;
            xOffsetM += segmentLengthM * Math.Sin(angleRad);
            zDepthM += segmentLengthM * Math.Cos(angleRad);
            alongLineM += segmentLengthM;

            var isBottom = row.Number == rows.Last().Number;
            var finalZDepthM = isBottom ? targetDepthM : zDepthM;
            originalBySegment.TryGetValue(row.SegmentNumber, out var originalNode);
            var originalX = originalNode?.XOffsetM ?? 0;
            var originalZ = originalNode?.ZDepthM ?? 0;
            var deltaX = xOffsetM - originalX;
            var deltaZ = finalZDepthM - originalZ;
            var nodeDelta = Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ);

            output.Add(new MooringDiscreteLoadShapeRow(
                output.Count,
                row.SegmentNumber,
                isBottom ? "Якорь / нижний конец линии" : row.SourceElement,
                alongLineM,
                xOffsetM,
                finalZDepthM,
                segmentLengthM,
                row.OriginalAngleFromVerticalDeg,
                row.DiscreteAngleFromVerticalDeg,
                usedAngleDeg,
                row.DiscreteTensionKn,
                originalX,
                originalZ,
                deltaX,
                deltaZ,
                nodeDelta <= 0.01 ? "OK" : "INFO: форма отличается от исходной"));
        }

        var anchor = output.Last();
        var verticalResidualM = Math.Abs(anchor.ZDepthM - targetDepthM);
        var maxNodeDelta = output.Count > 0 ? output.Max(x => Math.Sqrt(x.DeltaXM * x.DeltaXM + x.DeltaZM * x.DeltaZM)) : 0;
        var offsetDifference = anchor.XOffsetM - originalShape.HorizontalOffsetM;

        return new MooringDiscreteLoadShapeResult(
            output,
            originalShape.HorizontalOffsetM,
            anchor.XOffsetM,
            offsetDifference,
            anchor.ZDepthM,
            verticalResidualM,
            maxNodeDelta,
            scaleResult.AngleScale,
            scaleResult.Iterations,
            scaleResult.Converged && verticalResidualM <= DepthToleranceM,
            "v0.35: построена альтернативная форма X/Z по натяжениям с дискретными нагрузками. Это сравнительная форма: она показывает влияние приборов и соединителей на углы и снос, но ещё не заменяет основной solver.");
    }

    private static MooringDiscreteLoadShapeResult Empty(string note)
    {
        return new MooringDiscreteLoadShapeResult(
            Array.Empty<MooringDiscreteLoadShapeRow>(),
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            false,
            note);
    }

    private static IterationResult SolveAngleScale(
        IReadOnlyList<MooringDiscreteLoadTensionRow> rows,
        double lineLengthM,
        double targetDepthM)
    {
        if (lineLengthM <= targetDepthM)
        {
            return new IterationResult(1.0, 0, true);
        }

        var low = 0.0;
        var high = 1.0;
        var iterations = 0;

        while (VerticalSpan(rows, lineLengthM, targetDepthM, high) > targetDepthM && high < 128)
        {
            high *= 2.0;
            iterations++;
        }

        for (; iterations < MaxIterations; iterations++)
        {
            var mid = (low + high) / 2.0;
            var span = VerticalSpan(rows, lineLengthM, targetDepthM, mid);
            var residual = Math.Abs(span - targetDepthM);

            if (residual <= DepthToleranceM)
            {
                return new IterationResult(mid, iterations + 1, true);
            }

            if (span > targetDepthM)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        var finalScale = (low + high) / 2.0;
        var finalResidual = Math.Abs(VerticalSpan(rows, lineLengthM, targetDepthM, finalScale) - targetDepthM);
        return new IterationResult(finalScale, iterations, finalResidual <= DepthToleranceM);
    }

    private static double VerticalSpan(
        IReadOnlyList<MooringDiscreteLoadTensionRow> rows,
        double lineLengthM,
        double targetDepthM,
        double angleScale)
    {
        return rows.Sum(row =>
        {
            var angleDeg = ScaleAngle(row.DiscreteAngleFromVerticalDeg, angleScale, lineLengthM, targetDepthM);
            var angleRad = angleDeg * Math.PI / 180.0;
            return Math.Max(0, row.SegmentLengthM) * Math.Cos(angleRad);
        });
    }

    private static double ScaleAngle(double discreteAngleDeg, double angleScale, double lineLengthM, double targetDepthM)
    {
        var geometricAngleDeg = lineLengthM > targetDepthM && lineLengthM > 0
            ? Math.Acos(Math.Clamp(targetDepthM / lineLengthM, 0, 1)) * 180.0 / Math.PI
            : 0;

        var baseAngleDeg = Math.Abs(discreteAngleDeg) > 0.01
            ? Math.Abs(discreteAngleDeg)
            : geometricAngleDeg;

        return Math.Clamp(baseAngleDeg * Math.Max(0, angleScale), 0, 89.0);
    }

    private sealed record IterationResult(double AngleScale, int Iterations, bool Converged);
}
