using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record MooringDiscreteLoadTensionRow(
    int Number,
    int SegmentNumber,
    string SourceElement,
    double StartAlongLineM,
    double EndAlongLineM,
    double EstimatedDepthM,
    double SegmentLengthM,
    double SegmentWeightWaterKg,
    double SegmentForceN,
    double DiscreteWeightBelowKg,
    double DiscreteForceBelowN,
    double CumulativeHorizontalForceN,
    double CumulativeVerticalForceN,
    double OriginalTensionKn,
    double DiscreteTensionKn,
    double TensionDifferenceKn,
    double OriginalAngleFromVerticalDeg,
    double DiscreteAngleFromVerticalDeg,
    double AngleDifferenceDeg,
    string Status);

public sealed record MooringDiscreteLoadEntry(
    int Number,
    string Kind,
    string Title,
    double PositionAlongLineM,
    double WeightWaterKg,
    double CurrentForceN);

public sealed record MooringDiscreteLoadTensionResult(
    IReadOnlyList<MooringDiscreteLoadTensionRow> Rows,
    IReadOnlyList<MooringDiscreteLoadEntry> DiscreteLoads,
    double TotalDiscreteWeightWaterKg,
    double TotalDiscreteForceN,
    double MaxOriginalTensionKn,
    double MaxDiscreteTensionKn,
    double MaxTensionDifferenceKn,
    double TopOriginalTensionKn,
    double TopDiscreteTensionKn,
    double RelativeTopTensionDifference,
    double MaxAngleDifferenceDeg,
    bool WithinTolerance,
    string MethodNote);

public static class MooringDiscreteLoadTensionAnalyzer
{
    private const double G = 9.80665;
    private const double RelativeTolerance = 0.25;

    public static MooringDiscreteLoadTensionResult Build(
        CalculationResult result,
        IReadOnlyList<SegmentTensionRow> originalTensionRows,
        MooringSequencePositionResult sequencePositions)
    {
        if (result.SegmentRows.Count == 0 || sequencePositions.Rows.Count == 0)
        {
            return Empty("Нет сегментов линии или позиционной модели для расчёта дискретных нагрузок.");
        }

        var discreteLoads = sequencePositions.Rows
            .Where(x => x.IsDiscrete && x.Kind != "Буй" && x.Kind != "Якорь")
            .Select(x => new MooringDiscreteLoadEntry(
                x.Number,
                x.Kind,
                x.Title,
                x.PositionAlongLineM,
                x.WeightWaterKg,
                x.CurrentForceN))
            .ToList();

        var weightPerMeterByElement = result.ElementRows
            .Where(x => x.Kind == "Линия" && x.LengthM > 0)
            .GroupBy(x => x.Title)
            .ToDictionary(
                x => x.Key,
                x => x.Sum(v => v.WeightWaterKg) / Math.Max(0.0001, x.Sum(v => v.LengthM)));

        var originalByNumber = originalTensionRows.ToDictionary(x => x.Number);
        var rowsBySegment = new Dictionary<int, MooringDiscreteLoadTensionRow>();
        var orderedSegments = result.SegmentRows.OrderBy(x => x.Number).ToList();

        var cumulativeSegmentHorizontalForceN = 0.0;
        var cumulativeSegmentVerticalForceN = 0.0;

        foreach (var segment in orderedSegments.OrderByDescending(x => x.Number))
        {
            weightPerMeterByElement.TryGetValue(segment.SourceElement, out var weightPerMeterKgM);
            var segmentWeightKg = weightPerMeterKgM * segment.SegmentLengthM;
            cumulativeSegmentHorizontalForceN += segment.CurrentForceN;
            cumulativeSegmentVerticalForceN += segmentWeightKg * G;

            var discreteWeightBelowKg = discreteLoads
                .Where(x => x.PositionAlongLineM >= segment.StartLengthM)
                .Sum(x => x.WeightWaterKg);
            var discreteForceBelowN = discreteLoads
                .Where(x => x.PositionAlongLineM >= segment.StartLengthM)
                .Sum(x => x.CurrentForceN);

            var cumulativeHorizontalForceN = cumulativeSegmentHorizontalForceN + discreteForceBelowN;
            var cumulativeVerticalForceN = cumulativeSegmentVerticalForceN + discreteWeightBelowKg * G;
            var discreteTensionN = Math.Sqrt(
                cumulativeHorizontalForceN * cumulativeHorizontalForceN +
                cumulativeVerticalForceN * cumulativeVerticalForceN);
            var discreteTensionKn = discreteTensionN / 1000.0;
            var discreteAngleDeg = Math.Atan2(
                Math.Abs(cumulativeHorizontalForceN),
                Math.Max(0.0001, Math.Abs(cumulativeVerticalForceN))) * 180.0 / Math.PI;

            originalByNumber.TryGetValue(segment.Number, out var original);
            var originalTensionKn = original?.TensionKn ?? 0;
            var originalAngleDeg = original?.AngleFromVerticalDeg ?? 0;
            var tensionDifferenceKn = discreteTensionKn - originalTensionKn;
            var angleDifferenceDeg = discreteAngleDeg - originalAngleDeg;
            var relativeDifference = Math.Abs(tensionDifferenceKn) / Math.Max(0.001, Math.Abs(originalTensionKn));

            rowsBySegment[segment.Number] = new MooringDiscreteLoadTensionRow(
                segment.Number,
                segment.Number,
                segment.SourceElement,
                segment.StartLengthM,
                segment.EndLengthM,
                segment.EstimatedDepthM,
                segment.SegmentLengthM,
                segmentWeightKg,
                segment.CurrentForceN,
                discreteWeightBelowKg,
                discreteForceBelowN,
                cumulativeHorizontalForceN,
                cumulativeVerticalForceN,
                originalTensionKn,
                discreteTensionKn,
                tensionDifferenceKn,
                originalAngleDeg,
                discreteAngleDeg,
                angleDifferenceDeg,
                relativeDifference <= RelativeTolerance ? "OK" : "INFO: дискретные нагрузки заметно меняют натяжение");
        }

        var rows = orderedSegments.Select(x => rowsBySegment[x.Number]).ToList();
        var maxOriginal = rows.Count > 0 ? rows.Max(x => x.OriginalTensionKn) : 0;
        var maxDiscrete = rows.Count > 0 ? rows.Max(x => x.DiscreteTensionKn) : 0;
        var maxDifference = rows.Count > 0 ? rows.Max(x => Math.Abs(x.TensionDifferenceKn)) : 0;
        var maxAngleDifference = rows.Count > 0 ? rows.Max(x => Math.Abs(x.AngleDifferenceDeg)) : 0;
        var topRow = rows.OrderBy(x => x.Number).FirstOrDefault();
        var topOriginal = topRow?.OriginalTensionKn ?? 0;
        var topDiscrete = topRow?.DiscreteTensionKn ?? 0;
        var relativeTopDifference = Math.Abs(topDiscrete - topOriginal) / Math.Max(0.001, Math.Abs(topOriginal));

        return new MooringDiscreteLoadTensionResult(
            rows,
            discreteLoads,
            discreteLoads.Sum(x => x.WeightWaterKg),
            discreteLoads.Sum(x => x.CurrentForceN),
            maxOriginal,
            maxDiscrete,
            maxDifference,
            topOriginal,
            topDiscrete,
            relativeTopDifference,
            maxAngleDifference,
            relativeTopDifference <= RelativeTolerance,
            "v0.34: дискретные элементы с координатой s добавлены в альтернативную ведомость натяжений как локальные нагрузки ниже рассматриваемого сечения. Форма X/Z пока не перестраивается по этим натяжениям.");
    }

    private static MooringDiscreteLoadTensionResult Empty(string note)
    {
        return new MooringDiscreteLoadTensionResult(
            Array.Empty<MooringDiscreteLoadTensionRow>(),
            Array.Empty<MooringDiscreteLoadEntry>(),
            0,
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
}
