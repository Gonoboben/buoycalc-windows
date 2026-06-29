using System.Text;

namespace BuoyCalc.Windows.Services;

public static class PdfReportStructureGuide
{
    public static string Apply(string reportText)
    {
        var text = reportText ?? string.Empty;
        if (text.Contains("## Структура PDF-отчёта v0.45.1") || text.Contains("## PDF report structure v0.45"))
        {
            return text.Replace("## PDF report structure v0.45", "## Структура PDF-отчёта v0.45.1");
        }

        return BuildMarkdown() + text;
    }

    private static string BuildMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Структура PDF-отчёта v0.45.1");
        sb.AppendLine("Этот блок добавляется только при экспорте PDF. Он фиксирует порядок итогового отчёта и не меняет физику solver, координаты X/Z, силы или натяжения.");
        sb.AppendLine();
        sb.AppendLine("| № | Раздел | Источник данных |");
        sb.AppendLine("|---:|---|---|");
        sb.AppendLine("| 1 | Краткий итог расчёта | CalculationResult и инженерная диагностика |");
        sb.AppendLine("| 2 | Расчётная 2D-схема | MooringShapeStore и результат solver формы |");
        sb.AppendLine("| 3 | Сравнение форм | Основная форма и альтернативная форма с дискретными нагрузками, если она доступна |");
        sb.AppendLine("| 4 | Таблица элементов | CalculationResult.ElementRows |");
        sb.AppendLine("| 5 | Полный инженерный отчёт | Markdown-отчёт ReportBuilder |");
        sb.AppendLine("| 6 | Диагностика и ограничения | MethodNote расчётных сервисов |");
        sb.AppendLine();
        sb.AppendLine("Правило: PDF-страницы и схемы только отображают выходные данные расчётной модели. PDF не должен придумывать координаты, силы, натяжения или режим постановки.");
        sb.AppendLine();
        return sb.ToString();
    }
}
