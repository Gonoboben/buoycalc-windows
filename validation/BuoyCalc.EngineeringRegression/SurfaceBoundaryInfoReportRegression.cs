using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SurfaceBoundaryInfoReportRegression
{
    public static void Validate()
    {
        var environment = new EnvironmentInput(
            1025.0,
            10.0,
            0.0,
            0.0,
            0.0,
            new SeabedPreset("surface-info:report", "Synthetic", 1.0, string.Empty),
            true,
            new[]
            {
                new CurrentProfilePointInput(0.0, 0.8, 0.0, 0.0, 1025.0),
                new CurrentProfilePointInput(10.0, 0.8, 0.0, 0.0, 1025.0)
            });
        var buoy = new BuoyInput("Synthetic buoy", 0.8, 80.0, 0.5, 1.0);
        var rope = new RopePreset(
            "surface-info:report-rope",
            "Synthetic rope",
            "Synthetic",
            10.0,
            50.0,
            0.1,
            1.2,
            string.Empty);
        var assembly = new[]
        {
            new AssemblyItemInput(
                AssemblyItemKind.Line,
                "Synthetic line",
                true,
                rope,
                null,
                12.0,
                1,
                0.0,
                0.0,
                0.0,
                1.0)
        };
        var anchor = new AnchorInput(
            "Synthetic anchor",
            "Deadweight",
            "Concrete",
            1000.0,
            0.4,
            1.0);

        var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, 2.0);
        var typedReport = TechnicalReportMarkdownBuilder.Build(
            "Surface boundary report regression",
            environment,
            buoy,
            anchor,
            run.Snapshot);

        RequireContains(typedReport, "## Поверхностная вертикальная граница буя — frozen-load INFO", "typed heading");
        RequireContains(typedReport, "INFO-only read model", "typed INFO authority");
        RequireContains(typedReport, "Доступно: да", "typed availability");
        RequireContains(typedReport, "Классификация:", "typed classification");
        RequireContains(typedReport, "Предельный Q_capacity, Н", "typed capacity");
        RequireContains(typedReport, "wave excluded", "typed method provenance");
        RequireContains(typedReport, "diagnostic X/Z is not a selected-shape source", "typed selected-shape disclaimer");

        RequireContains(typedReport, "## Boundary-conditioned trace натяжения — INFO", "typed trace heading");
        RequireContains(typedReport, "Сегментов trace:", "typed trace segment count");
        RequireContains(typedReport, "H на поверхности, Н", "typed trace start H");
        RequireContains(typedReport, "V на якорной стороне, Н", "typed trace terminal V");
        RequireContains(typedReport, "Макс. midpoint tension:", "typed trace max tension");
        RequireContains(typedReport, "Реперные строки trace (0 / 25 / 50 / 75 / 100% по списку сегментов):", "typed trace compact samples");
        RequireContains(typedReport, "signed angle от +Z, °", "typed trace signed-angle convention");
        RequireContains(typedReport, "not selected-shape authority", "typed trace authority disclaimer");

        var compatibilitySnapshot = CalculationSnapshotBuilder.Build(environment, run.Result);
        var compatibilityReport = TechnicalReportMarkdownBuilder.Build(
            "Surface boundary compatibility report regression",
            environment,
            buoy,
            anchor,
            compatibilitySnapshot);

        RequireContains(compatibilityReport, "нет типизированного BuoyInput", "compatibility classification");
        RequireContains(compatibilityReport, "Доступно: нет", "compatibility availability");
        RequireContains(compatibilityReport, "Решение Q0 найдено: нет", "compatibility solved state");
        RequireContains(compatibilityReport, "## Boundary-conditioned trace натяжения — INFO", "compatibility trace heading");
        RequireContains(compatibilityReport, "Parent surface-boundary state is not a solved bounded Q0 state.", "compatibility trace unavailable reason");
    }

    private static void RequireContains(string text, string expected, string label)
    {
        if (!text.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Surface-boundary INFO report regression {label}: missing text '{expected}'.");
        }
    }
}
