using System.Text;

namespace BuoyCalc.Windows.Services;

public static class PdfReportStructureGuide
{
    public static string Apply(string reportText)
    {
        var text = reportText ?? string.Empty;
        if (text.Contains("## Структура PDF-отчёта v0.45.2"))
        {
            return text;
        }

        return BuildMarkdown() + text;
    }

    private static string BuildMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Структура PDF-отчёта v0.45.2");
        sb.AppendLine("Этот блок добавляется только при экспорте PDF. PDF выводит форму с дискретными элементами как расчётную форму отчёта. Внутренняя fallback-форма в PDF не выводится.");
        sb.AppendLine();
        sb.AppendLine("| № | Раздел | Источник данных |");
        sb.AppendLine("|---:|---|---|");
        sb.AppendLine("| 1 | Краткий итог расчёта | CalculationResult и инженерная диагностика |");
        sb.AppendLine("| 2 | Расчётная 2D-схема с дискретными элементами | MooringAlternativeShapeStore |");
        sb.AppendLine("| 3 | Текстовая цепочка постановки | ViewModel sequence lines |");
        sb.AppendLine("| 4 | Таблица элементов | CalculationResult.ElementRows |");
        sb.AppendLine("| 5 | Полный инженерный отчёт без блока выбора основной формы | Markdown-отчёт ReportBuilder после PDF-фильтра |");
        sb.AppendLine("| 6 | Диагностика и ограничения | MethodNote расчётных сервисов |");
        sb.AppendLine();
        sb.AppendLine("Правило: PDF-страницы и схемы только отображают выходные данные расчётной модели. PDF не должен придумывать координаты, силы, натяжения или режим постановки.");
        sb.AppendLine();
        return sb.ToString();
    }
}
