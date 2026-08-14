using BuoyCalc.Windows.ApplicationModel;

internal static class SurfaceBoundaryGlobalReactionAccountingRegression
{
    private const double G = 9.80665;
    private const double BalanceToleranceN = 1e-6;

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
                    $"Global reaction accounting regression {scenario.Label}: solved boundary state missing ({boundary.Classification}).");
            }

            var finalTensions = data.IterativeSolver.FinalDiscreteLoadTensions
                ?? throw new InvalidOperationException($"Global reaction accounting regression {scenario.Label}: final discrete tensions missing.");
            var finalShape = data.IterativeSolver.FinalDiscreteLoadShape
                ?? throw new InvalidOperationException($"Global reaction accounting regression {scenario.Label}: final discrete shape missing.");
            var topProduction = finalTensions.Rows.OrderBy(x => x.Number).FirstOrDefault()
                ?? throw new InvalidOperationException($"Global reaction accounting regression {scenario.Label}: production top row missing.");
            var bottomProduction = finalTensions.Rows.OrderBy(x => x.Number).LastOrDefault()
                ?? throw new InvalidOperationException($"Global reaction accounting regression {scenario.Label}: production bottom row missing.");
            var bottomUsed = finalShape.Rows
                .Where(x => x.SegmentLengthM > 0.0)
                .OrderBy(x => x.Number)
                .LastOrDefault()
                ?? throw new InvalidOperationException($"Global reaction accounting regression {scenario.Label}: bottom shape segment missing.");

            var segmentFxN = run.Result.SegmentRows.Sum(x => x.CurrentForceN);
            var pointFxN = data.SequencePositions.DiscreteCurrentForceN;
            var internalFxN = segmentFxN + pointFxN;
            var segmentWeightN = run.Result.SegmentRows.Sum(x => x.WeightWaterKg) * G;
            var pointWeightN = data.SequencePositions.DiscreteWeightWaterKg * G;
            var internalWeightN = segmentWeightN + pointWeightN;

            var expectedTerminalHN = boundary.BuoySteadyDragN.Value + internalFxN;
            var expectedTerminalVN = boundary.Q0N.Value - internalWeightN;
            var terminalHN = boundary.SolutionState.EndHN;
            var terminalVN = boundary.SolutionState.EndVN;
            var terminalAngleDeg = AngleFromVerticalDeg(terminalHN, terminalVN);
            var bottomRawAngleDeg = bottomProduction.DiscreteAngleFromVerticalDeg;
            var bottomUsedAngleDeg = bottomUsed.UsedAngleFromVerticalDeg;
            var terminalVOverQ0 = Math.Abs(boundary.Q0N.Value) > 1e-12
                ? terminalVN / boundary.Q0N.Value
                : (double?)null;
            var terminalMagnitudeN = Math.Sqrt(terminalHN * terminalHN + terminalVN * terminalVN);

            Near(internalFxN, topProduction.CumulativeHorizontalForceN, BalanceToleranceN, scenario.Label, "production top H = internal Fx");
            Near(internalWeightN, topProduction.CumulativeVerticalForceN, BalanceToleranceN, scenario.Label, "production top V = internal signed weight");
            Near(expectedTerminalHN, terminalHN, BalanceToleranceN, scenario.Label, "boundary terminal H accounting");
            Near(expectedTerminalVN, terminalVN, BalanceToleranceN, scenario.Label, "boundary terminal V accounting");
            Near(run.Result.CurrentForceN, terminalHN, BalanceToleranceN, scenario.Label, "boundary terminal H = steady CurrentForceN");
            Near(boundary.Q0N.Value, internalWeightN + terminalVN, BalanceToleranceN, scenario.Label, "Q0 vertical partition");

            RequireFinite(terminalAngleDeg, scenario.Label, "terminal angle");
            RequireFinite(bottomRawAngleDeg, scenario.Label, "production bottom raw angle");
            RequireFinite(bottomUsedAngleDeg, scenario.Label, "production bottom used angle");
            RequireFinite(terminalMagnitudeN, scenario.Label, "terminal magnitude");

            Console.WriteLine(string.Join("|",
                "SURFACE_BOUNDARY_GLOBAL_REACTION",
                scenario.Label,
                $"DbN={Format(boundary.BuoySteadyDragN.Value)}",
                $"InternalFxN={Format(internalFxN)}",
                $"SteadyCurrentForceN={Format(run.Result.CurrentForceN)}",
                $"TerminalHN={Format(terminalHN)}",
                $"Q0N={Format(boundary.Q0N.Value)}",
                $"InternalSignedWeightN={Format(internalWeightN)}",
                $"TerminalVN={Format(terminalVN)}",
                $"TerminalVOverQ0={Format(terminalVOverQ0)}",
                $"TerminalMagnitudeN={Format(terminalMagnitudeN)}",
                $"TerminalAngleDeg={Format(terminalAngleDeg)}",
                $"ProductionTopHN={Format(topProduction.CumulativeHorizontalForceN)}",
                $"ProductionTopVN={Format(topProduction.CumulativeVerticalForceN)}",
                $"ProductionBottomHN={Format(bottomProduction.CumulativeHorizontalForceN)}",
                $"ProductionBottomVN={Format(bottomProduction.CumulativeVerticalForceN)}",
                $"ProductionBottomRawAngleDeg={Format(bottomRawAngleDeg)}",
                $"ProductionBottomUsedAngleDeg={Format(bottomUsedAngleDeg)}"));
        }
    }

    private static double AngleFromVerticalDeg(double h, double v)
    {
        return Math.Atan2(Math.Abs(h), Math.Max(1e-12, Math.Abs(v))) * 180.0 / Math.PI;
    }

    private static void Near(double expected, double actual, double tolerance, string scenario, string label)
    {
        if (!double.IsFinite(expected) || !double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"Global reaction accounting regression {scenario} {label}: expected {expected:R}, got {actual:R}, tolerance {tolerance:R}.");
        }
    }

    private static void RequireFinite(double value, string scenario, string label)
    {
        if (!double.IsFinite(value))
            throw new InvalidOperationException($"Global reaction accounting regression {scenario}: non-finite {label}={value:R}.");
    }

    private static string Format(double value) =>
        value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    private static string Format(double? value) =>
        value.HasValue ? Format(value.Value) : "n/a";
}
