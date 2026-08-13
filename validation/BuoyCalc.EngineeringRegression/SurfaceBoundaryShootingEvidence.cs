using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SurfaceBoundaryShootingEvidence
{
    private const double G = 9.80665;
    private const double DepthToleranceM = 0.01;
    private const double LengthToleranceM = 1e-9;
    private const double ForceEpsilonN = 1e-9;
    private const int MaxRootIterations = 80;

    private static readonly SeabedPreset RegressionSeabed = new(
        "surface-shooting:sand",
        "Surface shooting sand",
        1.2,
        "Deterministic surface-boundary shooting seabed.");

    private static readonly BuoyInput RegressionBuoy = new(
        "Surface shooting buoy",
        1.0,
        100.0,
        0.8,
        0.8);

    private static readonly BuoyInput LowCapacityBuoy = new(
        "Low-capacity surface shooting buoy",
        0.101,
        100.0,
        0.8,
        0.8);

    private static readonly AnchorInput RegressionAnchor = new(
        "Surface shooting anchor",
        "Concrete block",
        "Concrete",
        1000.0,
        0.4,
        1.0);

    private static readonly RopePreset HeavyLine = new(
        "surface-shooting:heavy-line",
        "Surface shooting heavy line",
        "Polyester",
        20.0,
        100.0,
        0.1,
        1.2,
        "Deterministic heavy line.");

    private static readonly RopePreset BuoyantLine = new(
        "surface-shooting:buoyant-line",
        "Surface shooting buoyant line",
        "Synthetic buoyant",
        20.0,
        100.0,
        -0.05,
        1.2,
        "Negative signed water weight is intentional.");

    private static readonly ConnectorPreset RegressionConnector = new(
        "surface-shooting:connector",
        "Surface shooting connector",
        "Shackle",
        5.0,
        0.0007,
        60.0,
        0.01,
        1.0,
        "Deterministic connector.");

    public static void Print()
    {
        PrintScenario(new StudyScenario(
            "zero-current-vertical-heavy-line",
            Environment(depthM: 50, currentSpeedMS: 0, waveHeightM: 0, wavePeriodS: 0),
            RegressionBuoy,
            new[] { Line("Vertical line", HeavyLine, 50) }));

        PrintScenario(new StudyScenario(
            "uniform-current-slack-heavy-line",
            Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 1.0, wavePeriodS: 6.0),
            RegressionBuoy,
            new[] { Line("Slack line", HeavyLine, 55) }));

        PrintScenario(new StudyScenario(
            "buoyant-line-taut-limit",
            Environment(depthM: 30, currentSpeedMS: 0.3, waveHeightM: 0, wavePeriodS: 0),
            RegressionBuoy,
            new[] { Line("Buoyant line", BuoyantLine, 30) }));

        PrintScenario(new StudyScenario(
            "discrete-payload-slack-line",
            Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 0.5, wavePeriodS: 5.0),
            RegressionBuoy,
            new AssemblyItemInput[]
            {
                Line("Upper line", HeavyLine, 30),
                Connector("Shackle", RegressionConnector),
                Payload("Payload", 40.0, 0.005, 0.05, 1.0),
                Line("Lower line", HeavyLine, 25)
            }));

        PrintScenario(new StudyScenario(
            "depth-varying-current-profile-slack-line",
            new EnvironmentInput(
                1025.0,
                50.0,
                0.2,
                0,
                0,
                RegressionSeabed,
                true,
                new[]
                {
                    new CurrentProfilePointInput(0, 0.6, 0, 0, 1025),
                    new CurrentProfilePointInput(25, 0.3, 0, 0, 1025),
                    new CurrentProfilePointInput(50, 0.1, 0, 0, 1025)
                }),
            RegressionBuoy,
            new[] { Line("Profile slack line", HeavyLine, 55) }));

        PrintScenario(new StudyScenario(
            "line-shorter-than-depth",
            Environment(depthM: 50, currentSpeedMS: 0.2, waveHeightM: 0, wavePeriodS: 0),
            RegressionBuoy,
            new[] { Line("Short line", HeavyLine, 45) }));

        PrintScenario(new StudyScenario(
            "taut-equal-length-nonzero-current",
            Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 0, wavePeriodS: 0),
            RegressionBuoy,
            new[] { Line("Taut line", HeavyLine, 50) }));

        PrintScenario(new StudyScenario(
            "insufficient-buoyancy-capacity",
            Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 0, wavePeriodS: 0),
            LowCapacityBuoy,
            new[] { Line("Slack line", HeavyLine, 55) }));
    }

    private static void PrintScenario(StudyScenario scenario)
    {
        var result = BuoyCalculator.Calculate(
            scenario.Environment,
            scenario.Buoy,
            scenario.Assembly,
            RegressionAnchor,
            3.0);
        var sequence = MooringSequencePositioner.Build(result);
        var solved = Solve(scenario, result, sequence);

        Console.Error.WriteLine("BEGIN_SURFACE_BOUNDARY_SHOOTING_STUDY");
        Console.Error.WriteLine($"Scenario={scenario.Name}");
        Console.Error.WriteLine($"Classification={solved.Classification}");
        Console.Error.WriteLine($"DepthM={scenario.Environment.DepthM:R}");
        Console.Error.WriteLine($"LineLengthM={result.LineLengthM:R}");
        Console.Error.WriteLine($"SteadyCurrentForceN={result.CurrentForceN:R}");
        Console.Error.WriteLine($"WaveForceNExcluded={result.WaveForceN:R}");
        Console.Error.WriteLine($"ReconstructedBuoySteadyDragN={solved.BuoySteadyDragN:R}");
        Console.Error.WriteLine($"QCapacityN={solved.QCapacityN:R}");
        Console.Error.WriteLine($"Q0N={Format(solved.Q0N)}");
        Console.Error.WriteLine($"Q0CapacityRatio={Format(solved.Q0CapacityRatio)}");
        Console.Error.WriteLine($"BActualBMaxRatio={Format(solved.BActualBMaxRatio)}");
        Console.Error.WriteLine($"LowEndpointZM={Format(solved.Low.EndpointZM)}");
        Console.Error.WriteLine($"LowResidualM={Format(solved.LowResidualM)}");
        Console.Error.WriteLine($"HighEndpointZM={Format(solved.High.EndpointZM)}");
        Console.Error.WriteLine($"HighResidualM={Format(solved.HighResidualM)}");
        Console.Error.WriteLine($"Bracketed={solved.Bracketed}");
        Console.Error.WriteLine($"MonotoneSample={solved.MonotoneSample}");
        Console.Error.WriteLine($"RootIterations={solved.Iterations}");
        Console.Error.WriteLine($"EndpointXM={Format(solved.Solution?.EndpointXM)}");
        Console.Error.WriteLine($"EndpointZM={Format(solved.Solution?.EndpointZM)}");
        Console.Error.WriteLine($"VerticalResidualM={Format(solved.Solution is null ? null : solved.Solution.EndpointZM - scenario.Environment.DepthM)}");
        Console.Error.WriteLine($"MinHN={Format(solved.Solution?.MinHN)}");
        Console.Error.WriteLine($"MaxHN={Format(solved.Solution?.MaxHN)}");
        Console.Error.WriteLine($"MinVN={Format(solved.Solution?.MinVN)}");
        Console.Error.WriteLine($"MaxVN={Format(solved.Solution?.MaxVN)}");
        Console.Error.WriteLine($"VSignChange={solved.Solution?.VSignChange.ToString() ?? "null"}");
        Console.Error.WriteLine($"PointLoadCrossings={solved.Solution?.PointLoadCrossings.ToString() ?? "null"}");
        Console.Error.WriteLine($"IndeterminateSegmentCount={solved.Solution?.IndeterminateSegmentCount.ToString() ?? "null"}");
        Console.Error.WriteLine($"MinimumQForDownwardVerticalGeometryN={Format(solved.MinimumQForDownwardVerticalGeometryN)}");
        Console.Error.WriteLine("END_SURFACE_BOUNDARY_SHOOTING_STUDY");
    }

    private static ShootingResult Solve(
        StudyScenario scenario,
        CalculationResult result,
        MooringSequencePositionResult sequence)
    {
        var depthM = Math.Max(0, scenario.Environment.DepthM);
        var lineLengthM = Math.Max(0, result.LineLengthM);
        var segmentCurrentForceN = result.SegmentRows.Sum(x => x.CurrentForceN);
        var buoySteadyDragN =
            result.CurrentForceN -
            segmentCurrentForceN -
            sequence.DiscreteCurrentForceN;
        var qCapacityN = Math.Max(0, (result.BuoyancyKg - scenario.Buoy.WeightKg) * G);

        if (lineLengthM + LengthToleranceM < depthM)
        {
            var lowShort = Integrate(result, sequence, buoySteadyDragN, 0);
            var highShort = Integrate(result, sequence, buoySteadyDragN, qCapacityN);
            return Result(
                "LineShorterThanDepth",
                buoySteadyDragN,
                qCapacityN,
                null,
                lowShort,
                highShort,
                depthM,
                false,
                SampleMonotonicity(result, sequence, buoySteadyDragN, qCapacityN),
                0,
                null,
                null,
                scenario.Buoy,
                result);
        }

        var isTautLength = Math.Abs(lineLengthM - depthM) <= LengthToleranceM;
        if (isTautLength && result.CurrentForceN > ForceEpsilonN)
        {
            var lowTaut = Integrate(result, sequence, buoySteadyDragN, 0);
            var highTaut = Integrate(result, sequence, buoySteadyDragN, qCapacityN);
            return Result(
                "TautLimitNonZeroHorizontalLoad_NoFiniteRootExpected",
                buoySteadyDragN,
                qCapacityN,
                null,
                lowTaut,
                highTaut,
                depthM,
                false,
                SampleMonotonicity(result, sequence, buoySteadyDragN, qCapacityN),
                0,
                null,
                null,
                scenario.Buoy,
                result);
        }

        if (isTautLength && result.CurrentForceN <= ForceEpsilonN)
        {
            var lowVertical = Integrate(result, sequence, buoySteadyDragN, 0);
            var highVertical = Integrate(result, sequence, buoySteadyDragN, qCapacityN);
            var minimumQ = MinimumQForStrictlyDownwardVerticalGeometry(result, sequence);
            var classification =
                qCapacityN + ForceEpsilonN >= minimumQ &&
                Math.Abs(highVertical.EndpointZM - depthM) <= DepthToleranceM
                    ? "VerticalGeometryBoundaryNonUnique"
                    : "VerticalGeometryCapacityInsufficient";

            return Result(
                classification,
                buoySteadyDragN,
                qCapacityN,
                null,
                lowVertical,
                highVertical,
                depthM,
                false,
                true,
                0,
                null,
                minimumQ,
                scenario.Buoy,
                result);
        }

        var low = Integrate(result, sequence, buoySteadyDragN, 0);
        var high = Integrate(result, sequence, buoySteadyDragN, qCapacityN);
        var lowResidual = low.EndpointZM - depthM;
        var highResidual = high.EndpointZM - depthM;
        var monotone = SampleMonotonicity(result, sequence, buoySteadyDragN, qCapacityN);

        if (low.IndeterminateSegmentCount > 0 || high.IndeterminateSegmentCount > 0)
        {
            return Result(
                "IndeterminateEndpointState",
                buoySteadyDragN,
                qCapacityN,
                null,
                low,
                high,
                depthM,
                false,
                monotone,
                0,
                null,
                null,
                scenario.Buoy,
                result);
        }

        if (Math.Abs(lowResidual) <= DepthToleranceM)
        {
            return Result(
                "SolvedAtLowerBoundary",
                buoySteadyDragN,
                qCapacityN,
                0,
                low,
                high,
                depthM,
                true,
                monotone,
                0,
                low,
                null,
                scenario.Buoy,
                result);
        }

        if (Math.Abs(highResidual) <= DepthToleranceM)
        {
            return Result(
                "SolvedAtCapacityBoundary",
                buoySteadyDragN,
                qCapacityN,
                qCapacityN,
                low,
                high,
                depthM,
                true,
                monotone,
                0,
                high,
                null,
                scenario.Buoy,
                result);
        }

        var bracketed = lowResidual * highResidual < 0;
        if (!bracketed)
        {
            var classification = lowResidual > 0 && highResidual > 0
                ? "NoRootRequiresNegativeQ0"
                : lowResidual < 0 && highResidual < 0
                    ? "InsufficientBuoyancyCapacity"
                    : "NoRootUnclassified";

            return Result(
                classification,
                buoySteadyDragN,
                qCapacityN,
                null,
                low,
                high,
                depthM,
                false,
                monotone,
                0,
                null,
                null,
                scenario.Buoy,
                result);
        }

        var qLow = 0.0;
        var qHigh = qCapacityN;
        var rLow = lowResidual;
        IntegratedState? solution = null;
        double? qSolution = null;
        var iterations = 0;

        for (; iterations < MaxRootIterations; iterations++)
        {
            var qMid = (qLow + qHigh) / 2.0;
            var mid = Integrate(result, sequence, buoySteadyDragN, qMid);
            if (mid.IndeterminateSegmentCount > 0)
            {
                return Result(
                    "IndeterminateDuringRootSearch",
                    buoySteadyDragN,
                    qCapacityN,
                    null,
                    low,
                    high,
                    depthM,
                    true,
                    monotone,
                    iterations + 1,
                    null,
                    null,
                    scenario.Buoy,
                    result);
            }

            var residual = mid.EndpointZM - depthM;
            if (Math.Abs(residual) <= DepthToleranceM)
            {
                solution = mid;
                qSolution = qMid;
                iterations++;
                break;
            }

            if (rLow * residual <= 0)
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
            var mid = Integrate(result, sequence, buoySteadyDragN, qMid);
            if (Math.Abs(mid.EndpointZM - depthM) <= DepthToleranceM)
            {
                solution = mid;
                qSolution = qMid;
            }
        }

        return Result(
            solution is null ? "BracketedButDepthToleranceNotReached" : "SolvedByBoundedBisection",
            buoySteadyDragN,
            qCapacityN,
            qSolution,
            low,
            high,
            depthM,
            true,
            monotone,
            iterations,
            solution,
            null,
            scenario.Buoy,
            result);
    }

    private static ShootingResult Result(
        string classification,
        double buoySteadyDragN,
        double qCapacityN,
        double? q0N,
        IntegratedState low,
        IntegratedState high,
        double targetDepthM,
        bool bracketed,
        bool monotoneSample,
        int iterations,
        IntegratedState? solution,
        double? minimumQForDownwardVerticalGeometryN,
        BuoyInput buoy,
        CalculationResult result)
    {
        var qRatio = q0N.HasValue && qCapacityN > ForceEpsilonN
            ? q0N.Value / qCapacityN
            : null;
        var bMaxN = result.BuoyancyKg * G;
        var bActualRatio = q0N.HasValue && bMaxN > ForceEpsilonN
            ? (buoy.WeightKg * G + q0N.Value) / bMaxN
            : null;

        return new ShootingResult(
            classification,
            buoySteadyDragN,
            qCapacityN,
            q0N,
            qRatio,
            bActualRatio,
            low,
            high,
            low.EndpointZM - targetDepthM,
            high.EndpointZM - targetDepthM,
            bracketed,
            monotoneSample,
            iterations,
            solution,
            minimumQForDownwardVerticalGeometryN);
    }

    private static IntegratedState Integrate(
        CalculationResult result,
        MooringSequencePositionResult sequence,
        double buoySteadyDragN,
        double q0N)
    {
        var segments = result.SegmentRows.OrderBy(x => x.Number).ToList();
        var points = sequence.Rows
            .Where(x => x.IsDiscrete && x.Kind != "Буй" && x.Kind != "Якорь")
            .OrderBy(x => x.PositionAlongLineM)
            .ThenBy(x => x.Number)
            .ToList();

        var hN = buoySteadyDragN;
        var vN = q0N;
        var xM = 0.0;
        var zM = 0.0;
        var minHN = hN;
        var maxHN = hN;
        var minVN = vN;
        var maxVN = vN;
        var sawPositiveV = vN > ForceEpsilonN;
        var sawNegativeV = vN < -ForceEpsilonN;
        var pointIndex = 0;
        var pointCrossings = 0;
        var indeterminateSegments = 0;

        foreach (var segment in segments)
        {
            while (pointIndex < points.Count &&
                   points[pointIndex].PositionAlongLineM <= segment.StartLengthM + LengthToleranceM)
            {
                var point = points[pointIndex++];
                hN += point.CurrentForceN;
                vN -= point.WeightWaterKg * G;
                pointCrossings++;
                minHN = Math.Min(minHN, hN);
                maxHN = Math.Max(maxHN, hN);
                minVN = Math.Min(minVN, vN);
                maxVN = Math.Max(maxVN, vN);
                sawPositiveV |= vN > ForceEpsilonN;
                sawNegativeV |= vN < -ForceEpsilonN;
            }

            var hMidN = hN + 0.5 * segment.CurrentForceN;
            var vMidN = vN - 0.5 * segment.WeightWaterKg * G;
            var tensionMidN = Math.Sqrt(hMidN * hMidN + vMidN * vMidN);
            var segmentLengthM = Math.Max(0, segment.SegmentLengthM);

            minHN = Math.Min(minHN, hMidN);
            maxHN = Math.Max(maxHN, hMidN);
            minVN = Math.Min(minVN, vMidN);
            maxVN = Math.Max(maxVN, vMidN);
            sawPositiveV |= vMidN > ForceEpsilonN;
            sawNegativeV |= vMidN < -ForceEpsilonN;

            if (!double.IsFinite(tensionMidN) || tensionMidN <= ForceEpsilonN)
            {
                indeterminateSegments++;
            }
            else
            {
                xM += segmentLengthM * hMidN / tensionMidN;
                zM += segmentLengthM * vMidN / tensionMidN;
            }

            hN += segment.CurrentForceN;
            vN -= segment.WeightWaterKg * G;
            minHN = Math.Min(minHN, hN);
            maxHN = Math.Max(maxHN, hN);
            minVN = Math.Min(minVN, vN);
            maxVN = Math.Max(maxVN, vN);
            sawPositiveV |= vN > ForceEpsilonN;
            sawNegativeV |= vN < -ForceEpsilonN;
        }

        while (pointIndex < points.Count)
        {
            var point = points[pointIndex++];
            hN += point.CurrentForceN;
            vN -= point.WeightWaterKg * G;
            pointCrossings++;
            minHN = Math.Min(minHN, hN);
            maxHN = Math.Max(maxHN, hN);
            minVN = Math.Min(minVN, vN);
            maxVN = Math.Max(maxVN, vN);
            sawPositiveV |= vN > ForceEpsilonN;
            sawNegativeV |= vN < -ForceEpsilonN;
        }

        return new IntegratedState(
            xM,
            zM,
            hN,
            vN,
            minHN,
            maxHN,
            minVN,
            maxVN,
            sawPositiveV && sawNegativeV,
            pointCrossings,
            indeterminateSegments);
    }

    private static bool SampleMonotonicity(
        CalculationResult result,
        MooringSequencePositionResult sequence,
        double buoySteadyDragN,
        double qCapacityN)
    {
        if (qCapacityN <= ForceEpsilonN)
        {
            return true;
        }

        var fractions = new[] { 0.0, 0.25, 0.5, 0.75, 1.0 };
        var previousZ = double.NegativeInfinity;

        foreach (var fraction in fractions)
        {
            var state = Integrate(result, sequence, buoySteadyDragN, qCapacityN * fraction);
            if (state.IndeterminateSegmentCount > 0)
            {
                continue;
            }

            if (state.EndpointZM + DepthToleranceM < previousZ)
            {
                return false;
            }

            previousZ = state.EndpointZM;
        }

        return true;
    }

    private static double MinimumQForStrictlyDownwardVerticalGeometry(
        CalculationResult result,
        MooringSequencePositionResult sequence)
    {
        var zeroState = Integrate(result, sequence, 0, 0);
        return Math.Max(0, -zeroState.MinVN + ForceEpsilonN);
    }

    private static string Format(double? value)
    {
        return value.HasValue ? value.Value.ToString("R") : "null";
    }

    private static EnvironmentInput Environment(
        double depthM,
        double currentSpeedMS,
        double waveHeightM,
        double wavePeriodS)
    {
        return new EnvironmentInput(
            1025.0,
            depthM,
            currentSpeedMS,
            waveHeightM,
            wavePeriodS,
            RegressionSeabed);
    }

    private static AssemblyItemInput Line(string title, RopePreset preset, double lengthM)
    {
        return new AssemblyItemInput(
            AssemblyItemKind.Line,
            title,
            true,
            preset,
            null,
            lengthM,
            1,
            0,
            0,
            0,
            0);
    }

    private static AssemblyItemInput Connector(string title, ConnectorPreset preset)
    {
        return new AssemblyItemInput(
            AssemblyItemKind.Connector,
            title,
            true,
            null,
            preset,
            0,
            1,
            0,
            0,
            0,
            0);
    }

    private static AssemblyItemInput Payload(
        string title,
        double weightAirKg,
        double volumeM3,
        double projectedAreaM2,
        double dragCoefficient)
    {
        return new AssemblyItemInput(
            AssemblyItemKind.Payload,
            title,
            true,
            null,
            null,
            0,
            1,
            weightAirKg,
            volumeM3,
            projectedAreaM2,
            dragCoefficient);
    }

    private sealed record StudyScenario(
        string Name,
        EnvironmentInput Environment,
        BuoyInput Buoy,
        IReadOnlyList<AssemblyItemInput> Assembly);

    private sealed record IntegratedState(
        double EndpointXM,
        double EndpointZM,
        double TerminalHN,
        double TerminalVN,
        double MinHN,
        double MaxHN,
        double MinVN,
        double MaxVN,
        bool VSignChange,
        int PointLoadCrossings,
        int IndeterminateSegmentCount);

    private sealed record ShootingResult(
        string Classification,
        double BuoySteadyDragN,
        double QCapacityN,
        double? Q0N,
        double? Q0CapacityRatio,
        double? BActualBMaxRatio,
        IntegratedState Low,
        IntegratedState High,
        double LowResidualM,
        double HighResidualM,
        bool Bracketed,
        bool MonotoneSample,
        int Iterations,
        IntegratedState? Solution,
        double? MinimumQForDownwardVerticalGeometryN);
}
