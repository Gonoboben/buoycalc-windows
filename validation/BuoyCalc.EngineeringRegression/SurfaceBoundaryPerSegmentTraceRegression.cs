using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Services;

internal static class SurfaceBoundaryPerSegmentTraceRegression
{
    private const double G = 9.80665;
    private const double LengthToleranceM = 1e-9;

    public static void Validate()
    {
        foreach (var scenario in SurfaceBoundaryCanonicalMeasurementRegression.BuildCanonicalScenarios())
        {
            var run = ApplicationCalculationRunner.Run(
                scenario.Environment,
                scenario.Buoy,
                scenario.Assembly,
                scenario.Anchor,
                scenario.SafetyFactor);

            var data = run.Snapshot.TechnicalReportData;
            var boundary = data.SurfaceBoundaryInfo;
            if (!boundary.Solved || boundary.SolutionState is null ||
                !boundary.BuoySteadyDragN.HasValue || !boundary.Q0N.HasValue)
            {
                throw new InvalidOperationException(
                    $"Boundary segment-trace regression {scenario.Label}: solved boundary state missing ({boundary.Classification}).");
            }

            var finalTensions = data.IterativeSolver.FinalDiscreteLoadTensions
                ?? throw new InvalidOperationException($"Boundary segment-trace regression {scenario.Label}: final discrete tensions missing.");
            var finalShape = data.IterativeSolver.FinalDiscreteLoadShape
                ?? throw new InvalidOperationException($"Boundary segment-trace regression {scenario.Label}: final discrete shape missing.");

            var productionBySegment = finalTensions.Rows.ToDictionary(x => x.SegmentNumber);
            var usedAngleBySegment = finalShape.Rows
                .Where(x => x.SegmentLengthM > 0.0)
                .GroupBy(x => x.SegmentNumber)
                .ToDictionary(x => x.Key, x => x.Last().UsedAngleFromVerticalDeg);

            var orderedSequence = data.SequencePositions.Rows.OrderBy(x => x.Number).ToList();
            if (orderedSequence.Count < 2)
                throw new InvalidOperationException($"Boundary segment-trace regression {scenario.Label}: sequence boundaries missing.");
            var topBoundaryNumber = orderedSequence[0].Number;
            var bottomBoundaryNumber = orderedSequence[^1].Number;
            var points = orderedSequence
                .Where(x => x.IsDiscrete && x.Number != topBoundaryNumber && x.Number != bottomBoundaryNumber)
                .OrderBy(x => x.PositionAlongLineM)
                .ThenBy(x => x.Number)
                .ToList();

            var segments = run.Result.SegmentRows.OrderBy(x => x.Number).ToList();
            var trace = new List<TraceRow>(segments.Count);
            var hN = boundary.BuoySteadyDragN.Value;
            var vN = boundary.Q0N.Value;
            var pointIndex = 0;
            var crossings = 0;

            foreach (var segment in segments)
            {
                while (pointIndex < points.Count &&
                       points[pointIndex].PositionAlongLineM <= segment.StartLengthM + LengthToleranceM)
                {
                    hN += points[pointIndex].CurrentForceN;
                    vN -= points[pointIndex].WeightWaterKg * G;
                    pointIndex++;
                    crossings++;
                }

                var boundaryMidH = hN + 0.5 * segment.CurrentForceN;
                var boundaryMidV = vN - 0.5 * segment.WeightWaterKg * G;
                var boundaryMidAngle = AngleFromVerticalDeg(boundaryMidH, boundaryMidV);

                if (!productionBySegment.TryGetValue(segment.Number, out var production))
                    throw new InvalidOperationException($"Boundary segment-trace regression {scenario.Label}: missing production tension row for segment {segment.Number}.");
                if (!usedAngleBySegment.TryGetValue(segment.Number, out var usedAngle))
                    throw new InvalidOperationException($"Boundary segment-trace regression {scenario.Label}: missing used geometry angle for segment {segment.Number}.");

                var rawAngleDifference = boundaryMidAngle - production.DiscreteAngleFromVerticalDeg;
                var usedAngleDifference = boundaryMidAngle - usedAngle;
                var hDifference = boundaryMidH - production.CumulativeHorizontalForceN;
                var vDifference = boundaryMidV - production.CumulativeVerticalForceN;

                RequireFinite(boundaryMidH, scenario.Label, segment.Number, "boundary midpoint H");
                RequireFinite(boundaryMidV, scenario.Label, segment.Number, "boundary midpoint V");
                RequireFinite(boundaryMidAngle, scenario.Label, segment.Number, "boundary midpoint angle");
                RequireFinite(rawAngleDifference, scenario.Label, segment.Number, "raw angle delta");
                RequireFinite(usedAngleDifference, scenario.Label, segment.Number, "used angle delta");

                trace.Add(new TraceRow(
                    segment.Number,
                    segment.EstimatedDepthM,
                    segment.StartLengthM,
                    segment.EndLengthM,
                    boundaryMidH,
                    boundaryMidV,
                    boundaryMidAngle,
                    production.CumulativeHorizontalForceN,
                    production.CumulativeVerticalForceN,
                    production.DiscreteAngleFromVerticalDeg,
                    usedAngle,
                    hDifference,
                    vDifference,
                    rawAngleDifference,
                    usedAngleDifference));

                hN += segment.CurrentForceN;
                vN -= segment.WeightWaterKg * G;
            }

            while (pointIndex < points.Count)
            {
                hN += points[pointIndex].CurrentForceN;
                vN -= points[pointIndex].WeightWaterKg * G;
                pointIndex++;
                crossings++;
            }

            if (crossings != points.Count)
                throw new InvalidOperationException($"Boundary segment-trace regression {scenario.Label}: point-load crossings {crossings} != expected {points.Count}.");
            if (trace.Count != segments.Count)
                throw new InvalidOperationException($"Boundary segment-trace regression {scenario.Label}: trace count {trace.Count} != segment count {segments.Count}.");

            var maxRaw = trace.MaxBy(x => Math.Abs(x.BoundaryMinusRawAngleDeg))!;
            var maxUsed = trace.MaxBy(x => Math.Abs(x.BoundaryMinusUsedAngleDeg))!;
            var maxH = trace.MaxBy(x => Math.Abs(x.BoundaryMinusProductionHN))!;
            var maxV = trace.MaxBy(x => Math.Abs(x.BoundaryMinusProductionVN))!;
            var meanRaw = trace.Average(x => Math.Abs(x.BoundaryMinusRawAngleDeg));
            var meanUsed = trace.Average(x => Math.Abs(x.BoundaryMinusUsedAngleDeg));
            var representative = RepresentativeRows(trace);

            Console.WriteLine(string.Join("|",
                "SURFACE_BOUNDARY_SEGMENT_TRACE",
                scenario.Label,
                $"Segments={trace.Count}",
                $"PointLoads={crossings}",
                $"MeanAbsBoundaryMinusRawAngleDeg={Format(meanRaw)}",
                $"MaxAbsBoundaryMinusRawAngleDeg={Format(Math.Abs(maxRaw.BoundaryMinusRawAngleDeg))}",
                $"MaxRawAtSegment={maxRaw.SegmentNumber}",
                $"MaxRawAtDepth={Format(maxRaw.EstimatedDepthM)}",
                $"MeanAbsBoundaryMinusUsedAngleDeg={Format(meanUsed)}",
                $"MaxAbsBoundaryMinusUsedAngleDeg={Format(Math.Abs(maxUsed.BoundaryMinusUsedAngleDeg))}",
                $"MaxUsedAtSegment={maxUsed.SegmentNumber}",
                $"MaxUsedAtDepth={Format(maxUsed.EstimatedDepthM)}",
                $"MaxAbsHDeltaN={Format(Math.Abs(maxH.BoundaryMinusProductionHN))}",
                $"MaxHAtDepth={Format(maxH.EstimatedDepthM)}",
                $"MaxAbsVDeltaN={Format(Math.Abs(maxV.BoundaryMinusProductionVN))}",
                $"MaxVAtDepth={Format(maxV.EstimatedDepthM)}",
                $"Samples={string.Join(",", representative.Select(FormatSample))}"));
        }
    }

    private static IReadOnlyList<TraceRow> RepresentativeRows(IReadOnlyList<TraceRow> rows)
    {
        if (rows.Count == 0)
            return Array.Empty<TraceRow>();

        var indices = new[]
        {
            0,
            (int)Math.Round((rows.Count - 1) * 0.25),
            (int)Math.Round((rows.Count - 1) * 0.50),
            (int)Math.Round((rows.Count - 1) * 0.75),
            rows.Count - 1
        };
        return indices.Distinct().Select(index => rows[index]).ToList();
    }

    private static string FormatSample(TraceRow row)
    {
        return string.Join(";",
            $"n={row.SegmentNumber}",
            $"z={Format(row.EstimatedDepthM)}",
            $"bH={Format(row.BoundaryMidHN)}",
            $"bV={Format(row.BoundaryMidVN)}",
            $"bAng={Format(row.BoundaryMidAngleDeg)}",
            $"pH={Format(row.ProductionHN)}",
            $"pV={Format(row.ProductionVN)}",
            $"rawAng={Format(row.ProductionRawAngleDeg)}",
            $"usedAng={Format(row.ProductionUsedAngleDeg)}");
    }

    private static double AngleFromVerticalDeg(double h, double v)
    {
        return Math.Atan2(Math.Abs(h), Math.Max(1e-12, Math.Abs(v))) * 180.0 / Math.PI;
    }

    private static void RequireFinite(double value, string scenario, int segment, string label)
    {
        if (!double.IsFinite(value))
            throw new InvalidOperationException($"Boundary segment-trace regression {scenario} segment {segment}: non-finite {label}={value:R}.");
    }

    private static string Format(double value) =>
        value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    private sealed record TraceRow(
        int SegmentNumber,
        double EstimatedDepthM,
        double StartLengthM,
        double EndLengthM,
        double BoundaryMidHN,
        double BoundaryMidVN,
        double BoundaryMidAngleDeg,
        double ProductionHN,
        double ProductionVN,
        double ProductionRawAngleDeg,
        double ProductionUsedAngleDeg,
        double BoundaryMinusProductionHN,
        double BoundaryMinusProductionVN,
        double BoundaryMinusRawAngleDeg,
        double BoundaryMinusUsedAngleDeg);
}
