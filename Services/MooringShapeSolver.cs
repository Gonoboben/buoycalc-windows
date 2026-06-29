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
    string MethodNote,
    int IterationCount,
    double ConvergenceResidualM,
    double AngleScale,
    string ConvergenceCriterion);

public static class MooringShapeSolver
{
    private const double DepthToleranceM = 0.01;
    private const int MaxIterations = 60;

    public static MooringShapeResult Build(EnvironmentInput environment, CalculationResult result)
    {
        var depthM = Math.Max(0, environment.DepthM);
        var lineLengthM = Math.Max(0, result.LineLengthM);
        var targetAnchorDepthM = depthM > 0
            ? depthM
            : result.SegmentRows.Count > 0 ? result.SegmentRows.Max(x => x.EstimatedDepthM) : 0;

        if (result.SegmentRows.Count == 0 || lineLengthM <= 0 || targetAnchorDepthM <= 0)
        {
            return EmptyResult(depthM, lineLengthM, "Нет сегментов линии, длины линии или проектной глубины для построения формы.");
        }

        var orderedSegments = result.SegmentRows.OrderBy(x => x.Number).ToList();
        var tensionRows = SegmentTensionAnalyzer.Build(result).ToDictionary(x => x.Number);
        var iteration = SolveAngleScale(orderedSegments, tensionRows, lineLengthM, targetAnchorDepthM);
        var nodes = BuildNodes(orderedSegments, tensionRows, targetAnchorDepthM, iteration.AngleScale);

        var buoyPoint = nodes.FirstOrDefault();
        var anchorPoint = nodes.LastOrDefault();
        var horizontalOffsetM = anchorPoint?.XOffsetM ?? result.EstimatedOffsetM;
        var anchorDepthM = anchorPoint?.ZDepthM ?? 0;
        var verticalResidualM = Math.Abs(targetAnchorDepthM - anchorDepthM);
        var buoyState = DetermineBuoyState(result, targetAnchorDepthM, lineLengthM, buoyPoint);
        var lineShorterThanDepth = targetAnchorDepthM > 0 && lineLengthM + DepthToleranceM < targetAnchorDepthM;
        var converged = iteration.ResidualM <= DepthToleranceM && iteration.Converged && !lineShorterThanDepth;
        var methodNote = lineShorterThanDepth
            ? "v0.38.2: линия короче глубины. Форма построена как геометрия погружённого буя, но не считается сходимостью нормальной поверхностной постановки. Волновая нагрузка при этом не отключается."
            : "v0.38.2: итерационная геометрическая сходимость формы включена. Углы сегментов берутся из накопленных сил, затем масштаб углов подбирается бисекцией так, чтобы якорный узел попал на проектную глубину. Это ещё не полный нелинейный solver равновесия сил и формы.";
        var criterion = lineShorterThanDepth
            ? $"Для поверхностной постановки требуется L ≥ Depth; сейчас L={lineLengthM:0.####} м < Depth={targetAnchorDepthM:0.####} м"
            : $"|Zякоря - Depth| ≤ {DepthToleranceM:0.####} м";

        return new MooringShapeResult(
            nodes,
            buoyPoint,
            anchorPoint,
            buoyState,
            targetAnchorDepthM,
            lineLengthM,
            horizontalOffsetM,
            verticalResidualM,
            converged,
            methodNote,
            iteration.Iterations,
            iteration.ResidualM,
            iteration.AngleScale,
            criterion);
    }

    private static MooringShapeResult EmptyResult(double depthM, double lineLengthM, string note)
    {
        return new MooringShapeResult(
            Array.Empty<MooringShapePoint>(),
            null,
            null,
            BuoyShapeState.Unknown,
            depthM,
            lineLengthM,
            0,
            depthM,
            false,
            note,
            0,
            depthM,
            0,
            $"|Zякоря - Depth| ≤ {DepthToleranceM:0.####} м");
    }

    private static IterationResult SolveAngleScale(
        IReadOnlyList<SegmentCalculationRow> orderedSegments,
        IReadOnlyDictionary<int, SegmentTensionRow> tensionRows,
        double lineLengthM,
        double targetAnchorDepthM)
    {
        if (lineLengthM <= targetAnchorDepthM)
        {
            var residual = Math.Abs(targetAnchorDepthM - lineLengthM);
            return new IterationResult(1.0, 0, lineLengthM >= targetAnchorDepthM, residual);
        }

        var targetVerticalSpanM = targetAnchorDepthM;
        var low = 0.0;
        var high = 1.0;
        var iterations = 0;

        while (VerticalSpan(orderedSegments, tensionRows, high, lineLengthM, targetAnchorDepthM) > targetVerticalSpanM && high < 128)
        {
            high *= 2.0;
            iterations++;
        }

        for (; iterations < MaxIterations; iterations++)
        {
            var mid = (low + high) / 2.0;
            var span = VerticalSpan(orderedSegments, tensionRows, mid, lineLengthM, targetAnchorDepthM);
            var residual = Math.Abs(span - targetVerticalSpanM);

            if (residual <= DepthToleranceM)
            {
                return new IterationResult(mid, iterations + 1, true, residual);
            }

            if (span > targetVerticalSpanM)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        var finalScale = (low + high) / 2.0;
        var finalResidual = Math.Abs(VerticalSpan(orderedSegments, tensionRows, finalScale, lineLengthM, targetAnchorDepthM) - targetVerticalSpanM);
        return new IterationResult(finalScale, iterations, finalResidual <= DepthToleranceM, finalResidual);
    }

    private static IReadOnlyList<MooringShapePoint> BuildNodes(
        IReadOnlyList<SegmentCalculationRow> orderedSegments,
        IReadOnlyDictionary<int, SegmentTensionRow> tensionRows,
        double targetAnchorDepthM,
        double angleScale)
    {
        var nodes = new List<MooringShapePoint>();
        var lineLengthM = orderedSegments.Sum(x => x.SegmentLengthM);
        var verticalSpanM = VerticalSpan(orderedSegments, tensionRows, angleScale, lineLengthM, targetAnchorDepthM);
        var topNodeDepthM = Math.Max(0, targetAnchorDepthM - verticalSpanM);
        var firstSegment = orderedSegments.First();
        var lastSegment = orderedSegments.Last();
        var lineShorterThanDepth = targetAnchorDepthM > 0 && lineLengthM + DepthToleranceM < targetAnchorDepthM;

        tensionRows.TryGetValue(firstSegment.Number, out var firstTension);
        nodes.Add(new MooringShapePoint(
            0,
            firstSegment.Number,
            "Буй / верхний конец линии",
            0,
            0,
            topNodeDepthM,
            0,
            ScaleAngle(firstTension?.AngleFromVerticalDeg ?? 0, angleScale, lineLengthM, targetAnchorDepthM),
            firstTension?.TensionKn ?? 0,
            lineShorterThanDepth ? "WARNING: буй ниже поверхности из-за короткой линии" : "INFO: буй, граничный узел"));

        var alongLineM = 0.0;
        var xOffsetM = 0.0;
        var zDepthM = topNodeDepthM;

        foreach (var segment in orderedSegments)
        {
            tensionRows.TryGetValue(segment.Number, out var tension);
            var angleDeg = ScaleAngle(tension?.AngleFromVerticalDeg ?? 0, angleScale, lineLengthM, targetAnchorDepthM);
            var angleRad = angleDeg * Math.PI / 180.0;
            var segmentLengthM = Math.Max(0, segment.SegmentLengthM);

            xOffsetM += segmentLengthM * Math.Sin(angleRad);
            zDepthM += segmentLengthM * Math.Cos(angleRad);
            alongLineM += segmentLengthM;

            var isBottomNode = segment.Number == lastSegment.Number;
            nodes.Add(new MooringShapePoint(
                nodes.Count,
                segment.Number,
                isBottomNode ? "Якорь / нижний конец линии" : segment.SourceElement,
                alongLineM,
                xOffsetM,
                isBottomNode ? targetAnchorDepthM : zDepthM,
                segmentLengthM,
                angleDeg,
                tension?.TensionKn ?? 0,
                isBottomNode ? "INFO: якорь на дне, граничный узел" : lineShorterThanDepth ? "WARNING: участок короткой линии / погружённая постановка" : "OK"));
        }

        return nodes;
    }

    private static double VerticalSpan(
        IReadOnlyList<SegmentCalculationRow> orderedSegments,
        IReadOnlyDictionary<int, SegmentTensionRow> tensionRows,
        double angleScale,
        double lineLengthM,
        double targetAnchorDepthM)
    {
        return orderedSegments.Sum(segment =>
        {
            tensionRows.TryGetValue(segment.Number, out var tension);
            var angleDeg = ScaleAngle(tension?.AngleFromVerticalDeg ?? 0, angleScale, lineLengthM, targetAnchorDepthM);
            var angleRad = angleDeg * Math.PI / 180.0;
            return Math.Max(0, segment.SegmentLengthM) * Math.Cos(angleRad);
        });
    }

    private static double ScaleAngle(double tensionAngleDeg, double angleScale, double lineLengthM, double targetAnchorDepthM)
    {
        var geometricAngleDeg = lineLengthM > targetAnchorDepthM && lineLengthM > 0
            ? Math.Acos(Math.Clamp(targetAnchorDepthM / lineLengthM, 0, 1)) * 180.0 / Math.PI
            : 0;

        var baseAngleDeg = Math.Max(Math.Abs(tensionAngleDeg), geometricAngleDeg);
        return Math.Clamp(baseAngleDeg * Math.Max(0, angleScale), 0, 89.0);
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

        if (lineLengthM + DepthToleranceM < depthM)
        {
            return BuoyShapeState.Submerged;
        }

        if (buoyPoint.ZDepthM <= Math.Max(0.05, depthM * 0.01))
        {
            return BuoyShapeState.Surface;
        }

        return BuoyShapeState.Submerged;
    }

    private sealed record IterationResult(double AngleScale, int Iterations, bool Converged, double ResidualM);
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
