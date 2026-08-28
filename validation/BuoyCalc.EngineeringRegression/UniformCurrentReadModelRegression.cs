using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class UniformCurrentReadModelRegression
{
    public static void Validate()
    {
        ValidateRetiredRegardlessOfLegacyFlag(false);
        ValidateRetiredRegardlessOfLegacyFlag(true);
    }

    private static void ValidateRetiredRegardlessOfLegacyFlag(bool legacyUseProfileFlag)
    {
        const double rho = 1025.0;
        const double speed = 1.2;
        const double length = 2.0;

        var segment = new SegmentCalculationRow(
            1, "Synthetic line", "Synthetic", 0.0, length, length, 1.0,
            speed, 0.0, 0.0, speed, rho, 0.02, 1.5, 22.14, 0.0);
        var calculation = new CalculationResult(
            "Synthetic", string.Empty, 0.0, 0.0, 0.0, 22.14, 0.0, 22.14,
            0.0, 0.0, string.Empty, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, length, 0.0,
            Array.Empty<ElementCalculationRow>(), new[] { segment }, Array.Empty<string>());
        var projectionRow = new MooringShapeProjectionRow(
            1, 1, "Synthetic segment", length, 0.0, length, length,
            0.0, 0.0, 0.0, "OK");
        var projection = new MooringShapeProjectionResult(
            new[] { projectionRow }, 0.0, length, length, length, 0.0,
            0.0, length, 0.0, 0.0, 0.0, 0.0, true, "Synthetic case");
        var shapeForces = MooringShapeForceAnalyzer.Build(calculation, projection);
        var environment = new EnvironmentInput(
            rho,
            10.0,
            99.0,
            0.0,
            0.0,
            new SeabedPreset("synthetic", "Synthetic", 1.0, string.Empty),
            legacyUseProfileFlag,
            new[]
            {
                new CurrentProfilePointInput(0.0, speed, 0.0, 0.0, rho),
                new CurrentProfilePointInput(10.0, 0.6, 0.2, 0.0, rho)
            });

        var result = MooringUniformCurrentNormalVectorAnalyzer.Build(
            environment,
            calculation,
            projection,
            shapeForces);

        if (result.Available || result.Rows.Count != 0 ||
            result.SumNormalForceXN != 0 || result.SumNormalForceZN != 0 ||
            result.SumNormalForceMagnitudeN != 0 || result.MaxMagnitudeDifferenceN != 0)
        {
            throw new InvalidOperationException(
                $"Retired uniform-current diagnostic became active for legacy flag={legacyUseProfileFlag}.");
        }
    }
}
