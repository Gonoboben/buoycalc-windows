using System.Collections.Generic;
using BuoyCalc.Windows.ApplicationModel;
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
    SelectedShapeReadModel? SelectedShape,
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
        ApplicationCalculationRun run)
    {
        var result = run.Result;
        var snapshot = run.Snapshot;
        var reports = ReportBuildBoundary.Build(projectName, environment, buoy, anchor, snapshot);
        var elementRows = SelectedElementCalculationDisplayProjector.Project(snapshot);
        var sequenceVisualization = MainWindowSequenceVisualizationDisplayBuilder.Build(
            environment.DepthM,
            assemblyItems,
            sequenceItems,
            buoyName,
            anchorName,
            anchorType,
            result.EstimatedOffsetM);

        return new MainWindowCalculationDisplay(
            elementRows,
            snapshot.SelectedShape,
            reports.UserResultText,
            reports.TechnicalReportText,
            sequenceVisualization.SequenceSummary,
            sequenceVisualization.SequenceDiagramLines,
            sequenceVisualization.VisualizationDepthM,
            sequenceVisualization.VisualizationLineLengthM,
            sequenceVisualization.VisualizationOffsetM,
            sequenceVisualization.VisualizationDepthText,
            sequenceVisualization.VisualizationLineLengthText,
            sequenceVisualization.VisualizationOffsetText,
            sequenceVisualization.VisualizationSlackRatioText,
            sequenceVisualization.VisualizationStatusText);
    }
}
