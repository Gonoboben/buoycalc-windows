using System;

namespace BuoyCalc.Windows.Services;

internal static class TechnicalReportMarkdownSectionBridge
{
    public static void Append(string methodName, params object[] args)
    {
        if (TechnicalReportMarkdownMovedSections.TryAppend(methodName, args))
        {
            return;
        }

        if (TechnicalReportMarkdownDiscreteShapeSections.TryAppend(methodName, args))
        {
            return;
        }

        if (TechnicalReportMarkdownDiscreteTensionSections.TryAppend(methodName, args))
        {
            return;
        }

        if (TechnicalReportMarkdownDiscreteNodeSections.TryAppend(methodName, args))
        {
            return;
        }

        if (TechnicalReportMarkdownIterativeSolverSections.TryAppend(methodName, args))
        {
            return;
        }

        if (TechnicalReportMarkdownCheckSections.TryAppend(methodName, args))
        {
            return;
        }

        throw new InvalidOperationException($"Technical report Markdown section renderer not found: {methodName}");
    }
}
