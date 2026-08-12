using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuoyCalc.Windows.Services;

internal static class TechnicalReportMarkdownForceShapeSections
{
    public static bool TryAppend(string methodName, object[] args)
    {
        if (methodName != "AppendForceShapeConsistencyRows")
        {
            return false;
        }

        AppendForceShapeConsistencyRows(
            (StringBuilder)args[0],
            (MooringForceShapeConsistencyResult)args[1]);
        return true;
    }

    private static void AppendForceShapeConsistencyRows(
        StringBuilder sb,
        MooringForceShapeConsistencyResult consistency)
    {
        sb.AppendLine("## Согласованность направления силы и X/Z-касательной");
        sb.AppendLine("Candidate A — информационный force-direction / X-Z-tangent consistency proxy. Он сравнивает уже рассчитанное направление cumulative shape-force state с уже построенной X/Z-касательной. Это magnitude-only диагностика текущей модели, а не signed segment/node equilibrium solution.");
        sb.AppendLine("Инженерный pass/fail threshold для произвольной постановки не утверждён. Этот раздел не меняет solver convergence, MooringPrimaryShapeGate, selected X/Z, CalculationResult verdict, anchor reserve или weak-link checks.");
        sb.AppendLine();
        sb.AppendLine($"- Доступных строк: {consistency.AvailableRowCount}");
        sb.AppendLine($"- Неопределённых строк: {consistency.IndeterminateRowCount}");
        sb.AppendLine($"- Max R: {Format(consistency.MaxResidualN)} Н");
        sb.AppendLine($"- Max R_rel: {Format(consistency.MaxRelativeResidual)}");
        sb.AppendLine($"- Max Δугла: {Format(consistency.MaxAngleDifferenceDeg)}°");
        sb.AppendLine($"- Худший сегмент по R_rel: {Format(consistency.WorstSegmentNumber)}");
        sb.AppendLine($"- Источник худшего сегмента: {Escape(consistency.WorstSourceElement ?? "—")}");
        sb.AppendLine();

        if (consistency.Rows.Count == 0)
        {
            sb.AppendLine("Строки consistency proxy отсутствуют.");
            sb.AppendLine();
            sb.AppendLine(consistency.MethodNote);
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"Показаны первые 40 и последние 40 строк из {consistency.Rows.Count}; вычисления в renderer не выполняются.");
        sb.AppendLine();
        sb.AppendLine("| № | Сегмент | Элемент | dX, м | dZ, м | φгеом, ° | θсилы, ° | H, Н | V, Н | T, Н | R_H, Н | R_V, Н | R, Н | R_rel | Δθ, ° | Статус |");
        sb.AppendLine("|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in SampleRows(consistency.Rows, 40, 40))
        {
            sb.AppendLine(
                $"| {row.Number} | {row.SegmentNumber} | {Escape(row.SourceElement)} | {row.DeltaXM:0.####} | {row.DeltaZM:0.####} | {Format(row.GeometricAngleFromVerticalDeg)} | {Format(row.ForceAngleFromVerticalDeg)} | {Format(row.ForceHorizontalN)} | {Format(row.ForceVerticalN)} | {Format(row.TensionN)} | {Format(row.ResidualHorizontalN)} | {Format(row.ResidualVerticalN)} | {Format(row.ResidualN)} | {Format(row.RelativeResidual)} | {Format(row.AngleDifferenceDeg)} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        sb.AppendLine(consistency.MethodNote);
        sb.AppendLine();
    }

    private static IReadOnlyList<T> SampleRows<T>(IReadOnlyList<T> rows, int firstCount, int lastCount)
    {
        if (rows.Count <= firstCount + lastCount)
        {
            return rows;
        }

        return rows.Take(firstCount).Concat(rows.Skip(rows.Count - lastCount)).ToList();
    }

    private static string Format(double? value)
    {
        return value.HasValue && double.IsFinite(value.Value)
            ? value.Value.ToString("0.####")
            : "—";
    }

    private static string Format(int? value)
    {
        return value?.ToString() ?? "—";
    }

    private static string Escape(string value)
    {
        return (value ?? string.Empty).Replace("|", "/");
    }
}
