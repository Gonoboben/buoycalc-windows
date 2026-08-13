using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class UniformCurrentReportRegression
{
    public static void Validate()
    {
        ValidateUniformSection();
        ValidateProfileUnavailableSection();
    }

    private static void ValidateUniformSection()
    {
        var environment = BuildEnvironment(useProfile: false);
        var report = BuildReport(environment);

        RequireContains(report, "## Вектор нормального сопротивления линии — uniform current", "uniform heading");
        RequireContains(report, "- Статус: доступно для scalar/uniform-current mode.", "uniform available status");
        RequireContains(report, "вычисления в renderer не выполняются", "passive-renderer note");
    }

    private static void ValidateProfileUnavailableSection()
    {
        var environment = BuildEnvironment(useProfile: true);
        var report = BuildReport(environment);

        RequireContains(report, "## Вектор нормального сопротивления линии — uniform current", "profile heading");
        RequireContains(report, "- Статус: недоступно для текущего режима расчёта.", "profile unavailable status");
        RequireContains(report, "planar X/Z projection", "profile unavailable reason");
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
        return TechnicalReportMarkdownBuilder.Build("Uniform-current report regression", environment, buoy, anchor, run.Snapshot);
    }

    private static EnvironmentInput BuildEnvironment(bool useProfile)
    {
        return new EnvironmentInput(
            WaterDensityKgM3: 1025.0,
            DepthM: 10.0,
            CurrentSpeedMS: 0.8,
            WaveHeightM: 0.0,
            WavePeriodS: 0.0,
            Seabed: new SeabedPreset("synthetic", "Synthetic", 1.0, string.Empty),
            UseCurrentProfile: useProfile,
            CurrentProfile: useProfile
                ? new[]
                {
                    new CurrentProfilePointInput(0.0, 0.8, 0.0, 0.0, 1025.0),
                    new CurrentProfilePointInput(10.0, 0.6, 0.2, 0.0, 1025.0)
                }
                : Array.Empty<CurrentProfilePointInput>());
    }

    private static void RequireContains(string value, string expected, string label)
    {
        if (!value.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Uniform-current report regression {label}: report does not contain expected text: {expected}");
        }
    }
}
