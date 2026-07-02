using System.Text;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

internal static class TechnicalReportMarkdownCheckSections
{
    public static bool TryAppend(string methodName, object[] args)
    {
        switch (methodName)
        {
            case "AppendChecks":
                AppendChecks((StringBuilder)args[0], (CalculationResult)args[1]);
                return true;
            default:
                return false;
        }
    }

    private static void AppendChecks(StringBuilder sb, CalculationResult result)
    {
        sb.AppendLine("## Проверки");
        foreach (var check in result.Checks)
        {
            sb.AppendLine($"- {check}");
        }
        sb.AppendLine();
    }
}
