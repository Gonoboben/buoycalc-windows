using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SurfaceBoundaryTensionTraceReadModelRegression
{
    public static void Validate()
    {
        CalculationResult? firstResult = null;
        MooringSequencePositionResult? firstSequence = null;
        MooringSurfaceBoundaryInfoResult? firstParent = null;

        foreach (var scenario in SurfaceBoundaryCanonicalMeasurementRegression.BuildCanonicalScenarios())
        {
            var run = ApplicationCalculationRunner.Run(
                scenario.Environment,
                scenario.Buoy,
                scenario.Assembly,
                scenario.Anchor,
                scenario.SafetyFactor);
            var data = run.Snapshot.TechnicalReportData;
            var trace = MooringSurfaceBoundaryTensionTraceBuilder.Build(
                run.Result,
                data.SequencePositions,
                data.SurfaceBoundaryInfo);

            if (!trace.Available)
                throw new InvalidOperationException($"Boundary tension-trace regression {scenario.Label}: trace unavailable: {trace.UnavailableReason}");
            if (trace.ParentClassification != data.SurfaceBoundaryInfo.Classification)
                throw new InvalidOperationException($"Boundary tension-trace regression {scenario.Label}: parent classification changed.");
            if (trace.Rows.Count != run.Result.SegmentRows.Count)
                throw new InvalidOperationException($"Boundary tension-trace regression {scenario.Label}: row count {trace.Rows.Count} != segment count {run.Result.SegmentRows.Count}.");
            if (trace.PointLoadCrossings != data.SurfaceBoundaryInfo.SolutionState!.PointLoadCrossings)
                throw new InvalidOperationException($"Boundary tension-trace regression {scenario.Label}: point-load crossing count changed.");
            if (trace.IndeterminateSegmentCount != data.SurfaceBoundaryInfo.SolutionState.IndeterminateSegmentCount)
                throw new InvalidOperationException($"Boundary tension-trace regression {scenario.Label}: indeterminate-segment count changed.");

            Near(data.SurfaceBoundaryInfo.BuoySteadyDragN!.Value, trace.StartHN!.Value, 1e-12, scenario.Label + " start H");
            Near(data.SurfaceBoundaryInfo.Q0N!.Value, trace.StartVN!.Value, 1e-12, scenario.Label + " start V");
            Near(data.SurfaceBoundaryInfo.SolutionState.EndHN, trace.EndHN!.Value, 1e-10, scenario.Label + " terminal H");
            Near(data.SurfaceBoundaryInfo.SolutionState.EndVN, trace.EndVN!.Value, 1e-10, scenario.Label + " terminal V");

            foreach (var row in trace.Rows)
            {
                if (!row.TangentX.HasValue || !row.TangentZ.HasValue || !row.SignedAngleFromDownwardVerticalDeg.HasValue)
                    throw new InvalidOperationException($"Boundary tension-trace regression {scenario.Label}: canonical row {row.SegmentNumber} has unavailable tangent data.");
                Near(1.0, row.TangentX.Value * row.TangentX.Value + row.TangentZ.Value * row.TangentZ.Value, 1e-12, scenario.Label + " tangent norm");
                if (row.PointLoadCrossingsAppliedBeforeSegment < 0 || row.PointLoadCrossingsAppliedBeforeSegment > trace.PointLoadCrossings)
                    throw new InvalidOperationException($"Boundary tension-trace regression {scenario.Label}: invalid cumulative crossing count on segment {row.SegmentNumber}.");
            }

            if (scenario.Label == "A")
            {
                CheckSample(trace.Rows[0], 132.70880000000005, 813.2411402575195, 9.268123037063496, "A first");
                CheckSample(trace.Rows[^1], 564.0943999999984, 770.2487866575126, 36.21731349863753, "A last");
                firstResult = run.Result;
                firstSequence = data.SequencePositions;
                firstParent = data.SurfaceBoundaryInfo;
            }
            else if (scenario.Label == "D")
            {
                CheckSample(trace.Rows[0], 1114.5906470762145, 7381.4580617052725, 8.586720115273092, "D first");
                CheckSample(trace.Rows[^1], 3428.8161852806124, 7059.956848106152, 25.904500519625326, "D last");
            }
        }

        if (firstResult is null || firstSequence is null || firstParent is null)
            throw new InvalidOperationException("Boundary tension-trace regression: canonical A source state missing.");

        var unavailableParent = firstParent with
        {
            Solved = false,
            Q0N = null,
            SolutionState = null
        };
        var unavailable = MooringSurfaceBoundaryTensionTraceBuilder.Build(
            firstResult,
            firstSequence,
            unavailableParent);

        if (unavailable.Available || unavailable.Rows.Count != 0 || unavailable.ParentClassification != firstParent.Classification)
            throw new InvalidOperationException("Boundary tension-trace regression: unavailable parent state must remain unavailable and preserve parent classification.");
        if (!unavailable.MethodNote.Contains("not selected-shape authority", StringComparison.Ordinal))
            throw new InvalidOperationException("Boundary tension-trace regression: method provenance must preserve diagnostic-only authority.");
    }

    private static void CheckSample(
        MooringSurfaceBoundaryTensionTraceRow row,
        double expectedH,
        double expectedV,
        double expectedAngleDeg,
        string label)
    {
        Near(expectedH, row.MidHN, 1e-8, label + " H");
        Near(expectedV, row.MidVN, 1e-8, label + " V");
        Near(expectedAngleDeg, row.SignedAngleFromDownwardVerticalDeg!.Value, 1e-10, label + " angle");
    }

    private static void Near(double expected, double actual, double tolerance, string label)
    {
        if (!double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException($"Boundary tension-trace regression {label}: expected {expected:R}, got {actual:R}, tolerance {tolerance:R}.");
    }
}
