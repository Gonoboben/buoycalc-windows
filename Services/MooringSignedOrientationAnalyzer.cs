using System;
using System.Collections.Generic;
using System.Linq;

namespace BuoyCalc.Windows.Services;

public sealed record MooringSignedOrientationRow(
    int Number,
    int SegmentNumber,
    string SourceElement,
    double HorizontalForceN,
    double VerticalForceN,
    double TensionN,
    double? TangentX,
    double? TangentZ,
    double? SignedAngleFromVerticalDeg,
    double HistoricalUnsignedAngleFromVerticalDeg,
    string OrientationState);

public sealed record MooringSignedOrientationResult(
    IReadOnlyList<MooringSignedOrientationRow> Rows,
    int AvailableCount,
    int IndeterminateCount,
    string MethodNote);

public static class MooringSignedOrientationAnalyzer
{
    // Numerical degeneracy threshold only; this is not an engineering acceptance tolerance.
    private const double TensionEpsilonN = 1e-9;

    public static MooringSignedOrientationResult Build(IReadOnlyList<SegmentTensionRow> tensionRows)
    {
        if (tensionRows.Count == 0)
        {
            return new MooringSignedOrientationResult(
                Array.Empty<MooringSignedOrientationRow>(),
                0,
                0,
                "Нет сегментных натяжений для signed-orientation диагностики.");
        }

        var rows = tensionRows
            .OrderBy(x => x.Number)
            .Select(BuildRow)
            .ToList();

        return new MooringSignedOrientationResult(
            rows,
            rows.Count(x => x.OrientationState == "Available"),
            rows.Count(x => x.OrientationState != "Available"),
            "INFO-only диагностика сохраняет квадрант результирующего H/V: для ненулевого натяжения top-to-bottom tangent определяется как (H/T, V/T) в координатах +X и +Z вниз. HistoricalUnsignedAngleFromVerticalDeg сохраняется только для сравнения и не является authoritative directional state. Диагностика не участвует в solver, gate, verdict или выборе X/Z.");
    }

    private static MooringSignedOrientationRow BuildRow(SegmentTensionRow row)
    {
        var horizontalForceN = row.CumulativeHorizontalForceN;
        var verticalForceN = row.CumulativeVerticalForceN;
        var tensionN = Math.Sqrt(
            horizontalForceN * horizontalForceN +
            verticalForceN * verticalForceN);

        if (!double.IsFinite(tensionN) || tensionN <= TensionEpsilonN)
        {
            return new MooringSignedOrientationRow(
                row.Number,
                row.Number,
                row.SourceElement,
                horizontalForceN,
                verticalForceN,
                tensionN,
                null,
                null,
                null,
                row.AngleFromVerticalDeg,
                "Indeterminate");
        }

        var tangentX = horizontalForceN / tensionN;
        var tangentZ = verticalForceN / tensionN;
        var signedAngleFromVerticalDeg = Math.Atan2(tangentX, tangentZ) * 180.0 / Math.PI;

        return new MooringSignedOrientationRow(
            row.Number,
            row.Number,
            row.SourceElement,
            horizontalForceN,
            verticalForceN,
            tensionN,
            tangentX,
            tangentZ,
            signedAngleFromVerticalDeg,
            row.AngleFromVerticalDeg,
            "Available");
    }
}
