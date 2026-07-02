using System.Text;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Markdown renderer boundary for the full technical report.
///
/// The top-level Markdown assembly now lives here. Existing section renderers are
/// reused from ReportBuilder through TechnicalReportMarkdownSectionBridge to keep
/// the generated output byte-for-byte stable.
/// </summary>
public static class TechnicalReportMarkdownBuilder
{
    public static string Build(string projectName, EnvironmentInput environment, BuoyInput buoy, AnchorInput anchor, CalculationResult result)
    {
        var sb = new StringBuilder();
        var data = TechnicalReportDataBuilder.Build(environment, result);
        var tensionRows = data.TensionRows;
        var shape = data.Shape;
        var shapeProjection = data.ShapeProjection;
        var shapeForces = data.ShapeForces;
        var shapeTensions = data.ShapeTensions;
        var sequencePositions = data.SequencePositions;
        var discreteLoadTensions = data.DiscreteLoadTensions;
        var discreteLoadShape = data.DiscreteLoadShape;
        var alternativeDiscreteNodes = data.AlternativeDiscreteNodes;
        var iterativeSolver = data.IterativeSolver;
        var diagnostics = data.Diagnostics;
        var vectorBalance = data.VectorBalance;
        TechnicalReportStorePublisher.Publish(data);

        sb.AppendLine("# BuoyCalc Windows — предварительный отчёт");
        sb.AppendLine();
        sb.AppendLine($"Проект: {projectName}");
        sb.AppendLine($"Вердикт: {result.Verdict}");
        sb.AppendLine($"Главный риск: {result.MainRisk}");
        sb.AppendLine($"Инженерная диагностика: {diagnostics.Summary}");
        sb.AppendLine();

        TechnicalReportMarkdownSectionBridge.Append("AppendEnvironment", sb, environment);
        TechnicalReportMarkdownSectionBridge.Append("AppendBuoy", sb, buoy, shape);
        TechnicalReportMarkdownSectionBridge.Append("AppendAnchor", sb, anchor, result);
        TechnicalReportMarkdownSectionBridge.Append("AppendTotals", sb, result, tensionRows, shape, shapeProjection, shapeForces, shapeTensions, sequencePositions, discreteLoadTensions, discreteLoadShape, alternativeDiscreteNodes, iterativeSolver, diagnostics);
        TechnicalReportMarkdownSectionBridge.Append("AppendDiagnostics", sb, diagnostics);
        TechnicalReportMarkdownSectionBridge.Append("AppendVectorBalanceRows", sb, vectorBalance);
        TechnicalReportMarkdownSectionBridge.Append("AppendElementRows", sb, result);
        TechnicalReportMarkdownSectionBridge.Append("AppendSequencePositionRows", sb, sequencePositions);
        TechnicalReportMarkdownSectionBridge.Append("AppendModelCoverageRows", sb, result);
        TechnicalReportMarkdownSectionBridge.Append("AppendSegmentRows", sb, result);
        TechnicalReportMarkdownSectionBridge.Append("AppendTensionRows", sb, tensionRows);
        TechnicalReportMarkdownSectionBridge.Append("AppendShapeRows", sb, shape);
        TechnicalReportMarkdownSectionBridge.Append("AppendShapeProjectionRows", sb, shapeProjection);
        TechnicalReportMarkdownSectionBridge.Append("AppendShapeForceRows", sb, shapeForces);
        TechnicalReportMarkdownSectionBridge.Append("AppendShapeTensionRows", sb, shapeTensions);
        TechnicalReportMarkdownSectionBridge.Append("AppendDiscreteLoadTensionRows", sb, discreteLoadTensions);
        TechnicalReportMarkdownSectionBridge.Append("AppendDiscreteLoadShapeRows", sb, discreteLoadShape);
        TechnicalReportMarkdownSectionBridge.Append("AppendAlternativeDiscreteNodeRows", sb, alternativeDiscreteNodes);
        TechnicalReportMarkdownSectionBridge.Append("AppendIterativeSolverRows", sb, iterativeSolver);
        TechnicalReportMarkdownSectionBridge.Append("AppendChecks", sb, result);

        sb.AppendLine("## Ограничения");
        sb.AppendLine(shape.MethodNote);
        sb.AppendLine(shapeProjection.MethodNote);
        sb.AppendLine(shapeForces.MethodNote);
        sb.AppendLine(shapeTensions.MethodNote);
        sb.AppendLine(sequencePositions.MethodNote);
        sb.AppendLine(discreteLoadTensions.MethodNote);
        sb.AppendLine(discreteLoadShape.MethodNote);
        sb.AppendLine(alternativeDiscreteNodes.MethodNote);
        sb.AppendLine(iterativeSolver.MethodNote);
        sb.AppendLine(vectorBalance.MethodNote);
        sb.AppendLine("v0.39 добавляет диагностический итерационный solver-слой. Он замыкает существующие блоки в цикл, но основной solver, 2D и PDF-схемы пока не заменяются.");

        return sb.ToString();
    }
}
