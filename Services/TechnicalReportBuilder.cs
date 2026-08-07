using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class TechnicalReportBuilder
{
    public static string Build(
        string projectName,
        EnvironmentInput environment,
        BuoyInput buoy,
        AnchorInput anchor,
        CalculationSnapshot snapshot)
    {
        return TechnicalReportMarkdownBuilder.Build(projectName, environment, buoy, anchor, snapshot);
    }
}
