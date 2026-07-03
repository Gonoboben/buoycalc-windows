using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BuoyCalc.Windows.Services;

internal sealed record Mooring2DCalculatedNode(
    int Number,
    double X,
    double Z,
    string Label);

internal sealed record Mooring2DDiagramSource(
    SelectedShapeReadModel? SelectedShape,
    MooringAlternativeShapeDisplayData? AlternativeShape,
    IReadOnlyList<Mooring2DCalculatedNode> ParsedNodes,
    bool HasSelectedShape);

internal static class Mooring2DDiagramSourceSelector
{
    public static Mooring2DDiagramSource Select(string? reportText)
    {
        var selectedShape = SelectedShapeStore.Current;
        var alternativeShape = MooringAlternativeShapeStore.Current;
        var hasSelectedShape = selectedShape is not null && selectedShape.Shape.Nodes.Count >= 2;
        IReadOnlyList<Mooring2DCalculatedNode> parsedNodes = hasSelectedShape
            ? Array.Empty<Mooring2DCalculatedNode>()
            : ParseCalculatedNodes(reportText);

        return new Mooring2DDiagramSource(selectedShape, alternativeShape, parsedNodes, hasSelectedShape);
    }

    private static List<Mooring2DCalculatedNode> ParseCalculatedNodes(string? reportText)
    {
        var nodes = new List<Mooring2DCalculatedNode>();
        if (string.IsNullOrWhiteSpace(reportText))
        {
            return nodes;
        }

        var inNodeSection = false;
        foreach (var rawLine in reportText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("## Расчётная форма постановки X/Z", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("## Расчётные узлы линии X/Z", StringComparison.OrdinalIgnoreCase))
            {
                inNodeSection = true;
                continue;
            }

            if (inNodeSection && line.StartsWith("## ", StringComparison.Ordinal))
            {
                break;
            }

            if (!inNodeSection || !line.StartsWith("|", StringComparison.Ordinal) || line.Contains("---"))
            {
                continue;
            }

            var parts = line.Split('|').Select(x => x.Trim()).ToArray();
            if (parts.Length < 7 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                continue;
            }

            if (TryParseNumber(parts[5], out var x) && TryParseNumber(parts[6], out var z))
            {
                nodes.Add(new Mooring2DCalculatedNode(number, x, z, parts[3]));
            }
        }

        return nodes.OrderBy(x => x.Number).ToList();
    }

    private static bool TryParseNumber(string value, out double number)
    {
        value = (value ?? string.Empty).Replace(',', '.');
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out number);
    }
}
