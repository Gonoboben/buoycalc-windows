using System;
using System.Globalization;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

internal sealed record PdfDiagramSource(
    MooringAlternativeShapeDisplayData? AlternativeShape,
    SelectedShapeReadModel? SelectedShape,
    bool HasAlternativeShape,
    bool HasSelectedShape,
    double ShapeOffsetM);

internal static class PdfDiagramSourceSelector
{
    public static PdfDiagramSource Select(string reportText, double visualizationOffsetM)
    {
        var alternativeShape = MooringAlternativeShapeStore.Current;
        var selectedShape = SelectedShapeStore.Current;
        var hasAlternativeShape = alternativeShape is not null && alternativeShape.Shape.Rows.Count >= 2;
        var shapeOffsetM = hasAlternativeShape
            ? alternativeShape!.Shape.DiscreteHorizontalOffsetM
            : selectedShape?.Shape.HorizontalOffsetM
                ?? TryReadReportMetric(reportText, "- Снос формы X/Z:")
                ?? TryReadReportMetric(reportText, "- Горизонтальный снос по узлам X/Z:")
                ?? visualizationOffsetM;

        return new PdfDiagramSource(alternativeShape, selectedShape, hasAlternativeShape, selectedShape is not null, shapeOffsetM);
    }

    private static double? TryReadReportMetric(string reportText, string label)
    {
        foreach (var line in (reportText ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var index = line.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            var valuePart = line[(index + label.Length)..].Trim();
            var token = new string(valuePart.TakeWhile(ch => char.IsDigit(ch) || ch == '-' || ch == '+' || ch == ',' || ch == '.').ToArray());
            token = token.Replace(',', '.');
            if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
        }

        return null;
    }
}
