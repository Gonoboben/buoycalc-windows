using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using BuoyCalc.Windows.Services;
using BuoyCalc.Windows.ViewModels;

namespace BuoyCalc.Windows.Views;

public sealed class Mooring2DCanvas : Control
{
    private static readonly IBrush WaterBrush = new SolidColorBrush(Color.Parse("#DCEBFF"));
    private static readonly IBrush PlotBrush = new SolidColorBrush(Color.Parse("#F7F9FC"));
    private static readonly IBrush BottomBrush = new SolidColorBrush(Color.Parse("#E7DED3"));
    private static readonly IBrush ShapeBrush = new SolidColorBrush(Color.Parse("#D46B08"));
    private static readonly IBrush BuoyBrush = new SolidColorBrush(Color.Parse("#F2A33A"));
    private static readonly IBrush AnchorBrush = new SolidColorBrush(Color.Parse("#5C4634"));
    private static readonly IBrush NodeBrush = new SolidColorBrush(Color.Parse("#FFF4E5"));
    private static readonly IBrush TextBrush = new SolidColorBrush(Color.Parse("#172033"));
    private static readonly IBrush MutedTextBrush = new SolidColorBrush(Color.Parse("#697386"));
    private static readonly IPen BorderPen = new Pen(new SolidColorBrush(Color.Parse("#D7DEE9")), 1);
    private static readonly IPen ShapePen = new Pen(ShapeBrush, 3);
    private static readonly IPen ThinLinePen = new Pen(new SolidColorBrush(Color.Parse("#A7C7EE")), 1);
    private static readonly IPen NodePen = new Pen(ShapeBrush, 1.4);
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
        DrawLabel(context, lineLength > 0 ? $"линия {lineLength:0.##} м" : "линия не задана", new Point(width - padding - 145, surfaceY + 12), 11, false, MutedTextBrush);

        var selected = MooringAlternativeShapeStore.Current;
        if (selected is not null && selected.Shape.Rows.Count >= 2)
        {
            DrawSelectedShape(context, selected, vm, width, surfaceY, bottomY, usableHeight, padding, depth);
            return;
        }

        var technicalShape = MooringShapeStore.Current;
        if (technicalShape is { Nodes.Count: >= 2 })
        {
            var nodes = technicalShape.Nodes.Select(x => new PlotNode(x.Number, x.XOffsetM, x.ZDepthM, x.Label)).ToList();
            DrawSimpleShape(context, nodes, technicalShape.HorizontalOffsetM, technicalShape.Converged, vm, width, surfaceY, bottomY, usableHeight, padding, depth, "форма с дискретными элементами ещё не построена");
            return;
        }

        DrawNoShape(context, width, surfaceY, bottomY, padding);
    }

    private static void DrawSelectedShape(
        DrawingContext context,
        MooringAlternativeShapeDisplayData selected,
        MainWindowViewModel? vm,
        double width,
        double surfaceY,
        double bottomY,
        double usableHeight,
        double padding,
        double inputDepth)
    {
        var nodes = selected.Shape.Rows.Select(x => new PlotNode(x.Number, x.XOffsetM, x.ZDepthM, x.SourceElement)).ToList();
        DrawSimpleShape(
            context,
            nodes,
            selected.Shape.DiscreteHorizontalOffsetM,
            selected.Shape.Converged,
            vm,
            width,
            surfaceY,
            bottomY,
            usableHeight,
            padding,
            Math.Max(inputDepth, selected.Shape.AnchorDepthM),
            "выбранная форма: с дискретными элементами");

        var map = BuildMapper(nodes, width, surfaceY, usableHeight, padding, Math.Max(inputDepth, selected.Shape.AnchorDepthM));
        foreach (var node in selected.DiscreteNodes.Rows.Where(x => x.Kind != "Буй" && x.Kind != "Якорь").OrderBy(x => x.PositionAlongLineM).Take(12))
        {
            var point = map(node.AlternativeXOffsetM, node.AlternativeZDepthM);
            context.DrawEllipse(NodeBrush, NodePen, point, 6, 6);
            DrawLabel(context, Shorten(node.Title, 26), new Point(point.X + 9, point.Y - 7), 9, false, ShapeBrush);
        }
    }

    private static void DrawSimpleShape(
        DrawingContext context,
        IReadOnlyList<PlotNode> nodes,
        double offsetM,
        bool converged,
        MainWindowViewModel? vm,
        double width,
        double surfaceY,
        double bottomY,
        double usableHeight,
        double padding,
        double inputDepth,
        string title)
    {
        var map = BuildMapper(nodes, width, surfaceY, usableHeight, padding, inputDepth);
        var points = nodes.Select(x => map(x.X, x.Z)).ToList();

        DrawLinePath(context, points, ShapePen);

        var buoyPoint = points[0];
        var anchorPoint = points[^1];
        context.DrawLine(ThinLinePen, anchorPoint, new Point(anchorPoint.X, bottomY));

        var step = Math.Max(1, points.Count / 24);
        for (var i = 1; i < points.Count - 1; i += step)
        {
            context.DrawEllipse(NodeBrush, NodePen, points[i], 4.2, 4.2);
        }

        DrawBuoy(context, buoyPoint, vm?.BuoyName ?? "Буй");
        DrawAnchor(context, anchorPoint, vm?.AnchorName ?? "Якорь");

        DrawLegendLine(context, new Point(padding + 12, surfaceY + 32), ShapePen, title, ShapeBrush);
        DrawLabel(context, converged ? "форма: ОК" : "форма: требует проверки", new Point(padding + 12, surfaceY + 52), 10, false, converged ? MutedTextBrush : ShapeBrush);
        DrawLabel(context, "масштаб X=Z, без увеличения по горизонтали", new Point(width - padding - 250, surfaceY + 32), 10, false, MutedTextBrush);

        var y = bottomY + 48;
        context.DrawLine(ThinLinePen, new Point(buoyPoint.X, y), new Point(anchorPoint.X, y));
        DrawLabel(context, $"снос X/Z {offsetM:0.##} м", new Point(Math.Min(buoyPoint.X, anchorPoint.X) + 8, y - 18), 11, false, MutedTextBrush);
    }

    private static Func<double, double, Point> BuildMapper(IReadOnlyList<PlotNode> nodes, double width, double surfaceY, double usableHeight, double padding, double inputDepth)
    {
        var minNodeX = nodes.Min(x => x.X);
        var maxNodeX = nodes.Max(x => x.X);
        var maxNodeZ = Math.Max(0.0001, nodes.Max(x => x.Z));
        var drawingDepth = Math.Max(1, inputDepth > 0 ? Math.Max(inputDepth, maxNodeZ) : maxNodeZ);
        var horizontalSpanM = Math.Max(0.0001, maxNodeX - minNodeX);
        var scale = Math.Min(usableHeight / drawingDepth, (width - 2 * padding - 160) / horizontalSpanM);
        var spanX = horizontalSpanM * scale;
        var startX = width / 2.0 - spanX / 2.0;
        startX = Math.Clamp(startX, padding + 80, width - padding - 80 - spanX);

        return (x, z) => new Point(
            startX + (x - minNodeX) * scale,
            surfaceY + Math.Clamp(z, 0, drawingDepth) * scale);
    }

    private static void DrawNoShape(DrawingContext context, double width, double surfaceY, double bottomY, double padding)
    {
        DrawLabel(context, "сначала выполните расчёт", new Point(padding + 12, surfaceY + 34), 12, true, TextBrush);
        DrawLabel(context, "после расчёта здесь появится выбранная форма постановки", new Point(padding + 12, surfaceY + 54), 10, false, MutedTextBrush);
    }

    private static void DrawLinePath(DrawingContext context, IReadOnlyList<Point> points, IPen pen)
    {
        for (var i = 1; i < points.Count; i++)
        {
            context.DrawLine(pen, points[i - 1], points[i]);
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

    private static void DrawLegendLine(DrawingContext context, Point origin, IPen pen, string text, IBrush brush)
    {
        context.DrawLine(pen, origin, new Point(origin.X + 28, origin.Y));
        DrawLabel(context, text, new Point(origin.X + 36, origin.Y - 8), 10, false, brush);
    }

    private static void DrawLabel(DrawingContext context, string text, Point origin, double size, bool bold, IBrush brush)
    {
        var typeface = new Typeface("Arial", FontStyle.Normal, bold ? FontWeight.Bold : FontWeight.Normal);
        var formattedText = new FormattedText(text ?? string.Empty, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, size, brush);
        context.DrawText(formattedText, origin);
    }

    private static string Shorten(string value, int maxLength)
    {
        value ??= string.Empty;
        return value.Length <= maxLength ? value : value[..Math.Max(0, maxLength - 1)] + "…";
    }

    private sealed record PlotNode(int Number, double X, double Z, string Label);
}
