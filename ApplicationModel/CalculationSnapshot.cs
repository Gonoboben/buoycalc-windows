using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.ApplicationModel;

/// <summary>
/// Immutable application boundary for one completed engineering calculation pipeline.
///
/// Technical report data and selected engineering X/Z are retained directly in the snapshot.
/// User-facing consumers do not require mutable shape/report store publication.
/// Signed-candidate state, typed selected-core decision and validated selected F1-F4
/// engineering authorities are retained for diagnostics and downstream read models.
/// </summary>
public sealed partial record CalculationSnapshot(
    CalculationResult Result,
    TechnicalReportData TechnicalReportData,
    SelectedShapeReadModel? SelectedShape);

public sealed partial record CalculationSnapshot
{
    public MooringSignedCandidateResult? SignedCandidate { get; init; }
    public MooringSelectedShapeResult? ShadowSelectedCore { get; init; }
    public MooringSelectedDesignEnvelopeState? SelectedDesignEnvelope { get; init; }
    public MooringSelectedDesignTensionDemandState? SelectedDesignTensionDemand { get; init; }
    public MooringSelectedAnchorReactionState? SelectedAnchorReaction { get; init; }
    public MooringSelectedLocalElementDemandState? SelectedLocalElementDemand { get; init; }
    public MooringSelectedLocalStructuralCapacityState? SelectedLocalStructuralCapacity { get; init; }
    public MooringSelectedEngineeringAssessmentState? SelectedEngineeringAssessment { get; init; }
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

        // Build the complete legacy read model first so it remains the exact fallback path.
        // Package 5 replaces it only after typed core arbitration selects an accepted
        // SignedBoundaryFeedback result.
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

        selectedShape = SelectedMooringShapeReadModelProjector.Project(
            selectedShape,
            shadowSelectedCore);

        // F4-A retains the validated selected authority chain once per completed snapshot.
        // Downstream presentation consumers are intentionally unchanged in this package.
        var selectedDesignEnvelope = MooringSelectedDesignEnvelopeStateProjector.Project(
            result,
            shadowSelectedCore,
            signedCandidate);
        var selectedDesignTensionDemand = MooringSelectedDesignTensionDemandProjector.Project(
            selectedDesignEnvelope);
        var selectedAnchorReaction = MooringSelectedAnchorReactionStateProjector.Project(
            result,
            selectedDesignEnvelope);
        var selectedLocalElementDemand = MooringSelectedLocalElementDemandStateProjector.Project(
            result,
            data.SequencePositions,
            shadowSelectedCore,
            signedCandidate);
        var selectedLocalStructuralCapacity = MooringSelectedLocalStructuralCapacityStateProjector.Project(
            result,
            selectedLocalElementDemand);
        var selectedEngineeringAssessment = MooringSelectedEngineeringAssessmentStateProjector.Project(
            environment,
            result,
            selectedDesignTensionDemand,
            selectedAnchorReaction,
            selectedLocalStructuralCapacity);

        return new CalculationSnapshot(
            result,
            data,
            selectedShape)
        {
            SignedCandidate = signedCandidate,
            ShadowSelectedCore = shadowSelectedCore,
            SelectedDesignEnvelope = selectedDesignEnvelope,
            SelectedDesignTensionDemand = selectedDesignTensionDemand,
            SelectedAnchorReaction = selectedAnchorReaction,
            SelectedLocalElementDemand = selectedLocalElementDemand,
            SelectedLocalStructuralCapacity = selectedLocalStructuralCapacity,
            SelectedEngineeringAssessment = selectedEngineeringAssessment
        };
    }
}
