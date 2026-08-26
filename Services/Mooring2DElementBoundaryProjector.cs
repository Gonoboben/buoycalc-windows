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

public enum Mooring2DLabelZone
{
    NearSurface,
    InteriorAbove,
    InteriorBelow,
    NearBottom
}

public sealed record Mooring2DElementMarker(
    int ElementNumber,
    Mooring2DElementMarkerKind MarkerKind,
    string Title,
    double AlongLineM,
    double XOffsetM,
    double ZDepthM,
    Mooring2DLabelZone LabelZone,
    int LabelLane);

/// <summary>
/// Presentation-only projection of retained element s-positions onto the retained selected X/Z shape.
/// No force, tension, line geometry or engineering acceptance state is calculated here.
/// Label zones/lanes are also presentation-only and are shared by the window and PDF renderers.
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
        var raw = new List<(int Number, Mooring2DElementMarkerKind Kind, string Title, double S, double X, double Z)>();

        foreach (var row in positions.Rows.OrderBy(x => x.Number))
        {
            if (row.IsDistributed)
            {
                if (row.EndAlongLineM > 0.0 && row.EndAlongLineM < lineLengthM)
                {
                    var point = ProjectAtS(nodes, row.EndAlongLineM);
                    raw.Add((
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
            raw.Add((
                row.Number,
                markerKind,
                row.Title,
                row.PositionAlongLineM,
                projected.XOffsetM,
                projected.ZDepthM));
        }

        var surfaceLane = 0;
        var bottomLane = 0;
        var interiorIndex = 0;
        var markers = new List<Mooring2DElementMarker>(raw.Count);

        foreach (var marker in raw.OrderBy(x => x.S).ThenBy(x => x.Number))
        {
            var ratio = lineLengthM > 0.0 ? marker.S / lineLengthM : 0.0;
            Mooring2DLabelZone zone;
            int lane;

            if (ratio <= 0.08)
            {
                zone = Mooring2DLabelZone.NearSurface;
                lane = surfaceLane++;
            }
            else if (ratio >= 0.90)
            {
                zone = Mooring2DLabelZone.NearBottom;
                lane = bottomLane++;
            }
            else
            {
                zone = interiorIndex % 2 == 0
                    ? Mooring2DLabelZone.InteriorAbove
                    : Mooring2DLabelZone.InteriorBelow;
                lane = interiorIndex / 2;
                interiorIndex++;
            }

            markers.Add(new Mooring2DElementMarker(
                marker.Number,
                marker.Kind,
                marker.Title,
                marker.S,
                marker.X,
                marker.Z,
                zone,
                lane));
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
