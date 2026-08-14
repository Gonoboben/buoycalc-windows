using BuoyCalc.Windows.ApplicationModel;

internal static class SurfaceBoundaryTopVectorGapRegression
{
    private const double G = 9.80665;

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
                    $"Top-vector gap regression {scenario.Label}: boundary solution missing ({boundary.Classification}).");
            }

            var finalTensions = data.IterativeSolver.FinalDiscreteLoadTensions
                ?? throw new InvalidOperationException($"Top-vector gap regression {scenario.Label}: final discrete tensions missing.");
            var finalShape = data.IterativeSolver.FinalDiscreteLoadShape
                ?? throw new InvalidOperationException($"Top-vector gap regression {scenario.Label}: final discrete shape missing.");
            var topRow = finalTensions.Rows.OrderBy(x => x.Number).FirstOrDefault()
                ?? throw new InvalidOperationException($"Top-vector gap regression {scenario.Label}: top discrete-tension row missing.");
            var firstShapeSegment = finalShape.Rows
                .Where(x => x.SegmentLengthM > 0.0)
                .OrderBy(x => x.Number)
                .FirstOrDefault()
                ?? throw new InvalidOperationException($"Top-vector gap regression {scenario.Label}: first final-shape segment missing.");

            var boundaryH = boundary.BuoySteadyDragN.Value;
            var boundaryV = boundary.Q0N.Value;
            var internalH = topRow.CumulativeHorizontalForceN;
            var internalV = topRow.CumulativeVerticalForceN;
            var boundaryAngleDeg = AngleFromVerticalDeg(boundaryH, boundaryV);
            var internalAngleDeg = topRow.DiscreteAngleFromVerticalDeg;
            var usedAngleDeg = firstShapeSegment.UsedAngleFromVerticalDeg;
            var hRatio = Math.Abs(boundaryH) > 1e-12 ? internalH / boundaryH : (double?)null;
            var vRatio = Math.Abs(boundaryV) > 1e-12 ? internalV / boundaryV : (double?)null;
            var internalMagnitudeN = Math.Sqrt(internalH * internalH + internalV * internalV);
            var boundaryMagnitudeN = Math.Sqrt(boundaryH * boundaryH + boundaryV * boundaryV);
            var magnitudeRatio = boundaryMagnitudeN > 1e-12 ? internalMagnitudeN / boundaryMagnitudeN : (double?)null;
            var totalSignedInternalWeightN =
                run.Result.SegmentRows.Sum(x => x.WeightWaterKg) * G +
                data.SequencePositions.DiscreteWeightWaterKg * G;

            RequireFinite(boundaryH, scenario.Label, "boundary H");
            RequireFinite(boundaryV, scenario.Label, "boundary V");
            RequireFinite(internalH, scenario.Label, "internal cumulative H");
            RequireFinite(internalV, scenario.Label, "internal cumulative V");
            RequireFinite(boundaryAngleDeg, scenario.Label, "boundary angle");
            RequireFinite(internalAngleDeg, scenario.Label, "internal angle");
            RequireFinite(usedAngleDeg, scenario.Label, "used first-segment angle");
            RequireFinite(finalShape.AngleScale, scenario.Label, "final discrete angle scale");
            RequireFinite(totalSignedInternalWeightN, scenario.Label, "total signed internal weight");

            if (Math.Abs(internalV - totalSignedInternalWeightN) > 1e-8)
            {
                throw new InvalidOperationException(
                    $"Top-vector gap regression {scenario.Label}: production top cumulative V={internalV:R} N no longer matches signed line+internal discrete weight={totalSignedInternalWeightN:R} N.");
            }

            Console.WriteLine(string.Join("|",
                "SURFACE_BOUNDARY_TOP_VECTOR_GAP",
                scenario.Label,
                $"BoundaryH_DbN={Format(boundaryH)}",
                $"BoundaryV_Q0N={Format(boundaryV)}",
                $"InternalTopHN={Format(internalH)}",
                $"InternalTopVN={Format(internalV)}",
                $"InternalHOverBoundaryH={Format(hRatio)}",
                $"InternalVOverQ0={Format(vRatio)}",
                $"InternalMagnitudeOverBoundary={Format(magnitudeRatio)}",
                $"BoundaryAngleDeg={Format(boundaryAngleDeg)}",
                $"InternalRawAngleDeg={Format(internalAngleDeg)}",
                $"FinalAngleScale={Format(finalShape.AngleScale)}",
                $"UsedFirstSegmentAngleDeg={Format(usedAngleDeg)}",
                $"BoundaryX={Format(boundary.SolutionState.EndpointXM)}",
                $"SelectedX={Format(run.Snapshot.SelectedShape?.Shape.HorizontalOffsetM)}"));
        }
    }

    private static double AngleFromVerticalDeg(double h, double v)
    {
        return Math.Atan2(Math.Abs(h), Math.Max(1e-12, Math.Abs(v))) * 180.0 / Math.PI;
    }

    private static void RequireFinite(double value, string scenario, string label)
    {
        if (!double.IsFinite(value))
            throw new InvalidOperationException($"Top-vector gap regression {scenario}: non-finite {label}={value:R}.");
    }

    private static string Format(double value) =>
        value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    private static string Format(double? value) =>
        value.HasValue ? Format(value.Value) : "n/a";
}
