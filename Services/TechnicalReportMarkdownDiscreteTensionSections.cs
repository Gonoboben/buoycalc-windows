using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

internal static class TechnicalReportMarkdownDiscreteTensionSections
{
    public static bool TryAppend(string methodName, object[] args)
    {
        switch (methodName)
        {
            case "AppendDiscreteLoadTensionRows":
                AppendDiscreteLoadTensionRows((StringBuilder)args[0], (MooringDiscreteLoadTensionResult)args[1]);
                return true;
            default:
                return false;
        }
    }

    private static void AppendDiscreteLoadTensionRows(StringBuilder sb, MooringDiscreteLoadTensionResult tensions)
    {
        if (tensions.Rows.Count == 0) return;
        sb.AppendLine("## Натяжения линии с дискретными нагрузками по s");
        sb.AppendLine("Эта таблица добавляет к распределённой линии локальные нагрузки приборов и соединителей, у которых в v0.33 появилась координата s. Расчёт идёт снизу вверх: в каждом сечении учитываются дискретные элементы ниже или на этом s.");
        sb.AppendLine($"Дискретные нагрузки: вес в воде={tensions.TotalDiscreteWeightWaterKg:0.####} кг; Fx={tensions.TotalDiscreteForceN:0.####} Н.");
        sb.AppendLine($"Top T старая={tensions.TopOriginalTensionKn:0.####} кН; Top T с дискретными нагрузками={tensions.TopDiscreteTensionKn:0.####} кН; относительное отличие={tensions.RelativeTopTensionDifference:0.####}.");
        sb.AppendLine($"Max T старая={tensions.MaxOriginalTensionKn:0.####} кН; Max T с дискретными нагрузками={tensions.MaxDiscreteTensionKn:0.####} кН; Max ΔT={tensions.MaxTensionDifferenceKn:0.####} кН; Max Δугла={tensions.MaxAngleDifferenceDeg:0.####}°.");
        sb.AppendLine($"Статус={(tensions.WithinTolerance ? "OK" : "INFO: дискретные нагрузки заметно меняют натяжение")}");
        sb.AppendLine();
        sb.AppendLine("### Дискретные нагрузки по s");
        sb.AppendLine("| № | Тип | Элемент | s, м | Вес в воде, кг | Fx, Н |");
        sb.AppendLine("|---:|---|---|---:|---:|---:|");
        foreach (var load in tensions.DiscreteLoads)
        {
            sb.AppendLine($"| {load.Number} | {Escape(load.Kind)} | {Escape(load.Title)} | {load.PositionAlongLineM:0.####} | {load.WeightWaterKg:0.####} | {load.CurrentForceN:0.####} |");
        }
        sb.AppendLine();
        sb.AppendLine("### Накопленные натяжения с дискретными нагрузками");
        sb.AppendLine("| № | Сегмент | Элемент | s0, м | s1, м | z, м | Fсег, Н | Вес дискр. ниже, кг | Fx дискр. ниже, Н | T старая, кН | T дискр., кН | ΔT, кН | Угол старый, ° | Угол дискр., ° | Δугла, ° | Статус |");
        sb.AppendLine("|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in SampleRows(tensions.Rows, 45, 45))
        {
            sb.AppendLine($"| {row.Number} | {row.SegmentNumber} | {Escape(row.SourceElement)} | {row.StartAlongLineM:0.####} | {row.EndAlongLineM:0.####} | {row.EstimatedDepthM:0.####} | {row.SegmentForceN:0.####} | {row.DiscreteWeightBelowKg:0.####} | {row.DiscreteForceBelowN:0.####} | {row.OriginalTensionKn:0.####} | {row.DiscreteTensionKn:0.####} | {row.TensionDifferenceKn:0.####} | {row.OriginalAngleFromVerticalDeg:0.####} | {row.DiscreteAngleFromVerticalDeg:0.####} | {row.AngleDifferenceDeg:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        sb.AppendLine(tensions.MethodNote);
        sb.AppendLine();
    }

    private static IEnumerable<T> SampleRows<T>(IReadOnlyList<T> rows, int firstCount, int lastCount)
    {
        if (rows.Count <= firstCount + lastCount)
        {
            return rows;
        }

        return rows.Take(firstCount).Concat(rows.Skip(rows.Count - lastCount));
    }

    private static string Escape(string value)
    {
        return (value ?? string.Empty).Replace("|", "/");
    }
}
