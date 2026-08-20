using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.ApplicationModel;

/// <summary>
/// Immutable application boundary for one completed engineering calculation pipeline.
///
/// Technical report data and selected engineering X/Z are retained directly in the snapshot.
/// User-facing consumers do not require mutable shape/report store publication.
/// Signed-candidate/shadow state is diagnostic only until the explicit authority-switch package.
/// </summary>
public sealed partial record CalculationSnapshot(
    CalculationResult Result,
    TechnicalReportData TechnicalReportData,
    SelectedShapeReadModel? SelectedShape);

public sealed partial record CalculationSnapshot
{
    public MooringSignedCandidateResult? SignedCandidate { get; init; }
    public MooringSelectedShapeResult? ShadowSelectedCore { get; init; }
}

public static class CalculationSnapshotBuilder
{
    public static CalculationSnapshot Build(EnvironmentInput environment, CalculationResult result)
    {
        return Build(environment, null, result);
    }

    public static CalculationSnapshot Build(
        EnvironmentInput environment,
        BuoyInput? buoy,
        CalculationResult result)
    {
        var data = TechnicalReportDataBuilder.Build(environment, buoy, result);

        // Preserve the existing user-facing read-model path exactly. Signed candidate state
        // is retained separately below and cannot affect SelectedShape in this package.
        var selectedShape = SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver);

        var currentSelection = MooringPrimaryShapeSelector.Select(data.Shape, data.IterativeSolver);
        MooringSelectedShapeResult? currentSelectedCore = null;
        if (currentSelection.Shape.Nodes.Count >= 2)
        {
            var currentSource = currentSelection.UsesDiscreteLoads
                ? MooringShapeSourceIdentity.IterativeDiscreteSolver
                : MooringShapeSourceIdentity.FallbackShapeSolver;
            currentSelectedCore = MooringSelectedShapeResult.Create(
                currentSelection.Shape,
                currentSource,
                currentSelection.Shape.Converged,
                currentSelection.UsesDiscreteLoads,
                "Typed shadow mirror of the existing production primary-shape selection; user-facing authority is unchanged.");
        }

        var signedCandidate = MooringSignedCandidateEvaluator.Build(
            environment,
            buoy,
            result,
            data.SequencePositions);

        var shadowSelectedCore = MooringSelectedShapeArbitrator.Arbitrate(
            currentSelectedCore,
            signedCandidate);

        return new CalculationSnapshot(
            result,
            data,
            selectedShape)
        {
            SignedCandidate = signedCandidate,
            ShadowSelectedCore = shadowSelectedCore
        };
    }
}
