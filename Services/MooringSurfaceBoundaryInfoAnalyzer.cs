using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public enum MooringSurfaceBoundaryInfoClassification
{
    UnavailableMissingBuoyInput,
    UnavailableMissingBoundaryRows,
    InvalidInput,
    LineShorterThanDepth,
    TautNonZeroHorizontalLoadNoFiniteRoot,
    VerticalGeometryBoundaryNonUnique,
    VerticalGeometryUniqueForceStateFamily,
    VerticalGeometryCapacityInsufficient,
    IndeterminateEndpointState,
    NonMonotoneDepthResponse,
    SolvedAtLowerBoundary,
    SolvedAtCapacityBoundary,
    NoRootRequiresNegativeQ0,
    InsufficientBuoyancyCapacity,
    NoRootUnclassified,
    IndeterminateDuringRootSearch,
    BracketedButDepthToleranceNotReached,
    SolvedByBoundedBisection
}

public sealed record MooringSurfaceBoundaryIntegrationState(
    double EndpointXM,
    double EndpointZM,
    double EndHN,
    double EndVN,
    double MinHN,
    double MaxHN,
    double MinVN,
    double MaxVN,
    bool VSignChange,
    int PointLoadCrossings,
    int IndeterminateSegmentCount);

public sealed record MooringSurfaceBoundaryInfoResult(
    bool Available,
    bool Solved,
    MooringSurfaceBoundaryInfoClassification Classification,
    double? TargetDepthM,
    double? LineLengthM,
    double? BuoySteadyDragN,
    double? QCapacityN,
    double? Q0N,
    double? Q0CapacityRatio,
    double? ActualBuoyancyRatio,
    MooringSurfaceBoundaryIntegrationState? LowerBoundaryState,
    MooringSurfaceBoundaryIntegrationState? CapacityBoundaryState,
    MooringSurfaceBoundaryIntegrationState? SolutionState,
    double? LowerResidualM,
    double? CapacityResidualM,
    bool RootBracketed,
    bool MonotoneSample,
    int Iterations,
    double? MinimumQForDownwardVerticalGeometryN,
    string MethodNote);

public static class MooringSurfaceBoundaryInfoAnalyzer
{
    private const double G = MooringSurfaceBoundaryIntegrationKernel.GravityMS2;
    private const double DepthToleranceM = 0.01;
    private const double LengthToleranceM = MooringSurfaceBoundaryIntegrationKernel.LengthToleranceM;
    private const double ForceEpsilonN = MooringSurfaceBoundaryIntegrationKernel.ForceEpsilonN;
    private const int MaxRootIterations = 80;
    private const string Method =
        "INFO only: frozen-load midpoint integration; steady current; wave excluded; +Z down; " +
        "Q0 bounded by full-volume buoyancy capacity; diagnostic X/Z is not a selected-shape source.";

    public static MooringSurfaceBoundaryInfoResult Build(
        EnvironmentInput environment,
        BuoyInput? buoy,
        CalculationResult result,
        MooringSequencePositionResult sequence)
    {
        if (buoy is null)
            return Unavailable(MooringSurfaceBoundaryInfoClassification.UnavailableMissingBuoyInput);

        if (!TryPrepare(environment, buoy, result, sequence, out var prepared, out var unavailableClassification))
            return Unavailable(unavailableClassification);

        var low = Integrate(prepared!, 0.0, prepared!.BuoySteadyDragN);
        var high = Integrate(prepared, prepared.QCapacityN, prepared.BuoySteadyDragN);
        var monotone = SampleMonotonicity(prepared);

        if (prepared.LineLengthM + LengthToleranceM < prepared.DepthM)
        {
            return Result(
                MooringSurfaceBoundaryInfoClassification.LineShorterThanDepth,
                prepared,
                null,
                low,
                high,
                null,
                false,
                monotone,
                0,
                null);
        }

        var tautLength = Math.Abs(prepared.LineLengthM - prepared.DepthM) <= LengthToleranceM;
        if (tautLength && result.CurrentForceN > ForceEpsilonN)
        {
            return Result(
                MooringSurfaceBoundaryInfoClassification.TautNonZeroHorizontalLoadNoFiniteRoot,
                prepared,
                null,
                low,
                high,
                null,
                false,
                monotone,
                0,
                null);
        }

        if (tautLength)
        {
            var minimumQ = MinimumQForStrictlyDownwardVerticalGeometry(prepared);
            var verticalGeometryAvailable =
                prepared.QCapacityN + ForceEpsilonN >= minimumQ &&
                high.IndeterminateSegmentCount == 0 &&
                Math.Abs(high.EndpointZM - prepared.DepthM) <= DepthToleranceM;
            var classification = verticalGeometryAvailable
                ? prepared.QCapacityN > minimumQ + ForceEpsilonN
                    ? MooringSurfaceBoundaryInfoClassification.VerticalGeometryUniqueForceStateFamily
                    : MooringSurfaceBoundaryInfoClassification.VerticalGeometryBoundaryNonUnique
                : MooringSurfaceBoundaryInfoClassification.VerticalGeometryCapacityInsufficient;

            return Result(
                classification,
                prepared,
                null,
                low,
                high,
                null,
                false,
                true,
                0,
                minimumQ);
        }

        if (low.IndeterminateSegmentCount > 0 || high.IndeterminateSegmentCount > 0)
        {
            return Result(
                MooringSurfaceBoundaryInfoClassification.IndeterminateEndpointState,
                prepared,
                null,
                low,
                high,
                null,
                false,
                monotone,
                0,
                null);
        }

        if (!monotone)
        {
            return Result(
                MooringSurfaceBoundaryInfoClassification.NonMonotoneDepthResponse,
                prepared,
                null,
                low,
                high,
                null,
                false,
                false,
                0,
                null);
        }

        var lowResidual = low.EndpointZM - prepared.DepthM;
        var highResidual = high.EndpointZM - prepared.DepthM;

        if (Math.Abs(lowResidual) <= DepthToleranceM)
        {
            return Result(
                MooringSurfaceBoundaryInfoClassification.SolvedAtLowerBoundary,
                prepared,
                0.0,
                low,
                high,
                low,
                true,
                true,
                0,
                null);
        }

        if (Math.Abs(highResidual) <= DepthToleranceM)
        {
            return Result(
                MooringSurfaceBoundaryInfoClassification.SolvedAtCapacityBoundary,
                prepared,
                prepared.QCapacityN,
                low,
                high,
                high,
                true,
                true,
                0,
                null);
        }

        var bracketed = lowResidual * highResidual < 0.0;
        if (!bracketed)
        {
            var classification = lowResidual > 0.0 && highResidual > 0.0
                ? MooringSurfaceBoundaryInfoClassification.NoRootRequiresNegativeQ0
                : lowResidual < 0.0 && highResidual < 0.0
                    ? MooringSurfaceBoundaryInfoClassification.InsufficientBuoyancyCapacity
                    : MooringSurfaceBoundaryInfoClassification.NoRootUnclassified;

            return Result(
                classification,
                prepared,
                null,
                low,
                high,
                null,
                false,
                true,
                0,
                null);
        }

        var qLow = 0.0;
        var qHigh = prepared.QCapacityN;
        var rLow = lowResidual;
        MooringSurfaceBoundaryIntegrationState? solution = null;
        double? qSolution = null;
        var iterations = 0;

        for (; iterations < MaxRootIterations; iterations++)
        {
            var qMid = (qLow + qHigh) / 2.0;
            var mid = Integrate(prepared, qMid, prepared.BuoySteadyDragN);
            if (mid.IndeterminateSegmentCount > 0)
            {
                return Result(
                    MooringSurfaceBoundaryInfoClassification.IndeterminateDuringRootSearch,
                    prepared,
                    null,
                    low,
                    high,
                    null,
                    true,
                    true,
                    iterations + 1,
                    null);
            }

            var residual = mid.EndpointZM - prepared.DepthM;
            if (Math.Abs(residual) <= DepthToleranceM)
            {
                solution = mid;
                qSolution = qMid;
                iterations++;
                break;
            }

            if (rLow * residual <= 0.0)
            {
                qHigh = qMid;
            }
            else
            {
                qLow = qMid;
                rLow = residual;
            }
        }

        if (solution is null)
        {
            var qMid = (qLow + qHigh) / 2.0;
            var mid = Integrate(prepared, qMid, prepared.BuoySteadyDragN);
            if (mid.IndeterminateSegmentCount == 0 &&
                Math.Abs(mid.EndpointZM - prepared.DepthM) <= DepthToleranceM)
            {
                solution = mid;
                qSolution = qMid;
            }
        }

        return Result(
            solution is null
                ? MooringSurfaceBoundaryInfoClassification.BracketedButDepthToleranceNotReached
                : MooringSurfaceBoundaryInfoClassification.SolvedByBoundedBisection,
            prepared,
            qSolution,
            low,
            high,
            solution,
            true,
            true,
            iterations,
            null);
    }

    private static bool TryPrepare(
        EnvironmentInput environment,
        BuoyInput buoy,
        CalculationResult result,
        MooringSequencePositionResult sequence,
        out PreparedInput? prepared,
        out MooringSurfaceBoundaryInfoClassification classification)
    {
        prepared = null;
        classification = MooringSurfaceBoundaryInfoClassification.InvalidInput;

        if (!FiniteNonnegative(environment.DepthM) ||
            !FiniteNonnegative(result.LineLengthM) ||
            !FiniteNonnegative(result.BuoyancyKg) ||
            !FiniteNonnegative(result.CurrentForceN) ||
            !FiniteNonnegative(buoy.WeightKg) ||
            !FiniteNonnegative(sequence.TotalLineLengthM))
            return false;

        if (Math.Abs(result.LineLengthM - sequence.TotalLineLengthM) > 1e-6)
            return false;

        var segments = result.SegmentRows.OrderBy(x => x.Number).ToList();
        if (segments.Any(x =>
                !FiniteNonnegative(x.SegmentLengthM) ||
                !double.IsFinite(x.StartLengthM) ||
                !double.IsFinite(x.EndLengthM) ||
                !FiniteNonnegative(x.CurrentForceN) ||
                !double.IsFinite(x.WeightWaterKg)))
            return false;

        var segmentLength = segments.Sum(x => x.SegmentLengthM);
        if (Math.Abs(segmentLength - result.LineLengthM) > 1e-6)
            return false;

        var orderedRows = sequence.Rows.OrderBy(x => x.Number).ToList();
        if (orderedRows.Count < 2 || !orderedRows[0].IsDiscrete || !orderedRows[^1].IsDiscrete)
        {
            classification = MooringSurfaceBoundaryInfoClassification.UnavailableMissingBoundaryRows;
            return false;
        }

        if (orderedRows.Any(x =>
                !double.IsFinite(x.PositionAlongLineM) ||
                !FiniteNonnegative(x.CurrentForceN) ||
                !double.IsFinite(x.WeightWaterKg)))
            return false;

        var topNumber = orderedRows[0].Number;
        var bottomNumber = orderedRows[^1].Number;
        var points = orderedRows
            .Where(x => x.IsDiscrete && x.Number != topNumber && x.Number != bottomNumber)
            .OrderBy(x => x.PositionAlongLineM)
            .ThenBy(x => x.Number)
            .ToList();

        if (points.Any(x =>
                x.PositionAlongLineM < -LengthToleranceM ||
                x.PositionAlongLineM > result.LineLengthM + LengthToleranceM))
            return false;

        var segmentCurrentForceN = segments.Sum(x => x.CurrentForceN);
        var internalPointCurrentForceN = points.Sum(x => x.CurrentForceN);
        var buoySteadyDragN = result.CurrentForceN - segmentCurrentForceN - internalPointCurrentForceN;
        if (!double.IsFinite(buoySteadyDragN) || buoySteadyDragN < -1e-6)
            return false;
        buoySteadyDragN = Math.Max(0.0, buoySteadyDragN);

        var bMaxN = result.BuoyancyKg * G;
        var buoyWeightN = buoy.WeightKg * G;
        var qCapacityN = Math.Max(0.0, bMaxN - buoyWeightN);
        if (!double.IsFinite(qCapacityN))
            return false;

        prepared = new PreparedInput(
            environment.DepthM,
            result.LineLengthM,
            buoySteadyDragN,
            qCapacityN,
            bMaxN,
            buoyWeightN,
            segments,
            points);
        return true;
    }

    private static MooringSurfaceBoundaryInfoResult Result(
        MooringSurfaceBoundaryInfoClassification classification,
        PreparedInput input,
        double? q0N,
        MooringSurfaceBoundaryIntegrationState low,
        MooringSurfaceBoundaryIntegrationState high,
        MooringSurfaceBoundaryIntegrationState? solution,
        bool bracketed,
        bool monotone,
        int iterations,
        double? minimumQ)
    {
        double? qRatio = q0N.HasValue && input.QCapacityN > ForceEpsilonN
            ? q0N.Value / input.QCapacityN
            : null;
        double? bActualRatio = q0N.HasValue && input.BMaxN > ForceEpsilonN
            ? (input.BuoyWeightN + q0N.Value) / input.BMaxN
            : null;

        return new MooringSurfaceBoundaryInfoResult(
            true,
            solution is not null && q0N.HasValue,
            classification,
            input.DepthM,
            input.LineLengthM,
            input.BuoySteadyDragN,
            input.QCapacityN,
            q0N,
            qRatio,
            bActualRatio,
            low,
            high,
            solution,
            low.EndpointZM - input.DepthM,
            high.EndpointZM - input.DepthM,
            bracketed,
            monotone,
            iterations,
            minimumQ,
            Method);
    }

    private static MooringSurfaceBoundaryInfoResult Unavailable(
        MooringSurfaceBoundaryInfoClassification classification)
    {
        return new MooringSurfaceBoundaryInfoResult(
            false,
            false,
            classification,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            false,
            0,
            null,
            Method);
    }

    private static MooringSurfaceBoundaryIntegrationState Integrate(
        PreparedInput input,
        double q0N,
        double initialHN)
    {
        return MooringSurfaceBoundaryIntegrationKernel.Integrate(
            input.Segments,
            input.Points,
            q0N,
            initialHN);
    }

    private static bool SampleMonotonicity(PreparedInput input)
    {
        if (input.QCapacityN <= ForceEpsilonN)
            return true;

        var fractions = new[] { 0.0, 0.25, 0.5, 0.75, 1.0 };
        var previousZ = double.NegativeInfinity;
        foreach (var fraction in fractions)
        {
            var state = Integrate(input, input.QCapacityN * fraction, input.BuoySteadyDragN);
            if (state.IndeterminateSegmentCount > 0)
                return false;
            if (state.EndpointZM + DepthToleranceM < previousZ)
                return false;
            previousZ = state.EndpointZM;
        }
        return true;
    }

    private static double MinimumQForStrictlyDownwardVerticalGeometry(PreparedInput input)
    {
        var zeroState = Integrate(input, 0.0, 0.0);
        return Math.Max(0.0, -zeroState.MinVN + ForceEpsilonN);
    }

    private static bool FiniteNonnegative(double value) => double.IsFinite(value) && value >= 0.0;

    private sealed record PreparedInput(
        double DepthM,
        double LineLengthM,
        double BuoySteadyDragN,
        double QCapacityN,
        double BMaxN,
        double BuoyWeightN,
        IReadOnlyList<SegmentCalculationRow> Segments,
        IReadOnlyList<MooringSequencePositionRow> Points);
}
