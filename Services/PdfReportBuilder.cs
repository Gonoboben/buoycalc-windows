#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using System.Globalization;
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
        string reportText,
        double visualizationDepthM,
        double visualizationLineLengthM,
        double visualizationOffsetM)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");

        using var stream = File.Open(filePath, FileMode.Create, FileAccess.Write);
        using var document = SKDocument.CreatePdf(stream);
        using var regularTypeface = SKTypeface.FromFamilyName("Arial") ?? SKTypeface.Default;
        using var boldTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? regularTypeface;

        var writer = new PdfCanvasWriter(document, regularTypeface, boldTypeface);
        var sequence = sequenceLines.ToList();
        var shapeOffsetM = TryReadReportMetric(reportText, "- Снос формы X/Z:")
            ?? TryReadReportMetric(reportText, "- Горизонтальный снос по узлам X/Z:")
            ?? visualizationOffsetM;
        var clarifiedResultText = NormalizeResultText(resultText, shapeOffsetM);

        writer.BeginPage();
        writer.Title("BuoyCalc Windows - предварительный отчёт");
        writer.Text($"Проект: {projectName}");
        writer.Text($"Версия расчётной модели: {AppInfo.DisplayVersion}");
        writer.Space(12);

        writer.Section("Результат");
        writer.Multiline(clarifiedResultText, 11);
        writer.EndPage();

        writer.BeginPage();
        writer.Title("Схема постановки");
        writer.Text("Геометрическая 2D-схема: поверхность воды, буй, линия, активные элементы, якорь, дно и снос формы X/Z.", 10);
        writer.Space(10);
        writer.MooringDiagram(sequence, visualizationDepthM, visualizationLineLengthM, shapeOffsetM);
        writer.Space(12);
        writer.Section("Текстовая цепочка");
        foreach (var line in sequence)
        {
            writer.Text(line, 9.5f);
        }
        writer.EndPage();

        var mainShape = MooringShapeStore.Current;
        var alternativeShape = MooringAlternativeShapeStore.Current;
        if (mainShape is not null && mainShape.Nodes.Count >= 2 && alternativeShape is not null && alternativeShape.Shape.Rows.Count >= 2)
        {
            writer.BeginPage();
            writer.Title("Сравнение форм X/Z");
            writer.Text("Основная форма берётся из MooringShapeSolver. Альтернативная форма и дискретные точки берутся из расчётного слоя с дискретными нагрузками. PDF только отображает готовые X/Z-координаты.", 10);
            writer.Space(10);
            writer.ShapeComparisonDiagram(mainShape, alternativeShape);
            writer.EndPage();
        }

        writer.BeginPage();
        writer.Title("Таблица элементов");
        writer.ElementTable(elementRows.ToList());
        writer.Space(10);
        writer.Section("Полный текстовый отчёт");
        writer.Multiline(reportText, 7.2f);

        writer.EndPage();
        document.Close();
    }

    private static string NormalizeResultText(string resultText, double shapeOffsetM)
    {
        var lines = (resultText ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').ToList();
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith("Плавучесть:", StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = "Чистая плавучесть:" + lines[i]["Плавучесть:".Length..];
            }
            else if (lines[i].StartsWith("Натяжение:", StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = "Нагрузка слабого звена:" + lines[i]["Натяжение:".Length..];
            }
        }

        if (shapeOffsetM > 0 && !lines.Any(x => x.StartsWith("Снос формы X/Z:", StringComparison.OrdinalIgnoreCase)))
        {
            lines.Add($"Снос формы X/Z: {shapeOffsetM:0.##} м");
        }

        return string.Join("\n", lines);
    }

    private static double? TryReadReportMetric(string reportText, string label)
    {
        foreach (var line in (reportText ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var index = line.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            var valuePart = line[(index + label.Length)..].Trim();
            var token = new string(valuePart.TakeWhile(ch => char.IsDigit(ch) || ch == '-' || ch == '+' || ch == ',' || ch == '.').ToArray());
            token = token.Replace(',', '.');
            if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
        }

        return null;
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

        public void Multiline(string text, float size)
        {
            foreach (var rawLine in SplitLines(text))
            {
                foreach (var line in Wrap(rawLine, size, PageWidth - 2 * Margin))
                {
                    DrawText(line, size, false);
                }
            }
        }

        public void MooringDiagram(IReadOnlyList<string> sequenceLines, double depthM, double lineLengthM, double offsetM)
        {
            const float diagramHeight = 360;
            EnsureSpace(diagramHeight + 20);

            var x = Margin;
            var y = _y;
            var width = PageWidth - 2 * Margin;
            var surfaceY = y + 58;
            var bottomY = y + 280;
            var waterRect = new SKRect(x, surfaceY, x + width, bottomY);
            var bottomRect = new SKRect(x, bottomY, x + width, bottomY + 28);

            using var plotPaint = Fill("#F7F9FC");
            using var waterPaint = Fill("#DCEBFF");
            using var bottomPaint = Fill("#E7DED3");
            using var borderPaint = Stroke("#D7DEE9", 1);
            using var linePaint = Stroke("#315B9A", 3);
            using var thinPaint = Stroke("#A7C7EE", 1);
            using var buoyPaint = Fill("#F2A33A");
            using var anchorPaint = Fill("#5C4634");
            using var nodePaint = Fill("#FFFFFF");
            using var nodeBorderPaint = Stroke("#315B9A", 1.3f);

            _canvas!.DrawRect(new SKRect(x, y, x + width, y + diagramHeight), plotPaint);
            _canvas.DrawRect(new SKRect(x, y, x + width, y + diagramHeight), borderPaint);
            _canvas.DrawRect(waterRect, waterPaint);
            _canvas.DrawRect(waterRect, borderPaint);
            _canvas.DrawRect(bottomRect, bottomPaint);
            _canvas.DrawRect(bottomRect, borderPaint);

            DrawTextAt("поверхность воды", x + 14, surfaceY - 24, 10, true, SKColors.Black);
            DrawTextAt(depthM > 0 ? $"глубина {depthM:0.##} м" : "глубина не задана", x + 14, surfaceY + 18, 9, false, new SKColor(80, 92, 112));
            DrawTextAt(lineLengthM > 0 ? $"линия {lineLengthM:0.##} м" : "линия не задана", x + width - 120, surfaceY + 18, 9, false, new SKColor(80, 92, 112));
            DrawTextAt("дно / грунт", x + 14, bottomY + 18, 10, true, new SKColor(92, 70, 52));

            var safeDepth = Math.Max(1, depthM);
            var safeOffset = Math.Max(0, offsetM);
            var maxHorizontalMeters = Math.Max(safeDepth, Math.Max(safeOffset, 1));
            var maxHorizontalPixels = width - 180;
            var offsetPixels = safeOffset > 0 ? Math.Min(maxHorizontalPixels, (float)(safeOffset / maxHorizontalMeters * maxHorizontalPixels)) : 0;
            var centerX = x + width / 2;
            var buoyX = Math.Clamp(centerX - offsetPixels / 2, x + 80, x + width - 80);
            var anchorX = Math.Clamp(centerX + offsetPixels / 2, x + 80, x + width - 80);
            var buoyPoint = new SKPoint(buoyX, surfaceY);
            var lineStartPoint = new SKPoint(buoyX, surfaceY + 13);
            var anchorPoint = new SKPoint(anchorX, bottomY - 8);

            _canvas.DrawLine(lineStartPoint, anchorPoint, linePaint);
            _canvas.DrawLine(new SKPoint(anchorPoint.X, bottomY), anchorPoint, thinPaint);

            var labels = sequenceLines.Where(v => !string.IsNullOrWhiteSpace(v) && v != "↓").ToList();
            var buoyLabel = CleanLabel(labels.FirstOrDefault(), "Буй");
            var anchorLabel = CleanLabel(labels.LastOrDefault(), "Якорь");
            var nodes = labels.Skip(1).SkipLast(1).ToList();

            _canvas.DrawCircle(buoyPoint, 13, buoyPaint);
            _canvas.DrawCircle(buoyPoint, 13, nodeBorderPaint);
            DrawTextAt(Shorten(buoyLabel, 26), buoyPoint.X + 18, buoyPoint.Y + 4, 9.5f, true, SKColors.Black);

            for (var i = 0; i < nodes.Count; i++)
            {
                var t = (i + 1f) / (nodes.Count + 1f);
                var px = lineStartPoint.X + (anchorPoint.X - lineStartPoint.X) * t;
                var py = lineStartPoint.Y + (anchorPoint.Y - lineStartPoint.Y) * t;
                _canvas.DrawCircle(px, py, 5.8f, nodePaint);
                _canvas.DrawCircle(px, py, 5.8f, nodeBorderPaint);

                if (i < 5)
                {
                    DrawTextAt(Shorten(CleanLabel(nodes[i], "Элемент"), 32), px + 10, py + 3, 7.6f, false, new SKColor(80, 92, 112));
                }
            }

            var anchorRect = new SKRect(anchorPoint.X - 18, anchorPoint.Y - 10, anchorPoint.X + 18, anchorPoint.Y + 10);
            _canvas.DrawRect(anchorRect, anchorPaint);
            DrawTextAt(Shorten(anchorLabel, 28), anchorPoint.X + 22, anchorPoint.Y + 4, 9.5f, true, SKColors.Black);

            if (safeOffset > 0)
            {
                var offsetY = bottomY + 52;
                _canvas.DrawLine(new SKPoint(buoyPoint.X, offsetY), new SKPoint(anchorPoint.X, offsetY), thinPaint);
                DrawTextAt($"снос X/Z {safeOffset:0.##} м", Math.Min(buoyPoint.X, anchorPoint.X) + 8, offsetY - 8, 9, false, new SKColor(80, 92, 112));
            }
            else
            {
                DrawTextAt("снос X/Z: 0 м / после расчёта", x + width - 180, bottomY + 52, 9, false, new SKColor(80, 92, 112));
            }

            _y += diagramHeight + 10;
        }

        public void ShapeComparisonDiagram(MooringShapeResult mainShape, MooringAlternativeShapeDisplayData alternative)
        {
            const float diagramHeight = 520;
            EnsureSpace(diagramHeight + 10);

            var x = Margin;
            var y = _y;
            var width = PageWidth - 2 * Margin;
            var plotHeight = 410f;
            var surfaceY = y + 52;
            var plotBottomY = surfaceY + plotHeight;
            var plotRect = new SKRect(x, y, x + width, y + diagramHeight);
            var waterRect = new SKRect(x, surfaceY, x + width, plotBottomY);

            using var plotPaint = Fill("#F7F9FC");
            using var waterPaint = Fill("#DCEBFF");
            using var bottomPaint = Fill("#E7DED3");
            using var borderPaint = Stroke("#D7DEE9", 1);
            using var mainPaint = Stroke("#315B9A", 2.5f);
            using var alternativePaint = Stroke("#D46B08", 2.0f);
            using var thinPaint = Stroke("#A7C7EE", 1);
            using var buoyPaint = Fill("#F2A33A");
            using var anchorPaint = Fill("#5C4634");
            using var nodePaint = Fill("#FFF4E5");
            using var nodeBorderPaint = Stroke("#D46B08", 1.2f);
            using var mainNodePaint = Fill("#FFFFFF");
            using var mainNodeBorderPaint = Stroke("#315B9A", 1.2f);

            _canvas!.DrawRect(plotRect, plotPaint);
            _canvas.DrawRect(plotRect, borderPaint);
            _canvas.DrawRect(waterRect, waterPaint);
            _canvas.DrawRect(waterRect, borderPaint);
            _canvas.DrawRect(new SKRect(x, plotBottomY, x + width, plotBottomY + 28), bottomPaint);
            _canvas.DrawRect(new SKRect(x, plotBottomY, x + width, plotBottomY + 28), borderPaint);

            var mainNodes = mainShape.Nodes.Select(v => new PlotNode(v.XOffsetM, v.ZDepthM, v.Label)).ToList();
            var altNodes = alternative.Shape.Rows.Select(v => new PlotNode(v.XOffsetM, v.ZDepthM, v.SourceElement)).ToList();
            var allNodes = mainNodes.Concat(altNodes).ToList();
            var minX = allNodes.Min(v => v.X);
            var maxX = allNodes.Max(v => v.X);
            var maxZ = Math.Max(0.0001, allNodes.Max(v => v.Z));
            var drawingDepth = Math.Max(1, Math.Max(mainShape.DepthM, maxZ));
            var horizontalSpan = Math.Max(0.0001, maxX - minX);
            var scale = Math.Min((width - 110) / horizontalSpan, plotHeight / drawingDepth);
            var spanX = horizontalSpan * scale;
            var startX = x + width / 2f - (float)(spanX / 2.0);
            var bottomLineY = surfaceY + (float)(drawingDepth * scale);

            SKPoint Map(double mx, double mz) => new(
                (float)(startX + (mx - minX) * scale),
                (float)(surfaceY + Math.Clamp(mz, 0, drawingDepth) * scale));

            var mainPoints = mainNodes.Select(v => Map(v.X, v.Z)).ToList();
            var altPoints = altNodes.Select(v => Map(v.X, v.Z)).ToList();

            _canvas.DrawLine(new SKPoint(x + 8, bottomLineY), new SKPoint(x + width - 8, bottomLineY), thinPaint);
            DrawPolyline(_canvas, mainPoints, mainPaint);
            DrawPolyline(_canvas, altPoints, alternativePaint);

            var step = Math.Max(1, mainPoints.Count / 20);
            for (var i = 1; i < mainPoints.Count - 1; i += step)
            {
                _canvas.DrawCircle(mainPoints[i], 3.5f, mainNodePaint);
                _canvas.DrawCircle(mainPoints[i], 3.5f, mainNodeBorderPaint);
            }

            foreach (var discrete in alternative.DiscreteNodes.Rows.Where(v => v.Kind != "Буй" && v.Kind != "Якорь").Take(12))
            {
                var point = Map(discrete.AlternativeXOffsetM, discrete.AlternativeZDepthM);
                _canvas.DrawCircle(point, 5.5f, nodePaint);
                _canvas.DrawCircle(point, 5.5f, nodeBorderPaint);
            }

            var buoyPoint = mainPoints[0];
            var anchorPoint = mainPoints[^1];
            _canvas.DrawCircle(buoyPoint, 12, buoyPaint);
            _canvas.DrawCircle(buoyPoint, 12, mainNodeBorderPaint);
            _canvas.DrawRect(new SKRect(anchorPoint.X - 15, anchorPoint.Y - 8, anchorPoint.X + 15, anchorPoint.Y + 8), anchorPaint);

            DrawTextAt("поверхность воды", x + 14, surfaceY - 24, 10, true, SKColors.Black);
            DrawTextAt($"глубина {drawingDepth:0.##} м", x + 14, surfaceY + 18, 9, false, new SKColor(80, 92, 112));
            DrawTextAt("дно / грунт", x + 14, bottomLineY + 18, 10, true, new SKColor(92, 70, 52));
            DrawLegendLine(x + 14, y + 18, mainPaint, "основная форма X/Z", SKColors.Black);
            DrawLegendLine(x + 170, y + 18, alternativePaint, "альтернативная форма", new SKColor(212, 107, 8));
            DrawTextAt(alternative.Shape.Converged ? "альтернативная форма: OK" : "альтернативная форма: WARNING", x + 340, y + 23, 9, false, alternative.Shape.Converged ? new SKColor(80, 92, 112) : new SKColor(212, 107, 8));
            DrawTextAt($"основной снос {mainShape.HorizontalOffsetM:0.##} м", x + 14, plotBottomY + 52, 9, false, new SKColor(80, 92, 112));
            DrawTextAt($"альт. снос {alternative.Shape.DiscreteHorizontalOffsetM:0.##} м", x + 170, plotBottomY + 52, 9, false, new SKColor(212, 107, 8));
            DrawTextAt($"дискретных X/Z точек {alternative.DiscreteNodes.DiscreteNodeCount}", x + 340, plotBottomY + 52, 9, false, new SKColor(80, 92, 112));
            DrawTextAt("масштаб X=Z; координаты взяты из расчётных объектов, не из текста отчёта", x + 14, plotBottomY + 72, 8.5f, false, new SKColor(80, 92, 112));

            _y += diagramHeight + 10;
        }

        public void ElementTable(IReadOnlyList<ElementCalculationDisplayRow> rows)
        {
            var headers = new[] { "№", "Тип", "Элемент", "Вес в воде", "Fx, Н", "Запас", "Статус" };
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

        private void DrawTextAt(string text, float x, float y, float size, bool bold, SKColor color)
        {
            using var paint = CreatePaint(size, bold, color);
            _canvas!.DrawText(text ?? string.Empty, x, y, paint);
        }

        private SKPaint CreatePaint(float size, bool bold)
        {
            return CreatePaint(size, bold, SKColors.Black);
        }

        private SKPaint CreatePaint(float size, bool bold, SKColor color)
        {
            return new SKPaint
            {
                Color = color,
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
            _canvas!.DrawText($"BuoyCalc Windows {AppInfo.Version} · стр. {_pageNumber}", Margin, PageHeight - 18, paint);
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

        private static void DrawPolyline(SKCanvas canvas, IReadOnlyList<SKPoint> points, SKPaint paint)
        {
            for (var i = 1; i < points.Count; i++)
            {
                canvas.DrawLine(points[i - 1], points[i], paint);
            }
        }

        private void DrawLegendLine(float x, float y, SKPaint linePaint, string text, SKColor textColor)
        {
            _canvas!.DrawLine(new SKPoint(x, y), new SKPoint(x + 28, y), linePaint);
            DrawTextAt(text, x + 36, y + 4, 9, false, textColor);
        }

        private static SKPaint Fill(string color)
        {
            return new SKPaint { Color = SKColor.Parse(color), IsAntialias = true, Style = SKPaintStyle.Fill };
        }

        private static SKPaint Stroke(string color, float width)
        {
            return new SKPaint { Color = SKColor.Parse(color), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width };
        }

        private static IEnumerable<string> SplitLines(string text)
        {
            return (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }

        private static string CleanLabel(string? value, string fallback)
        {
            value ??= fallback;
            value = value.Replace("● Буй:", string.Empty)
                .Replace("■ Якорь:", string.Empty)
                .Replace("○", string.Empty)
                .Trim();
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static string Shorten(string value, int maxLength)
        {
            value ??= string.Empty;
            return value.Length <= maxLength ? value : value[..Math.Max(0, maxLength - 1)] + "…";
        }

        private sealed record PlotNode(double X, double Z, string Label);
    }
}
