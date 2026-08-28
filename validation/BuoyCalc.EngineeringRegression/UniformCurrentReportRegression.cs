using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class UniformCurrentReportRegression
{
    public static void Validate()
    {
        var environment = BuildEnvironment();
        var report = BuildReport(environment);

        RequireNotContains(report, "## Вектор нормального сопротивления линии — uniform current", "retired uniform heading");
        RequireNotContains(report, "scalar/uniform-current mode", "retired scalar mode status");
        RequireContains(report, "## Профиль течения по глубине", "mandatory current profile section");
    }

    private static string BuildReport(EnvironmentInput environment)
    {
        var buoy = new BuoyInput(
            "Synthetic buoy",
            VolumeM3: 0.8,
            WeightKg: 80.0,
            ProjectedAreaM2: 0.5,
            DragCoefficient: 1.0);

        var rope = new RopePreset(
            "synthetic_rope",
            "Synthetic rope",
            "Synthetic",
            DiameterMm: 10.0,
            BreakingLoadKn: 50.0,
            WeightWaterKgM: 0.1,
            DragCoefficient: 1.2,
            Note: string.Empty);

        var assembly = new[]
        {
            new AssemblyItemInput(
                AssemblyItemKind.Line,
                "Synthetic line",
                true,
                rope,
                null,
                LengthM: 12.0,
                Count: 1,
                PayloadWeightAirKg: 0.0,
                PayloadVolumeM3: 0.0,
                PayloadProjectedAreaM2: 0.0,
                PayloadDragCoefficient: 1.0)
        };

        var anchor = new AnchorInput(
            "Synthetic anchor",
            "Deadweight",
            "Concrete",
            WeightAirKg: 1000.0,
            VolumeM3: 0.4,
            BaseHoldingCoefficient: 1.0);

        var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, safetyFactor: 2.0);
        return TechnicalReportMarkdownBuilder.Build("Profile-only report regression", environment, buoy, anchor, run.Snapshot);
    }

    private static EnvironmentInput BuildEnvironment()
    {
        return new EnvironmentInput(
            WaterDensityKgM3: 1025.0,
            DepthM: 10.0,
            CurrentSpeedMS: 99.0,
            WaveHeightM: 0.0,
            WavePeriodS: 0.0,
            Seabed: new SeabedPreset("synthetic", "Synthetic", 1.0, string.Empty),
            UseCurrentProfile: true,
            CurrentProfile: new[]
            {
                new CurrentProfilePointInput(0.0, 0.8, 0.0, 0.0, 1025.0),
                new CurrentProfilePointInput(10.0, 0.6, 0.2, 0.0, 1025.0)
            });
    }

    private static void RequireContains(string value, string expected, string label)
    {
        if (!value.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Profile-only report regression {label}: report does not contain expected text: {expected}");
        }
    }

    private static void RequireNotContains(string value, string forbidden, string label)
    {
        if (value.Contains(forbidden, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Profile-only report regression {label}: report contains retired text: {forbidden}");
        }
    }
}
