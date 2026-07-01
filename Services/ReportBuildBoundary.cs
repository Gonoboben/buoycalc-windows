using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record ReportBuildResult(
    string UserResultText,
    string TechnicalReportText);

public static class ReportBuildBoundary
{
    public static ReportBuildResult Build(
        string projectName,
        EnvironmentInput environment,
        BuoyInput buoy,
        AnchorInput anchor,
        CalculationResult result)
    {
        return new ReportBuildResult(
            UserResultTextBuilder.Build(environment, result),
            ReportBuilder.Build(projectName, environment, buoy, anchor, result));
    }
}
