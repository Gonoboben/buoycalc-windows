using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class TechnicalReportBuilder
{
    public static string Build(
        string projectName,
        EnvironmentInput environment,
        BuoyInput buoy,
        AnchorInput anchor,
        CalculationResult result)
    {
        return ReportBuilder.Build(projectName, environment, buoy, anchor, result);
    }
}
