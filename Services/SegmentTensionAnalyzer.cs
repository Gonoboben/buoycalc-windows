using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record SegmentTensionRow(
    int Number,
    string SourceElement,
    double EstimatedDepthM,
    double SegmentLengthM,
    double WeightWaterKg,
    double SegmentCurrentForceN,
    double CumulativeHorizontalForceN,
    double CumulativeVerticalForceN,
    double TensionKn,
    double AngleFromVerticalDeg,
    string Status);

public static class SegmentTensionAnalyzer
{
    private const double G = 9.80665;

    public static IReadOnlyList<SegmentTensionRow> Build(CalculationResult result)
    {
        if (result.SegmentRows.Count == 0)
        {
            return Array.Empty<SegmentTensionRow>();
        }

        var weightPerMeterByElement = result.ElementRows
            .Where(x => x.Kind == "Линия" && x.LengthM > 0)
            .GroupBy(x => x.Title)
            .ToDictionary(
                x => x.Key,
                x => x.Sum(v => v.WeightWaterKg) / Math.Max(0.0001, x.Sum(v => v.LengthM)));

        var topToBottom = result.SegmentRows
            .Select(segment =>
            {
                weightPerMeterByElement.TryGetValue(segment.SourceElement, out var weightPerMeterKgM);
                var weightWaterKg = weightPerMeterKgM * segment.SegmentLengthM;
                return new SegmentWorkRow(segment, weightWaterKg);
            })
            .ToList();

        var byNumber = new Dictionary<int, SegmentTensionRow>();
        var cumulativeHorizontalForceN = 0.0;
        var cumulativeVerticalForceN = 0.0;

        foreach (var row in topToBottom.OrderByDescending(x => x.Segment.Number))
        {
            cumulativeHorizontalForceN += row.Segment.CurrentForceN;
            cumulativeVerticalForceN += row.WeightWaterKg * G;

            var tensionN = Math.Sqrt(
                cumulativeHorizontalForceN * cumulativeHorizontalForceN +
                cumulativeVerticalForceN * cumulativeVerticalForceN);

            var tensionKn = tensionN / 1000.0;
            var angleFromVerticalDeg = Math.Atan2(
                Math.Abs(cumulativeHorizontalForceN),
                Math.Max(0.0001, Math.Abs(cumulativeVerticalForceN))) * 180.0 / Math.PI;

            byNumber[row.Segment.Number] = new SegmentTensionRow(
                row.Segment.Number,
                row.Segment.SourceElement,
                row.Segment.EstimatedDepthM,
                row.Segment.SegmentLengthM,
                row.WeightWaterKg,
                row.Segment.CurrentForceN,
                cumulativeHorizontalForceN,
                cumulativeVerticalForceN,
                tensionKn,
                angleFromVerticalDeg,
                tensionKn <= 0 ? "INFO" : "OK");
        }

        return topToBottom
            .Select(x => byNumber[x.Segment.Number])
            .OrderBy(x => x.Number)
            .ToList();
    }

    private sealed record SegmentWorkRow(SegmentCalculationRow Segment, double WeightWaterKg);
}
