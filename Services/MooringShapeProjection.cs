using System;
using System.Collections.Generic;
using System.Linq;

namespace BuoyCalc.Windows.Services;

public sealed record MooringShapeProjectionRow(
    int Number,
    int SegmentNumber,
    string Label,
    double SegmentLengthM,
    double DeltaXM,
    double DeltaZM,
    double ProjectedLengthM,
    double LengthResidualM,
    double AngleFromVerticalDeg,
    double TensionKn,
    string Status);

public sealed record MooringShapeProjectionResult(
    IReadOnlyList<MooringShapeProjectionRow> Rows,
    double SumDeltaXM,
    double SumDeltaZM,
    double TotalSegmentLengthM,
    double TotalProjectedLengthM,
    double LengthResidualM,
    double EndpointHorizontalOffsetM,
    double EndpointVerticalSpanM,
    double EndpointResidualXM,
    double EndpointResidualZM,
    double MaxAngleFromVerticalDeg,
    double AverageAngleFromVerticalDeg,
    bool GeometryClosed,
    string MethodNote);

public static class MooringShapeProjection
{
    private const double GeometryToleranceM = 0.01;

    public static MooringShapeProjectionResult Build(MooringShapeResult shape)
    {
        if (shape.Nodes.Count < 2)
        {
            return new MooringShapeProjectionResult(
                Array.Empty<MooringShapeProjectionRow>(),
                0,
                0,
                0,
                0,
                shape.LineLengthM,
                0,
                0,
                0,
                0,
                0,
                0,
                false,
                "Нет достаточного количества узлов формы для расчёта проекций.");
        }

        var rows = new List<MooringShapeProjectionRow>();
        for (var i = 1; i < shape.Nodes.Count; i++)
        {
            var previous = shape.Nodes[i - 1];
            var current = shape.Nodes[i];
            var dxM = current.XOffsetM - previous.XOffsetM;
            var dzM = current.ZDepthM - previous.ZDepthM;
            var projectedLengthM = Math.Sqrt(dxM * dxM + dzM * dzM);
            var segmentLengthM = Math.Max(0, current.SegmentLengthM);
            var lengthResidualM = Math.Abs(projectedLengthM - segmentLengthM);
            var angleFromVerticalDeg = Math.Atan2(Math.Abs(dxM), Math.Max(0.0001, Math.Abs(dzM))) * 180.0 / Math.PI;

            rows.Add(new MooringShapeProjectionRow(
                rows.Count + 1,
                current.SegmentNumber,
                current.Label,
                segmentLengthM,
                dxM,
                dzM,
                projectedLengthM,
                lengthResidualM,
                angleFromVerticalDeg,
                current.SegmentTensionKn,
                lengthResidualM <= GeometryToleranceM ? "OK" : "WARNING"));
        }

        var sumDxM = rows.Sum(x => x.DeltaXM);
        var sumDzM = rows.Sum(x => x.DeltaZM);
        var totalSegmentLengthM = rows.Sum(x => x.SegmentLengthM);
        var totalProjectedLengthM = rows.Sum(x => x.ProjectedLengthM);
        var lengthResidual = Math.Abs(totalProjectedLengthM - totalSegmentLengthM);
        var endpointHorizontalOffsetM = (shape.AnchorPoint?.XOffsetM ?? 0) - (shape.BuoyPoint?.XOffsetM ?? 0);
        var endpointVerticalSpanM = (shape.AnchorPoint?.ZDepthM ?? 0) - (shape.BuoyPoint?.ZDepthM ?? 0);
        var endpointResidualXM = Math.Abs(sumDxM - endpointHorizontalOffsetM);
        var endpointResidualZM = Math.Abs(sumDzM - endpointVerticalSpanM);
        var maxAngle = rows.Count > 0 ? rows.Max(x => x.AngleFromVerticalDeg) : 0;
        var averageAngle = rows.Count > 0 ? rows.Average(x => x.AngleFromVerticalDeg) : 0;
        var geometryClosed = lengthResidual <= GeometryToleranceM &&
            endpointResidualXM <= GeometryToleranceM &&
            endpointResidualZM <= GeometryToleranceM &&
            rows.All(x => x.LengthResidualM <= GeometryToleranceM);

        return new MooringShapeProjectionResult(
            rows,
            sumDxM,
            sumDzM,
            totalSegmentLengthM,
            totalProjectedLengthM,
            lengthResidual,
            endpointHorizontalOffsetM,
            endpointVerticalSpanM,
            endpointResidualXM,
            endpointResidualZM,
            maxAngle,
            averageAngle,
            geometryClosed,
            "v0.30: форма X/Z разложена на проекции сегментов. Это связывает результат MooringShapeSolver с будущим пересчётом локальных сил по фактической ориентации сегментов, но ещё не замыкает полный нелинейный баланс.");
    }
}
