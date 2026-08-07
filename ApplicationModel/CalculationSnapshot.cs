using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.ApplicationModel;

/// <summary>
/// Immutable application boundary for one completed engineering calculation pipeline.
///
/// This is intentionally transitional: the existing TechnicalReportData pipeline and
/// store publication order are preserved while consumers migrate away from mutable stores.
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
        TechnicalReportStorePublisher.Publish(data);
        var selectedShape = SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver);

        return new CalculationSnapshot(
            result,
            data,
            selectedShape);
    }
}
