using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using BuoyCalc.Windows.ViewModels;

namespace BuoyCalc.Windows.Views;

public sealed class Mooring2DCanvas : Control
{
    private static readonly IBrush WaterBrush = new SolidColorBrush(Color.Parse("#DCEBFF"));
    private static readonly IBrush PlotBrush = new SolidColorBrush(Color.Parse("#F7F9FC"));
    private static readonly IBrush BottomBrush = new SolidColorBrush(Color.Parse("#E7DED3"));
    private static readonly IBrush LineBrush = new SolidColorBrush(Color.Parse("#315B9A"));
    private static readonly IBrush BuoyBrush = new SolidColorBrush(Color.Parse("#F2A33A"));
    private static readonly IBrush AnchorBrush = new SolidColorBrush(Color.Parse("#5C4634"));
    private static readonly IBrush NodeBrush = new SolidColorBrush(Color.Parse("#FFFFFF"));
    private static readonly IBrush TextBrush = new SolidColorBrush(Color.Parse("#172033"));
    private static readonly IBrush MutedTextBrush = new SolidColorBrush(Color.Parse("#697386"));
    private static readonly IPen BorderPen = new Pen(new SolidColorBrush(Color.Parse("#D7DEE9")), 1);
    private static readonly IPen LinePen = new Pen(LineBrush, 3);
    private static readonly IPen ThinLinePen = new Pen(new SolidColorBrush(Color.Parse("#A7C7EE")), 1);
    private static readonly IPen NodePen = new Pen(LineBrush, 1.4);
    private static readonly IPen AnchorPen = new Pen(AnchorBrush, 1.4);

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width < 240 || height < 240)
        {
            return;
        }

        var vm = DataContext as MainWindowViewModel;
        var depth = Math.Max(0, vm?.VisualizationDepthM ?? 0);
        var offset = Math.Max(0, vm?.VisualizationOffsetM ?? 0);
        var lineLength = Math.Max(0, vm?.VisualizationLineLengthM ?? 0);

        var padding = 24.0;
        var surfaceY = 58.0;
        var bottomY = height - 72.0;
        var usableHeight = Math.Max(120, bottomY - surfaceY);
        var waterRect = new Rect(padding, surfaceY, width - 2 * padding, usableHeight);
        var bottomRect = new Rect(padding, bottomY, width - 2 * padding, 30);

        context.DrawRectangle(PlotBrush, BorderPen, new Rect(0.5, 0.5, width - 1, height - 1), 16, 16);
        context.DrawRectangle(WaterBrush, null, waterRect);
        context.DrawRectangle(null, BorderPen, waterRect);
        context.DrawRectangle(BottomBrush, BorderPen, bottomRect, 8, 8);

        DrawLabel(context, "поверхность воды", new Point(padding + 12, surfaceY - 28), 12, true, TextBrush);
        DrawLabel(context, "дно / грунт", new Point(padding + 12, bottomY + 8), 12, true, AnchorBrush);
        DrawLabel(context, depth > 0 ? $"глубина {depth:0.##} м" : "глубина не задана", new Point(padding + 12, surfaceY + 12), 11, false, MutedTextBrush);

        var maxHorizontalMeters = Math.Max(depth, Math.Max(offset, 1));
        var maxHorizontalPixels = Math.Max(90, width - 2 * padding - 170);
        var offsetPixels = offset > 0 ? Math.Min(maxHorizontalPixels, offset / maxHorizontalMeters * maxHorizontalPixels) : 0;
        var centerX = width / 2.0;
        var buoyX = centerX - offsetPixels / 2.0;
        var anchorX = centerX + offsetPixels / 2.0;
        buoyX = Math.Clamp(buoyX, padding + 80, width - padding - 80);
        anchorX = Math.Clamp(anchorX, padding + 80, width - padding - 80);

        var buoyPoint = new Point(buoyX, surfaceY + 30);
        var anchorPoint = new Point(anchorX, bottomY - 8);

        context.DrawLine(LinePen, buoyPoint, anchorPoint);
        context.DrawLine(ThinLinePen, new Point(buoyPoint.X, surfaceY), buoyPoint);
        context.DrawLine(ThinLinePen, new Point(anchorPoint.X, bottomY), anchorPoint);

        DrawElementNodes(context, vm, buoyPoint, anchorPoint);
        DrawBuoy(context, buoyPoint, vm?.BuoyName ?? "Буй");
        DrawAnchor(context, anchorPoint, vm?.AnchorName ?? "Якорь");

        if (offset > 0)
        {
            var y = bottomY + 48;
            context.DrawLine(ThinLinePen, new Point(buoyPoint.X, y), new Point(anchorPoint.X, y));
            DrawLabel(context, $"снос {offset:0.##} м", new Point(Math.Min(buoyPoint.X, anchorPoint.X) + 8, y - 18), 11, false, MutedTextBrush);
        }
        else
        {
            DrawLabel(context, "снос: после расчёта или 0 м", new Point(width - padding - 170, bottomY + 40), 11, false, MutedTextBrush);
        }

        DrawLabel(context, lineLength > 0 ? $"линия {lineLength:0.##} м" : "линия не задана", new Point(width - padding - 145, surfaceY + 12), 11, false, MutedTextBrush);
    }

    private static void DrawElementNodes(DrawingContext context, MainWindowViewModel? vm, Point buoyPoint, Point anchorPoint)
    {
        if (vm is null)
        {
            return;
        }

        var labels = vm.SequenceDiagramLines
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "↓")
            .Skip(1)
            .SkipLast(1)
            .ToList();

        if (labels.Count == 0)
        {
            return;
        }

        for (var i = 0; i < labels.Count; i++)
        {
            var t = (i + 1.0) / (labels.Count + 1.0);
            var x = buoyPoint.X + (anchorPoint.X - buoyPoint.X) * t;
            var y = buoyPoint.Y + (anchorPoint.Y - buoyPoint.Y) * t;
            var point = new Point(x, y);
            context.DrawEllipse(NodeBrush, NodePen, point, 6, 6);

            if (i < 5)
            {
                DrawLabel(context, Shorten(labels[i].Replace("○ ", string.Empty), 30), new Point(x + 10, y - 7), 9.5, false, MutedTextBrush);
            }
        }
    }

    private static void DrawBuoy(DrawingContext context, Point point, string title)
    {
        context.DrawEllipse(BuoyBrush, NodePen, point, 14, 14);
        DrawLabel(context, Shorten(title, 22), new Point(point.X + 18, point.Y - 9), 11, true, TextBrush);
    }

    private static void DrawAnchor(DrawingContext context, Point point, string title)
    {
        var rect = new Rect(point.X - 18, point.Y - 10, 36, 20);
        context.DrawRectangle(AnchorBrush, AnchorPen, rect, 4, 4);
        DrawLabel(context, Shorten(title, 24), new Point(point.X + 22, point.Y - 8), 11, true, TextBrush);
    }

    private static void DrawLabel(DrawingContext context, string text, Point origin, double size, bool bold, IBrush brush)
    {
        var typeface = new Typeface("Arial", FontStyle.Normal, bold ? FontWeight.Bold : FontWeight.Normal);
        var formattedText = new FormattedText(
            text ?? string.Empty,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            size,
            brush);

        context.DrawText(formattedText, origin);
    }

    private static string Shorten(string value, int maxLength)
    {
        value ??= string.Empty;
        return value.Length <= maxLength ? value : value[..Math.Max(0, maxLength - 1)] + "…";
    }
}
