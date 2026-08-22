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
        var legacyReport = TechnicalReportMarkdownBuilder.Build(projectName, environment, buoy, anchor, snapshot);
        return SelectedTechnicalReportProjector.Project(legacyReport, snapshot);
    }
}
