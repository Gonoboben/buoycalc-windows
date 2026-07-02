using System;
using System.Reflection;

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

        var method = typeof(ReportBuilder).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"ReportBuilder helper not found: {methodName}");

        method.Invoke(null, args);
    }
}
