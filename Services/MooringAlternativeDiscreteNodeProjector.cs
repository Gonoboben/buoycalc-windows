using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record MooringAlternativeDiscreteNodeRow(
    int Number,
    int ElementNumber,
    string Kind,
    string Title,
    string PresetName,
    double PositionAlongLineM,
    double AlternativeXOffsetM,
    double AlternativeZDepthM,
    double OriginalXOffsetM,
    double OriginalZDepthM,
    double DeltaXM,
    double DeltaZM,
    double WeightWaterKg,
    double CurrentForceN,
    string NodeRole,
    string Status);

public sealed record MooringAlternativeDiscreteNodeResult(
    IReadOnlyList<MooringAlternativeDiscreteNodeRow> Rows,
    int DiscreteNodeCount,
    double TotalDiscreteWeightWaterKg,
    double TotalDiscreteForceN,
    double MaxNodeDeltaM,
    string MethodNote);

public static class MooringAlternativeDiscreteNodeProjector
{
    public static MooringAlternativeDiscreteNodeResult Build(
        MooringSequencePositionResult positions,
        MooringDiscreteLoadShapeResult alternativeShape,
        MooringShapeResult originalShape)
    {
        if (positions.Rows.Count == 0 || alternativeShape.Rows.Count == 0)
        {
            return Empty("Нет позиционной модели или альтернативной формы для проекции дискретных X/Z-узлов.");
        }

        var candidateRows = positions.Rows
            .Where(x => x.IsDiscrete)
            .OrderBy(x => x.PositionAlongLineM)
            .ThenBy(x => x.Number)
            .ToList();

        var rows = new List<MooringAlternativeDiscreteNodeRow>();
        foreach (var item in candidateRows)
        {
            var alternativePoint = InterpolateAlternative(alternativeShape.Rows, item.PositionAlongLineM);
            var originalPoint = InterpolateOriginal(originalShape, item.PositionAlongLineM);
            var deltaX = alternativePoint.X - originalPoint.X;
            var deltaZ = alternativePoint.Z - originalPoint.Z;
            var nodeDeltaM = Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
            var isInternalDiscreteLoad = item.Kind != "Буй" && item.Kind != "Якорь";

            rows.Add(new MooringAlternativeDiscreteNodeRow(
                rows.Count + 1,
                item.Number,
                item.Kind,
                item.Title,
                item.PresetName,
                item.PositionAlongLineM,
                alternativePoint.X,
                alternativePoint.Z,
                originalPoint.X,
                originalPoint.Z,
                deltaX,
                deltaZ,
                item.WeightWaterKg,
                item.CurrentForceN,
                item.Kind switch
                {
                    "Буй" => "верхний граничный X/Z-узел",
                    "Якорь" => "нижний граничный X/Z-узел",
                    _ => "внутренний дискретный X/Z-узел альтернативной формы"
                },
                isInternalDiscreteLoad ? "INFO: узел спроецирован на альтернативную форму" : "INFO: граничный узел"));
        }

        var internalRows = rows.Where(x => x.Kind != "Буй" && x.Kind != "Якорь").ToList();
        return new MooringAlternativeDiscreteNodeResult(
            rows,
            internalRows.Count,
            internalRows.Sum(x => x.WeightWaterKg),
            internalRows.Sum(x => x.CurrentForceN),
            rows.Count > 0 ? rows.Max(x => Math.Sqrt(x.DeltaXM * x.DeltaXM + x.DeltaZM * x.DeltaZM)) : 0,
            "v0.36: дискретные элементы спроецированы на альтернативную X/Z-форму как отдельные точки. Это отчётный слой; основной solver и 2D-визуализация пока не заменены.");
    }

    private static MooringAlternativeDiscreteNodeResult Empty(string note)
    {
        return new MooringAlternativeDiscreteNodeResult(
            Array.Empty<MooringAlternativeDiscreteNodeRow>(),
            0,
            0,
            0,
            0,
            note);
    }

    private static PointXZ InterpolateAlternative(IReadOnlyList<MooringDiscreteLoadShapeRow> rows, double sM)
    {
        if (rows.Count == 0)
        {
            return new PointXZ(0, 0);
        }

        var ordered = rows.OrderBy(x => x.AlongLineM).ToList();
        if (sM <= ordered.First().AlongLineM)
        {
            return new PointXZ(ordered.First().XOffsetM, ordered.First().ZDepthM);
        }

        if (sM >= ordered.Last().AlongLineM)
        {
            return new PointXZ(ordered.Last().XOffsetM, ordered.Last().ZDepthM);
        }

        for (var i = 1; i < ordered.Count; i++)
        {
            var upper = ordered[i - 1];
            var lower = ordered[i];
            if (sM > lower.AlongLineM)
            {
                continue;
            }

            var span = Math.Max(0.0001, lower.AlongLineM - upper.AlongLineM);
            var t = Math.Clamp((sM - upper.AlongLineM) / span, 0, 1);
            return new PointXZ(
                upper.XOffsetM + (lower.XOffsetM - upper.XOffsetM) * t,
                upper.ZDepthM + (lower.ZDepthM - upper.ZDepthM) * t);
        }

        return new PointXZ(ordered.Last().XOffsetM, ordered.Last().ZDepthM);
    }

    private static PointXZ InterpolateOriginal(MooringShapeResult shape, double sM)
    {
        if (shape.Nodes.Count == 0)
        {
            return new PointXZ(0, 0);
        }

        var ordered = shape.Nodes.OrderBy(x => x.AlongLineM).ToList();
        if (sM <= ordered.First().AlongLineM)
        {
            return new PointXZ(ordered.First().XOffsetM, ordered.First().ZDepthM);
        }

        if (sM >= ordered.Last().AlongLineM)
        {
            return new PointXZ(ordered.Last().XOffsetM, ordered.Last().ZDepthM);
        }

        for (var i = 1; i < ordered.Count; i++)
        {
            var upper = ordered[i - 1];
            var lower = ordered[i];
            if (sM > lower.AlongLineM)
            {
                continue;
            }

            var span = Math.Max(0.0001, lower.AlongLineM - upper.AlongLineM);
            var t = Math.Clamp((sM - upper.AlongLineM) / span, 0, 1);
            return new PointXZ(
                upper.XOffsetM + (lower.XOffsetM - upper.XOffsetM) * t,
                upper.ZDepthM + (lower.ZDepthM - upper.ZDepthM) * t);
        }

        return new PointXZ(ordered.Last().XOffsetM, ordered.Last().ZDepthM);
    }

    private sealed record PointXZ(double X, double Z);
}
