using System.Text;

namespace BuoyCalc.Windows.Services;

public static class PdfReportStructureGuide
{
    public static string Apply(string reportText)
    {
        var text = reportText ?? string.Empty;
        if (text.Contains("## PDF report structure v0.45"))
        {
            return text;
        }

        return BuildMarkdown() + text;
    }

    private static string BuildMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## PDF report structure v0.45");
        sb.AppendLine("This block is added only to the PDF export text stream. It defines the final report order without changing solver physics or X/Z coordinates.");
        sb.AppendLine();
        sb.AppendLine("| Part | Purpose | Source of truth |");
        sb.AppendLine("|---:|---|---|");
        sb.AppendLine("| 1 | Result summary | CalculationResult and EngineeringDiagnostics |");
        sb.AppendLine("| 2 | Solver 2D scheme | MooringShapeStore / MooringShapeSolver output |");
        sb.AppendLine("| 3 | Shape comparison | Main shape plus alternative discrete-load shape when available |");
        sb.AppendLine("| 4 | Element table | CalculationResult.ElementRows |");
        sb.AppendLine("| 5 | Full engineering report | ReportBuilder markdown |");
        sb.AppendLine("| 6 | Diagnostics and limitations | MethodNote fields from calculation services |");
        sb.AppendLine();
        sb.AppendLine("Rule: PDF pages and diagrams must display model outputs only. They must not invent coordinates, forces, tensions or deployment states.");
        sb.AppendLine();
        return sb.ToString();
    }
}
