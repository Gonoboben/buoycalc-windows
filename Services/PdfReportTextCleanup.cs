using System;
using System.Collections.Generic;
using System.Linq;

namespace BuoyCalc.Windows.Services;

public static class PdfReportTextCleanup
{
    public static string Apply(string reportText)
    {
        var lines = (reportText ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n');

        var result = new List<string>();
        var skipBlock = false;

        foreach (var line in lines)
        {
            if (StartsSection(line, "## Выбор основной формы"))
            {
                skipBlock = true;
                continue;
            }

            if (skipBlock && line.StartsWith("## ", StringComparison.OrdinalIgnoreCase))
            {
                skipBlock = false;
            }

            if (skipBlock || ShouldDropLine(line))
            {
                continue;
            }

            result.Add(RewriteLine(line));
        }

        return string.Join("\n", CollapseBlankLines(result));
    }

    private static bool ShouldDropLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var text = line.ToLowerInvariant();
        return text.Contains("primaryshape") ||
            text.Contains("mooringprimaryshapegate") ||
            text.Contains("mooringprimaryshapeselector") ||
            text.Contains("moorshapesolver fallback") ||
            text.Contains("основная форма должна остаться") ||
            text.Contains("основной solver пока не заменяется") ||
            text.Contains("основной solver, 2d и pdf-схемы пока не замен") ||
            text.Contains("основная форма x/z") ||
            text.Contains("основной снос") ||
            text.Contains("выбор основной формы") ||
            text.Contains("эта таблица показывает, какая форма передана") ||
            text.Contains("v0.39 добавляет диагностический итерационный solver-слой") ||
            text.Contains("v0.40: gate оценивает") ||
            text.Contains("v0.40: кандидатная форма") ||
            text.Contains("v0.40 primary shape");
    }

    private static string RewriteLine(string line)
    {
        return line
            .Replace("Итерационный solver v0.39", "Итерационный solver")
            .Replace("## Итерационный solver v0.39 — диагностика", "## Итерационный solver — диагностика")
            .Replace("v0.39 feedback-цикле", "feedback-цикле")
            .Replace("v0.39 feedback-цикл", "feedback-цикл")
            .Replace("в v0.33 появилась координата s", "используется координата s")
            .Replace("Старая оценка сноса", "Оценка сноса исходной модели")
            .Replace("Top T старая", "Top T исходная")
            .Replace("Max T старая", "Max T исходная")
            .Replace("F старая", "F исходная")
            .Replace("Угол старый", "Угол исходный")
            .Replace("старая сила", "исходная сила")
            .Replace("старой силы", "исходной силы")
            .Replace("старой оценки", "исходной оценки")
            .Replace("старого", "исходного")
            .Replace("Снос альтернативной формы с дискретными нагрузками", "Снос формы с дискретными элементами")
            .Replace("Статус альтернативной формы", "Статус формы с дискретными элементами")
            .Replace("альтернативной формы", "формы с дискретными элементами")
            .Replace("альтернативная форма", "форма с дискретными элементами")
            .Replace("альт.", "дискр.")
            .Replace("Альт. форма X/Z", "Форма с дискретными элементами X/Z");
    }

    private static bool StartsSection(string line, string section)
    {
        return line.StartsWith(section, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> CollapseBlankLines(IEnumerable<string> lines)
    {
        var previousBlank = false;
        foreach (var line in lines)
        {
            var blank = string.IsNullOrWhiteSpace(line);
            if (blank && previousBlank)
            {
                continue;
            }

            previousBlank = blank;
            yield return line;
        }
    }
}
