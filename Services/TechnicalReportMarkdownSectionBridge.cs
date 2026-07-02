using System;
using System.Reflection;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Temporary bridge to the legacy ReportBuilder section renderers.
///
/// It keeps the Markdown output stable while section helpers are moved from
/// ReportBuilder into TechnicalReportMarkdownBuilder in later small steps.
/// </summary>
internal static class TechnicalReportMarkdownSectionBridge
{
    public static void Append(string methodName, params object[] args)
    {
        var method = typeof(ReportBuilder).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"ReportBuilder helper not found: {methodName}");

        method.Invoke(null, args);
    }
}
