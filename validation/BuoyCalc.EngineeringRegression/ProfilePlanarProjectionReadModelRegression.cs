using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class ProfilePlanarProjectionReadModelRegression
{
    private const double Tol = 1e-12;

    public static void Validate()
    {
        var profile = new[]
        {
            new CurrentProfilePointInput(12.0, 0.6, -0.2, 0.1, 1025.0)
        };

        var unavailable = ProfilePlanarProjectionReadModelBuilder.Build(profile, null);
        if (unavailable.Available || unavailable.AxisAzimuthDeg is not null || unavailable.Rows.Count != 0)
            throw new InvalidOperationException("Profile planar read model must be unavailable when axis azimuth is absent.");

        var northAxis = ProfilePlanarProjectionReadModelBuilder.Build(profile, 0.0);
        RequireOne(northAxis, 0.0, "north axis");
        Near(-0.2, northAxis.Rows[0].UXMS, "north U_X");
        Near(-0.6, northAxis.Rows[0].UOutMS, "north U_out");
        Near(0.1, northAxis.Rows[0].UZMS, "north U_Z");

        var eastAxis = ProfilePlanarProjectionReadModelBuilder.Build(profile, 90.0);
        RequireOne(eastAxis, 90.0, "east axis");
        Near(0.6, eastAxis.Rows[0].UXMS, "east U_X");
        Near(-0.2, eastAxis.Rows[0].UOutMS, "east U_out");
        Near(0.6, eastAxis.Rows[0].RetainedHorizontalSpeedMS, "east retained magnitude");
        Near(0.2, eastAxis.Rows[0].DiscardedHorizontalSpeedMS, "east discarded magnitude");

        var normalized = ProfilePlanarProjectionReadModelBuilder.Build(profile, 450.0);
        RequireOne(normalized, 90.0, "normalized axis");
        Near(eastAxis.Rows[0].UXMS, normalized.Rows[0].UXMS, "normalized U_X");
        Near(eastAxis.Rows[0].UOutMS, normalized.Rows[0].UOutMS, "normalized U_out");

        RequireRejected(() => ProfilePlanarProjectionReadModelBuilder.Build(profile, double.NaN), "non-finite axis");
        RequireRejected(() => ProfilePlanarProjectionReadModelBuilder.Build(
            new[] { new CurrentProfilePointInput(0.0, double.PositiveInfinity, 0.0, 0.0, 1025.0) },
            90.0), "non-finite current");
    }

    private static void RequireOne(ProfilePlanarProjectionReadModel result, double expectedAzimuth, string label)
    {
        if (!result.Available || result.Rows.Count != 1 || result.AxisAzimuthDeg is null)
            throw new InvalidOperationException($"Profile planar read model regression {label}: expected one available row.");
        Near(expectedAzimuth, result.AxisAzimuthDeg.Value, label + " azimuth");
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

        throw new InvalidOperationException("Profile planar read model regression expected rejection: " + label);
    }

    private static void Near(double expected, double actual, string label)
    {
        if (Math.Abs(expected - actual) > Tol * Math.Max(1.0, Math.Abs(expected)))
            throw new InvalidOperationException($"Profile planar read model regression {label}: expected {expected:R}, got {actual:R}.");
    }
}
