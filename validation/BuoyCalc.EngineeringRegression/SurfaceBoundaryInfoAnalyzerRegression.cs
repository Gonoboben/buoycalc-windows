using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SurfaceBoundaryInfoAnalyzerRegression
{
    private const double G = 9.80665;
    private const double LengthM = 55.0;
    private const double DepthM = 50.0;
    private const double H0N = 82.0;
    private const double QxNPerM = 3.075;
    private const double WeightNPerM = 0.980665;
    private const double ExpectedExactQ0N = 405.4394635275295;

    public static void Validate()
    {
        ValidateMissingTypedBuoy();
        ValidateConstantLoadSolution();
        ValidatePointLoadCrossing();
        ValidateGeometricClassifications();
        ValidateInsufficientCapacity();
        ValidateSignedBuoyantWeight();
    }

    private static void ValidateMissingTypedBuoy()
    {
        var scenario = Build();
        var info = MooringSurfaceBoundaryInfoAnalyzer.Build(
            scenario.Environment,
            null,
            scenario.Result,
            scenario.Sequence);

        RequireClassification(
            info,
            MooringSurfaceBoundaryInfoClassification.UnavailableMissingBuoyInput,
            available: false,
            solved: false,
            "missing typed buoy");
    }

    private static void ValidateConstantLoadSolution()
    {
        var scenario = Build();
        var info = Analyze(scenario);
        RequireClassification(
            info,
            MooringSurfaceBoundaryInfoClassification.SolvedByBoundedBisection,
            available: true,
            solved: true,
            "constant load");

        Near(H0N, Required(info.BuoySteadyDragN, "constant buoy drag"), 1e-9, "constant buoy drag");
        Near(9071.15125, Required(info.QCapacityN, "constant capacity"), 1e-8, "constant Q capacity");
        Near(ExpectedExactQ0N, Required(info.Q0N, "constant Q0"), 1.0, "constant Q0 vs exact reference");

        var solution = info.SolutionState
            ?? throw new InvalidOperationException("Surface-boundary INFO regression: constant solution state missing.");
        if (Math.Abs(solution.EndpointZM - DepthM) > 0.01)
            throw new InvalidOperationException($"Surface-boundary INFO regression: constant depth residual too large: {solution.EndpointZM - DepthM:R} m.");
        Near(21.911468597524454, solution.EndpointXM, 0.03, "constant endpoint X vs exact reference");
        if (solution.PointLoadCrossings != 0)
            throw new InvalidOperationException("Surface-boundary INFO regression: constant case must not cross internal point loads.");
        if (!info.MethodNote.Contains("not a selected-shape source", StringComparison.Ordinal))
            throw new InvalidOperationException("Surface-boundary INFO regression: method note must preserve diagnostic-only authority.");
    }

    private static void ValidatePointLoadCrossing()
    {
        var scenario = Build(pointCurrentForceN: 10.0, pointWeightWaterKg: 1.0);
        var info = Analyze(scenario);
        RequireClassification(
            info,
            MooringSurfaceBoundaryInfoClassification.SolvedByBoundedBisection,
            available: true,
            solved: true,
            "point load");

        Near(425.21021484375, Required(info.Q0N, "point Q0"), 1.0, "point-load Q0 measurement");
        var solution = info.SolutionState
            ?? throw new InvalidOperationException("Surface-boundary INFO regression: point-load solution state missing.");
        if (solution.PointLoadCrossings != 1)
            throw new InvalidOperationException($"Surface-boundary INFO regression: expected one point-load crossing, got {solution.PointLoadCrossings}.");
    }

    private static void ValidateGeometricClassifications()
    {
        var baseScenario = Build();

        var shortEnvironment = baseScenario.Environment with { DepthM = 56.0 };
        var shortInfo = MooringSurfaceBoundaryInfoAnalyzer.Build(
            shortEnvironment,
            baseScenario.Buoy,
            baseScenario.Result,
            baseScenario.Sequence);
        RequireClassification(
            shortInfo,
            MooringSurfaceBoundaryInfoClassification.LineShorterThanDepth,
            available: true,
            solved: false,
            "line shorter than depth");

        var tautEnvironment = baseScenario.Environment with { DepthM = LengthM };
        var tautInfo = MooringSurfaceBoundaryInfoAnalyzer.Build(
            tautEnvironment,
            baseScenario.Buoy,
            baseScenario.Result,
            baseScenario.Sequence);
        RequireClassification(
            tautInfo,
            MooringSurfaceBoundaryInfoClassification.TautNonZeroHorizontalLoadNoFiniteRoot,
            available: true,
            solved: false,
            "taut nonzero horizontal load");
    }

    private static void ValidateInsufficientCapacity()
    {
        var scenario = Build(buoyancyKg: 100.1);
        var info = Analyze(scenario);
        RequireClassification(
            info,
            MooringSurfaceBoundaryInfoClassification.InsufficientBuoyancyCapacity,
            available: true,
            solved: false,
            "insufficient capacity");
        Near(0.980665, Required(info.QCapacityN, "low capacity"), 1e-9, "low Q capacity");
    }

    private static void ValidateSignedBuoyantWeight()
    {
        var heavy = Analyze(Build());
        var buoyant = Analyze(Build(weightNPerM: -WeightNPerM));
        RequireClassification(
            buoyant,
            MooringSurfaceBoundaryInfoClassification.SolvedByBoundedBisection,
            available: true,
            solved: true,
            "signed buoyant line");

        var heavyQ = Required(heavy.Q0N, "heavy Q0");
        var buoyantQ = Required(buoyant.Q0N, "buoyant Q0");
        Near(338.83938995361336, buoyantQ, 1.0, "buoyant signed-weight Q0 measurement");
        if (buoyantQ >= heavyQ)
            throw new InvalidOperationException($"Surface-boundary INFO regression: signed buoyant line must reduce required Q0 in this controlled case; heavy={heavyQ:R}, buoyant={buoyantQ:R}.");
    }

    private static Scenario Build(
        double depthM = DepthM,
        double buoyancyKg = 1025.0,
        double weightNPerM = WeightNPerM,
        double pointCurrentForceN = 0.0,
        double pointWeightWaterKg = 0.0)
    {
        const int segmentCount = 275;
        var ds = LengthM / segmentCount;
        var segmentCurrentN = QxNPerM * ds;
        var segmentWeightKg = weightNPerM * ds / G;
        var segments = new List<SegmentCalculationRow>(segmentCount);
        for (var i = 0; i < segmentCount; i++)
        {
            var start = i * ds;
            segments.Add(new SegmentCalculationRow(
                i + 1,
                "Synthetic line",
                "Synthetic rope",
                start,
                start + ds,
                ds,
                start + ds / 2.0,
                0.0,
                0.0,
                0.0,
                0.0,
                1025.0,
                0.0,
                1.0,
                segmentCurrentN,
                segmentWeightKg));
        }

        var segmentCurrentTotal = segments.Sum(x => x.CurrentForceN);
        var currentForceN = H0N + segmentCurrentTotal + pointCurrentForceN;
        var result = new CalculationResult(
            "Synthetic",
            string.Empty,
            buoyancyKg,
            0.0,
            0.0,
            currentForceN,
            0.0,
            currentForceN,
            0.0,
            0.0,
            string.Empty,
            0.0,
            0.0,
            1.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            LengthM,
            0.0,
            Array.Empty<ElementCalculationRow>(),
            segments,
            Array.Empty<string>());

        var rows = new List<MooringSequencePositionRow>
        {
            new(100, "TOP", "Top boundary", string.Empty, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, false, true, string.Empty, string.Empty),
            new(200, "DISTRIBUTED", "Synthetic line", string.Empty, 0.0, LengthM, LengthM / 2.0, LengthM, weightNPerM * LengthM / G, segmentCurrentTotal, true, false, string.Empty, string.Empty)
        };
        if (pointCurrentForceN != 0.0 || pointWeightWaterKg != 0.0)
        {
            rows.Add(new MooringSequencePositionRow(
                300,
                "POINT",
                "Internal point",
                string.Empty,
                LengthM / 2.0,
                LengthM / 2.0,
                LengthM / 2.0,
                0.0,
                pointWeightWaterKg,
                pointCurrentForceN,
                false,
                true,
                string.Empty,
                string.Empty));
        }
        rows.Add(new MooringSequencePositionRow(
            400,
            "BOTTOM",
            "Bottom boundary",
            string.Empty,
            LengthM,
            LengthM,
            LengthM,
            0.0,
            0.0,
            0.0,
            false,
            true,
            string.Empty,
            string.Empty));

        var sequence = new MooringSequencePositionResult(
            rows,
            LengthM,
            1,
            pointCurrentForceN != 0.0 || pointWeightWaterKg != 0.0 ? 1 : 0,
            pointWeightWaterKg,
            pointCurrentForceN,
            "Synthetic boundary test; boundary kinds intentionally non-localized.");

        var environment = new EnvironmentInput(
            1025.0,
            depthM,
            0.0,
            0.0,
            0.0,
            new SeabedPreset("surface-info:synthetic", "Synthetic", 1.0, string.Empty));
        var buoy = new BuoyInput("Synthetic buoy", 1.0, 100.0, 1.0, 1.0);
        return new Scenario(environment, buoy, result, sequence);
    }

    private static MooringSurfaceBoundaryInfoResult Analyze(Scenario scenario)
    {
        return MooringSurfaceBoundaryInfoAnalyzer.Build(
            scenario.Environment,
            scenario.Buoy,
            scenario.Result,
            scenario.Sequence);
    }

    private static void RequireClassification(
        MooringSurfaceBoundaryInfoResult info,
        MooringSurfaceBoundaryInfoClassification expected,
        bool available,
        bool solved,
        string label)
    {
        if (info.Classification != expected || info.Available != available || info.Solved != solved)
        {
            throw new InvalidOperationException(
                $"Surface-boundary INFO regression {label}: expected {expected}/Available={available}/Solved={solved}, got {info.Classification}/Available={info.Available}/Solved={info.Solved}.");
        }
    }

    private static double Required(double? value, string label)
    {
        return value ?? throw new InvalidOperationException("Surface-boundary INFO regression missing value: " + label);
    }

    private static void Near(double expected, double actual, double tolerance, string label)
    {
        if (Math.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException($"Surface-boundary INFO regression {label}: expected {expected:R}, got {actual:R}.");
    }

    private sealed record Scenario(
        EnvironmentInput Environment,
        BuoyInput Buoy,
        CalculationResult Result,
        MooringSequencePositionResult Sequence);
}
