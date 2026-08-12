using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class ShapeLineLengthSourceRegression
{
    private const double ExactTolerance = 1e-12;

    private static readonly SeabedPreset RegressionSeabed = new(
        "reg:shape-length-sand",
        "Regression sand",
        1.2,
        "Shape line-length source regression seabed.");

    private static readonly BuoyInput RegressionBuoy = new(
        "Regression buoy",
        1.0,
        100.0,
        0.8,
        0.8);

    private static readonly AnchorInput RegressionAnchor = new(
        "Regression concrete anchor",
        "Concrete block",
        "Concrete",
        1000.0,
        0.4,
        1.0);

    private static readonly RopePreset HeavyLine = new(
        "reg:shape-heavy-line",
        "Regression heavy line",
        "Polyester",
        20.0,
        100.0,
        0.1,
        1.2,
        "Shape line-length source regression rope.");

    internal static void Validate()
    {
        ValidateExactVerticalLine();
        ValidateSegmentReconstructionDriftDoesNotRedefineGlobalLength();
        ValidateMultipleLineItems();
        ValidateShortLineMode();
        ValidateSlackLineMode();
    }

    private static void ValidateExactVerticalLine()
    {
        var environment = Environment(depthM: 50, currentSpeedMS: 0);
        var result = Calculate(environment, new[] { Line("Vertical line", 50) });
        var shape = MooringShapeSolver.Build(environment, result);

        AssertNear(result.LineLengthM, 50, ExactTolerance, "vertical: authoritative line length");
        AssertZeroCurrent(result, "vertical");
        AssertVerticalShape(shape, "vertical");

        var segmentSum = result.SegmentRows.Sum(x => x.SegmentLengthM);
        if (segmentSum == result.LineLengthM)
        {
            throw new InvalidOperationException(
                "vertical: regression fixture no longer reproduces segment-sum representation drift; review #378 validation assumptions.");
        }
    }

    private static void ValidateSegmentReconstructionDriftDoesNotRedefineGlobalLength()
    {
        var environment = Environment(depthM: 50, currentSpeedMS: 0);
        var result = Calculate(environment, new[] { Line("Vertical line", 50) });
        var rows = result.SegmentRows.ToList();
        if (rows.Count == 0)
        {
            throw new InvalidOperationException("synthetic drift: no segment rows.");
        }

        const double injectedDriftM = 1e-6;
        rows[^1] = rows[^1] with { SegmentLengthM = rows[^1].SegmentLengthM + injectedDriftM };
        var drifted = result with { SegmentRows = rows };
        var reconstructedLength = drifted.SegmentRows.Sum(x => x.SegmentLengthM);

        if (reconstructedLength <= drifted.LineLengthM)
        {
            throw new InvalidOperationException("synthetic drift: injected segment-sum drift was not created.");
        }

        var shape = MooringShapeSolver.Build(environment, drifted);
        AssertNear(shape.LineLengthM, drifted.LineLengthM, ExactTolerance, "synthetic drift: shape authoritative line length");
        AssertVerticalShape(shape, "synthetic drift");
    }

    private static void ValidateMultipleLineItems()
    {
        var environment = Environment(depthM: 50, currentSpeedMS: 0);
        var result = Calculate(
            environment,
            new[]
            {
                Line("Upper line", 20),
                Line("Lower line", 30)
            });
        var shape = MooringShapeSolver.Build(environment, result);

        AssertNear(result.LineLengthM, 50, ExactTolerance, "multiple lines: authoritative line length");
        AssertZeroCurrent(result, "multiple lines");
        AssertVerticalShape(shape, "multiple lines");
    }

    private static void ValidateShortLineMode()
    {
        var environment = Environment(depthM: 50, currentSpeedMS: 0);
        var result = Calculate(environment, new[] { Line("Short line", 49) });
        var shape = MooringShapeSolver.Build(environment, result);

        AssertNear(shape.LineLengthM, 49, ExactTolerance, "short line: line length");
        if (shape.BuoyState != BuoyShapeState.Submerged)
        {
            throw new InvalidOperationException($"short line: expected Submerged, got {shape.BuoyState}.");
        }

        if (shape.Converged)
        {
            throw new InvalidOperationException("short line: L < Depth must not become a converged normal surface shape.");
        }

        if (shape.BuoyPoint is null || shape.BuoyPoint.ZDepthM <= 0)
        {
            throw new InvalidOperationException("short line: buoy boundary must remain below surface.");
        }
    }

    private static void ValidateSlackLineMode()
    {
        var environment = Environment(depthM: 50, currentSpeedMS: 0);
        var result = Calculate(environment, new[] { Line("Slack line", 55) });
        var shape = MooringShapeSolver.Build(environment, result);

        AssertNear(shape.LineLengthM, 55, ExactTolerance, "slack line: line length");
        if (shape.Nodes.Count < 2)
        {
            throw new InvalidOperationException("slack line: fallback shape was not produced.");
        }

        if (!shape.Converged)
        {
            throw new InvalidOperationException("slack line: existing fallback geometry should still converge.");
        }

        if (!double.IsFinite(shape.HorizontalOffsetM) || shape.HorizontalOffsetM <= 0)
        {
            throw new InvalidOperationException(
                $"slack line: genuine L > Depth geometric offset must remain positive, got {shape.HorizontalOffsetM:R}.");
        }
    }

    private static void AssertVerticalShape(MooringShapeResult shape, string label)
    {
        if (shape.Nodes.Count < 2)
        {
            throw new InvalidOperationException($"{label}: no fallback shape nodes.");
        }

        AssertNear(shape.HorizontalOffsetM, 0, ExactTolerance, $"{label}: horizontal offset");
        AssertNear(shape.AngleScale, 1, ExactTolerance, $"{label}: angle scale");

        foreach (var node in shape.Nodes)
        {
            AssertNear(node.XOffsetM, 0, ExactTolerance, $"{label}: node {node.Number} X");
            AssertNear(node.SegmentAngleFromVerticalDeg, 0, ExactTolerance, $"{label}: node {node.Number} angle");
        }
    }

    private static void AssertZeroCurrent(CalculationResult result, string label)
    {
        if (Math.Abs(result.CurrentForceN) > ExactTolerance ||
            result.SegmentRows.Any(x => Math.Abs(x.CurrentForceN) > ExactTolerance))
        {
            throw new InvalidOperationException($"{label}: zero-current fixture contains non-zero current force.");
        }
    }

    private static CalculationResult Calculate(
        EnvironmentInput environment,
        IReadOnlyList<AssemblyItemInput> items)
    {
        return BuoyCalculator.Calculate(
            environment,
            RegressionBuoy,
            items,
            RegressionAnchor,
            3.0);
    }

    private static EnvironmentInput Environment(double depthM, double currentSpeedMS)
    {
        return new EnvironmentInput(
            1025.0,
            depthM,
            currentSpeedMS,
            0,
            0,
            RegressionSeabed);
    }

    private static AssemblyItemInput Line(string title, double lengthM)
    {
        return new AssemblyItemInput(
            AssemblyItemKind.Line,
            title,
            true,
            HeavyLine,
            null,
            lengthM,
            1,
            0,
            0,
            0,
            0);
    }

    private static void AssertNear(double actual, double expected, double tolerance, string label)
    {
        if (!double.IsFinite(actual) || Math.Abs(actual - expected) > tolerance)
        {
            throw new InvalidOperationException(
                $"{label}: expected {expected:R} ± {tolerance:R}, got {actual:R}.");
        }
    }
}
