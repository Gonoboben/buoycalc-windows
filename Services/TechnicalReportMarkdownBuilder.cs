using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Compatibility boundary for the full technical Markdown report.
///
/// This step intentionally delegates to the current ReportBuilder implementation
/// so the generated Markdown stays unchanged while the rendering boundary becomes explicit.
/// </summary>
public static class TechnicalReportMarkdownBuilder
{
    public static string Build(string projectName, EnvironmentInput environment, BuoyInput buoy, AnchorInput anchor, CalculationResult result)
    {
        return ReportBuilder.Build(projectName, environment, buoy, anchor, result);
    }
}
