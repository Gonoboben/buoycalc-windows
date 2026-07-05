using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.ViewModels;

internal sealed record MainWindowVisualizationDisplay(
    double VisualizationDepthM,
    double VisualizationLineLengthM,
    double VisualizationOffsetM,
    string VisualizationDepthText,
    string VisualizationLineLengthText,
    string VisualizationOffsetText,
    string VisualizationSlackRatioText,
    string VisualizationStatusText);

internal sealed record MainWindowSequenceVisualizationDisplay(
    string SequenceSummary,
    IReadOnlyList<string> SequenceDiagramLines,
    double VisualizationDepthM,
    double VisualizationLineLengthM,
    double VisualizationOffsetM,
    string VisualizationDepthText,
    string VisualizationLineLengthText,
    string VisualizationOffsetText,
    string VisualizationSlackRatioText,
    string VisualizationStatusText);

internal static class MainWindowSequenceVisualizationDisplayBuilder
{
    internal static MainWindowSequenceVisualizationDisplay Build(
        double depthM,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        IReadOnlyList<MainWindowSequenceDisplayItem> sequenceItems,
        string buoyName,
        string anchorName,
        string anchorType,
        double? offsetM)
    {
        var sequenceSummary = BuildSummary(assemblyItems);
        var sequenceDiagramLines = BuildDiagram(sequenceItems, buoyName, anchorName, anchorType);
        var visualization = BuildVisualization(depthM, assemblyItems, offsetM);

        return new MainWindowSequenceVisualizationDisplay(
            sequenceSummary,
            sequenceDiagramLines,
            visualization.VisualizationDepthM,
            visualization.VisualizationLineLengthM,
            visualization.VisualizationOffsetM,
            visualization.VisualizationDepthText,
            visualization.VisualizationLineLengthText,
            visualization.VisualizationOffsetText,
            visualization.VisualizationSlackRatioText,
            visualization.VisualizationStatusText);
    }

    internal static string BuildSummary(IReadOnlyList<AssemblyItemInput> assemblyItems)
    {
        var enabledItems = assemblyItems.Where(x => x.IsEnabled).ToList();
        var lineLengthM = enabledItems.Where(x => x.Kind == AssemblyItemKind.Line).Sum(x => x.LengthM);
        var connectorCount = enabledItems.Count(x => x.Kind == AssemblyItemKind.Connector);
        var payloadWeightKg = enabledItems.Where(x => x.Kind == AssemblyItemKind.Payload).Sum(x => x.PayloadWeightAirKg);
        return $"Активных элементов: {enabledItems.Count} · линия: {lineLengthM:0.##} м · соединителей: {connectorCount} · приборы: {payloadWeightKg:0.##} кг";
    }

    internal static MainWindowVisualizationDisplay BuildVisualization(
        double depthM,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        double? offsetM)
    {
        var lineLengthM = assemblyItems
            .Where(x => x.IsEnabled && x.Kind == AssemblyItemKind.Line)
            .Sum(x => x.LengthM);
        var resolvedOffsetM = offsetM ?? 0;
        var slackRatio = depthM > 0 ? lineLengthM / depthM : 0;
        var visualizationStatusText = depthM <= 0
            ? "WARNING: глубина не задана"
            : lineLengthM >= depthM
                ? "OK: длина линии не меньше глубины"
                : "WARNING: линия короче глубины";

        return new MainWindowVisualizationDisplay(
            depthM,
            lineLengthM,
            resolvedOffsetM,
            $"Глубина: {depthM:0.##} м",
            $"Длина линии: {lineLengthM:0.##} м",
            offsetM.HasValue ? $"Оценочный снос: {resolvedOffsetM:0.##} м" : "Оценочный снос: после расчёта",
            depthM > 0 ? $"L/Depth: {slackRatio:0.###}" : "L/Depth: не определено",
            visualizationStatusText);
    }

    internal static IReadOnlyList<string> BuildDiagram(
        IReadOnlyList<MainWindowSequenceDisplayItem> sequenceItems,
        string buoyName,
        string anchorName,
        string anchorType)
    {
        var lines = new List<string>
        {
            $"● Буй: {SafeText(buoyName, "Буй")}"
        };

        foreach (var item in sequenceItems.Where(x => x.IsEnabled))
        {
            lines.Add("↓");
            lines.Add($"○ {item.KindDisplayName}: {SafeText(item.Title, "Элемент")} · {item.Summary}");
        }

        lines.Add("↓");
        lines.Add($"■ Якорь: {SafeText(anchorName, "Якорь")} · {SafeText(anchorType, "тип не задан")}");
        return lines;
    }

    private static string SafeText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
