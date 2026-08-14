using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SurfaceBoundaryShapeNormalFieldRegression
{
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
            var selected = run.Snapshot.SelectedShape
                ?? throw new InvalidOperationException($"Shape-normal boundary regression {scenario.Label}: selected shape missing.");
            var originalInfo = data.SurfaceBoundaryInfo;
            if (!originalInfo.Solved || originalInfo.SolutionState is null || !originalInfo.BuoySteadyDragN.HasValue)
            {
                throw new InvalidOperationException(
                    $"Shape-normal boundary regression {scenario.Label}: original boundary solution missing ({originalInfo.Classification}).");
            }

            var forceBySegment = data.ShapeForces.Rows.ToDictionary(x => x.SegmentNumber);
            var alternateSegments = run.Result.SegmentRows
                .Select(segment =>
                {
                    if (!forceBySegment.TryGetValue(segment.Number, out var force))
                    {
                        throw new InvalidOperationException(
                            $"Shape-normal boundary regression {scenario.Label}: missing shape force for segment {segment.Number}.");
                    }

                    return segment with { CurrentForceN = force.ShapeForceN };
                })
                .ToList();

            var alternateLineForceN = alternateSegments.Sum(x => x.CurrentForceN);
            var alternateCurrentForceN =
                originalInfo.BuoySteadyDragN.Value +
                alternateLineForceN +
                data.SequencePositions.DiscreteCurrentForceN;
            var alternateResult = run.Result with
            {
                CurrentForceN = alternateCurrentForceN,
                HorizontalForceN = alternateCurrentForceN + run.Result.WaveForceN,
                SegmentRows = alternateSegments
            };

            var shapeNormalInfo = MooringSurfaceBoundaryInfoAnalyzer.Build(
                scenario.Environment,
                scenario.Buoy,
                alternateResult,
                data.SequencePositions);
            if (!shapeNormalInfo.Solved || shapeNormalInfo.SolutionState is null)
            {
                throw new InvalidOperationException(
                    $"Shape-normal boundary regression {scenario.Label}: alternate boundary solution missing ({shapeNormalInfo.Classification}).");
            }

            var originalBoundaryX = originalInfo.SolutionState.EndpointXM;
            var shapeNormalBoundaryX = shapeNormalInfo.SolutionState.EndpointXM;
            var selectedX = selected.Shape.HorizontalOffsetM;
            var fieldDeltaX = shapeNormalBoundaryX - originalBoundaryX;
            var remainingToSelectedX = shapeNormalBoundaryX - selectedX;
            var originalLineForceN = run.Result.SegmentRows.Sum(x => x.CurrentForceN);

            RequireFinite(originalBoundaryX, scenario.Label, "original boundary X");
            RequireFinite(shapeNormalBoundaryX, scenario.Label, "shape-normal boundary X");
            RequireFinite(selectedX, scenario.Label, "selected X");
            RequireFinite(fieldDeltaX, scenario.Label, "shape-normal minus original boundary X");
            RequireFinite(remainingToSelectedX, scenario.Label, "shape-normal boundary minus selected X");
            RequireFinite(alternateLineForceN, scenario.Label, "shape-normal line force");
            RequireFinite(alternateCurrentForceN, scenario.Label, "shape-normal total current force");

            Console.WriteLine(string.Join("|",
                "SURFACE_BOUNDARY_SHAPE_NORMAL_FIELD",
                scenario.Label,
                $"DbN={Format(originalInfo.BuoySteadyDragN.Value)}",
                $"PointFxN={Format(data.SequencePositions.DiscreteCurrentForceN)}",
                $"OriginalLineForceN={Format(originalLineForceN)}",
                $"ShapeNormalLineForceN={Format(alternateLineForceN)}",
                $"OriginalQ0N={Format(originalInfo.Q0N)}",
                $"ShapeNormalQ0N={Format(shapeNormalInfo.Q0N)}",
                $"OriginalBoundaryX={Format(originalBoundaryX)}",
                $"ShapeNormalBoundaryX={Format(shapeNormalBoundaryX)}",
                $"SelectedX={Format(selectedX)}",
                $"ShapeNormalMinusOriginalBoundary={Format(fieldDeltaX)}",
                $"ShapeNormalMinusSelected={Format(remainingToSelectedX)}",
                $"OriginalClass={originalInfo.Classification}",
                $"ShapeNormalClass={shapeNormalInfo.Classification}"));
        }
    }

    private static void RequireFinite(double value, string scenario, string label)
    {
        if (!double.IsFinite(value))
            throw new InvalidOperationException($"Shape-normal boundary regression {scenario}: non-finite {label}={value:R}.");
    }

    private static string Format(double value) =>
        value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    private static string Format(double? value) =>
        value.HasValue ? Format(value.Value) : "n/a";
}
