using BuoyCalc.Windows.Services;

internal static class SignedOrientationRegression
{
    private const double Tolerance = 1e-12;

    public static void Validate()
    {
        ValidateHeavyAndBuoyantQuadrants();
        ValidatePureVerticalDirections();
        ValidateDegenerateState();
    }

    private static void ValidateHeavyAndBuoyantQuadrants()
    {
        var historicalUnsignedAngle = Math.Atan2(3.0, 4.0) * 180.0 / Math.PI;
        var result = MooringSignedOrientationAnalyzer.Build(new[]
        {
            Row(1, 3.0, 4.0, historicalUnsignedAngle),
            Row(2, 3.0, -4.0, historicalUnsignedAngle)
        });

        if (result.Rows.Count != 2 || result.AvailableCount != 2 || result.IndeterminateCount != 0)
        {
            throw new InvalidOperationException("signed-orientation quadrants: expected two available rows.");
        }

        var heavy = result.Rows[0];
        var buoyant = result.Rows[1];

        AssertNear(5.0, heavy.TensionN, "heavy tension");
        AssertNear(0.6, Require(heavy.TangentX, "heavy tx"), "heavy tx");
        AssertNear(0.8, Require(heavy.TangentZ, "heavy tz"), "heavy tz");

        AssertNear(5.0, buoyant.TensionN, "buoyant tension");
        AssertNear(0.6, Require(buoyant.TangentX, "buoyant tx"), "buoyant tx");
        AssertNear(-0.8, Require(buoyant.TangentZ, "buoyant tz"), "buoyant tz");

        if (Require(heavy.SignedAngleFromVerticalDeg, "heavy signed angle") >= 90.0)
        {
            throw new InvalidOperationException("signed-orientation quadrants: heavy case must remain in the downward first quadrant.");
        }

        if (Require(buoyant.SignedAngleFromVerticalDeg, "buoyant signed angle") <= 90.0)
        {
            throw new InvalidOperationException("signed-orientation quadrants: buoyant case must preserve the upward vertical quadrant (> 90 deg from +Z down).");
        }

        AssertNear(
            heavy.HistoricalUnsignedAngleFromVerticalDeg,
            buoyant.HistoricalUnsignedAngleFromVerticalDeg,
            "historical unsigned angle collapse");
    }

    private static void ValidatePureVerticalDirections()
    {
        var result = MooringSignedOrientationAnalyzer.Build(new[]
        {
            Row(1, 0.0, 10.0, 0.0),
            Row(2, 0.0, -10.0, 0.0)
        });

        var downward = result.Rows[0];
        var upward = result.Rows[1];

        AssertNear(0.0, Require(downward.TangentX, "downward tx"), "downward tx");
        AssertNear(1.0, Require(downward.TangentZ, "downward tz"), "downward tz");
        AssertNear(0.0, Require(downward.SignedAngleFromVerticalDeg, "downward angle"), "downward angle");

        AssertNear(0.0, Require(upward.TangentX, "upward tx"), "upward tx");
        AssertNear(-1.0, Require(upward.TangentZ, "upward tz"), "upward tz");
        AssertNear(180.0, Require(upward.SignedAngleFromVerticalDeg, "upward angle"), "upward angle");
    }

    private static void ValidateDegenerateState()
    {
        var result = MooringSignedOrientationAnalyzer.Build(new[]
        {
            Row(1, 0.0, 0.0, 0.0)
        });

        if (result.AvailableCount != 0 || result.IndeterminateCount != 1)
        {
            throw new InvalidOperationException("signed-orientation degenerate: zero resultant must be indeterminate.");
        }

        var row = result.Rows[0];
        if (row.TangentX.HasValue || row.TangentZ.HasValue || row.SignedAngleFromVerticalDeg.HasValue)
        {
            throw new InvalidOperationException("signed-orientation degenerate: no artificial tangent or angle is allowed.");
        }
    }

    private static SegmentTensionRow Row(
        int number,
        double horizontalForceN,
        double verticalForceN,
        double historicalUnsignedAngleDeg)
    {
        return new SegmentTensionRow(
            number,
            $"Synthetic {number}",
            number,
            1.0,
            0.0,
            0.0,
            horizontalForceN,
            verticalForceN,
            Math.Sqrt(horizontalForceN * horizontalForceN + verticalForceN * verticalForceN) / 1000.0,
            historicalUnsignedAngleDeg,
            "OK");
    }

    private static double Require(double? value, string label)
    {
        return value ?? throw new InvalidOperationException($"signed-orientation: missing {label}.");
    }

    private static void AssertNear(double expected, double actual, string label)
    {
        if (Math.Abs(expected - actual) > Tolerance)
        {
            throw new InvalidOperationException(
                $"signed-orientation {label}: expected {expected:R}, got {actual:R}.");
        }
    }
}
