using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public enum Mooring2DElementMarkerKind
{
    LineBoundary,
    Connector,
    Payload,
    OtherDiscrete
}

public sealed record Mooring2DElementMarker(
    int ElementNumber,
    Mooring2DElementMarkerKind MarkerKind,
    string Title,
    double AlongLineM,
    double XOffsetM,
    double ZDepthM);

/// <summary>
/// Presentation-only projection of retained element s-positions onto the retained selected X/Z shape.
/// No force, tension, line geometry or engineering acceptance state is calculated here.
/// </summary>
public static class Mooring2DElementBoundaryProjector
{
    public static IReadOnlyList<Mooring2DElementMarker> Project(
        SelectedShapeReadModel selectedShape,
        IReadOnlyList<ElementCalculationDisplayRow> elementRows)
    {
        ArgumentNullException.ThrowIfNull(selectedShape);
        ArgumentNullException.ThrowIfNull(elementRows);

        var nodes = selectedShape.Shape.Nodes.OrderBy(x => x.AlongLineM).ToList();
        if (nodes.Count == 0)
            return Array.Empty<Mooring2DElementMarker>();

        var positions = MooringSequencePositioner.BuildDisplayPositions(elementRows);
        var lineLengthM = Math.Max(0.0, selectedShape.Shape.LineLengthM);
        var markers = new List<Mooring2DElementMarker>();

        foreach (var row in positions.Rows.OrderBy(x => x.Number))
        {
            if (row.IsDistributed)
            {
                if (row.EndAlongLineM > 0.0 && row.EndAlongLineM < lineLengthM)
                {
                    var point = ProjectAtS(nodes, row.EndAlongLineM);
                    markers.Add(new Mooring2DElementMarker(
                        row.Number,
                        Mooring2DElementMarkerKind.LineBoundary,
                        row.Title,
                        row.EndAlongLineM,
                        point.XOffsetM,
                        point.ZDepthM));
                }
                continue;
            }

            if (string.Equals(row.Kind, "Буй", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(row.Kind, "Якорь", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var markerKind = row.Kind switch
            {
                "Соединитель" => Mooring2DElementMarkerKind.Connector,
                "Прибор" => Mooring2DElementMarkerKind.Payload,
                _ => Mooring2DElementMarkerKind.OtherDiscrete
            };
            var projected = ProjectAtS(nodes, row.PositionAlongLineM);
            markers.Add(new Mooring2DElementMarker(
                row.Number,
                markerKind,
                row.Title,
                row.PositionAlongLineM,
                projected.XOffsetM,
                projected.ZDepthM));
        }

        return markers;
    }

    private static MooringShapePoint ProjectAtS(IReadOnlyList<MooringShapePoint> nodes, double alongLineM)
    {
        var s = Math.Clamp(alongLineM, nodes[0].AlongLineM, nodes[^1].AlongLineM);

        for (var i = 1; i < nodes.Count; i++)
        {
            var upper = nodes[i];
            if (upper.AlongLineM < s)
                continue;

            var lower = nodes[i - 1];
            var ds = upper.AlongLineM - lower.AlongLineM;
            if (ds <= 1e-12)
                return upper;

            var t = Math.Clamp((s - lower.AlongLineM) / ds, 0.0, 1.0);
            return new MooringShapePoint(
                lower.Number,
                lower.SegmentNumber,
                lower.Label,
                s,
                lower.XOffsetM + (upper.XOffsetM - lower.XOffsetM) * t,
                lower.ZDepthM + (upper.ZDepthM - lower.ZDepthM) * t,
                lower.SegmentLengthM,
                lower.SegmentAngleFromVerticalDeg,
                lower.SegmentTensionKn,
                lower.Status);
        }

        return nodes[^1];
    }
}