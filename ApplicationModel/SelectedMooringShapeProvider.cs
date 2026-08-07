using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.ApplicationModel;

/// <summary>
/// Stateless application boundary for the engineering-selected X/Z shape.
///
/// The provider reuses the existing primary-shape selector and gate semantics directly
/// from immutable calculation pipeline data. It does not read or write mutable stores.
/// </summary>
public static class SelectedMooringShapeProvider
{
    public static SelectedShapeReadModel Build(
        MooringShapeResult fallbackShape,
        MooringIterativeSolverResult iterativeSolver)
    {
        var selection = MooringPrimaryShapeSelector.Select(fallbackShape, iterativeSolver);

        return new SelectedShapeReadModel(
            selection.Shape,
            selection.Source,
            selection.UsesDiscreteLoads,
            true,
            selection.Gate.Decision,
            selection.Gate.DecisionText,
            selection.MethodNote);
    }
}
