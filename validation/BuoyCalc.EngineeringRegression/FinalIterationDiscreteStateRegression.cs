using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class FinalIterationDiscreteStateRegression
{
    private const double IdentityTolerance = 1e-12;

    private static readonly SeabedPreset RegressionSeabed = new(
        "final-state:sand",
        "Final-state regression sand",
        1.2,
        "Deterministic final-state regression seabed.");

    private static readonly BuoyInput RegressionBuoy = new(
        "Final-state regression buoy",
        1.0,
        100.0,
        0.8,
        0.8);

    private static readonly AnchorInput RegressionAnchor = new(
        "Final-state regression anchor",
        "Concrete block",
        "Concrete",
        1000.0,
        0.4,
        1.0);

    private static readonly RopePreset RegressionLine = new(
        "final-state:line",
        "Final-state line",
        "Polyester",
        20.0,
        100.0,
        0.1,
        1.2,
        "Deterministic final-state line.");

    public static void Validate()
    {
        ValidateNoIterationStateIsUnavailable();
        ValidateOneIterationRetention();
        ValidateDefaultSolverRetention();
    }

    private static void ValidateNoIterationStateIsUnavailable()
    {
        var environment = Environment(depthM: 50, currentSpeedMS: 0.3);
        var result = BuoyCalculator.Calculate(
            environment,
            RegressionBuoy,
            Array.Empty<AssemblyItemInput>(),
            RegressionAnchor,
            3.0);
        var shape = MooringShapeSolver.Build(environment, result);
        var positions = MooringSequencePositioner.Build(result);
        var tensions = SegmentTensionAnalyzer.Build(result);
        var iterative = MooringIterativeSolver.Build(result, shape, positions, tensions);

        if (iterative.IterationCount != 0)
        {
            throw new InvalidOperationException($"final-state invalid case: expected zero iterations, got {iterative.IterationCount}.");
        }

        if (iterative.FinalShape is not null ||
            iterative.FinalDiscreteLoadTensions is not null ||
            iterative.FinalDiscreteLoadShape is not null)
        {
            throw new InvalidOperationException("final-state invalid case: no-iteration solver must not publish synthetic final state.");
        }
    }

    private static void ValidateOneIterationRetention()
    {
        var pipeline = BuildPipeline(maxIterations: 1);
        ValidateRetainedState("one-iteration", pipeline);

        if (pipeline.Iterative.IterationCount != 1)
        {
            throw new InvalidOperationException($"one-iteration: expected exactly one iteration, got {pipeline.Iterative.IterationCount}.");
        }
    }

    private static void ValidateDefaultSolverRetention()
    {
        var pipeline = BuildPipeline(maxIterations: null);
        ValidateRetainedState("default-solver", pipeline);

        if (pipeline.Iterative.IterationCount <= 0)
        {
            throw new InvalidOperationException("default-solver: expected at least one executed iteration.");
        }
    }

    private static void ValidateRetainedState(string name, Pipeline pipeline)
    {
        var iterative = pipeline.Iterative;
        var finalShape = iterative.FinalShape
            ?? throw new InvalidOperationException($"{name}: FinalShape is missing after an executed iteration.");
        var finalTensions = iterative.FinalDiscreteLoadTensions
            ?? throw new InvalidOperationException($"{name}: FinalDiscreteLoadTensions is missing after an executed iteration.");
        var finalDiscreteShape = iterative.FinalDiscreteLoadShape
            ?? throw new InvalidOperationException($"{name}: FinalDiscreteLoadShape is missing after an executed iteration.");

        if (iterative.Rows.Count != iterative.IterationCount)
        {
            throw new InvalidOperationException($"{name}: iteration row count does not match IterationCount.");
        }

        if (finalDiscreteShape.Rows.Count != finalShape.Nodes.Count)
        {
            throw new InvalidOperationException(
                $"{name}: retained discrete-shape row count {finalDiscreteShape.Rows.Count} != FinalShape node count {finalShape.Nodes.Count}.");
        }

        var orderedRows = finalDiscreteShape.Rows.OrderBy(x => x.Number).ToList();
        var orderedNodes = finalShape.Nodes.OrderBy(x => x.Number).ToList();
        for (var i = 0; i < orderedRows.Count; i++)
        {
            var row = orderedRows[i];
            var node = orderedNodes[i];
            AssertNear(row.XOffsetM, node.XOffsetM, IdentityTolerance, $"{name}: node {i} X identity");
            AssertNear(row.ZDepthM, node.ZDepthM, IdentityTolerance, $"{name}: node {i} Z identity");
            AssertNear(row.AlongLineM, node.AlongLineM, IdentityTolerance, $"{name}: node {i} s identity");
        }

        var lastIteration = iterative.Rows[^1];
        AssertNear(
            lastIteration.TopDiscreteTensionKn,
            finalTensions.TopDiscreteTensionKn,
            IdentityTolerance,
            $"{name}: final top discrete tension identity");

        AssertNear(
            finalDiscreteShape.DiscreteHorizontalOffsetM,
            finalShape.HorizontalOffsetM,
            IdentityTolerance,
            $"{name}: final offset identity");

        if (iterative.StopReason == MooringIterativeSolverStopReason.InvalidInput)
        {
            throw new InvalidOperationException($"{name}: executed iteration unexpectedly ended with InvalidInput.");
        }
    }

    private static Pipeline BuildPipeline(int? maxIterations)
    {
        var environment = Environment(depthM: 50, currentSpeedMS: 0.5);
        var assembly = new AssemblyItemInput[]
        {
            Line("Upper line", 30),
            Payload("Instrument", 40.0, 0.005, 0.05, 1.0),
            Line("Lower line", 25)
        };
        var result = BuoyCalculator.Calculate(
            environment,
            RegressionBuoy,
            assembly,
            RegressionAnchor,
            3.0);
        var shape = MooringShapeSolver.Build(environment, result);
        var positions = MooringSequencePositioner.Build(result);
        var tensions = SegmentTensionAnalyzer.Build(result);
        var iterative = maxIterations.HasValue
            ? MooringIterativeSolver.Build(result, shape, positions, tensions, maxIterations.Value)
            : MooringIterativeSolver.Build(result, shape, positions, tensions);

        return new Pipeline(iterative);
    }

    private static EnvironmentInput Environment(double depthM, double currentSpeedMS)
    {
        return new EnvironmentInput(
            1025.0,
            depthM,
            currentSpeedMS,
            0.5,
            5.0,
            RegressionSeabed);
    }

    private static AssemblyItemInput Line(string title, double lengthM)
    {
        return new AssemblyItemInput(
            AssemblyItemKind.Line,
            title,
            true,
            RegressionLine,
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

    private static void AssertNear(double expected, double actual, double tolerance, string name)
    {
        if (!double.IsFinite(expected) ||
            !double.IsFinite(actual) ||
            Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"{name}: expected {expected:R}, got {actual:R}, tolerance {tolerance:R}.");
        }
    }

    private sealed record Pipeline(MooringIterativeSolverResult Iterative);
}
