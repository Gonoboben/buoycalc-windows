using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class IterativeFeedbackCouplingRegression
{
    private const double G = 9.80665;
    private const double Tolerance = 1e-10;

    private static readonly SeabedPreset Seabed = new(
        "feedback:sand",
        "Feedback regression sand",
        1.2,
        "Deterministic feedback coupling regression seabed.");

    private static readonly BuoyInput Buoy = new(
        "Feedback regression buoy",
        1.0,
        100.0,
        0.8,
        0.8);

    private static readonly AnchorInput Anchor = new(
        "Feedback regression anchor",
        "Concrete block",
        "Concrete",
        1000.0,
        0.4,
        1.0);

    private static readonly RopePreset LinePreset = new(
        "feedback:line",
        "Feedback regression line",
        "Polyester",
        20.0,
        100.0,
        0.1,
        1.2,
        "Deterministic feedback coupling line.");

    public static void Validate()
    {
        ValidatePreIterativeIdentity();
        ValidateSuppliedFeedbackDrivesCandidateState();
        ValidateNoPointLoadIdentity();
        ValidateSignedBuoyantPointLoad();
    }

    private static void ValidatePreIterativeIdentity()
    {
        var pipeline = BuildPipeline(new AssemblyItemInput[]
        {
            Line("Upper line", 30),
            Payload("Payload", 40.0, 0.005, 0.05, 1.0),
            Line("Lower line", 25)
        });

        foreach (var row in pipeline.Discrete.Rows)
        {
            var segment = pipeline.Result.SegmentRows.Single(x => x.Number == row.SegmentNumber);
            var expectedDistributedH = pipeline.Result.SegmentRows
                .Where(x => x.Number >= row.SegmentNumber)
                .Sum(x => x.CurrentForceN);
            var expectedDistributedV = pipeline.Result.SegmentRows
                .Where(x => x.Number >= row.SegmentNumber)
                .Sum(x => x.WeightWaterKg) * G;
            var expectedPointH = pipeline.Discrete.DiscreteLoads
                .Where(x => x.PositionAlongLineM >= row.StartAlongLineM)
                .Sum(x => x.CurrentForceN);
            var expectedPointV = pipeline.Discrete.DiscreteLoads
                .Where(x => x.PositionAlongLineM >= row.StartAlongLineM)
                .Sum(x => x.WeightWaterKg) * G;

            AssertNear(expectedDistributedH + expectedPointH, row.CumulativeHorizontalForceN,
                $"pre identity segment {row.SegmentNumber} H");
            AssertNear(expectedDistributedV + expectedPointV, row.CumulativeVerticalForceN,
                $"pre identity segment {row.SegmentNumber} V");
            AssertNear(segment.CurrentForceN, row.SegmentForceN,
                $"pre identity segment {row.SegmentNumber} local Fx");
            AssertNear(segment.WeightWaterKg, row.SegmentWeightWaterKg,
                $"pre identity segment {row.SegmentNumber} local Ww");
        }
    }

    private static void ValidateSuppliedFeedbackDrivesCandidateState()
    {
        var pipeline = BuildPipeline(new AssemblyItemInput[]
        {
            Line("Upper line", 30),
            Payload("Payload", 40.0, 0.005, 0.05, 1.0),
            Line("Lower line", 25)
        });

        const double horizontalShiftN = 37.5;
        var feedbackRows = pipeline.BaseTensions
            .Select(row =>
            {
                var h = row.CumulativeHorizontalForceN + horizontalShiftN;
                var v = row.CumulativeVerticalForceN;
                var tensionN = Math.Sqrt(h * h + v * v);
                var angleDeg = Math.Atan2(Math.Abs(h), Math.Max(0.0001, Math.Abs(v))) * 180.0 / Math.PI;
                return row with
                {
                    SegmentCurrentForceN = row.SegmentCurrentForceN + 0.25,
                    CumulativeHorizontalForceN = h,
                    CumulativeVerticalForceN = v,
                    TensionKn = tensionN / 1000.0,
                    AngleFromVerticalDeg = angleDeg
                };
            })
            .ToList();

        var feedbackDiscrete = MooringDiscreteLoadTensionAnalyzer.Build(
            pipeline.Result,
            feedbackRows,
            pipeline.Positions);

        foreach (var row in feedbackDiscrete.Rows)
        {
            var supplied = feedbackRows.Single(x => x.Number == row.SegmentNumber);
            var pointH = feedbackDiscrete.DiscreteLoads
                .Where(x => x.PositionAlongLineM >= row.StartAlongLineM)
                .Sum(x => x.CurrentForceN);
            var pointV = feedbackDiscrete.DiscreteLoads
                .Where(x => x.PositionAlongLineM >= row.StartAlongLineM)
                .Sum(x => x.WeightWaterKg) * G;

            AssertNear(supplied.CumulativeHorizontalForceN + pointH, row.CumulativeHorizontalForceN,
                $"feedback segment {row.SegmentNumber} H");
            AssertNear(supplied.CumulativeVerticalForceN + pointV, row.CumulativeVerticalForceN,
                $"feedback segment {row.SegmentNumber} V");
            AssertNear(supplied.SegmentCurrentForceN, row.SegmentForceN,
                $"feedback segment {row.SegmentNumber} local Fx provenance");

            var baseline = pipeline.Discrete.Rows.Single(x => x.SegmentNumber == row.SegmentNumber);
            AssertNear(horizontalShiftN,
                row.CumulativeHorizontalForceN - baseline.CumulativeHorizontalForceN,
                $"feedback segment {row.SegmentNumber} propagated H shift");
        }
    }

    private static void ValidateNoPointLoadIdentity()
    {
        var pipeline = BuildPipeline(new[] { Line("Line", 55) });
        if (pipeline.Discrete.DiscreteLoads.Count != 0)
        {
            throw new InvalidOperationException("no-point-load case unexpectedly has discrete loads.");
        }

        foreach (var row in pipeline.Discrete.Rows)
        {
            var distributed = pipeline.BaseTensions.Single(x => x.Number == row.SegmentNumber);
            AssertNear(distributed.CumulativeHorizontalForceN, row.CumulativeHorizontalForceN,
                $"no-point segment {row.SegmentNumber} H");
            AssertNear(distributed.CumulativeVerticalForceN, row.CumulativeVerticalForceN,
                $"no-point segment {row.SegmentNumber} V");
        }
    }

    private static void ValidateSignedBuoyantPointLoad()
    {
        var pipeline = BuildPipeline(new AssemblyItemInput[]
        {
            Line("Upper line", 30),
            Payload("Buoyant payload", 1.0, 0.01, 0.03, 1.0),
            Line("Lower line", 25)
        });

        var load = pipeline.Discrete.DiscreteLoads.Single();
        if (load.WeightWaterKg >= 0)
        {
            throw new InvalidOperationException(
                $"buoyant point-load regression expected negative WeightWaterKg, got {load.WeightWaterKg:R}.");
        }

        var firstBelow = pipeline.Discrete.Rows
            .Where(x => x.StartAlongLineM <= load.PositionAlongLineM)
            .OrderByDescending(x => x.StartAlongLineM)
            .First();
        var distributed = pipeline.BaseTensions.Single(x => x.Number == firstBelow.SegmentNumber);
        var expectedV = distributed.CumulativeVerticalForceN + load.WeightWaterKg * G;
        AssertNear(expectedV, firstBelow.CumulativeVerticalForceN, "buoyant point-load signed V");
    }

    private static Pipeline BuildPipeline(IReadOnlyList<AssemblyItemInput> assembly)
    {
        var environment = new EnvironmentInput(
            1025.0,
            50.0,
            0.5,
            0.5,
            5.0,
            Seabed);
        var result = BuoyCalculator.Calculate(environment, Buoy, assembly, Anchor, 3.0);
        var baseTensions = SegmentTensionAnalyzer.Build(result);
        var positions = MooringSequencePositioner.Build(result);
        var discrete = MooringDiscreteLoadTensionAnalyzer.Build(result, baseTensions, positions);
        return new Pipeline(result, baseTensions, positions, discrete);
    }

    private static AssemblyItemInput Line(string title, double lengthM)
    {
        return new AssemblyItemInput(
            AssemblyItemKind.Line,
            title,
            true,
            LinePreset,
            null,
            lengthM,
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

    private static void AssertNear(double expected, double actual, string name)
    {
        if (!double.IsFinite(expected) || !double.IsFinite(actual) || Math.Abs(expected - actual) > Tolerance)
        {
            throw new InvalidOperationException(
                $"{name}: expected {expected:R}, got {actual:R}, tolerance {Tolerance:R}.");
        }
    }

    private sealed record Pipeline(
        CalculationResult Result,
        IReadOnlyList<SegmentTensionRow> BaseTensions,
        MooringSequencePositionResult Positions,
        MooringDiscreteLoadTensionResult Discrete);
}
