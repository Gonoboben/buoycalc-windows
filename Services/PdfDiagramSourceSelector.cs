namespace BuoyCalc.Windows.Services;

internal sealed record PdfDiagramSource(
    SelectedShapeReadModel? SelectedShape,
    bool HasSelectedShape,
    double ShapeOffsetM);

internal static class PdfDiagramSourceSelector
{
    public static PdfDiagramSource Select(SelectedShapeReadModel? selectedShape)
    {
        var hasSelectedShape = selectedShape is not null && selectedShape.Shape.Nodes.Count >= 2;
        var shapeOffsetM = hasSelectedShape ? selectedShape!.Shape.HorizontalOffsetM : 0;
        return new PdfDiagramSource(selectedShape, hasSelectedShape, shapeOffsetM);
    }
}
