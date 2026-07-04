using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.ViewModels;

internal sealed record MainWindowSequenceDisplayItem(
    bool IsEnabled,
    string KindDisplayName,
    string Title,
    string Summary);

internal sealed record MainWindowCalculationDisplay(
    IReadOnlyList<ElementCalculationDisplayRow> ElementRows,
    string UserResultText,
    string TechnicalReportText,
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

internal static class MainWindowCalculationDisplayBuilder
{
    internal static MainWindowCalculationDisplay Build(
        string projectName,
        EnvironmentInput environment,
        BuoyInput buoy,
        AnchorInput anchor,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        IReadOnlyList<MainWindowSequenceDisplayItem> sequenceItems,
        string buoyName,
        string anchorName,
        string anchorType,
        CalculationResult result)
    {
        var reports = ReportBuildBoundary.Build(projectName, environment, buoy, anchor, result);
        var elementRows = result.ElementRows.Select(ElementCalculationDisplayRow.From).ToList();
        var enabledItems = assemblyItems.Where(x => x.IsEnabled).ToList();
        var lineLengthM = enabledItems.Where(x => x.Kind == AssemblyItemKind.Line).Sum(x => x.LengthM);
        var connectorCount = enabledItems.Count(x => x.Kind == AssemblyItemKind.Connector);
        var payloadWeightKg = enabledItems.Where(x => x.Kind == AssemblyItemKind.Payload).Sum(x => x.PayloadWeightAirKg);
        var sequenceSummary = $"Активных элементов: {enabledItems.Count} · линия: {lineLengthM:0.##} м · соединителей: {connectorCount} · приборы: {payloadWeightKg:0.##} кг";

        var sequenceDiagramLines = new List<string>
        {
            $"● Буй: {SafeText(buoyName, "Буй")}"
        };

        foreach (var item in sequenceItems.Where(x => x.IsEnabled))
        {
            sequenceDiagramLines.Add("↓");
            sequenceDiagramLines.Add($"○ {item.KindDisplayName}: {SafeText(item.Title, "Элемент")} · {item.Summary}");
        }

        sequenceDiagramLines.Add("↓");
        sequenceDiagramLines.Add($"■ Якорь: {SafeText(anchorName, "Якорь")} · {SafeText(anchorType, "тип не задан")}");

        var depthM = environment.DepthM;
        var slackRatio = depthM > 0 ? lineLengthM / depthM : 0;
        var offsetM = result.EstimatedOffsetM;
        var visualizationStatusText = depthM <= 0
            ? "WARNING: глубина не задана"
            : lineLengthM >= depthM
                ? "OK: длина линии не меньше глубины"
                : "WARNING: линия короче глубины";

        return new MainWindowCalculationDisplay(
            elementRows,
            reports.UserResultText,
            reports.TechnicalReportText,
            sequenceSummary,
            sequenceDiagramLines,
            depthM,
            lineLengthM,
            offsetM,
            $"Глубина: {depthM:0.##} м",
            $"Длина линии: {lineLengthM:0.##} м",
            $"Оценочный снос: {offsetM:0.##} м",
            depthM > 0 ? $"L/Depth: {slackRatio:0.###}" : "L/Depth: не определено",
            visualizationStatusText);
    }

    private static string SafeText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
