using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Presentation-only contract shared by the interactive 2D window and the user PDF.
/// It only groups retained selected X/Z state with retained calculated element positions;
/// no engineering quantity is calculated here.
/// </summary>
public sealed record Mooring2DDiagramReadModel(
    SelectedShapeReadModel SelectedShape,
    IReadOnlyList<Mooring2DElementMarker> ElementMarkers,
    string BuoyTitle,
    string AnchorTitle);

public static class Mooring2DDiagramReadModelBuilder
{
    public static Mooring2DDiagramReadModel Build(
        SelectedShapeReadModel selectedShape,
        IReadOnlyList<ElementCalculationDisplayRow> elementRows)
    {
        ArgumentNullException.ThrowIfNull(selectedShape);
        ArgumentNullException.ThrowIfNull(elementRows);

        var buoyTitle = elementRows
            .OrderBy(x => x.Number)
            .FirstOrDefault(x => string.Equals(x.Kind, "Буй", StringComparison.OrdinalIgnoreCase))
            ?.Title;
        var anchorTitle = elementRows
            .OrderBy(x => x.Number)
            .FirstOrDefault(x => string.Equals(x.Kind, "Якорь", StringComparison.OrdinalIgnoreCase))
            ?.Title;

        return new Mooring2DDiagramReadModel(
            selectedShape,
            Mooring2DElementBoundaryProjector.Project(selectedShape, elementRows),
            string.IsNullOrWhiteSpace(buoyTitle) ? "Буй" : buoyTitle.Trim(),
            string.IsNullOrWhiteSpace(anchorTitle) ? "Якорь" : anchorTitle.Trim());
    }
}
