using BuoyCalc.Windows.Models;

internal static class MandatoryCurrentProfileRegression
{
    private const double TightTolerance = 1e-12;

    private static readonly SeabedPreset Seabed = new(
        "profile-reg",
        "Profile regression seabed",
        1.0,
        string.Empty);

    private static readonly BuoyInput Buoy = new(
        "Profile regression buoy",
        1.0,
        100.0,
        0.8,
        0.8);

    private static readonly AnchorInput Anchor = new(
        "Profile regression anchor",
        "Concrete block",
        "Concrete",
        1000.0,
        0.4,
        1.0);

    private static readonly RopePreset Line = new(
        "profile-reg-line",
        "Profile regression line",
        "Synthetic",
        20.0,
        100.0,
        -0.05,
        1.2,
        string.Empty);

    public static void Validate()
    {
        ValidateRequirementBoundary();
        ValidateScalarOnlyRejected();
        ValidateLegacyScalarAndFlagAreNonAuthoritative();
        ValidateExplicitConstantProfileReference();
        ValidateProfileDensityAndSignedLineInvariants();
    }

    private static void ValidateRequirementBoundary()
    {
        if (CurrentProfileRequirement.IsUsable(Array.Empty<CurrentProfilePointInput>()))
            throw new InvalidOperationException("Mandatory profile regression: empty profile was accepted.");

        var one = new[] { Point(0, 0.5, 1025) };
        if (CurrentProfileRequirement.IsUsable(one))
            throw new InvalidOperationException("Mandatory profile regression: one profile point was accepted.");

        var duplicateDepth = new[] { Point(0, 0.5, 1025), Point(0, 0.2, 1025) };
        if (CurrentProfileRequirement.IsUsable(duplicateDepth))
            throw new InvalidOperationException("Mandatory profile regression: duplicate-depth profile was accepted.");

        var valid = new[] { Point(0, 0.5, 1025), Point(50, 0.2, 1025) };
        if (!CurrentProfileRequirement.IsUsable(valid))
            throw new InvalidOperationException("Mandatory profile regression: two distinct finite profile depths were rejected.");
    }

    private static void ValidateScalarOnlyRejected()
    {
        var environment = new EnvironmentInput(
            1025,
            50,
            0.5,
            0,
            0,
            Seabed,
            false,
            Array.Empty<CurrentProfilePointInput>());

        try
        {
            _ = BuoyCalculator.Calculate(environment, Buoy, Items(50), Anchor, 3.0);
        }
        catch (InvalidOperationException ex) when (ex.Message == CurrentProfileRequirement.UserMessage)
        {
            return;
        }

        throw new InvalidOperationException(
            "Mandatory profile regression: scalar-only environment calculated instead of being rejected.");
    }

    private static void ValidateLegacyScalarAndFlagAreNonAuthoritative()
    {
        var profile = new[]
        {
            Point(0, 0.6, 1025),
            Point(25, 0.3, 1025),
            Point(50, 0.1, 1025)
        };

        var legacyLooking = new EnvironmentInput(
            1025, 50, 9.9, 0, 0, Seabed, false, profile);
        var canonical = new EnvironmentInput(
            1025, 50, 0, 0, 0, Seabed, true, profile);

        var legacyResult = BuoyCalculator.Calculate(legacyLooking, Buoy, Items(50), Anchor, 3.0);
        var canonicalResult = BuoyCalculator.Calculate(canonical, Buoy, Items(50), Anchor, 3.0);

        Near(canonical.EffectiveCurrentSpeedMS, legacyLooking.EffectiveCurrentSpeedMS,
            "legacy scalar must not change effective profile speed");
        Near(canonicalResult.CurrentForceN, legacyResult.CurrentForceN,
            "legacy scalar/flag must not change total current force");

        if (legacyResult.SegmentRows.Count != canonicalResult.SegmentRows.Count)
            throw new InvalidOperationException("Mandatory profile regression: legacy flag changed segment count.");

        for (var i = 0; i < canonicalResult.SegmentRows.Count; i++)
        {
            Near(canonicalResult.SegmentRows[i].LocalSpeedMS, legacyResult.SegmentRows[i].LocalSpeedMS,
                $"segment {i + 1} local profile speed");
            Near(canonicalResult.SegmentRows[i].CurrentForceN, legacyResult.SegmentRows[i].CurrentForceN,
                $"segment {i + 1} profile drag");
        }
    }

    private static void ValidateExplicitConstantProfileReference()
    {
        const double rho = 1025.0;
        const double u = 0.5;
        const double length = 10.0;
        const double diameterM = 0.020;
        const double cd = 1.2;

        var environment = new EnvironmentInput(
            rho,
            length,
            0,
            0,
            0,
            Seabed,
            true,
            new[] { Point(0, u, rho), Point(length, u, rho) });

        var result = BuoyCalculator.Calculate(environment, Buoy, Items(length), Anchor, 3.0);
        var lineDrag = result.SegmentRows.Sum(x => x.CurrentForceN);
        var historicalScalarReference = 0.5 * rho * u * u * (length * diameterM) * cd;

        Near(historicalScalarReference, lineDrag,
            "explicit constant profile must preserve constitutive scalar drag reference");

        if (result.SegmentRows.Count == 0 || result.SegmentRows.Max(x => x.SegmentLengthM) > 0.200000001)
            throw new InvalidOperationException("Mandatory profile regression: 0.20 m production segmentation changed.");

        if (result.SegmentRows.Any(x => Math.Abs(x.LocalSpeedMS - u) > TightTolerance))
            throw new InvalidOperationException("Mandatory profile regression: explicit constant profile was not sampled consistently.");
    }

    private static void ValidateProfileDensityAndSignedLineInvariants()
    {
        var environment = new EnvironmentInput(
            1025,
            20,
            0,
            0,
            0,
            Seabed,
            true,
            new[] { Point(0, 0.4, 1000), Point(20, 0.2, 1040) });

        Near(1020.0, environment.EffectiveWaterDensityKgM3,
            "profile density average semantics");

        var result = BuoyCalculator.Calculate(environment, Buoy, Items(20), Anchor, 3.0);
        if (!result.SegmentRows.Any(x => x.WeightWaterKg < 0))
            throw new InvalidOperationException("Mandatory profile regression: signed buoyant-line water weight was normalized away.");
    }

    private static IReadOnlyList<AssemblyItemInput> Items(double lengthM)
    {
        return new[]
        {
            new AssemblyItemInput(
                AssemblyItemKind.Line,
                "Profile regression line",
                true,
                Line,
                null,
                lengthM,
                1,
                0,
                0,
                0,
                0)
        };
    }

    private static CurrentProfilePointInput Point(double depthM, double eastCurrentMS, double rho)
    {
        return new CurrentProfilePointInput(depthM, eastCurrentMS, 0, 0, rho);
    }

    private static void Near(double expected, double actual, string label)
    {
        var difference = Math.Abs(expected - actual);
        var scale = Math.Max(1.0, Math.Max(Math.Abs(expected), Math.Abs(actual)));
        if (!double.IsFinite(expected) || !double.IsFinite(actual) ||
            (difference > TightTolerance && difference > TightTolerance * scale))
        {
            throw new InvalidOperationException(
                $"Mandatory profile regression {label}: expected {expected:R}, got {actual:R}.");
        }
    }
}
