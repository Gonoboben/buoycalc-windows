using System;
using System.Collections.Generic;
using System.Linq;

namespace BuoyCalc.Windows.Services;

public sealed record MooringForceShapeConsistencyRow(
    int Number,
    int SegmentNumber,
    string SourceElement,
    double DeltaXM,
    double DeltaZM,
    double GeometryLengthM,
    bool IsAvailable,
    double? GeometricAngleFromVerticalDeg,
    double? ForceAngleFromVerticalDeg,
    double? ForceHorizontalN,
    double? ForceVerticalN,
    double? TensionN,
    double? GeometricHorizontalForceN,
    double? GeometricVerticalForceN,
    double? ResidualHorizontalN,
    double? ResidualVerticalN,
    double? ResidualN,
    double? RelativeResidual,
    double? AngleDifferenceDeg,
    string Status,
    string Note);

public sealed record MooringForceShapeConsistencyResult(
    IReadOnlyList<MooringForceShapeConsistencyRow> Rows,
    int AvailableRowCount,
    int IndeterminateRowCount,
    double? MaxResidualN,
    double? MaxRelativeResidual,
    double? MaxAngleDifferenceDeg,
    int? WorstSegmentNumber,
    string? WorstSourceElement,
    string MethodNote);

public static class MooringForceShapeConsistencyAnalyzer
{
    private const double GeometryFloorM = 1e-12;
    private const double ForceFloorN = 1e-12;

    public static MooringForceShapeConsistencyResult Build(
        MooringShapeProjectionResult projection,
        MooringShapeTensionResult shapeTensions)
    {
        if (projection.Rows.Count == 0)
        {
            return Empty("Нет X/Z-проекций сегментов для force-direction / tangent consistency proxy.");
        }

        var tensionBySegment = shapeTensions.Rows
            .GroupBy(x => x.SegmentNumber)
            .ToDictionary(x => x.Key, x => x.Last());
        var rows = new List<MooringForceShapeConsistencyRow>();

        foreach (var projectionRow in projection.Rows.OrderBy(x => x.Number))
        {
            tensionBySegment.TryGetValue(projectionRow.SegmentNumber, out var tensionRow);
            rows.Add(BuildRow(rows.Count + 1, projectionRow, tensionRow));
        }

        var availableRows = rows.Where(x => x.IsAvailable).ToList();
        var worstRelative = availableRows
            .Where(x => x.RelativeResidual.HasValue)
            .OrderByDescending(x => x.RelativeResidual!.Value)
            .FirstOrDefault();

        return new MooringForceShapeConsistencyResult(
            rows,
            availableRows.Count,
            rows.Count - availableRows.Count,
            MaxOrNull(availableRows.Select(x => x.ResidualN)),
            MaxOrNull(availableRows.Select(x => x.RelativeResidual)),
            MaxOrNull(availableRows.Select(x => x.AngleDifferenceDeg)),
            worstRelative?.SegmentNumber,
            worstRelative?.SourceElement,
            "Candidate A — только force-direction / X-Z-tangent consistency proxy. Он сравнивает magnitude-only cumulative shape-force direction с фактической X/Z-касательной уже построенной формы. Это не signed segment/node equilibrium residual и он не участвует в solver feedback, convergence, primary-shape gate, verdict, anchor или weak-link checks.");
    }

    private static MooringForceShapeConsistencyRow BuildRow(
        int number,
        MooringShapeProjectionRow projectionRow,
        MooringShapeTensionRow? tensionRow)
    {
        var dxM = projectionRow.DeltaXM;
        var dzM = projectionRow.DeltaZM;
        var geometryLengthM = Math.Sqrt(dxM * dxM + dzM * dzM);

        if (tensionRow is null)
        {
            return Indeterminate(
                number,
                projectionRow,
                geometryLengthM,
                "Нет shape-based cumulative tension row для этого сегмента.");
        }

        if (!AllFinite(dxM, dzM, geometryLengthM) || geometryLengthM <= GeometryFloorM)
        {
            return Indeterminate(
                number,
                projectionRow,
                geometryLengthM,
                "X/Z-касательная сегмента вырождена или содержит non-finite geometry.");
        }

        var horizontalForceN = Math.Abs(tensionRow.CumulativeShapeHorizontalForceN);
        var verticalForceN = Math.Abs(tensionRow.CumulativeVerticalForceN);
        var tensionN = Math.Sqrt(horizontalForceN * horizontalForceN + verticalForceN * verticalForceN);

        if (!AllFinite(horizontalForceN, verticalForceN, tensionN) || tensionN <= ForceFloorN)
        {
            return Indeterminate(
                number,
                projectionRow,
                geometryLengthM,
                "Cumulative shape-force vector вырожден или содержит non-finite value; нулевая невязка не подставляется.");
        }

        var tangentHorizontal = Math.Abs(dxM) / geometryLengthM;
        var tangentVertical = Math.Abs(dzM) / geometryLengthM;
        var geometricAngleDeg = Math.Atan2(Math.Abs(dxM), Math.Abs(dzM)) * 180.0 / Math.PI;
        var forceAngleDeg = Math.Atan2(horizontalForceN, verticalForceN) * 180.0 / Math.PI;

        var geometricHorizontalForceN = tensionN * tangentHorizontal;
        var geometricVerticalForceN = tensionN * tangentVertical;
        var residualHorizontalN = horizontalForceN - geometricHorizontalForceN;
        var residualVerticalN = verticalForceN - geometricVerticalForceN;
        var residualN = Math.Sqrt(
            residualHorizontalN * residualHorizontalN +
            residualVerticalN * residualVerticalN);
        var relativeResidual = residualN / Math.Max(tensionN, 1.0);
        var angleDifferenceDeg = Math.Abs(geometricAngleDeg - forceAngleDeg);

        if (!AllFinite(
                tangentHorizontal,
                tangentVertical,
                geometricAngleDeg,
                forceAngleDeg,
                geometricHorizontalForceN,
                geometricVerticalForceN,
                residualHorizontalN,
                residualVerticalN,
                residualN,
                relativeResidual,
                angleDifferenceDeg))
        {
            return Indeterminate(
                number,
                projectionRow,
                geometryLengthM,
                "Consistency proxy получил non-finite derived value; строка не считается доступной.");
        }

        return new MooringForceShapeConsistencyRow(
            number,
            projectionRow.SegmentNumber,
            tensionRow.SourceElement,
            dxM,
            dzM,
            geometryLengthM,
            true,
            geometricAngleDeg,
            forceAngleDeg,
            horizontalForceN,
            verticalForceN,
            tensionN,
            geometricHorizontalForceN,
            geometricVerticalForceN,
            residualHorizontalN,
            residualVerticalN,
            residualN,
            relativeResidual,
            angleDifferenceDeg,
            "INFO",
            "Magnitude-only Candidate A: сравнение направления cumulative shape-force state с фактической X/Z-касательной. Signed WeightWaterKg в исходной модели не изменяется; этот proxy не является signed equilibrium check.");
    }

    private static MooringForceShapeConsistencyRow Indeterminate(
        int number,
        MooringShapeProjectionRow projectionRow,
        double geometryLengthM,
        string note)
    {
        return new MooringForceShapeConsistencyRow(
            number,
            projectionRow.SegmentNumber,
            projectionRow.Label,
            projectionRow.DeltaXM,
            projectionRow.DeltaZM,
            geometryLengthM,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "INDETERMINATE",
            note);
    }

    private static MooringForceShapeConsistencyResult Empty(string note)
    {
        return new MooringForceShapeConsistencyResult(
            Array.Empty<MooringForceShapeConsistencyRow>(),
            0,
            0,
            null,
            null,
            null,
            null,
            null,
            note);
    }

    private static double? MaxOrNull(IEnumerable<double?> values)
    {
        var finite = values
            .Where(x => x.HasValue && double.IsFinite(x.Value))
            .Select(x => x!.Value)
            .ToList();
        return finite.Count == 0 ? null : finite.Max();
    }

    private static bool AllFinite(params double[] values)
    {
        return values.All(double.IsFinite);
    }
}
