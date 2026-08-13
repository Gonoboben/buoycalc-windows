internal static class ProfilePlanarProjectionRegression
{
    private const double Tol = 1e-12;

    public static void Validate()
    {
        Check(0.6, -0.2, 0.1, 1.0, 0.0, 1.0, 0.6, -0.2, 0.1, "east axis");
        Check(0.6, -0.2, 0.1, 0.0, 1.0, 1.0, -0.2, -0.6, 0.1, "north axis");

        var q = 1.0 / Math.Sqrt(2.0);
        Check(3.0, 4.0, 0.0, q, q, 1.0, 7.0 * q, q, 0.0, "oblique axis");
        Check(0.0, 1.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0, 0.0, "out-of-plane");
        Check(0.0, 0.0, -0.35, 0.6, 0.8, 1.0, 0.0, 0.0, -0.35, "vertical only");
        Check(0.0, 0.0, 0.3, 1.0, 0.0, -1.0, 0.0, 0.0, -0.3, "vertical sign");

        var axisEast = Math.Cos(0.37);
        var axisNorth = Math.Sin(0.37);
        var forward = Project(0.7, -0.4, 0.2, axisEast, axisNorth, 1.0);
        var reverse = Project(-0.7, 0.4, 0.2, axisEast, axisNorth, 1.0);
        Near(-forward.X, reverse.X, "reversal X");
        Near(-forward.OutOfPlane, reverse.OutOfPlane, "reversal out-of-plane");
        Near(forward.Z, reverse.Z, "reversal Z");

        CheckNorm(0.6, -0.2, 0.0);
        CheckNorm(-1.3, 0.9, 0.43);
        CheckNorm(2.0, 5.0, -1.1);
        CheckNorm(0.0, 0.0, 2.4);

        RequireRejected(() => Project(1.0, 2.0, 3.0, 0.0, 0.0, 1.0), "zero horizontal axis");
        RequireRejected(() => Project(1.0, 2.0, 3.0, 1.0, 0.0, 0.0), "invalid vertical sign");
    }

    private static void CheckNorm(double east, double north, double angle)
    {
        var p = Project(east, north, 0.0, Math.Cos(angle), Math.Sin(angle), 1.0);
        Near(east * east + north * north, p.X * p.X + p.OutOfPlane * p.OutOfPlane, $"horizontal norm {angle:R}");
    }

    private static void Check(
        double east,
        double north,
        double vertical,
        double axisEast,
        double axisNorth,
        double verticalSign,
        double expectedX,
        double expectedOutOfPlane,
        double expectedZ,
        string label)
    {
        var p = Project(east, north, vertical, axisEast, axisNorth, verticalSign);
        Near(expectedX, p.X, label + " X");
        Near(expectedOutOfPlane, p.OutOfPlane, label + " out-of-plane");
        Near(expectedZ, p.Z, label + " Z");
    }

    private static Projection Project(
        double east,
        double north,
        double vertical,
        double axisEast,
        double axisNorth,
        double verticalSign)
    {
        var axisNorm = Math.Sqrt(axisEast * axisEast + axisNorth * axisNorth);
        if (!double.IsFinite(axisNorm) || axisNorm <= 1e-15)
            throw new InvalidOperationException("Profile planar projection regression: horizontal axis is degenerate.");

        if (!double.IsFinite(verticalSign) || Math.Abs(Math.Abs(verticalSign) - 1.0) > Tol)
            throw new InvalidOperationException("Profile planar projection regression: vertical sign must be +1 or -1.");

        var eEast = axisEast / axisNorm;
        var eNorth = axisNorth / axisNorm;
        return new Projection(
            east * eEast + north * eNorth,
            -east * eNorth + north * eEast,
            verticalSign * vertical);
    }

    private static void RequireRejected(Action action, string label)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException("Profile planar projection regression expected rejection: " + label);
    }

    private static void Near(double expected, double actual, string label)
    {
        if (Math.Abs(expected - actual) > Tol * Math.Max(1.0, Math.Abs(expected)))
            throw new InvalidOperationException($"Profile planar projection regression {label}: expected {expected:R}, got {actual:R}.");
    }

    private sealed record Projection(double X, double OutOfPlane, double Z);
}
