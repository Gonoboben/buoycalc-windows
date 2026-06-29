using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public enum MooringIterativeSolverStopReason
{
    NotStarted,
    Continue,
    Converged,
    MaxIterationsReached,
    GeometryNotClosed,
    DivergenceGuard,
    InvalidInput
}

public sealed record MooringIterativeSolverCriteria(
    int MaxIterations,
    double OffsetToleranceM,
    double NodeDeltaToleranceM,
    double GeometryResidualToleranceM,
    double DivergenceOffsetChangeM,
    double DivergenceNodeDeltaM);

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
    bool OffsetWithinTolerance,
    bool NodeDeltaWithinTolerance,
    bool GeometryResidualWithinTolerance,
    bool DivergenceDetected,
    bool Converged,
    MooringIterativeSolverStopReason StopReason,
    string StopReasonText,
    string Status);

public sealed record MooringIterativeSolverResult(
    IReadOnlyList<MooringIterativeSolverIteration> Rows,
    MooringShapeResult? FinalShape,
    bool Converged,
    int IterationCount,
    double FinalOffsetChangeM,
    double FinalMaxNodeDeltaM,
    double FinalGeometryResidualM,
    bool Diverged,
    MooringIterativeSolverCriteria Criteria,
    MooringIterativeSolverStopReason StopReason,
    string StopReasonText,
    string ConvergenceCriterion,
    string MethodNote);

public static class MooringIterativeSolver
{
    private const int DefaultMaxIterations = 4;
    private const double OffsetToleranceM = 0.05;
    private const double NodeDeltaToleranceM = 0.10;
    private const double GeometryResidualToleranceM = 0.05;

    public static MooringIterativeSolverResult Build(
        CalculationResult result,
        MooringShapeResult initialShape,
        MooringSequencePositionResult sequencePositions,
        IReadOnlyList<SegmentTensionRow> initialTensions,
        int maxIterations = DefaultMaxIterations)
    {
        var criteria = BuildCriteria(maxIterations, initialShape);

        if (initialShape.Nodes.Count < 2 || result.SegmentRows.Count == 0)
        {
            return Empty(criteria, "Нет исходной X/Z-формы или сегментов линии для запуска каркаса итерационного solver.");
        }

        if (sequencePositions.Rows.Count == 0 || initialTensions.Count == 0)
        {
            return Empty(criteria, "Нет позиционной модели или базовых натяжений для замыкания итерационного цикла.");
        }

        var rows = new List<MooringIterativeSolverIteration>();
        var currentShape = initialShape;
        var feedbackTensions = initialTensions;

        for (var iteration = 1; iteration <= criteria.MaxIterations; iteration++)
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

            var offsetWithinTolerance = Math.Abs(offsetChangeM) <= criteria.OffsetToleranceM;
            var nodeDeltaWithinTolerance = nextShape.MaxNodeDeltaM <= criteria.NodeDeltaToleranceM;
            var geometryResidualWithinTolerance = nextShape.Converged &&
                Math.Abs(geometryResidualM) <= criteria.GeometryResidualToleranceM;
            var divergenceDetected = IsDivergent(offsetChangeM, nextShape.MaxNodeDeltaM, geometryResidualM, criteria);
            var iterationConverged = geometryResidualWithinTolerance &&
                offsetWithinTolerance &&
                nodeDeltaWithinTolerance &&
                !divergenceDetected;

            var stopReason = ResolveStopReason(
                iteration,
                criteria,
                nextShape.Converged,
                iterationConverged,
                divergenceDetected);
            var stopReasonText = DescribeStopReason(stopReason);

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
                offsetWithinTolerance,
                nodeDeltaWithinTolerance,
                geometryResidualWithinTolerance,
                divergenceDetected,
                iterationConverged,
                stopReason,
                stopReasonText,
                BuildIterationStatus(
                    stopReason,
                    offsetWithinTolerance,
                    nodeDeltaWithinTolerance,
                    geometryResidualWithinTolerance,
                    offsetChangeM,
                    nextShape.MaxNodeDeltaM,
                    geometryResidualM)));

            currentShape = ToShapeResult(currentShape, nextShape, iteration, iterationConverged, offsetChangeM);
            if (nextFeedbackTensions.Count > 0)
            {
                feedbackTensions = nextFeedbackTensions;
            }

            if (iterationConverged || divergenceDetected)
            {
                break;
            }
        }

        var last = rows.LastOrDefault();
        var finalStopReason = last?.StopReason ?? MooringIterativeSolverStopReason.NotStarted;
        var converged = last?.Converged ?? false;
        return new MooringIterativeSolverResult(
            rows,
            currentShape,
            converged,
            rows.Count,
            last?.OffsetChangeM ?? 0,
            last?.MaxNodeDeltaM ?? 0,
            last?.GeometryResidualM ?? 0,
            finalStopReason == MooringIterativeSolverStopReason.DivergenceGuard,
            criteria,
            finalStopReason,
            last?.StopReasonText ?? DescribeStopReason(finalStopReason),
            BuildConvergenceCriterion(criteria),
            "v0.39.1: критерии сходимости итерационного solver формализованы отдельно от визуализации. Solver считается сошедшимся только при одновременном выполнении допуска по X-сносу, max Δузла и геометрической невязке Z; при грубой расходимости срабатывает защитная остановка.");
    }

    private static MooringIterativeSolverCriteria BuildCriteria(int requestedMaxIterations, MooringShapeResult initialShape)
    {
        var maxIterations = Math.Clamp(requestedMaxIterations, 1, DefaultMaxIterations);
        var scale = Math.Max(1.0, Math.Max(initialShape.DepthM, initialShape.LineLengthM));
        return new MooringIterativeSolverCriteria(
            maxIterations,
            OffsetToleranceM,
            NodeDeltaToleranceM,
            GeometryResidualToleranceM,
            Math.Max(1.0, scale * 0.25),
            Math.Max(1.0, scale * 0.25));
    }

    private static MooringIterativeSolverResult Empty(MooringIterativeSolverCriteria criteria, string note)
    {
        return new MooringIterativeSolverResult(
            Array.Empty<MooringIterativeSolverIteration>(),
            null,
            false,
            0,
            0,
            0,
            0,
            false,
            criteria,
            MooringIterativeSolverStopReason.InvalidInput,
            DescribeStopReason(MooringIterativeSolverStopReason.InvalidInput),
            BuildConvergenceCriterion(criteria),
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

    private static bool IsDivergent(
        double offsetChangeM,
        double maxNodeDeltaM,
        double geometryResidualM,
        MooringIterativeSolverCriteria criteria)
    {
        if (!IsFinite(offsetChangeM) || !IsFinite(maxNodeDeltaM) || !IsFinite(geometryResidualM))
        {
            return true;
        }

        return Math.Abs(offsetChangeM) > criteria.DivergenceOffsetChangeM ||
            maxNodeDeltaM > criteria.DivergenceNodeDeltaM;
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static MooringIterativeSolverStopReason ResolveStopReason(
        int iteration,
        MooringIterativeSolverCriteria criteria,
        bool geometryConverged,
        bool iterationConverged,
        bool divergenceDetected)
    {
        if (iterationConverged)
        {
            return MooringIterativeSolverStopReason.Converged;
        }

        if (divergenceDetected)
        {
            return MooringIterativeSolverStopReason.DivergenceGuard;
        }

        if (!geometryConverged && iteration == criteria.MaxIterations)
        {
            return MooringIterativeSolverStopReason.GeometryNotClosed;
        }

        if (iteration == criteria.MaxIterations)
        {
            return MooringIterativeSolverStopReason.MaxIterationsReached;
        }

        return MooringIterativeSolverStopReason.Continue;
    }

    private static string DescribeStopReason(MooringIterativeSolverStopReason reason)
    {
        return reason switch
        {
            MooringIterativeSolverStopReason.NotStarted => "Итерационный solver не запускался.",
            MooringIterativeSolverStopReason.Continue => "Итерационный solver продолжает расчёт: критерии ещё не выполнены.",
            MooringIterativeSolverStopReason.Converged => "Расчёт остановлен: критерии сходимости выполнены.",
            MooringIterativeSolverStopReason.MaxIterationsReached => "Расчёт остановлен: достигнут лимит итераций без выполнения всех критериев.",
            MooringIterativeSolverStopReason.GeometryNotClosed => "Расчёт остановлен: достигнут лимит итераций, геометрия новой формы не замкнулась.",
            MooringIterativeSolverStopReason.DivergenceGuard => "Расчёт остановлен защитой от расходимости.",
            MooringIterativeSolverStopReason.InvalidInput => "Расчёт не запущен: недостаточно входных данных.",
            _ => "Неизвестная причина остановки итерационного solver."
        };
    }

    private static string BuildIterationStatus(
        MooringIterativeSolverStopReason stopReason,
        bool offsetWithinTolerance,
        bool nodeDeltaWithinTolerance,
        bool geometryResidualWithinTolerance,
        double offsetChangeM,
        double maxNodeDeltaM,
        double geometryResidualM)
    {
        if (stopReason == MooringIterativeSolverStopReason.Converged)
        {
            return "OK: все критерии v0.39.1 выполнены";
        }

        if (stopReason == MooringIterativeSolverStopReason.DivergenceGuard)
        {
            return $"WARNING: защитная остановка; |ΔX|={Math.Abs(offsetChangeM):0.####} м, max Δузла={maxNodeDeltaM:0.####} м";
        }

        if (stopReason == MooringIterativeSolverStopReason.GeometryNotClosed)
        {
            return $"WARNING: геометрия не замкнулась; невязка Z={geometryResidualM:0.####} м";
        }

        if (stopReason == MooringIterativeSolverStopReason.MaxIterationsReached)
        {
            return "INFO: достигнут лимит итераций, часть критериев не выполнена";
        }

        return $"INFO: критерии: ΔX={(offsetWithinTolerance ? "OK" : "нет")}, max Δузла={(nodeDeltaWithinTolerance ? "OK" : "нет")}, Z={(geometryResidualWithinTolerance ? "OK" : "нет")}";
    }

    private static string BuildConvergenceCriterion(MooringIterativeSolverCriteria criteria)
    {
        return $"v0.39.1: сходимость = |ΔXсноса| ≤ {criteria.OffsetToleranceM:0.####} м, max Δузла ≤ {criteria.NodeDeltaToleranceM:0.####} м, |невязка Z| ≤ {criteria.GeometryResidualToleranceM:0.####} м. Лимит итераций: {criteria.MaxIterations}. Защитная остановка: |ΔX| > {criteria.DivergenceOffsetChangeM:0.####} м или max Δузла > {criteria.DivergenceNodeDeltaM:0.####} м.";
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
            $"v0.39.1: временная форма итерации {iteration}; получена из альтернативной формы с дискретными нагрузками и не заменяет основной MooringShapeSolver.",
            iteration,
            verticalResidualM,
            nextShape.AngleScale,
            $"v0.39.1 feedback: |ΔXсноса|={Math.Abs(offsetChangeM):0.####} м, max Δузла={nextShape.MaxNodeDeltaM:0.####} м");
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