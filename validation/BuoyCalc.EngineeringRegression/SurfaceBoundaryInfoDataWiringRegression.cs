using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SurfaceBoundaryInfoDataWiringRegression
{
    public static void Validate()
    {
        var environment = new EnvironmentInput(
            1025.0,
            10.0,
            0.8,
            0.0,
            0.0,
            new SeabedPreset("surface-info:wiring", "Synthetic", 1.0, string.Empty));
        var buoy = new BuoyInput("Synthetic buoy", 0.8, 80.0, 0.5, 1.0);
        var rope = new RopePreset(
            "surface-info:wiring-rope",
            "Synthetic rope",
            "Synthetic",
            10.0,
            50.0,
            0.1,
            1.2,
            string.Empty);
        var assembly = new[]
        {
            new AssemblyItemInput(
                AssemblyItemKind.Line,
                "Synthetic line",
                true,
                rope,
                null,
                12.0,
                1,
                0.0,
                0.0,
                0.0,
                1.0)
        };
        var anchor = new AnchorInput(
            "Synthetic anchor",
            "Deadweight",
            "Concrete",
            1000.0,
            0.4,
            1.0);
        var result = BuoyCalculator.Calculate(environment, buoy, assembly, anchor, 2.0);

        var compatibility = CalculationSnapshotBuilder.Build(environment, result);
        var typed = CalculationSnapshotBuilder.Build(environment, buoy, result);

        var compatibilityInfo = compatibility.TechnicalReportData.SurfaceBoundaryInfo;
        if (compatibilityInfo.Available ||
            compatibilityInfo.Classification != MooringSurfaceBoundaryInfoClassification.UnavailableMissingBuoyInput)
        {
            throw new InvalidOperationException(
                $"Surface-boundary data wiring regression: compatibility path must remain unavailable without typed buoy; got {compatibilityInfo.Classification} / Available={compatibilityInfo.Available}.");
        }

        var typedInfo = typed.TechnicalReportData.SurfaceBoundaryInfo;
        if (!typedInfo.Available ||
            typedInfo.Classification == MooringSurfaceBoundaryInfoClassification.UnavailableMissingBuoyInput)
        {
            throw new InvalidOperationException(
                $"Surface-boundary data wiring regression: typed path must reach the INFO analyzer; got {typedInfo.Classification} / Available={typedInfo.Available}.");
        }

        RequireSameSelectedShape(compatibility.SelectedShape, typed.SelectedShape);
    }

    private static void RequireSameSelectedShape(SelectedShapeReadModel? expected, SelectedShapeReadModel? actual)
    {
        if (expected is null || actual is null)
        {
            if (expected is not null || actual is not null)
                throw new InvalidOperationException("Surface-boundary data wiring regression: selected-shape nullability changed.");
            return;
        }

        if (expected.Source != actual.Source ||
            expected.UsesDiscreteLoads != actual.UsesDiscreteLoads ||
            expected.HasGateSelection != actual.HasGateSelection ||
            expected.GateDecision != actual.GateDecision ||
            expected.Shape.Nodes.Count != actual.Shape.Nodes.Count ||
            Math.Abs(expected.Shape.HorizontalOffsetM - actual.Shape.HorizontalOffsetM) > 1e-12 ||
            Math.Abs(expected.Shape.VerticalResidualM - actual.Shape.VerticalResidualM) > 1e-12)
        {
            throw new InvalidOperationException("Surface-boundary data wiring regression: passive INFO wiring changed selected X/Z state.");
        }
    }
}
