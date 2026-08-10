using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.ApplicationModel;

/// <summary>
/// Immutable application boundary for one completed engineering calculation pipeline.
///
/// Technical report data and selected engineering X/Z are retained directly in the snapshot.
/// User-facing consumers do not require mutable shape/report store publication.
/// </summary>
public sealed record CalculationSnapshot(
    CalculationResult Result,
    TechnicalReportData TechnicalReportData,
    SelectedShapeReadModel? SelectedShape);

public static class CalculationSnapshotBuilder
{
    public static CalculationSnapshot Build(EnvironmentInput environment, CalculationResult result)
    {
        var data = TechnicalReportDataBuilder.Build(environment, result);
        var selectedShape = SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver);

        return new CalculationSnapshot(
            result,
            data,
            selectedShape);
    }
}
