using System.Text;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Section renderers already moved out of legacy ReportBuilder.
///
/// This class is intentionally narrow while sections are migrated in small,
/// output-stable PRs.
/// </summary>
internal static class TechnicalReportMarkdownMovedSections
{
    public static bool TryAppend(string methodName, object[] args)
    {
        switch (methodName)
        {
            case "AppendVectorBalanceRows":
                AppendVectorBalanceRows((StringBuilder)args[0], (MooringVectorBalanceResult)args[1]);
                return true;
            case "AppendElementRows":
                AppendElementRows((StringBuilder)args[0], (CalculationResult)args[1]);
                return true;
            default:
                return false;
        }
    }

    private static void AppendVectorBalanceRows(StringBuilder sb, MooringVectorBalanceResult balance)
    {
        sb.AppendLine("## Векторная ведомость сил постановки");
        sb.AppendLine("Положительное направление X принято по направлению горизонтальной нагрузки. Положительное направление Z принято вверх.");
        sb.AppendLine("Эта таблица показывает силовые члены, уже переданные расчётным ядром. Она не является результатом итерационного solver равновесной формы.");
        sb.AppendLine();
        sb.AppendLine("| № | Тип | Элемент | Fx, Н | Fz, Н | Примечание |");
        sb.AppendLine("|---:|---|---|---:|---:|---|");
        var number = 1;
        foreach (var term in balance.Terms)
        {
            sb.AppendLine($"| {number++} | {Escape(term.Kind)} | {Escape(term.Name)} | {term.FxN:0.####} | {term.FzN:0.####} | {Escape(term.Note)} |");
        }
        sb.AppendLine($"|  | **Σ** | **Сумма учтённых сил** | **{balance.SumExternalFxN:0.####}** | **{balance.SumExternalFzN:0.####}** | Сумма строк таблицы. |");
        sb.AppendLine($"|  | **R** | **Требуемая реакция для ΣF=0** | **{balance.RequiredReactionFxN:0.####}** | **{balance.RequiredReactionFzN:0.####}** | Вычислена как минус сумма учтённых сил; пока не найдена solver-ом. |");
        sb.AppendLine();
        sb.AppendLine($"- Горизонтальная удерживающая способность якоря: {balance.AnchorHorizontalCapacityN:0.####} Н");
        sb.AppendLine($"- Запас горизонтального удержания по требуемой реакции Rx: {balance.AnchorHorizontalReserve:0.####}");
        sb.AppendLine($"- Статус баланса: {(balance.IsSolved ? "решён" : "каркас собран; реакции не решены")}");
        sb.AppendLine($"- Методическое примечание: {balance.MethodNote}");
        sb.AppendLine();
    }

    private static void AppendElementRows(StringBuilder sb, CalculationResult result)
    {
        sb.AppendLine("## Таблица элементов");
        sb.AppendLine("| № | Тип | Элемент | Пресет | Длина, м | Кол-во | Вес в воде, кг | Площадь, м² | Cd | Сила, Н | MBL, кН | WLL, кН | Запас | Статус |");
        sb.AppendLine("|---:|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in result.ElementRows)
        {
            sb.AppendLine($"| {row.Number} | {Escape(row.Kind)} | {Escape(row.Title)} | {Escape(row.PresetName)} | {row.LengthM:0.####} | {row.Count} | {row.WeightWaterKg:0.####} | {row.ProjectedAreaM2:0.####} | {row.DragCoefficient:0.####} | {row.CurrentForceN:0.####} | {row.BreakingLoadKn:0.####} | {row.WorkingLoadKn:0.####} | {row.Reserve:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
    }

    private static string Escape(string value)
    {
        return (value ?? string.Empty).Replace("|", "/");
    }
}
