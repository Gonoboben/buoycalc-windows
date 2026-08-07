namespace BuoyCalc.Windows.Services;

internal sealed record PdfDiagramSource(
    SelectedShapeReadModel? SelectedShape,
    bool HasSelectedShape,
    double ShapeOffsetM);

internal static class PdfDiagramSourceSelector
{
    public static PdfDiagramSource Select(string reportText, double visualizationOffsetM)
    {
        var selectedShape = SelectedShapeStore.Current;
        var hasSelectedShape = selectedShape is not null && selectedShape.Shape.Nodes.Count >= 2;
        var shapeOffsetM = hasSelectedShape
            ? selectedShape!.Shape.HorizontalOffsetM
            : 0;

        return new PdfDiagramSource(selectedShape, hasSelectedShape, shapeOffsetM);
    }
}
