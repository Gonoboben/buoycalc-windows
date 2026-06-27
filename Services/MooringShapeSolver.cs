using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public enum BuoyShapeState
{
    Unknown,
    Surface,
    Submerged,
    Overloaded
}

public sealed record MooringShapePoint(
    int Number,
    int SegmentNumber,
    string Label,
    double AlongLineM,
    double XOffsetM,
    double ZDepthM,
    double SegmentLengthM,
    double SegmentAngleFromVerticalDeg,
    double SegmentTensionKn,
    string Status);

public sealed record MooringShapeResult(
    IReadOnlyList<MooringShapePoint> Nodes,
    MooringShapePoint? BuoyPoint,
    MooringShapePoint? AnchorPoint,
    BuoyShapeState BuoyState,
    double DepthM,
    double LineLengthM,
    double HorizontalOffsetM,
    double VerticalResidualM,
    bool Converged,
    string MethodNote);

public static class MooringShapeSolver
{
    public static MooringShapeResult Build(EnvironmentInput environment, CalculationResult result)
    {
        var depthM = Math.Max(0, environment.DepthM);
        var lineLengthM = Math.Max(0, result.LineLengthM);
        var nodeRows = MooringNodeAnalyzer.Build(result, depthM);

        var nodes = nodeRows
            .Select(row => new MooringShapePoint(
                row.Number,
                row.SegmentNumber,
                row.SourceElement,
                row.AlongLineM,
                row.XOffsetM,
                row.ZDepthM,
                row.SegmentLengthM,
                row.SegmentAngleFromVerticalDeg,
                row.SegmentTensionKn,
                row.Status))
            .ToList();

        var buoyPoint = nodes.FirstOrDefault();
        var anchorPoint = nodes.LastOrDefault();
        var horizontalOffsetM = anchorPoint?.XOffsetM ?? result.EstimatedOffsetM;
        var anchorDepthM = anchorPoint?.ZDepthM ?? 0;
        var verticalResidualM = Math.Abs(depthM - anchorDepthM);
        var buoyState = DetermineBuoyState(result, depthM, lineLengthM, buoyPoint);

        return new MooringShapeResult(
            nodes,
            buoyPoint,
            anchorPoint,
            buoyState,
            depthM,
            lineLengthM,
            horizontalOffsetM,
            verticalResidualM,
            Converged: false,
            MethodNote: "Предварительная квазистатическая форма: сегменты, локальные силы, оценочные углы, якорное граничное условие Z=Depth. Полная итерационная сходимость ещё не включена.");
    }

    private static BuoyShapeState DetermineBuoyState(CalculationResult result, double depthM, double lineLengthM, MooringShapePoint? buoyPoint)
    {
        if (result.NetBuoyancyKg <= 0)
        {
            return BuoyShapeState.Overloaded;
        }

        if (buoyPoint is null || depthM <= 0)
        {
            return BuoyShapeState.Unknown;
        }

        if (buoyPoint.ZDepthM <= Math.Max(0.05, depthM * 0.01))
        {
            return BuoyShapeState.Surface;
        }

        if (lineLengthM < depthM)
        {
            return BuoyShapeState.Submerged;
        }

        return BuoyShapeState.Surface;
    }
}

public static class MooringShapeStore
{
    public static MooringShapeResult? Current { get; private set; }

    public static void Set(MooringShapeResult shape)
    {
        Current = shape;
    }

    public static void Clear()
    {
        Current = null;
    }
}
