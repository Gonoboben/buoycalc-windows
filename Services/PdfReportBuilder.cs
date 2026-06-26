#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BuoyCalc.Windows.Models;
using SkiaSharp;

namespace BuoyCalc.Windows.Services;

public static class PdfReportBuilder
{
    private const float PageWidth = 595;
    private const float PageHeight = 842;
    private const float Margin = 36;
    private const float LineGap = 5;

    public static void Build(
        string filePath,
        string projectName,
        string resultText,
        IEnumerable<string> sequenceLines,
        IEnumerable<ElementCalculationDisplayRow> elementRows,
        string reportText)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");

        using var stream = File.Open(filePath, FileMode.Create, FileAccess.Write);
        using var document = SKDocument.CreatePdf(stream);
        using var regularTypeface = SKTypeface.FromFamilyName("Arial") ?? SKTypeface.Default;
        using var boldTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? regularTypeface;

        var writer = new PdfCanvasWriter(document, regularTypeface, boldTypeface);
        writer.BeginPage();

        writer.Title("BuoyCalc Windows - предварительный отчёт");
        writer.Text($"Проект: {projectName}");
        writer.Space(12);

        writer.Section("Результат");
        writer.Multiline(resultText, 11);
        writer.Space(10);

        writer.Section("Визуальная схема постановки");
        foreach (var line in sequenceLines)
        {
            writer.Text(line, 10);
        }
        writer.Space(10);

        writer.Section("Таблица элементов");
        writer.ElementTable(elementRows.ToList());
        writer.Space(10);

        writer.Section("Полный текстовый отчёт");
        writer.Multiline(reportText, 8, maxLines: 180);

        writer.EndPage();
        document.Close();
    }

    private sealed class PdfCanvasWriter
    {
        private readonly SKDocument _document;
        private readonly SKTypeface _regularTypeface;
        private readonly SKTypeface _boldTypeface;
        private SKCanvas? _canvas;
        private int _pageNumber;
        private float _y;

        public PdfCanvasWriter(SKDocument document, SKTypeface regularTypeface, SKTypeface boldTypeface)
        {
            _document = document;
            _regularTypeface = regularTypeface;
            _boldTypeface = boldTypeface;
        }

        public void BeginPage()
        {
            _canvas = _document.BeginPage(PageWidth, PageHeight);
            _pageNumber++;
            _y = Margin;
            DrawFooter();
        }

        public void EndPage()
        {
            _document.EndPage();
            _canvas = null;
        }

        public void Title(string text)
        {
            DrawText(text, 19, true);
            Space(6);
        }

        public void Section(string text)
        {
            EnsureSpace(34);
            DrawText(text, 13, true);
            Space(5);
        }

        public void Text(string text, float size = 10)
        {
            foreach (var line in Wrap(text, size, PageWidth - 2 * Margin))
            {
                DrawText(line, size, false);
            }
        }

        public void Multiline(string text, float size, int maxLines = 500)
        {
            var count = 0;
            foreach (var rawLine in SplitLines(text))
            {
                foreach (var line in Wrap(rawLine, size, PageWidth - 2 * Margin))
                {
                    if (count >= maxLines)
                    {
                        DrawText("... отчёт обрезан для компактного PDF", size, false);
                        return;
                    }

                    DrawText(line, size, false);
                    count++;
                }
            }
        }

        public void ElementTable(IReadOnlyList<ElementCalculationDisplayRow> rows)
        {
            var headers = new[] { "№", "Тип", "Элемент", "Вес", "Сила", "Запас", "Статус" };
            var widths = new[] { 24f, 58f, 142f, 58f, 58f, 48f, 134f };
            DrawTableRow(headers, widths, true);

            foreach (var row in rows)
            {
                DrawTableRow(
                    new[]
                    {
                        row.Number.ToString(),
                        row.Kind,
                        row.Title,
                        row.WeightWaterKg,
                        row.CurrentForceN,
                        row.Reserve,
                        row.Status
                    },
                    widths,
                    false);
            }
        }

        public void Space(float value)
        {
            _y += value;
        }

        private void DrawTableRow(IReadOnlyList<string> values, IReadOnlyList<float> widths, bool isHeader)
        {
            const float rowHeight = 24;
            EnsureSpace(rowHeight + 4);

            var x = Margin;
            for (var i = 0; i < values.Count; i++)
            {
                var rect = new SKRect(x, _y, x + widths[i], _y + rowHeight);
                using var fill = new SKPaint { Color = isHeader ? new SKColor(233, 239, 250) : SKColors.White, IsAntialias = true };
                using var border = new SKPaint { Color = new SKColor(215, 222, 233), Style = SKPaintStyle.Stroke, StrokeWidth = 0.7f, IsAntialias = true };
                _canvas!.DrawRect(rect, fill);
                _canvas.DrawRect(rect, border);

                var text = Shorten(values[i], isHeader ? 16 : 28);
                using var paint = CreatePaint(isHeader ? 8.2f : 7.6f, isHeader);
                _canvas.DrawText(text, rect.Left + 3, rect.Top + 15, paint);
                x += widths[i];
            }

            _y += rowHeight;
        }

        private void DrawText(string text, float size, bool bold)
        {
            EnsureSpace(size + LineGap + 3);
            using var paint = CreatePaint(size, bold);
            _canvas!.DrawText(text ?? string.Empty, Margin, _y + size, paint);
            _y += size + LineGap;
        }

        private SKPaint CreatePaint(float size, bool bold)
        {
            return new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                TextSize = size,
                Typeface = bold ? _boldTypeface : _regularTypeface
            };
        }

        private void EnsureSpace(float required)
        {
            if (_y + required < PageHeight - Margin)
            {
                return;
            }

            EndPage();
            BeginPage();
        }

        private void DrawFooter()
        {
            using var paint = CreatePaint(8, false);
            _canvas!.DrawText($"BuoyCalc Windows · стр. {_pageNumber}", Margin, PageHeight - 18, paint);
        }

        private IEnumerable<string> Wrap(string text, float size, float maxWidth)
        {
            text ??= string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                yield return string.Empty;
                yield break;
            }

            using var paint = CreatePaint(size, false);
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = string.Empty;

            foreach (var word in words)
            {
                var candidate = string.IsNullOrEmpty(line) ? word : line + " " + word;
                if (paint.MeasureText(candidate) <= maxWidth)
                {
                    line = candidate;
                }
                else
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        yield return line;
                    }
                    line = word;
                }
            }

            if (!string.IsNullOrEmpty(line))
            {
                yield return line;
            }
        }

        private static IEnumerable<string> SplitLines(string text)
        {
            return (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }

        private static string Shorten(string value, int maxLength)
        {
            value ??= string.Empty;
            return value.Length <= maxLength ? value : value[..Math.Max(0, maxLength - 1)] + "…";
        }
    }
}
