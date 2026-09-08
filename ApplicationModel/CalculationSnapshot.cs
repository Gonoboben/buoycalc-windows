using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.ApplicationModel;

/// <summary>
/// Immutable application boundary for one completed engineering calculation pipeline.
///
/// Technical report data and selected engineering X/Z are retained directly in the snapshot.
/// User-facing consumers do not require mutable shape/report store publication.
/// Signed-candidate state, typed selected-core decision, physical-rejection disposition and
/// validated selected F1-F4 engineering authorities are retained for downstream read models.
/// </summary>
public sealed partial record CalculationSnapshot(
    CalculationResult Result,
    TechnicalReportData TechnicalReportData,
    SelectedShapeReadModel? SelectedShape);

public sealed partial record CalculationSnapshot
{
    public MooringSignedCandidateResult? SignedCandidate { get; init; }
    public MooringSignedPhysicalDispositionState? PhysicalDisposition { get; init; }
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

        // Build the complete legacy read model first so it remains the exact fallback path
        // for non-physical non-Accepted states. RejectedPhysical is handled separately below
        // and must not expose fallback geometry as engineering authority.
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

        var physicalDisposition = MooringSignedPhysicalDispositionStateProjector.Project(
            signedCandidate);

        var shadowSelectedCore = MooringSelectedShapeArbitrator.Arbitrate(
            currentSelectedCore,
            signedCandidate);

        selectedShape = physicalDisposition?.BlocksEngineeringGeometry == true
            ? null
            : SelectedMooringShapeReadModelProjector.Project(
                selectedShape,
                shadowSelectedCore);

        // Accepted SignedBoundaryFeedback retains the validated selected F1-F4 authority chain.
        // RejectedPhysical has no selected geometry and therefore cannot fabricate F1/F2/F3/F4.
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
            PhysicalDisposition = physicalDisposition,
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
