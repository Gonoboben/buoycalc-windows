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
    ApplicationRunReportReadModel ApplicationRunReport,
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
        var userEngineeringReport = UserEngineeringReportReadModelProjector.Project(
            projectName,
            environment,
            buoy,
            anchor,
            snapshot);
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
            ApplicationRunReportReadModelProjector.ProjectCalculated(userEngineeringReport),
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

    internal static MainWindowCalculationDisplay Build(
        string projectName,
        EnvironmentInput environment,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        IReadOnlyList<MainWindowSequenceDisplayItem> sequenceItems,
        string buoyName,
        string anchorName,
        string anchorType,
        PreflightPhysicalRejectedApplicationRunOutcome outcome)
    {
        var report = ApplicationRunReportReadModelProjector.ProjectPreflightPhysicalRejected(
            projectName,
            outcome);
        var sequenceVisualization = MainWindowSequenceVisualizationDisplayBuilder.Build(
            environment.DepthM,
            assemblyItems,
            sequenceItems,
            buoyName,
            anchorName,
            anchorType,
            offsetM: null);

        return new MainWindowCalculationDisplay(
            Array.Empty<ElementCalculationDisplayRow>(),
            null,
            report,
            PreflightPhysicalRejectionReportBuilder.BuildUserResult(report),
            PreflightPhysicalRejectionReportBuilder.BuildTechnicalReport(report),
            sequenceVisualization.SequenceSummary,
            sequenceVisualization.SequenceDiagramLines,
            sequenceVisualization.VisualizationDepthM,
            sequenceVisualization.VisualizationLineLengthM,
            0,
            sequenceVisualization.VisualizationDepthText,
            sequenceVisualization.VisualizationLineLengthText,
            "Расчётный снос: не вычислялся (preflight rejection)",
            sequenceVisualization.VisualizationSlackRatioText,
            "Не подходит: активная линия короче глубины; расчётная геометрия не вычислялась.");
    }
}
