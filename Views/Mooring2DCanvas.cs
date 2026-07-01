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
    private static readonly IBrush LineBrush = new SolidColorBrush(Color.Parse("#315B9A"));
    private static readonly IBrush AlternativeLineBrush = new SolidColorBrush(Color.Parse("#D46B08"));
    private static readonly IBrush BuoyBrush = new SolidColorBrush(Color.Parse("#F2A33A"));
    private static readonly IBrush AnchorBrush = new SolidColorBrush(Color.Parse("#5C4634"));
    private static readonly IBrush NodeBrush = new SolidColorBrush(Color.Parse("#FFFFFF"));
    private static readonly IBrush TextBrush = new SolidColorBrush(Color.Parse("#172033"));
    private static readonly IBrush MutedTextBrush = new SolidColorBrush(Color.Parse("#697386"));
    private static readonly IBrush AlternativeNodeBrush = new SolidColorBrush(Color.Parse("#FFF4E5"));
    private static readonly IPen BorderPen = new Pen(new SolidColorBrush(Color.Parse("#D7DEE9")), 1);
    private static readonly IPen LinePen = new Pen(LineBrush, 3);
    private static readonly IPen AlternativeLinePen = new Pen(AlternativeLineBrush, 2);
    private static readonly IPen ThinLinePen = new Pen(new SolidColorBrush(Color.Parse("#A7C7EE")), 1);
    private static readonly IPen NodePen = new Pen(LineBrush, 1.4);
    private static readonly IPen AlternativeNodePen = new Pen(AlternativeLineBrush, 1.4);
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
        DrawLabel(context, lineLength > 0 ? $"линия {lineLength:0.##} м" : "линия не задана", new Point(width - padding - 145, surfaceY + 12), 11, false, MutedTextBrush);

        var shape = SelectedShapeStore.Current?.Shape;
        var alternative = MooringAlternativeShapeStore.Current;
        if (shape is { Nodes.Count: >= 2 })
        {
            DrawEngineeringComparison(context, shape, alternative, vm, width, surfaceY, bottomY, usableHeight, padding);
            return;
        }

        var parsedNodes = ParseCalculatedNodes(vm?.ReportText);
        if (parsedNodes.Count >= 2)
        {
            DrawCalculatedLine(context, parsedNodes, vm, depth, offset, lineLength, BuoyShapeState.Unknown, width, surfaceY, bottomY, usableHeight, padding, fromEngineeringCore: false);
        }
        else
        {
            DrawFallbackLine(context, vm, depth, offset, width, surfaceY, bottomY, padding);
        }
    }

    private static void DrawEngineeringComparison(
        DrawingContext context,
        MooringShapeResult shape,
        MooringAlternativeShapeDisplayData? alternative,
        MainWindowViewModel? vm,
        double width,
        double surfaceY,
        double bottomY,
        double usableHeight,
        double padding)
    {
        var mainNodes = shape.Nodes.Select(x => new CalculatedNode(x.Number, x.XOffsetM, x.ZDepthM, x.Label)).ToList();
        var altNodes = alternative?.Shape.Rows.Select(x => new CalculatedNode(x.Number, x.XOffsetM, x.ZDepthM, x.SourceElement)).ToList() ?? new List<CalculatedNode>();
        var allNodes = mainNodes.Concat(altNodes).ToList();

        var minNodeX = allNodes.Min(x => x.X);
        var maxNodeX = allNodes.Max(x => x.X);
        var maxNodeZ = Math.Max(0.0001, allNodes.Max(x => x.Z));
        var drawingDepth = Math.Max(1, Math.Max(shape.DepthM, maxNodeZ));
        var horizontalSpanM = Math.Max(0.0001, maxNodeX - minNodeX);
        var zScale = usableHeight / drawingDepth;
        var xScale = zScale;
        var spanX = horizontalSpanM * xScale;
        var startX = width / 2.0 - spanX / 2.0;
        startX = Math.Clamp(startX, padding + 80, width - padding - 80 - spanX);

        Point Map(double x, double z) => new(
            startX + (x - minNodeX) * xScale,
            surfaceY + Math.Clamp(z, 0, drawingDepth) * zScale);

        var mainPoints = mainNodes.Select(x => Map(x.X, x.Z)).ToList();
        var altPoints = altNodes.Select(x => Map(x.X, x.Z)).ToList();

        DrawLinePath(context, mainPoints, LinePen);
        if (altPoints.Count >= 2)
        {
            DrawLinePath(context, altPoints, AlternativeLinePen);
        }

        var mainBuoyPoint = mainPoints[0];
        var mainAnchorPoint = mainPoints[^1];
        context.DrawLine(ThinLinePen, mainAnchorPoint, new Point(mainAnchorPoint.X, bottomY));

        var nodeStep = Math.Max(1, mainPoints.Count / 24);
        for (var i = 1; i < mainPoints.Count - 1; i += nodeStep)
        {
            context.DrawEllipse(NodeBrush, NodePen, mainPoints[i], 4.2, 4.2);
        }

        if (alternative is not null && altPoints.Count >= 2)
        {
            DrawAlternativeDiscreteNodes(context, alternative.DiscreteNodes, Map);
            var altAnchorPoint = altPoints[^1];
            context.DrawLine(ThinLinePen, altAnchorPoint, new Point(altAnchorPoint.X, bottomY));
        }

        DrawBuoy(context, mainBuoyPoint, vm?.BuoyName ?? "Буй");
        DrawAnchor(context, mainAnchorPoint, vm?.AnchorName ?? "Якорь");

        DrawLabel(context, "основная форма: MooringShapeSolver", new Point(padding + 12, surfaceY + 32), 11, true, TextBrush);
        DrawLegendLine(context, new Point(padding + 12, surfaceY + 52), LinePen, "основная X/Z", TextBrush);
        if (alternative is not null && altPoints.Count >= 2)
        {
            DrawLegendLine(context, new Point(padding + 12, surfaceY + 70), AlternativeLinePen, "альтернативная X/Z с дискретными нагрузками", AlternativeLineBrush);
            DrawLabel(context, alternative.Shape.Converged ? "альтернативная форма: OK" : "альтернативная форма: WARNING", new Point(padding + 12, surfaceY + 88), 10, false, alternative.Shape.Converged ? MutedTextBrush : AlternativeLineBrush);
        }
        else
        {
            DrawLabel(context, "альтернативная форма ещё не рассчитана", new Point(padding + 12, surfaceY + 70), 10, false, MutedTextBrush);
        }

        DrawLabel(context, DisplayBuoyState(shape.BuoyState), new Point(width - padding - 210, surfaceY + 32), 10, false, MutedTextBrush);
        DrawLabel(context, "масштаб X=Z, без увеличения по горизонтали", new Point(width - padding - 250, surfaceY + 48), 10, false, MutedTextBrush);

        var y = bottomY + 48;
        var mainOffsetText = $"основной снос {shape.HorizontalOffsetM:0.##} м";
        context.DrawLine(ThinLinePen, new Point(mainBuoyPoint.X, y), new Point(mainAnchorPoint.X, y));
        DrawLabel(context, mainOffsetText, new Point(Math.Min(mainBuoyPoint.X, mainAnchorPoint.X) + 8, y - 18), 11, false, MutedTextBrush);

        if (alternative is not null && altPoints.Count >= 2)
        {
            var altBuoyPoint = altPoints[0];
            var altAnchorPoint = altPoints[^1];
            var y2 = bottomY + 62;
            context.DrawLine(AlternativeLinePen, new Point(altBuoyPoint.X, y2), new Point(altAnchorPoint.X, y2));
            DrawLabel(context, $"альт. снос {alternative.Shape.DiscreteHorizontalOffsetM:0.##} м", new Point(Math.Min(altBuoyPoint.X, altAnchorPoint.X) + 8, y2 - 14), 10, false, AlternativeLineBrush);
        }
    }

    private static void DrawLinePath(DrawingContext context, IReadOnlyList<Point> points, IPen pen)
    {
        for (var i = 1; i < points.Count; i++)
        {
            context.DrawLine(pen, points[i - 1], points[i]);
        }
    }

    private static void DrawAlternativeDiscreteNodes(
        DrawingContext context,
        MooringAlternativeDiscreteNodeResult nodes,
        Func<double, double, Point> map)
    {
        var internalNodes = nodes.Rows
            .Where(x => x.Kind != "Буй" && x.Kind != "Якорь")
            .OrderBy(x => x.PositionAlongLineM)
            .ToList();

        for (var i = 0; i < internalNodes.Count; i++)
        {
            var node = internalNodes[i];
            var point = map(node.AlternativeXOffsetM, node.AlternativeZDepthM);
            context.DrawEllipse(AlternativeNodeBrush, AlternativeNodePen, point, 6, 6);

            if (i < 6)
            {
                DrawLabel(context, Shorten(node.Title, 28), new Point(point.X + 9, point.Y - 7), 9, false, AlternativeLineBrush);
            }
        }
    }

    private static void DrawCalculatedLine(
        DrawingContext context,
        IReadOnlyList<CalculatedNode> nodes,
        MainWindowViewModel? vm,
        double depth,
        double offset,
        double lineLength,
        BuoyShapeState buoyState,
        double width,
        double surfaceY,
        double bottomY,
        double usableHeight,
        double padding,
        bool fromEngineeringCore)
    {
        var minNodeX = nodes.Min(x => x.X);
        var maxNodeX = nodes.Max(x => x.X);
        var maxNodeZ = Math.Max(0.0001, nodes.Max(x => x.Z));
        var drawingDepth = Math.Max(1, depth > 0 ? Math.Max(depth, maxNodeZ) : maxNodeZ);
        var horizontalSpanM = Math.Max(0.0001, maxNodeX - minNodeX);

        var zScale = usableHeight / drawingDepth;
        var xScale = zScale;
        var spanX = horizontalSpanM * xScale;

        var startX = width / 2.0 - spanX / 2.0;
        startX = Math.Clamp(startX, padding + 80, width - padding - 80 - spanX);

        var points = nodes
            .Select(node => new Point(
                startX + (node.X - minNodeX) * xScale,
                surfaceY + Math.Clamp(node.Z, 0, drawingDepth) * zScale))
            .ToList();

        DrawLinePath(context, points, LinePen);

        var buoyPoint = points[0];
        var anchorPoint = points[^1];
        context.DrawLine(ThinLinePen, anchorPoint, new Point(anchorPoint.X, bottomY));

        var nodeStep = Math.Max(1, points.Count / 24);
        for (var i = 1; i < points.Count - 1; i += nodeStep)
        {
            context.DrawEllipse(NodeBrush, NodePen, points[i], 4.2, 4.2);
        }

        DrawBuoy(context, buoyPoint, vm?.BuoyName ?? "Буй");
        DrawAnchor(context, anchorPoint, vm?.AnchorName ?? "Якорь");

        DrawLabel(context, fromEngineeringCore ? "форма из MooringShapeSolver" : "форма из X/Z таблицы", new Point(padding + 12, surfaceY + 32), 11, true, TextBrush);
        DrawLabel(context, DisplayBuoyState(buoyState), new Point(padding + 12, surfaceY + 50), 10, false, MutedTextBrush);
        DrawLabel(context, "масштаб X=Z, без увеличения по горизонтали", new Point(padding + 12, surfaceY + 66), 10, false, MutedTextBrush);

        var offsetText = offset > 0 ? $"снос расчётный {offset:0.##} м" : $"снос по узлам {horizontalSpanM:0.##} м";
        var y = bottomY + 48;
        context.DrawLine(ThinLinePen, new Point(buoyPoint.X, y), new Point(anchorPoint.X, y));
        DrawLabel(context, offsetText, new Point(Math.Min(buoyPoint.X, anchorPoint.X) + 8, y - 18), 11, false, MutedTextBrush);
    }

    private static void DrawFallbackLine(
        DrawingContext context,
        MainWindowViewModel? vm,
        double depth,
        double offset,
        double width,
        double surfaceY,
        double bottomY,
        double padding)
    {
        var maxHorizontalMeters = Math.Max(depth, Math.Max(offset, 1));
        var maxHorizontalPixels = Math.Max(90, width - 2 * padding - 170);
        var offsetPixels = offset > 0 ? Math.Min(maxHorizontalPixels, offset / maxHorizontalMeters * maxHorizontalPixels) : 0;
        var centerX = width / 2.0;
        var buoyX = centerX - offsetPixels / 2.0;
        var anchorX = centerX + offsetPixels / 2.0;
        buoyX = Math.Clamp(buoyX, padding + 80, width - padding - 80);
        anchorX = Math.Clamp(anchorX, padding + 80, width - padding - 80);

        var buoyPoint = new Point(buoyX, surfaceY);
        var lineStartPoint = new Point(buoyX, surfaceY + 14);
        var anchorPoint = new Point(anchorX, bottomY - 8);

        context.DrawLine(LinePen, lineStartPoint, anchorPoint);
        context.DrawLine(ThinLinePen, new Point(anchorPoint.X, bottomY), anchorPoint);

        DrawElementNodes(context, vm, lineStartPoint, anchorPoint);
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
    }

    private static List<CalculatedNode> ParseCalculatedNodes(string? reportText)
    {
        var nodes = new List<CalculatedNode>();
        if (string.IsNullOrWhiteSpace(reportText))
        {
            return nodes;
        }

        var inNodeSection = false;
        foreach (var rawLine in reportText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("## Расчётная форма постановки X/Z", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("## Расчётные узлы линии X/Z", StringComparison.OrdinalIgnoreCase))
            {
                inNodeSection = true;
                continue;
            }

            if (inNodeSection && line.StartsWith("## ", StringComparison.Ordinal))
            {
                break;
            }

            if (!inNodeSection || !line.StartsWith("|", StringComparison.Ordinal) || line.Contains("---"))
            {
                continue;
            }

            var parts = line.Split('|').Select(x => x.Trim()).ToArray();
            if (parts.Length < 7 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                continue;
            }

            if (TryParseNumber(parts[5], out var x) && TryParseNumber(parts[6], out var z))
            {
                nodes.Add(new CalculatedNode(number, x, z, parts[3]));
            }
        }

        return nodes.OrderBy(x => x.Number).ToList();
    }

    private static bool TryParseNumber(string value, out double number)
    {
        value = (value ?? string.Empty).Replace(',', '.');
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out number);
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

    private static string DisplayBuoyState(BuoyShapeState state)
    {
        return state switch
        {
            BuoyShapeState.Surface => "буй у поверхности",
            BuoyShapeState.Submerged => "буй под водой",
            BuoyShapeState.Overloaded => "буй перегружен",
            _ => "состояние буя не определено"
        };
    }

    private static string Shorten(string value, int maxLength)
    {
        value ??= string.Empty;
        return value.Length <= maxLength ? value : value[..Math.Max(0, maxLength - 1)] + "…";
    }

    private sealed record CalculatedNode(int Number, double X, double Z, string Label);
}
