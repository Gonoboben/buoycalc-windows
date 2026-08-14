using BuoyCalc.Windows.ApplicationModel;

internal static class SurfaceBoundaryIterationPathRegression
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
                ?? throw new InvalidOperationException($"Surface-boundary iteration-path regression {scenario.Label}: selected shape missing.");
            var boundary = data.SurfaceBoundaryInfo.SolutionState
                ?? throw new InvalidOperationException($"Surface-boundary iteration-path regression {scenario.Label}: boundary solution missing.");
            var rows = data.IterativeSolver.Rows;
            if (rows.Count == 0)
                throw new InvalidOperationException($"Surface-boundary iteration-path regression {scenario.Label}: iterative rows missing.");

            var fallbackX = data.Shape.HorizontalOffsetM;
            var selectedX = selected.Shape.HorizontalOffsetM;
            var boundaryX = boundary.EndpointXM;
            var iterativeDeltaX = selectedX - fallbackX;
            var boundaryFromFallbackX = boundaryX - fallbackX;
            var boundaryFromSelectedX = boundaryX - selectedX;

            RequireFinite(fallbackX, scenario.Label, "fallback X");
            RequireFinite(selectedX, scenario.Label, "selected X");
            RequireFinite(boundaryX, scenario.Label, "boundary X");
            RequireFinite(iterativeDeltaX, scenario.Label, "selected-fallback X");
            RequireFinite(boundaryFromFallbackX, scenario.Label, "boundary-fallback X");
            RequireFinite(boundaryFromSelectedX, scenario.Label, "boundary-selected X");

            var iterationPath = string.Join(",",
                rows.Select(row => string.Join(";",
                    $"i={row.IterationNumber}",
                    $"in={Format(row.InputOffsetM)}",
                    $"out={Format(row.OutputOffsetM)}",
                    $"dX={Format(row.OffsetChangeM)}",
                    $"lineF={Format(row.ShapeLineForceN)}",
                    $"topShapeKn={Format(row.TopShapeTensionKn)}",
                    $"topDiscreteKn={Format(row.TopDiscreteTensionKn)}",
                    $"stop={row.StopReason}")));

            Console.WriteLine(string.Join("|",
                "SURFACE_BOUNDARY_ITERATION_PATH",
                scenario.Label,
                $"FallbackX={Format(fallbackX)}",
                $"SelectedX={Format(selectedX)}",
                $"BoundaryX={Format(boundaryX)}",
                $"SelectedMinusFallback={Format(iterativeDeltaX)}",
                $"BoundaryMinusFallback={Format(boundaryFromFallbackX)}",
                $"BoundaryMinusSelected={Format(boundaryFromSelectedX)}",
                $"InitialShapeLineForceN={Format(data.ShapeForces.ShapeLineForceN)}",
                $"InitialOriginalLineForceN={Format(data.ShapeForces.OriginalLineForceN)}",
                $"Iterations={rows.Count}",
                $"FinalStop={data.IterativeSolver.StopReason}",
                $"Path={iterationPath}"));
        }
    }

    private static void RequireFinite(double value, string scenario, string label)
    {
        if (!double.IsFinite(value))
            throw new InvalidOperationException($"Surface-boundary iteration-path regression {scenario}: non-finite {label}={value:R}.");
    }

    private static string Format(double value) =>
        value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
}
