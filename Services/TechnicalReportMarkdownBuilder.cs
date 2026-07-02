using System;
using System.Reflection;
using System.Text;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Markdown renderer boundary for the full technical report.
///
/// The top-level Markdown assembly now lives here. Existing section renderers are
/// reused from ReportBuilder to keep the generated output byte-for-byte stable.
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

        InvokeReportBuilderAppend("AppendEnvironment", sb, environment);
        InvokeReportBuilderAppend("AppendBuoy", sb, buoy, shape);
        InvokeReportBuilderAppend("AppendAnchor", sb, anchor, result);
        InvokeReportBuilderAppend("AppendTotals", sb, result, tensionRows, shape, shapeProjection, shapeForces, shapeTensions, sequencePositions, discreteLoadTensions, discreteLoadShape, alternativeDiscreteNodes, iterativeSolver, diagnostics);
        InvokeReportBuilderAppend("AppendDiagnostics", sb, diagnostics);
        InvokeReportBuilderAppend("AppendVectorBalanceRows", sb, vectorBalance);
        InvokeReportBuilderAppend("AppendElementRows", sb, result);
        InvokeReportBuilderAppend("AppendSequencePositionRows", sb, sequencePositions);
        InvokeReportBuilderAppend("AppendModelCoverageRows", sb, result);
        InvokeReportBuilderAppend("AppendSegmentRows", sb, result);
        InvokeReportBuilderAppend("AppendTensionRows", sb, tensionRows);
        InvokeReportBuilderAppend("AppendShapeRows", sb, shape);
        InvokeReportBuilderAppend("AppendShapeProjectionRows", sb, shapeProjection);
        InvokeReportBuilderAppend("AppendShapeForceRows", sb, shapeForces);
        InvokeReportBuilderAppend("AppendShapeTensionRows", sb, shapeTensions);
        InvokeReportBuilderAppend("AppendDiscreteLoadTensionRows", sb, discreteLoadTensions);
        InvokeReportBuilderAppend("AppendDiscreteLoadShapeRows", sb, discreteLoadShape);
        InvokeReportBuilderAppend("AppendAlternativeDiscreteNodeRows", sb, alternativeDiscreteNodes);
        InvokeReportBuilderAppend("AppendIterativeSolverRows", sb, iterativeSolver);
        InvokeReportBuilderAppend("AppendChecks", sb, result);

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

    private static void InvokeReportBuilderAppend(string methodName, params object[] args)
    {
        var method = typeof(ReportBuilder).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"ReportBuilder helper not found: {methodName}");
        method.Invoke(null, args);
    }
}
