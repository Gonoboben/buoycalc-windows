using System;
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
        var diagramSource = Mooring2DDiagramSourceSelector.Select(vm?.SelectedShape);
        var selectedShape = diagramSource.HasSelectedShape ? diagramSource.SelectedShape : null;
        var depth = Math.Max(0, selectedShape?.Shape.DepthM ?? vm?.VisualizationDepthM ?? 0);
        var lineLength = Math.Max(0, selectedShape?.Shape.LineLengthM ?? vm?.VisualizationLineLengthM ?? 0);

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

        if (selectedShape is null)
        {
            DrawUnavailableState(context, width, surfaceY, padding);
            return;
        }

        DrawSelectedShape(context, selectedShape, vm, width, surfaceY, bottomY, usableHeight, padding);
    }

    private static void DrawSelectedShape(
        DrawingContext context,
        SelectedShapeReadModel selectedShape,
        MainWindowViewModel? vm,
        double width,
        double surfaceY,
        double bottomY,
        double usableHeight,
        double padding)
    {
        var shape = selectedShape.Shape;
        var nodes = shape.Nodes.OrderBy(x => x.Number).ToList();
        var minNodeX = nodes.Min(x => x.XOffsetM);
        var maxNodeX = nodes.Max(x => x.XOffsetM);
        var maxNodeZ = Math.Max(0.0001, nodes.Max(x => x.ZDepthM));
        var drawingDepth = Math.Max(1, Math.Max(shape.DepthM, maxNodeZ));
        var horizontalSpanM = Math.Max(0.0001, maxNodeX - minNodeX);
        var zScale = usableHeight / drawingDepth;
        var xScale = zScale;
        var spanX = horizontalSpanM * xScale;
        var startX = width / 2.0 - spanX / 2.0;

        Point Map(double x, double z) => new(
            startX + (x - minNodeX) * xScale,
            surfaceY + Math.Clamp(z, 0, drawingDepth) * zScale);

        var points = nodes.Select(x => Map(x.XOffsetM, x.ZDepthM)).ToList();
        for (var i = 1; i < points.Count; i++)
        {
            context.DrawLine(LinePen, points[i - 1], points[i]);
        }

        var buoyPoint = points[0];
        var anchorPoint = points[^1];
        context.DrawLine(ThinLinePen, anchorPoint, new Point(anchorPoint.X, bottomY));

        if (vm is not null && vm.ElementRows.Count > 0)
        {
            var markers = Mooring2DElementBoundaryProjector.Project(selectedShape, vm.ElementRows.ToList());
            var markerIndex = 0;
            foreach (var marker in markers)
            {
                DrawElementMarker(context, Map(marker.XOffsetM, marker.ZDepthM), marker, markerIndex++);
            }
        }

        DrawBuoy(context, buoyPoint, vm?.BuoyName ?? "Буй");
        DrawAnchor(context, anchorPoint, vm?.AnchorName ?? "Якорь");

        DrawLabel(context, "выбранная расчётная форма X/Z", new Point(padding + 12, surfaceY + 32), 11, true, TextBrush);
        DrawLabel(
            context,
            $"источник: {selectedShape.SourceDescription}",
            new Point(padding + 12, surfaceY + 50),
            10,
            false,
            MutedTextBrush);
        DrawLabel(context, DisplayBuoyState(shape.BuoyState), new Point(padding + 12, surfaceY + 66), 10, false, MutedTextBrush);
        DrawLabel(context, "масштаб X=Z, без увеличения по горизонтали", new Point(width - padding - 250, surfaceY + 32), 10, false, MutedTextBrush);

        var y = bottomY + 48;
        context.DrawLine(ThinLinePen, new Point(buoyPoint.X, y), new Point(anchorPoint.X, y));
        DrawLabel(context, $"расчётный снос {shape.HorizontalOffsetM:0.##} м", new Point(Math.Min(buoyPoint.X, anchorPoint.X) + 8, y - 18), 11, false, MutedTextBrush);
    }

    private static void DrawElementMarker(
        DrawingContext context,
        Point point,
        Mooring2DElementMarker marker,
        int markerIndex)
    {
        var labelOrigin = markerIndex % 2 == 0
            ? new Point(point.X + 9, point.Y - 16)
            : new Point(point.X + 9, point.Y + 5);

        switch (marker.MarkerKind)
        {
            case Mooring2DElementMarkerKind.LineBoundary:
                context.DrawLine(NodePen, new Point(point.X - 6, point.Y), new Point(point.X + 6, point.Y));
                DrawLabel(context, $"граница: {Shorten(marker.Title, 20)}", labelOrigin, 9.5, false, MutedTextBrush);
                break;

            case Mooring2DElementMarkerKind.Payload:
                context.DrawEllipse(BuoyBrush, NodePen, point, 5.2, 5.2);
                DrawLabel(context, $"прибор: {Shorten(marker.Title, 20)}", labelOrigin, 9.5, true, TextBrush);
                break;

            case Mooring2DElementMarkerKind.Connector:
                context.DrawRectangle(NodeBrush, NodePen, new Rect(point.X - 4.5, point.Y - 4.5, 9, 9), 2, 2);
                DrawLabel(context, $"соединитель: {Shorten(marker.Title, 18)}", labelOrigin, 9.5, true, TextBrush);
                break;

            default:
                context.DrawEllipse(NodeBrush, NodePen, point, 4.5, 4.5);
                DrawLabel(context, Shorten(marker.Title, 20), labelOrigin, 9.5, false, TextBrush);
                break;
        }
    }

    private static void DrawUnavailableState(DrawingContext context, double width, double surfaceY, double padding)
    {
        DrawLabel(context, "Расчётная форма X/Z недоступна", new Point(padding + 24, surfaceY + 72), 13, true, TextBrush);
        DrawLabel(
            context,
            "Выполните расчёт. 2D-схема не строит приблизительную геометрию без выбранных расчётных узлов.",
            new Point(padding + 24, surfaceY + 96),
            10.5,
            false,
            MutedTextBrush);
        DrawLabel(context, "Инженерная линия не отображается до появления selected X/Z.", new Point(Math.Max(padding + 24, width - 410), surfaceY + 120), 10, false, MutedTextBrush);
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
}