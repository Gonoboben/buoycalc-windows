using System.Globalization;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SignedGeometryProductionBlockerFeasibilityRegression
{
    private const double ExactFixtureLengthToleranceM = 1e-12;
    private const double ExactZeroForceToleranceN = 1e-12;

    public static void Validate()
    {
        var fixtures = BuildBlockedHistoricalFixtures();

        Console.WriteLine("SIGNED_GEOMETRY_BLOCKER_FEASIBILITY_BEGIN");

        foreach (var fixture in fixtures)
        {
            var run = ApplicationCalculationRunner.Run(
                fixture.Environment,
                fixture.Buoy,
                fixture.Assembly,
                fixture.Anchor,
                fixture.SafetyFactor);

            var data = run.Snapshot.TechnicalReportData;
            var boundary = data.SurfaceBoundaryInfo;
            var lineLengthM = fixture.Assembly
                .Where(x => x.Kind == AssemblyItemKind.Line)
                .Sum(x => x.LengthM);
            var segmentWeightWaterSumKg = run.Result.SegmentRows.Sum(x => x.WeightWaterKg);
            var pointLoadCount = fixture.Assembly.Count(x =>
                x.Kind == AssemblyItemKind.Connector || x.Kind == AssemblyItemKind.Payload);

            Near(fixture.Environment.DepthM, fixture.ExpectedDepthM, ExactFixtureLengthToleranceM, fixture.Name + " depth fixture identity");
            Near(lineLengthM, fixture.ExpectedLineLengthM, ExactFixtureLengthToleranceM, fixture.Name + " line-length fixture identity");
            Near(run.Result.CurrentForceN, fixture.ExpectedCurrentForceN, 1e-9, fixture.Name + " historical current-force identity");

            if (boundary.Classification != fixture.ExpectedBoundaryClassification)
            {
                throw new InvalidOperationException(
                    $"Signed-geometry blocker feasibility {fixture.Name}: expected boundary classification {fixture.ExpectedBoundaryClassification}, got {boundary.Classification}.");
            }

            var analytical = ClassifyExactInextensibleGeometry(
                fixture.Environment.DepthM,
                lineLengthM,
                run.Result.CurrentForceN);

            if (analytical != fixture.ExpectedAnalyticalClassification)
            {
                throw new InvalidOperationException(
                    $"Signed-geometry blocker feasibility {fixture.Name}: expected analytical classification {fixture.ExpectedAnalyticalClassification}, got {analytical}.");
            }

            Console.WriteLine(string.Join("|",
                "SIGNED_GEOMETRY_BLOCKER_FEASIBILITY",
                fixture.Name,
                $"DepthM={Format(fixture.Environment.DepthM)}",
                $"LineLengthM={Format(lineLengthM)}",
                $"LengthMinusDepthM={Format(lineLengthM - fixture.Environment.DepthM)}",
                $"CurrentForceN={Format(run.Result.CurrentForceN)}",
                $"HorizontalForceN={Format(run.Result.HorizontalForceN)}",
                $"SegmentWeightWaterSumKg={Format(segmentWeightWaterSumKg)}",
                $"PointLoads={pointLoadCount}",
                $"DiscreteCurrentForceN={Format(data.SequencePositions.DiscreteCurrentForceN)}",
                $"DiscreteWeightWaterKg={Format(data.SequencePositions.DiscreteWeightWaterKg)}",
                $"QCapacityN={Format(boundary.QCapacityN)}",
                $"UseCurrentProfile={fixture.Environment.UseCurrentProfile}",
                $"BoundaryClass={boundary.Classification}",
                $"AnalyticalClass={analytical}",
                $"ProductionSwitchBlocker={analytical != AnalyticalFixtureClassification.SolvableUnique}"));
        }

        ValidateControlledLimits();

        Console.WriteLine("SIGNED_GEOMETRY_BLOCKER_FEASIBILITY_END");
    }

    private static void ValidateControlledLimits()
    {
        Expect(
            ClassifyExactInextensibleGeometry(50.0, 49.0, 0.0),
            AnalyticalFixtureClassification.PhysicallyInfeasibleUnderCurrentInextensibleModel,
            "L < depth");

        Expect(
            ClassifyExactInextensibleGeometry(50.0, 50.0, 0.0),
            AnalyticalFixtureClassification.SolvableUnique,
            "L = depth, zero horizontal load");

        Expect(
            ClassifyExactInextensibleGeometry(50.0, 50.0, 1.0),
            AnalyticalFixtureClassification.PhysicallyInfeasibleUnderCurrentInextensibleModel,
            "L = depth, non-zero horizontal load");

        var slackEligibility = ClassifyLengthOnly(50.0, 55.0);
        if (slackEligibility != LengthFeasibility.SlackBoundarySearchEligible)
        {
            throw new InvalidOperationException(
                $"Signed-geometry blocker feasibility controlled L > depth: expected {LengthFeasibility.SlackBoundarySearchEligible}, got {slackEligibility}.");
        }

        Console.WriteLine(string.Join("|",
            "SIGNED_GEOMETRY_BLOCKER_LIMITS",
            "LltD=PhysicallyInfeasibleUnderCurrentInextensibleModel",
            "LeqD_H0=SolvableUnique",
            "LeqD_Hnonzero=PhysicallyInfeasibleUnderCurrentInextensibleModel",
            "LgtD=SlackBoundarySearchEligible",
            "ToleranceRole=Exact validation fixture comparison only; no production physical tolerance introduced"));
    }

    private static AnalyticalFixtureClassification ClassifyExactInextensibleGeometry(
        double depthM,
        double lineLengthM,
        double horizontalSteadyLoadN)
    {
        var lengthClass = ClassifyLengthOnly(depthM, lineLengthM);

        if (lengthClass == LengthFeasibility.LengthShorterThanDepth)
            return AnalyticalFixtureClassification.PhysicallyInfeasibleUnderCurrentInextensibleModel;

        if (lengthClass == LengthFeasibility.TautVerticalOnly)
        {
            return Math.Abs(horizontalSteadyLoadN) <= ExactZeroForceToleranceN
                ? AnalyticalFixtureClassification.SolvableUnique
                : AnalyticalFixtureClassification.PhysicallyInfeasibleUnderCurrentInextensibleModel;
        }

        return AnalyticalFixtureClassification.IndeterminateMissingModelState;
    }

    private static LengthFeasibility ClassifyLengthOnly(double depthM, double lineLengthM)
    {
        if (lineLengthM + ExactFixtureLengthToleranceM < depthM)
            return LengthFeasibility.LengthShorterThanDepth;

        if (Math.Abs(lineLengthM - depthM) <= ExactFixtureLengthToleranceM)
            return LengthFeasibility.TautVerticalOnly;

        return LengthFeasibility.SlackBoundarySearchEligible;
    }

    private static IReadOnlyList<BlockedHistoricalFixture> BuildBlockedHistoricalFixtures()
    {
        var seabed = new SeabedPreset("reg:sand", "Regression sand", 1.2, "Deterministic regression seabed preset.");
        var buoy = new BuoyInput("Regression buoy", 1.0, 100.0, 0.8, 0.8);
        var anchor = new AnchorInput("Regression concrete anchor", "Concrete block", "Concrete", 1000.0, 0.4, 1.0);
        var heavyLine = new RopePreset("reg:heavy-line", "Regression heavy line", "Polyester", 20.0, 100.0, 0.1, 1.2, "Deterministic heavy-line regression preset.");
        var buoyantLine = new RopePreset("reg:buoyant-line", "Regression buoyant line", "Synthetic buoyant", 20.0, 100.0, -0.05, 1.2, "Negative signed water weight is intentional and must be preserved.");

        return new[]
        {
            new BlockedHistoricalFixture(
                "vertical-zero-current",
                Environment(50.0, 0.0, 0.0, 0.0, seabed),
                buoy,
                new[] { Line("Vertical line", heavyLine, 50.0) },
                anchor,
                3.0,
                50.0,
                50.0,
                0.0,
                MooringSurfaceBoundaryInfoClassification.VerticalGeometryBoundaryNonUnique,
                AnalyticalFixtureClassification.SolvableUnique),

            new BlockedHistoricalFixture(
                "buoyant-line",
                Environment(30.0, 0.3, 0.0, 0.0, seabed),
                buoy,
                new[] { Line("Buoyant line", buoyantLine, 30.0) },
                anchor,
                3.0,
                30.0,
                30.0,
                62.72999999999993,
                MooringSurfaceBoundaryInfoClassification.TautNonZeroHorizontalLoadNoFiniteRoot,
                AnalyticalFixtureClassification.PhysicallyInfeasibleUnderCurrentInextensibleModel),

            new BlockedHistoricalFixture(
                "depth-varying-current-profile",
                new EnvironmentInput(
                    1025.0,
                    50.0,
                    0.2,
                    0.0,
                    0.0,
                    seabed,
                    true,
                    new[]
                    {
                        new CurrentProfilePointInput(0.0, 0.6, 0.0, 0.0, 1025.0),
                        new CurrentProfilePointInput(25.0, 0.3, 0.0, 0.0, 1025.0),
                        new CurrentProfilePointInput(50.0, 0.1, 0.0, 0.0, 1025.0)
                    }),
                buoy,
                new[] { Line("Profile line", heavyLine, 50.0) },
                anchor,
                3.0,
                50.0,
                50.0,
                195.9797868,
                MooringSurfaceBoundaryInfoClassification.TautNonZeroHorizontalLoadNoFiniteRoot,
                AnalyticalFixtureClassification.PhysicallyInfeasibleUnderCurrentInextensibleModel)
        };
    }

    private static EnvironmentInput Environment(
        double depthM,
        double currentSpeedMS,
        double waveHeightM,
        double wavePeriodS,
        SeabedPreset seabed) =>
        new(1025.0, depthM, currentSpeedMS, waveHeightM, wavePeriodS, seabed);

    private static AssemblyItemInput Line(string title, RopePreset preset, double lengthM) =>
        new(AssemblyItemKind.Line, title, true, preset, null, lengthM, 1, 0, 0, 0, 0);

    private static void Expect(
        AnalyticalFixtureClassification actual,
        AnalyticalFixtureClassification expected,
        string label)
    {
        if (actual != expected)
            throw new InvalidOperationException($"Signed-geometry blocker feasibility {label}: expected {expected}, got {actual}.");
    }

    private static void Near(double actual, double expected, double tolerance, string label)
    {
        if (!double.IsFinite(actual) || Math.Abs(actual - expected) > tolerance)
            throw new InvalidOperationException($"Signed-geometry blocker feasibility {label}: expected {expected:R}, got {actual:R}.");
    }

    private static string Format(double? value) => value.HasValue ? Format(value.Value) : "n/a";
    private static string Format(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private enum LengthFeasibility
    {
        LengthShorterThanDepth,
        TautVerticalOnly,
        SlackBoundarySearchEligible
    }

    private enum AnalyticalFixtureClassification
    {
        SolvableUnique,
        PhysicallyInfeasibleUnderCurrentInextensibleModel,
        IndeterminateMissingModelState
    }

    private sealed record BlockedHistoricalFixture(
        string Name,
        EnvironmentInput Environment,
        BuoyInput Buoy,
        IReadOnlyList<AssemblyItemInput> Assembly,
        AnchorInput Anchor,
        double SafetyFactor,
        double ExpectedDepthM,
        double ExpectedLineLengthM,
        double ExpectedCurrentForceN,
        MooringSurfaceBoundaryInfoClassification ExpectedBoundaryClassification,
        AnalyticalFixtureClassification ExpectedAnalyticalClassification);
}
