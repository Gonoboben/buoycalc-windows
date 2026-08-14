using BuoyCalc.Windows.ApplicationModel;

internal static class SurfaceBoundarySelectedShapeImpactRegression
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

            var selected = run.Snapshot.SelectedShape
                ?? throw new InvalidOperationException($"Surface-boundary impact regression {scenario.Label}: selected shape missing.");
            var info = run.Snapshot.TechnicalReportData.SurfaceBoundaryInfo;
            if (!info.Solved || info.SolutionState is null)
            {
                throw new InvalidOperationException(
                    $"Surface-boundary impact regression {scenario.Label}: canonical boundary solution missing ({info.Classification}).");
            }

            var selectedAnchorZ = selected.Shape.AnchorPoint?.ZDepthM
                ?? throw new InvalidOperationException($"Surface-boundary impact regression {scenario.Label}: selected anchor node missing.");
            var selectedX = selected.Shape.HorizontalOffsetM;
            var selectedResidual = selected.Shape.VerticalResidualM;
            var boundaryX = info.SolutionState.EndpointXM;
            var boundaryZ = info.SolutionState.EndpointZM;
            var deltaX = boundaryX - selectedX;
            var deltaZ = boundaryZ - selectedAnchorZ;
            double? boundaryToSelectedX = Math.Abs(selectedX) > 1e-12
                ? boundaryX / selectedX
                : null;

            RequireFinite(selectedX, scenario.Label, "selected X");
            RequireFinite(selectedAnchorZ, scenario.Label, "selected anchor Z");
            RequireFinite(selectedResidual, scenario.Label, "selected vertical residual");
            RequireFinite(boundaryX, scenario.Label, "boundary X");
            RequireFinite(boundaryZ, scenario.Label, "boundary Z");
            RequireFinite(deltaX, scenario.Label, "delta X");
            RequireFinite(deltaZ, scenario.Label, "delta Z");
            if (boundaryToSelectedX.HasValue)
                RequireFinite(boundaryToSelectedX.Value, scenario.Label, "boundary/selected X ratio");

            Console.WriteLine(string.Join("|",
                "SURFACE_BOUNDARY_SELECTED_IMPACT",
                scenario.Label,
                $"SelectedSource={Sanitize(selected.Source)}",
                $"UsesDiscreteLoads={selected.UsesDiscreteLoads}",
                $"HasGateSelection={selected.HasGateSelection}",
                $"GateDecision={selected.GateDecision?.ToString() ?? "n/a"}",
                $"SelectedX={Format(selectedX)}",
                $"SelectedAnchorZ={Format(selectedAnchorZ)}",
                $"SelectedVerticalResidual={Format(selectedResidual)}",
                $"BoundaryX={Format(boundaryX)}",
                $"BoundaryZ={Format(boundaryZ)}",
                $"DeltaX={Format(deltaX)}",
                $"DeltaZ={Format(deltaZ)}",
                $"BoundaryToSelectedX={Format(boundaryToSelectedX)}",
                $"BoundaryClass={info.Classification}"));
        }
    }

    private static void RequireFinite(double value, string scenario, string label)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException(
                $"Surface-boundary impact regression {scenario}: non-finite {label}={value:R}.");
        }
    }

    private static string Format(double value) =>
        value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    private static string Format(double? value) =>
        value.HasValue ? Format(value.Value) : "n/a";

    private static string Sanitize(string value) =>
        (value ?? string.Empty).Replace("|", "/");
}
