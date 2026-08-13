using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuoyCalc.Windows.Services;

internal static class TechnicalReportMarkdownUniformCurrentSections
{
    public static bool TryAppend(string methodName, object[] args)
    {
        if (methodName != "AppendUniformCurrentNormalVectorRows")
        {
            return false;
        }

        AppendUniformCurrentNormalVectorRows(
            (StringBuilder)args[0],
            (MooringUniformCurrentNormalVectorResult)args[1]);
        return true;
    }

    private static void AppendUniformCurrentNormalVectorRows(
        StringBuilder sb,
        MooringUniformCurrentNormalVectorResult diagnostic)
    {
        sb.AppendLine("## Вектор нормального сопротивления линии — uniform current");
        sb.AppendLine("INFO-only read model для подтверждённого случая Берто γ=0. Renderer только отображает уже рассчитанные X/Z-компоненты и не пересчитывает гидродинамику.");
        sb.AppendLine("Этот раздел не участвует в solver feedback, MooringPrimaryShapeGate, selected X/Z, verdict, anchor reserve или weak-link checks. Касательная составляющая сопротивления здесь не моделируется.");
        sb.AppendLine();

        if (!diagnostic.Available)
        {
            sb.AppendLine("- Статус: недоступно для текущего режима расчёта.");
            sb.AppendLine($"- Причина: {Escape(diagnostic.MethodNote)}");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("- Статус: доступно для scalar/uniform-current mode.");
        sb.AppendLine($"- ΣFx нормального сопротивления: {diagnostic.SumNormalForceXN:0.####} Н");
        sb.AppendLine($"- ΣFz нормального сопротивления: {diagnostic.SumNormalForceZN:0.####} Н");
        sb.AppendLine($"- Σ|F| по сегментам: {diagnostic.SumNormalForceMagnitudeN:0.####} Н");
        sb.AppendLine($"- Max |Δ| между |Fvector| и существующим ShapeForceN: {diagnostic.MaxMagnitudeDifferenceN:0.########} Н");
        sb.AppendLine();

        if (diagnostic.Rows.Count == 0)
        {
            sb.AppendLine("Строки normal-vector diagnostic отсутствуют.");
            sb.AppendLine();
            sb.AppendLine(diagnostic.MethodNote);
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"Показаны первые 30 и последние 30 строк из {diagnostic.Rows.Count}; вычисления в renderer не выполняются.");
        sb.AppendLine();
        sb.AppendLine("| № | Сегмент | Элемент | Ux, м/с | Uz, м/с | tx | tz | Unx, м/с | Unz, м/с | |Un|, м/с | Fx, Н | Fz, Н | |F|, Н | ShapeForceN, Н | Δ, Н | Статус |");
        sb.AppendLine("|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in SampleRows(diagnostic.Rows, 30, 30))
        {
            sb.AppendLine(
                $"| {row.Number} | {row.SegmentNumber} | {Escape(row.SourceElement)} | {row.CurrentXMS:0.####} | {row.CurrentZMS:0.####} | {row.TangentX:0.####} | {row.TangentZ:0.####} | {row.NormalVelocityXMS:0.####} | {row.NormalVelocityZMS:0.####} | {row.NormalSpeedMS:0.####} | {row.NormalForceXN:0.####} | {row.NormalForceZN:0.####} | {row.NormalForceMagnitudeN:0.####} | {row.ExistingShapeForceN:0.####} | {row.MagnitudeDifferenceN:0.########} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        sb.AppendLine(diagnostic.MethodNote);
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

    private static string Escape(string value)
    {
        return (value ?? string.Empty).Replace("|", "/");
    }
}
