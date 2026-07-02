using System.Text;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

internal static class TechnicalReportMarkdownDiscreteNodeSections
{
    public static bool TryAppend(string methodName, object[] args)
    {
        switch (methodName)
        {
            case "AppendAlternativeDiscreteNodeRows":
                AppendAlternativeDiscreteNodeRows((StringBuilder)args[0], (MooringAlternativeDiscreteNodeResult)args[1]);
                return true;
            default:
                return false;
        }
    }

    private static void AppendAlternativeDiscreteNodeRows(StringBuilder sb, MooringAlternativeDiscreteNodeResult nodes)
    {
        if (nodes.Rows.Count == 0) return;
        sb.AppendLine("## Дискретные X/Z-узлы альтернативной формы");
        sb.AppendLine("Этот раздел переводит позицию s каждого точечного элемента в координаты X/Z на альтернативной форме. Буй и якорь показаны как граничные узлы; соединители и приборы — как внутренние дискретные точки.");
        sb.AppendLine($"Внутренних дискретных узлов={nodes.DiscreteNodeCount}; вес в воде={nodes.TotalDiscreteWeightWaterKg:0.####} кг; Fx={nodes.TotalDiscreteForceN:0.####} Н; max Δузла={nodes.MaxNodeDeltaM:0.####} м.");
        sb.AppendLine();
        sb.AppendLine("| № | № элем. | Тип | Элемент | Пресет | s, м | X альт., м | Z альт., м | X осн., м | Z осн., м | ΔX, м | ΔZ, м | Вес в воде, кг | Fx, Н | Роль | Статус |");
        sb.AppendLine("|---:|---:|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|");
        foreach (var row in nodes.Rows)
        {
            sb.AppendLine($"| {row.Number} | {row.ElementNumber} | {Escape(row.Kind)} | {Escape(row.Title)} | {Escape(row.PresetName)} | {row.PositionAlongLineM:0.####} | {row.AlternativeXOffsetM:0.####} | {row.AlternativeZDepthM:0.####} | {row.OriginalXOffsetM:0.####} | {row.OriginalZDepthM:0.####} | {row.DeltaXM:0.####} | {row.DeltaZM:0.####} | {row.WeightWaterKg:0.####} | {row.CurrentForceN:0.####} | {Escape(row.NodeRole)} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        sb.AppendLine(nodes.MethodNote);
        sb.AppendLine();
    }

    private static string Escape(string value)
    {
        return (value ?? string.Empty).Replace("|", "/");
    }
}
