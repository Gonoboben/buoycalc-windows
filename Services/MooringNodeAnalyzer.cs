using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record MooringNodeRow(
    int Number,
    int SegmentNumber,
    string SourceElement,
    double AlongLineM,
    double XOffsetM,
    double ZDepthM,
    double SegmentLengthM,
    double SegmentAngleFromVerticalDeg,
    double SegmentTensionKn,
    string Status);

public static class MooringNodeAnalyzer
{
    public static IReadOnlyList<MooringNodeRow> Build(CalculationResult result)
    {
        if (result.SegmentRows.Count == 0)
        {
            return Array.Empty<MooringNodeRow>();
        }

        var orderedSegments = result.SegmentRows.OrderBy(x => x.Number).ToList();
        var tensionRows = SegmentTensionAnalyzer.Build(result)
            .ToDictionary(x => x.Number);

        var firstSegment = orderedSegments.First();
        var lastSegment = orderedSegments.Last();
        tensionRows.TryGetValue(firstSegment.Number, out var firstTension);

        var workRows = new List<NodeWorkRow>();
        var alongLineM = 0.0;
        var rawX = 0.0;
        var rawZ = 0.0;

        workRows.Add(new NodeWorkRow(
            0,
            firstSegment.Number,
            "Буй / верхний конец линии",
            alongLineM,
            rawX,
            rawZ,
            0,
            firstTension?.AngleFromVerticalDeg ?? 0,
            firstTension?.TensionKn ?? 0,
            "INFO: буй, граничный узел"));

        foreach (var segment in orderedSegments)
        {
            tensionRows.TryGetValue(segment.Number, out var tension);
            var angleDeg = tension?.AngleFromVerticalDeg ?? 0;
            var angleRad = angleDeg * Math.PI / 180.0;

            var dx = segment.SegmentLengthM * Math.Sin(angleRad);
            var dz = segment.SegmentLengthM * Math.Cos(angleRad);

            rawX += dx;
            rawZ += dz;
            alongLineM += segment.SegmentLengthM;

            var isBottomNode = segment.Number == lastSegment.Number;
            workRows.Add(new NodeWorkRow(
                workRows.Count,
                segment.Number,
                isBottomNode ? "Якорь / нижний конец линии" : segment.SourceElement,
                alongLineM,
                rawX,
                rawZ,
                segment.SegmentLengthM,
                angleDeg,
                tension?.TensionKn ?? 0,
                isBottomNode ? "INFO: якорь, граничный узел" : "OK"));
        }

        var estimatedDepthFromSegments = result.SegmentRows.Max(x => x.EstimatedDepthM);
        var scaleZ = rawZ > 0 && estimatedDepthFromSegments > 0 ? estimatedDepthFromSegments / rawZ : 1.0;
        var scaleX = scaleZ;

        return workRows
            .Select(row => new MooringNodeRow(
                row.Number,
                row.SegmentNumber,
                row.SourceElement,
                row.AlongLineM,
                row.RawX * scaleX,
                row.RawZ * scaleZ,
                row.SegmentLengthM,
                row.SegmentAngleFromVerticalDeg,
                row.SegmentTensionKn,
                row.Status))
            .ToList();
    }

    private sealed record NodeWorkRow(
        int Number,
        int SegmentNumber,
        string SourceElement,
        double AlongLineM,
        double RawX,
        double RawZ,
        double SegmentLengthM,
        double SegmentAngleFromVerticalDeg,
        double SegmentTensionKn,
        string Status);
}
