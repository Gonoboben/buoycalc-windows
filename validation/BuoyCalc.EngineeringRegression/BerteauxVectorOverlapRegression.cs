using BuoyCalc.Windows.Services;

internal static class BerteauxVectorOverlapRegression
{
    private const double Tolerance = 1e-12;
    private const double GravityMps2 = 9.80665;

    public static void Validate()
    {
        ValidateHeavyDistributedElementBalance();
        ValidateBuoyantDistributedElementBalance();
        ValidateConstantLoadSubstepComposition();
        ValidatePointLoadJumpBalance();
        ValidateSignedTangentMapping();
    }

    private static void ValidateHeavyDistributedElementBalance()
    {
        const double h0 = 82.0;
        const double v0 = 500.0;
        const double qx = 3.075;
        const double w = 0.980665;
        const double ds = 0.2;

        var h1 = h0 + qx * ds;
        var v1 = v0 - w * ds;

        AssertNear(0.0, -h0 + h1 - qx * ds, "heavy element horizontal free-body closure");
        AssertNear(0.0, -v0 + v1 + w * ds, "heavy element vertical free-body closure");
        AssertNear(qx, (h1 - h0) / ds, "heavy element dH/ds");
        AssertNear(-w, (v1 - v0) / ds, "heavy element dV/ds");

        if (!(v1 < v0))
        {
            throw new InvalidOperationException(
                "Berteaux vector overlap heavy element: positive submerged weight must reduce the downward cable component when crossing top-to-bottom.");
        }
    }

    private static void ValidateBuoyantDistributedElementBalance()
    {
        const double h0 = 82.0;
        const double v0 = 50.0;
        const double qx = 3.075;
        const double w = -2.5;
        const double ds = 0.2;

        var h1 = h0 + qx * ds;
        var v1 = v0 - w * ds;

        AssertNear(0.0, -h0 + h1 - qx * ds, "buoyant element horizontal free-body closure");
        AssertNear(0.0, -v0 + v1 + w * ds, "buoyant element vertical free-body closure");
        AssertNear(qx, (h1 - h0) / ds, "buoyant element dH/ds");
        AssertNear(-w, (v1 - v0) / ds, "buoyant element dV/ds");

        if (!(v1 > v0))
        {
            throw new InvalidOperationException(
                "Berteaux vector overlap buoyant element: negative submerged weight must increase the downward cable component when crossing top-to-bottom.");
        }
    }

    private static void ValidateConstantLoadSubstepComposition()
    {
        const double h0 = 25.0;
        const double v0 = -12.0;
        const double qx = 1.75;
        const double w = -0.625;
        const double ds = 0.4;

        var oneStepH = h0 + qx * ds;
        var oneStepV = v0 - w * ds;

        var halfStepH = h0 + qx * (ds / 2.0);
        var halfStepV = v0 - w * (ds / 2.0);
        halfStepH += qx * (ds / 2.0);
        halfStepV -= w * (ds / 2.0);

        AssertNear(oneStepH, halfStepH, "constant-load substep H composition");
        AssertNear(oneStepV, halfStepV, "constant-load substep V composition");
    }

    private static void ValidatePointLoadJumpBalance()
    {
        const double hBefore = 174.25;
        const double vBefore = 910.0;
        const double pointForceX = 7.6875;
        const double pointWeightWaterKg = 39.1575;

        var pointWeightN = pointWeightWaterKg * GravityMps2;
        var hAfter = hBefore + pointForceX;
        var vAfter = vBefore - pointWeightN;

        AssertNear(384.003897375, pointWeightN, "deterministic point submerged weight force");
        AssertNear(0.0, -hBefore + hAfter - pointForceX, "point horizontal free-body closure");
        AssertNear(0.0, -vBefore + vAfter + pointWeightN, "point vertical free-body closure");
        AssertNear(pointForceX, hAfter - hBefore, "point H jump");
        AssertNear(-pointWeightN, vAfter - vBefore, "point V jump");
    }

    private static void ValidateSignedTangentMapping()
    {
        const double h0 = 3.0;
        const double qx = 0.0;
        const double w = 4.0;
        const double ds = 2.0;

        var heavyV = 12.0 - w * ds;
        var buoyantV = -12.0 - (-w) * ds;
        var historicalUnsignedAngle = Math.Atan2(Math.Abs(h0), Math.Abs(heavyV)) * 180.0 / Math.PI;

        var result = MooringSignedOrientationAnalyzer.Build(new[]
        {
            Row(1, h0 + qx * ds, heavyV, historicalUnsignedAngle),
            Row(2, h0 + qx * ds, buoyantV, historicalUnsignedAngle)
        });

        if (result.Rows.Count != 2 || result.AvailableCount != 2 || result.IndeterminateCount != 0)
        {
            throw new InvalidOperationException("Berteaux vector overlap tangent mapping: expected two available signed rows.");
        }

        var heavy = result.Rows[0];
        var buoyant = result.Rows[1];

        if (!(Require(heavy.TangentZ, "heavy tangent Z") > 0.0))
        {
            throw new InvalidOperationException("Berteaux vector overlap tangent mapping: heavy resultant must preserve +Z tangent direction.");
        }

        if (!(Require(buoyant.TangentZ, "buoyant tangent Z") < 0.0))
        {
            throw new InvalidOperationException("Berteaux vector overlap tangent mapping: buoyant resultant must preserve -Z tangent direction.");
        }

        AssertNear(
            heavy.HistoricalUnsignedAngleFromVerticalDeg,
            buoyant.HistoricalUnsignedAngleFromVerticalDeg,
            "historical unsigned angle collapse remains diagnostic only");
    }

    private static SegmentTensionRow Row(
        int number,
        double horizontalForceN,
        double verticalForceN,
        double historicalUnsignedAngleDeg)
    {
        return new SegmentTensionRow(
            number,
            $"Berteaux overlap synthetic {number}",
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
        return value ?? throw new InvalidOperationException($"Berteaux vector overlap: missing {label}.");
    }

    private static void AssertNear(double expected, double actual, string label)
    {
        if (Math.Abs(expected - actual) > Tolerance)
        {
            throw new InvalidOperationException(
                $"Berteaux vector overlap {label}: expected {expected:R}, got {actual:R}.");
        }
    }
}
