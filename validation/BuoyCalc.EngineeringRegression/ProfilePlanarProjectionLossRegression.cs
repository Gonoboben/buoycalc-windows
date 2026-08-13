using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class ProfilePlanarProjectionLossRegression
{
    private const double Tol = 1e-10;

    public static void Validate()
    {
        ValidateCanonicalProfile();
        ValidateRotatingProfile();
    }

    private static void ValidateCanonicalProfile()
    {
        var profile = new[]
        {
            new CurrentProfilePointInput(0.0, 0.6, 0.0, 0.0, 1025.0),
            new CurrentProfilePointInput(25.0, 0.3, 0.0, 0.0, 1025.0),
            new CurrentProfilePointInput(50.0, 0.1, 0.0, 0.0, 1025.0)
        };

        var aligned = Measure(ProfilePlanarProjectionReadModelBuilder.Build(profile, 90.0));
        Near(0.6, aligned.MaxHorizontalSpeedMS, "canonical max horizontal speed");
        Near(0.0, aligned.MaxOutOfPlaneSpeedMS, "canonical aligned max U_out");
        Near(0.0, aligned.MaxDiscardedFraction, "canonical aligned discarded fraction");

        var crossAxis = Measure(ProfilePlanarProjectionReadModelBuilder.Build(profile, 0.0));
        Near(0.6, crossAxis.MaxOutOfPlaneSpeedMS, "canonical cross-axis max U_out");
        Near(1.0, crossAxis.MaxDiscardedFraction, "canonical cross-axis max discarded fraction");
        Near(1.0, crossAxis.MeanDiscardedFraction, "canonical cross-axis mean discarded fraction");
    }

    private static void ValidateRotatingProfile()
    {
        var profile = new[]
        {
            new CurrentProfilePointInput(0.0, 1.0, 0.0, 0.0, 1025.0),
            new CurrentProfilePointInput(10.0, 0.0, 1.0, 0.0, 1025.0),
            new CurrentProfilePointInput(20.0, -1.0, 0.0, 0.0, 1025.0),
            new CurrentProfilePointInput(30.0, 0.0, -1.0, 0.0, 1025.0)
        };

        var measure = Measure(ProfilePlanarProjectionReadModelBuilder.Build(profile, 90.0));
        Near(1.0, measure.MaxOutOfPlaneSpeedMS, "rotating max U_out");
        Near(1.0, measure.MaxDiscardedFraction, "rotating max discarded fraction");
        Near(0.5, measure.MeanDiscardedFraction, "rotating mean discarded fraction");
    }

    private static LossMeasure Measure(ProfilePlanarProjectionReadModel projection)
    {
        if (!projection.Available || projection.Rows.Count == 0)
            throw new InvalidOperationException("Profile planar loss regression requires an available projection.");

        var maxHorizontal = 0.0;
        var maxOut = 0.0;
        var maxFraction = 0.0;
        var sumFraction = 0.0;
        var activeCount = 0;

        foreach (var row in projection.Rows)
        {
            var horizontal = Math.Sqrt(row.EastCurrentMS * row.EastCurrentMS + row.NorthCurrentMS * row.NorthCurrentMS);
            maxHorizontal = Math.Max(maxHorizontal, horizontal);
            maxOut = Math.Max(maxOut, Math.Abs(row.UOutMS));
            if (horizontal <= Tol) continue;

            var fraction = Math.Abs(row.UOutMS) / horizontal;
            maxFraction = Math.Max(maxFraction, fraction);
            sumFraction += fraction;
            activeCount++;
        }

        if (activeCount == 0)
            throw new InvalidOperationException("Profile planar loss regression requires non-zero horizontal current.");

        return new(maxHorizontal, maxOut, maxFraction, sumFraction / activeCount);
    }

    private static void Near(double expected, double actual, string label)
    {
        if (Math.Abs(expected - actual) > Tol * Math.Max(1.0, Math.Abs(expected)))
            throw new InvalidOperationException($"Profile planar loss regression {label}: expected {expected:R}, got {actual:R}.");
    }

    private sealed record LossMeasure(double MaxHorizontalSpeedMS, double MaxOutOfPlaneSpeedMS, double MaxDiscardedFraction, double MeanDiscardedFraction);
}
