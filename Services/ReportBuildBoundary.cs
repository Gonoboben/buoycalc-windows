using BuoyCalc.Windows.ApplicationModel;
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
        CalculationSnapshot snapshot)
    {
        return new ReportBuildResult(
            UserReportBuilder.Build(environment, snapshot.Result),
            TechnicalReportBuilder.Build(projectName, environment, buoy, anchor, snapshot));
    }
}
