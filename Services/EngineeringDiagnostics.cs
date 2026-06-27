using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public enum EngineeringCheckSeverity
{
    Info,
    Ok,
    Warning,
    Error
}

public sealed record EngineeringDiagnosticRow(
    string CheckName,
    string Value,
    string Tolerance,
    EngineeringCheckSeverity Severity,
    string Note);

public sealed record EngineeringDiagnosticsResult(
    IReadOnlyList<EngineeringDiagnosticRow> Rows,
    EngineeringCheckSeverity OverallSeverity,
    string Summary);

public static class EngineeringDiagnostics
{
    public static EngineeringDiagnosticsResult Build(
        EnvironmentInput environment,
        CalculationResult result,
        MooringShapeResult shape,
        IReadOnlyList<SegmentTensionRow> tensionRows)
    {
        var rows = new List<EngineeringDiagnosticRow>();
        var depthM = Math.Max(0, environment.DepthM);
        var lineLengthM = Math.Max(0, result.LineLengthM);
        var buoyDepthM = shape.BuoyPoint?.ZDepthM ?? double.NaN;
        var anchorDepthM = shape.AnchorPoint?.ZDepthM ?? double.NaN;
        var lengthResidualM = shape.AnchorPoint is null ? double.NaN : Math.Abs(shape.AnchorPoint.AlongLineM - lineLengthM);
        var anchorDepthResidualM = double.IsNaN(anchorDepthM) ? double.NaN : Math.Abs(anchorDepthM - depthM);
        var maxTensionKn = tensionRows.Count > 0 ? tensionRows.Max(x => x.TensionKn) : result.TensionKn;

        rows.Add(Check(
            "Якорь на проектной глубине",
            double.IsNaN(anchorDepthResidualM) ? "нет данных" : $"невязка {anchorDepthResidualM:0.####} м",
            "≤ 0,01 м",
            !double.IsNaN(anchorDepthResidualM) && anchorDepthResidualM <= 0.01,
            double.IsNaN(anchorDepthResidualM) ? EngineeringCheckSeverity.Warning : EngineeringCheckSeverity.Error,
            double.IsNaN(anchorDepthResidualM) ? "Якорный узел отсутствует." : $"Z якоря = {anchorDepthM:0.####} м, Depth = {depthM:0.####} м."));

        rows.Add(Check(
            "Длина расчётной линии",
            double.IsNaN(lengthResidualM) ? "нет данных" : $"невязка {lengthResidualM:0.####} м",
            "≤ 0,01 м",
            !double.IsNaN(lengthResidualM) && lengthResidualM <= 0.01,
            double.IsNaN(lengthResidualM) ? EngineeringCheckSeverity.Warning : EngineeringCheckSeverity.Error,
            double.IsNaN(lengthResidualM) ? "Последний узел формы отсутствует." : $"s последнего узла = {shape.AnchorPoint!.AlongLineM:0.####} м, L линии = {lineLengthM:0.####} м."));

        rows.Add(new EngineeringDiagnosticRow(
            "Состояние буя",
            DisplayBuoyState(shape.BuoyState),
            "Surface / Submerged / Overloaded",
            shape.BuoyState == BuoyShapeState.Overloaded ? EngineeringCheckSeverity.Error : EngineeringCheckSeverity.Ok,
            double.IsNaN(buoyDepthM) ? "Глубина буя не определена." : $"Z буя = {buoyDepthM:0.####} м."));

        rows.Add(new EngineeringDiagnosticRow(
            "Геометрия: линия и глубина",
            $"L/Depth = {(depthM > 0 ? lineLengthM / depthM : 0):0.####}",
            "информационно",
            EngineeringCheckSeverity.Info,
            depthM <= 0
                ? "Глубина не задана."
                : lineLengthM >= depthM
                    ? "Длина линии не меньше глубины; поверхностное положение буя геометрически возможно."
                    : "Длина линии меньше глубины; верхний узел должен быть под водой."));

        rows.Add(Check(
            "Положение буя выше якоря",
            double.IsNaN(buoyDepthM) || double.IsNaN(anchorDepthM) ? "нет данных" : $"Zбуй={buoyDepthM:0.####}, Zякорь={anchorDepthM:0.####}",
            "Zбуй < Zякорь",
            !double.IsNaN(buoyDepthM) && !double.IsNaN(anchorDepthM) && buoyDepthM < anchorDepthM,
            EngineeringCheckSeverity.Error,
            "Буй должен находиться выше нижнего граничного узла якоря."));

        rows.Add(new EngineeringDiagnosticRow(
            "Максимальное натяжение",
            $"{maxTensionKn:0.####} кН",
            "информационно",
            EngineeringCheckSeverity.Info,
            tensionRows.Count > 0 ? "По сегментной оценке натяжений." : "По общей оценке результата."));

        rows.Add(new EngineeringDiagnosticRow(
            "Запас слабого звена",
            $"{result.TensionReserve:0.####}",
            "> 1",
            result.TensionReserve >= 1 ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Error,
            $"WLL слабого звена = {result.WorkingLoadKn:0.####} кН."));

        rows.Add(new EngineeringDiagnosticRow(
            "Запас якоря",
            $"{result.AnchorReserve:0.####}",
            "> 1",
            result.AnchorReserve >= 1 ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Error,
            $"Удержание = {result.AnchorHoldingKg:0.####} кг, требуется = {result.RequiredAnchorHoldingKg:0.####} кг."));

        rows.Add(new EngineeringDiagnosticRow(
            "Статус сходимости формы",
            shape.Converged ? "Converged" : "Preliminary",
            "Converged для финального solver",
            shape.Converged ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Warning,
            shape.MethodNote));

        var overall = rows.Any(x => x.Severity == EngineeringCheckSeverity.Error)
            ? EngineeringCheckSeverity.Error
            : rows.Any(x => x.Severity == EngineeringCheckSeverity.Warning)
                ? EngineeringCheckSeverity.Warning
                : EngineeringCheckSeverity.Ok;

        return new EngineeringDiagnosticsResult(rows, overall, DisplayOverall(overall));
    }

    private static EngineeringDiagnosticRow Check(
        string checkName,
        string value,
        string tolerance,
        bool ok,
        EngineeringCheckSeverity failureSeverity,
        string note)
    {
        return new EngineeringDiagnosticRow(
            checkName,
            value,
            tolerance,
            ok ? EngineeringCheckSeverity.Ok : failureSeverity,
            note);
    }

    private static string DisplayBuoyState(BuoyShapeState state)
    {
        return state switch
        {
            BuoyShapeState.Surface => "на поверхности",
            BuoyShapeState.Submerged => "под водой",
            BuoyShapeState.Overloaded => "перегружен / отрицательная плавучесть",
            _ => "не определено"
        };
    }

    public static string DisplaySeverity(EngineeringCheckSeverity severity)
    {
        return severity switch
        {
            EngineeringCheckSeverity.Ok => "OK",
            EngineeringCheckSeverity.Warning => "WARNING",
            EngineeringCheckSeverity.Error => "ERROR",
            _ => "INFO"
        };
    }

    private static string DisplayOverall(EngineeringCheckSeverity severity)
    {
        return severity switch
        {
            EngineeringCheckSeverity.Ok => "диагностика без ошибок",
            EngineeringCheckSeverity.Warning => "есть предупреждения предварительной модели",
            EngineeringCheckSeverity.Error => "есть инженерные ошибки/несогласованности",
            _ => "информационная диагностика"
        };
    }
}
