using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class UniformCurrentReadModelRegression
{
    private const double Rho = 1025.0;
    private const double U = 1.2;
    private const double DiameterM = 0.01;
    private const double L = 2.0;
    private const double Cd = 1.5;
    private const double Tol = 1e-10;

    public static void Validate()
    {
        ValidateVertical();
        ValidateDiagonal();
        ValidateProfileUnavailable();
    }

    private static void ValidateVertical()
    {
        var result = Build(dx: 0.0, dz: L, useProfile: false);
        RequireOne(result, "vertical");
        var row = result.Rows[0];
        var r = ReferenceForce();

        Near(-U, row.CurrentXMS, "vertical current X");
        Near(0.0, row.CurrentZMS, "vertical current Z");
        Near(0.0, row.TangentX, "vertical tangent X");
        Near(1.0, row.TangentZ, "vertical tangent Z");
        Near(-U, row.NormalVelocityXMS, "vertical normal velocity X");
        Near(0.0, row.NormalVelocityZMS, "vertical normal velocity Z");
        Near(U, row.NormalSpeedMS, "vertical normal speed");
        Near(-r, row.NormalForceXN, "vertical force X");
        Near(0.0, row.NormalForceZN, "vertical force Z");
        Near(r, row.NormalForceMagnitudeN, "vertical magnitude");
        Near(row.ExistingShapeForceN, row.NormalForceMagnitudeN, "vertical magnitude overlap");
        Near(0.0, row.MagnitudeDifferenceN, "vertical difference");
    }

    private static void ValidateDiagonal()
    {
        var c = L / Math.Sqrt(2.0);
        var result = Build(dx: c, dz: c, useProfile: false);
        RequireOne(result, "diagonal");
        var row = result.Rows[0];
        var r = ReferenceForce();
        var expectedComponent = r / (2.0 * Math.Sqrt(2.0));

        Near(1.0 / Math.Sqrt(2.0), row.TangentX, "diagonal tangent X");
        Near(1.0 / Math.Sqrt(2.0), row.TangentZ, "diagonal tangent Z");
        Near(-U / 2.0, row.NormalVelocityXMS, "diagonal normal velocity X");
        Near(U / 2.0, row.NormalVelocityZMS, "diagonal normal velocity Z");
        Near(U / Math.Sqrt(2.0), row.NormalSpeedMS, "diagonal normal speed");
        Near(-expectedComponent, row.NormalForceXN, "diagonal force X");
        Near(expectedComponent, row.NormalForceZN, "diagonal force Z");
        Near(0.5 * r, row.NormalForceMagnitudeN, "diagonal magnitude");
        Near(row.ExistingShapeForceN, row.NormalForceMagnitudeN, "diagonal magnitude overlap");
        Near(0.0, row.NormalForceXN * row.TangentX + row.NormalForceZN * row.TangentZ, "diagonal orthogonality");
    }

    private static void ValidateProfileUnavailable()
    {
        var result = Build(dx: 0.0, dz: L, useProfile: true);
        if (result.Available || result.Rows.Count != 0)
        {
            throw new InvalidOperationException(
                $"Uniform-current read model regression: profile case must be unavailable with zero rows; Available={result.Available}, Rows={result.Rows.Count}.");
        }

        Near(0.0, result.SumNormalForceXN, "profile sum X");
        Near(0.0, result.SumNormalForceZN, "profile sum Z");
        Near(0.0, result.SumNormalForceMagnitudeN, "profile magnitude sum");
    }

    private static MooringUniformCurrentNormalVectorResult Build(double dx, double dz, bool useProfile)
    {
        var projected = Math.Sqrt(dx * dx + dz * dz);
        var baseForce = ReferenceForce();
        var segment = new SegmentCalculationRow(
            1, "Synthetic line", "Synthetic", 0.0, L, L, 1.0,
            U, 0.0, 0.0, U, Rho, DiameterM * L, Cd, baseForce, 0.0);

        var calculation = new CalculationResult(
            "Synthetic", string.Empty, 0.0, 0.0, 0.0, baseForce, 0.0, baseForce,
            0.0, 0.0, string.Empty, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, L, 0.0,
            Array.Empty<ElementCalculationRow>(), new[] { segment }, Array.Empty<string>());

        var angle = Math.Atan2(Math.Abs(dx), Math.Max(0.0001, Math.Abs(dz))) * 180.0 / Math.PI;
        var projectionRow = new MooringShapeProjectionRow(
            1, 1, "Synthetic segment", L, dx, dz, projected,
            Math.Abs(projected - L), angle, 0.0, "OK");
        var projection = new MooringShapeProjectionResult(
            new[] { projectionRow }, dx, dz, L, projected, Math.Abs(projected - L),
            dx, dz, 0.0, 0.0, angle, angle,
            Math.Abs(projected - L) <= 1e-12, "Synthetic case");
        var shapeForces = MooringShapeForceAnalyzer.Build(calculation, projection);

        var environment = new EnvironmentInput(
            Rho, 10.0, U, 0.0, 0.0,
            new SeabedPreset("synthetic", "Synthetic", 1.0, string.Empty),
            useProfile,
            useProfile
                ? new[] { new CurrentProfilePointInput(0.0, U, 0.0, 0.0, Rho) }
                : Array.Empty<CurrentProfilePointInput>());

        return MooringUniformCurrentNormalVectorAnalyzer.Build(environment, calculation, projection, shapeForces);
    }

    private static double ReferenceForce() => 0.5 * Rho * Cd * DiameterM * L * U * U;

    private static void RequireOne(MooringUniformCurrentNormalVectorResult result, string label)
    {
        if (!result.Available || result.Rows.Count != 1)
        {
            throw new InvalidOperationException(
                $"Uniform-current read model regression {label}: expected one available row; Available={result.Available}, Rows={result.Rows.Count}.");
        }
    }

    private static void Near(double expected, double actual, string label)
    {
        if (Math.Abs(expected - actual) > Tol)
        {
            throw new InvalidOperationException(
                $"Uniform-current read model regression {label}: expected {expected:R}, got {actual:R}.");
        }
    }
}
