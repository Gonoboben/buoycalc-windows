namespace BuoyCalc.Windows.Services;

internal sealed record Mooring2DDiagramSource(
    SelectedShapeReadModel? SelectedShape,
    bool HasSelectedShape);

internal static class Mooring2DDiagramSourceSelector
{
    public static Mooring2DDiagramSource Select()
    {
        var selectedShape = SelectedShapeStore.Current;
        var hasSelectedShape = selectedShape is not null && selectedShape.Shape.Nodes.Count >= 2;
        return new Mooring2DDiagramSource(selectedShape, hasSelectedShape);
    }
}
