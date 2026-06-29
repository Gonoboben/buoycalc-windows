using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record MooringIterativeSolverIteration(
    int IterationNumber,
    double InputOffsetM,
    double ShapeLineForceN,
    double TopShapeTensionKn,
    double TopDiscreteTensionKn,
    double OutputOffsetM,
    double OffsetChangeM,
    double MaxNodeDeltaM,
    double GeometryResidualM,
    bool GeometryConverged,
    bool Converged,
    string Status);

public sealed record MooringIterativeSolverResult(
    IReadOnlyList<MooringIterativeSolverIteration> Rows,
    MooringShapeResult? FinalShape,
    bool Converged,
    int IterationCount,
    double FinalOffsetChangeM,
    double FinalMaxNodeDeltaM,
    string ConvergenceCriterion,
    string MethodNote);

public static class MooringIterativeSolver
{
    private const int DefaultMaxIterations = 4;
    private const double OffsetToleranceM = 0.05;
    private const double NodeDeltaToleranceM = 0.10;

    public static MooringIterativeSolverResult Build(
        CalculationResult result,
        MooringShapeResult initialShape,
        MooringSequencePositionResult sequencePositions,
        IReadOnlyList<SegmentTensionRow> initialTensions,
        int maxIterations = DefaultMaxIterations)
    {
        if (initialShape.Nodes.Count < 2 || result.SegmentRows.Count == 0)
        {
            return Empty("Нет исходной X/Z-формы или сегментов линии для запуска каркаса итерационного solver.");
        }

        if (sequencePositions.Rows.Count == 0 || initialTensions.Count == 0)
        {
            return Empty("Нет позиционной модели или базовых натяжений для замыкания итерационного цикла.");
        }

        var rows = new List<MooringIterativeSolverIteration>();
        var currentShape = initialShape;
        var feedbackTensions = initialTensions;
        var iterations = Math.Clamp(maxIterations, 1, DefaultMaxIterations);

        for (var iteration = 1; iteration <= iterations; iteration++)
        {
            var projection = MooringShapeProjection.Build(currentShape);
            var shapeForces = MooringShapeForceAnalyzer.Build(result, projection);
            var shapeTensions = MooringShapeTensionAnalyzer.Build(result, feedbackTensions, shapeForces);
            var nextFeedbackTensions = BuildFeedbackTensions(shapeTensions);
            var discreteTensions = MooringDiscreteLoadTensionAnalyzer.Build(
                result,
                nextFeedbackTensions.Count > 0 ? nextFeedbackTensions : feedbackTensions,
                sequencePositions);
            var nextShape = MooringDiscreteLoadShapeBuilder.Build(currentShape, discreteTensions);

            var outputOffsetM = nextShape.DiscreteHorizontalOffsetM;
            var offsetChangeM = outputOffsetM - currentShape.HorizontalOffsetM;
            var geometryResidualM = nextShape.VerticalResidualM;
            var iterationConverged = nextShape.Converged &&
                Math.Abs(offsetChangeM) <= OffsetToleranceM &&
                nextShape.MaxNodeDeltaM <= NodeDeltaToleranceM;

            rows.Add(new MooringIterativeSolverIteration(
                iteration,
                currentShape.HorizontalOffsetM,
                shapeForces.ShapeLineForceN,
                shapeTensions.TopShapeTensionKn,
                discreteTensions.TopDiscreteTensionKn,
                outputOffsetM,
                offsetChangeM,
                nextShape.MaxNodeDeltaM,
                geometryResidualM,
                nextShape.Converged,
                iterationConverged,
                iterationConverged
                    ? "OK: критерий v0.39 выполнен"
                    : nextShape.Converged
                        ? "INFO: геометрия замкнута, но изменение формы ещё выше допуска"
                        : "WARNING: геометрия новой формы не сошлась"));

            currentShape = ToShapeResult(currentShape, nextShape, iteration, iterationConverged, offsetChangeM);
            if (nextFeedbackTensions.Count > 0)
            {
                feedbackTensions = nextFeedbackTensions;
            }

            if (iterationConverged)
            {
                break;
            }
        }

        var last = rows.LastOrDefault();
        var converged = last?.Converged ?? false;
        return new MooringIterativeSolverResult(
            rows,
            currentShape,
            converged,
            rows.Count,
            last?.OffsetChangeM ?? 0,
            last?.MaxNodeDeltaM ?? 0,
            $"|ΔXсноса| ≤ {OffsetToleranceM:0.####} м и max Δузла ≤ {NodeDeltaToleranceM:0.####} м после цикла форма → силы → натяжения → дискретные нагрузки → новая форма.",
            "v0.39: добавлен отдельный каркас итерационного solver. Он не заменяет основной solver и не меняет 2D/PDF-формы; результат используется как диагностический слой для проверки будущего нелинейного замыкания.");
    }

    private static MooringIterativeSolverResult Empty(string note)
    {
        return new MooringIterativeSolverResult(
            Array.Empty<MooringIterativeSolverIteration>(),
            null,
            false,
            0,
            0,
            0,
            $"|ΔXсноса| ≤ {OffsetToleranceM:0.####} м и max Δузла ≤ {NodeDeltaToleranceM:0.####} м",
            note);
    }

    private static IReadOnlyList<SegmentTensionRow> BuildFeedbackTensions(MooringShapeTensionResult shapeTensions)
    {
        if (shapeTensions.Rows.Count == 0)
        {
            return Array.Empty<SegmentTensionRow>();
        }

        return shapeTensions.Rows
            .OrderBy(x => x.SegmentNumber)
            .Select(row => new SegmentTensionRow(
                row.SegmentNumber,
                row.SourceElement,
                row.EstimatedDepthM,
                row.SegmentLengthM,
                row.WeightWaterKg,
                row.ShapeSegmentForceN,
                row.CumulativeShapeHorizontalForceN,
                row.CumulativeVerticalForceN,
                row.ShapeTensionKn,
                row.ShapeAngleFromVerticalDeg,
                row.Status.StartsWith("OK", StringComparison.OrdinalIgnoreCase)
                    ? "OK"
                    : "INFO: натяжение передано в v0.39 feedback-цикл"))
            .ToList();
    }

    private static MooringShapeResult ToShapeResult(
        MooringShapeResult previousShape,
        MooringDiscreteLoadShapeResult nextShape,
        int iteration,
        bool converged,
        double offsetChangeM)
    {
        if (nextShape.Rows.Count == 0)
        {
            return previousShape;
        }

        var nodes = nextShape.Rows
            .OrderBy(x => x.Number)
            .Select(row => new MooringShapePoint(
                row.Number,
                row.SegmentNumber,
                row.SourceElement,
                row.AlongLineM,
                row.XOffsetM,
                row.ZDepthM,
                row.SegmentLengthM,
                row.UsedAngleFromVerticalDeg,
                row.DiscreteTensionKn,
                row.Status))
            .ToList();

        var buoyPoint = nodes.FirstOrDefault();
        var anchorPoint = nodes.LastOrDefault();
        var horizontalOffsetM = anchorPoint?.XOffsetM ?? nextShape.DiscreteHorizontalOffsetM;
        var verticalResidualM = Math.Abs((anchorPoint?.ZDepthM ?? 0) - previousShape.DepthM);

        return new MooringShapeResult(
            nodes,
            buoyPoint,
            anchorPoint,
            previousShape.BuoyState,
            previousShape.DepthM,
            previousShape.LineLengthM,
            horizontalOffsetM,
            verticalResidualM,
            converged,
            $"v0.39: временная форма итерации {iteration}; получена из альтернативной формы с дискретными нагрузками и не заменяет основной MooringShapeSolver.",
            iteration,
            verticalResidualM,
            nextShape.AngleScale,
            $"v0.39 feedback: |ΔXсноса|={Math.Abs(offsetChangeM):0.####} м, max Δузла={nextShape.MaxNodeDeltaM:0.####} м");
    }
}

public static class MooringIterativeSolverStore
{
    public static MooringIterativeSolverResult? Current { get; private set; }

    public static void Set(MooringIterativeSolverResult result)
    {
        Current = result;
    }

    public static void Clear()
    {
        Current = null;
    }
}