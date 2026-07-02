using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

internal static class TechnicalReportMarkdownDiscreteShapeSections
{
    public static bool TryAppend(string methodName, object[] args)
    {
        switch (methodName)
        {
            case "AppendDiscreteLoadShapeRows":
                AppendDiscreteLoadShapeRows((StringBuilder)args[0], (MooringDiscreteLoadShapeResult)args[1]);
                return true;
            default:
                return false;
        }
    }

    private static void AppendDiscreteLoadShapeRows(StringBuilder sb, MooringDiscreteLoadShapeResult shape)
    {
        if (shape.Rows.Count == 0) return;
        sb.AppendLine("## Альтернативная форма X/Z с дискретными нагрузками");
        sb.AppendLine("Эта таблица строит сравнительную форму по углам, полученным из натяжений с дискретными нагрузками. Основной solver пока не заменяется.");
        sb.AppendLine($"Снос основной формы={shape.OriginalHorizontalOffsetM:0.####} м; снос альтернативной формы={shape.DiscreteHorizontalOffsetM:0.####} м; Δсноса={shape.OffsetDifferenceM:0.####} м.");
        sb.AppendLine($"Глубина якоря={shape.AnchorDepthM:0.####} м; невязка={shape.VerticalResidualM:0.####} м; max Δузла={shape.MaxNodeDeltaM:0.####} м; scale={shape.AngleScale:0.####}; итераций={shape.IterationCount}; статус={(shape.Converged ? "OK" : "WARNING")}.");
        sb.AppendLine();
        sb.AppendLine("| Узел | Сегмент | Элемент | s, м | X дискр., м | Z дискр., м | Lсег, м | Угол старый, ° | Угол дискр., ° | Угол формы, ° | T дискр., кН | X осн., м | Z осн., м | ΔX, м | ΔZ, м | Статус |");
        sb.AppendLine("|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in SampleRows(shape.Rows, 45, 45))
        {
            sb.AppendLine($"| {row.Number} | {row.SegmentNumber} | {Escape(row.SourceElement)} | {row.AlongLineM:0.####} | {row.XOffsetM:0.####} | {row.ZDepthM:0.####} | {row.SegmentLengthM:0.####} | {row.OriginalAngleFromVerticalDeg:0.####} | {row.DiscreteAngleFromVerticalDeg:0.####} | {row.UsedAngleFromVerticalDeg:0.####} | {row.DiscreteTensionKn:0.####} | {row.OriginalXOffsetM:0.####} | {row.OriginalZDepthM:0.####} | {row.DeltaXM:0.####} | {row.DeltaZM:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        sb.AppendLine(shape.MethodNote);
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
