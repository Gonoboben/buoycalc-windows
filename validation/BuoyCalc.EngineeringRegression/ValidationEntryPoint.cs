using System.Text.Json;
using BuoyCalc.Windows.Models;

internal static class ValidationEntryPoint
{
    private const string F4B1DiagnosticStagePath = "f4b1-diagnostic-stage.txt";

    public static int Main(string[] args)
    {
        try
        {
            RunStage(nameof(ShapeLineLengthSourceRegression), ShapeLineLengthSourceRegression.Validate);
            RunStage(nameof(ForceShapeConsistencyRegression), ForceShapeConsistencyRegression.Validate);
            RunStage(nameof(SignedOrientationRegression), SignedOrientationRegression.Validate);
            RunStage(nameof(BoundaryLoadOwnershipRegression), BoundaryLoadOwnershipRegression.Validate);
            RunStage(nameof(ConstantLoadAnalyticalReferenceRegression), ConstantLoadAnalyticalReferenceRegression.Validate);
            RunStage(nameof(PiecewisePointLoadAnalyticalReferenceRegression), PiecewisePointLoadAnalyticalReferenceRegression.Validate);
            RunStage(nameof(BerteauxVectorOverlapRegression), BerteauxVectorOverlapRegression.Validate);
            RunStage(nameof(BerteauxConstitutiveDragBoundaryRegression), BerteauxConstitutiveDragBoundaryRegression.Validate);
            RunStage(nameof(BerteauxPlanarResistanceVectorRegression), BerteauxPlanarResistanceVectorRegression.Validate);
            RunStage(nameof(UniformCurrentReadModelRegression), UniformCurrentReadModelRegression.Validate);
            RunStage(nameof(UniformCurrentReportRegression), UniformCurrentReportRegression.Validate);
            RunStage(nameof(RopeMetadataRegression), RopeMetadataRegression.Validate);
            RunStage(nameof(ProfilePlanarProjectionRegression), ProfilePlanarProjectionRegression.Validate);
            RunStage(nameof(ProfilePlanarProjectionReadModelRegression), ProfilePlanarProjectionReadModelRegression.Validate);
            RunStage(nameof(SegmentPlanarProjectionRegression), SegmentPlanarProjectionRegression.Validate);
            RunStage(nameof(ProfilePlanarProjectionLossRegression), ProfilePlanarProjectionLossRegression.Validate);
            RunStage(nameof(SurfaceBoundaryInfoAnalyzerRegression), SurfaceBoundaryInfoAnalyzerRegression.Validate);
            RunStage(nameof(SurfaceBoundaryInfoDataWiringRegression), SurfaceBoundaryInfoDataWiringRegression.Validate);
            RunStage(nameof(SurfaceBoundaryInfoReportRegression), SurfaceBoundaryInfoReportRegression.Validate);
            RunStage(nameof(SurfaceBoundaryCanonicalMeasurementRegression), SurfaceBoundaryCanonicalMeasurementRegression.Validate);
            RunStage(nameof(SurfaceBoundarySelectedShapeImpactRegression), SurfaceBoundarySelectedShapeImpactRegression.Validate);
            RunStage(nameof(SurfaceBoundaryIterationPathRegression), SurfaceBoundaryIterationPathRegression.Validate);
            RunStage(nameof(SurfaceBoundaryShapeNormalFieldRegression), SurfaceBoundaryShapeNormalFieldRegression.Validate);
            RunStage(nameof(SurfaceBoundaryTopVectorGapRegression), SurfaceBoundaryTopVectorGapRegression.Validate);
            RunStage(nameof(SurfaceBoundaryPerSegmentTraceRegression), SurfaceBoundaryPerSegmentTraceRegression.Validate);
            RunStage(nameof(SurfaceBoundaryGlobalReactionAccountingRegression), SurfaceBoundaryGlobalReactionAccountingRegression.Validate);
            RunStage(nameof(SurfaceBoundaryTensionTraceReadModelRegression), SurfaceBoundaryTensionTraceReadModelRegression.Validate);
            RunStage(nameof(BoundaryConditionedSignedGeometryRegression), BoundaryConditionedSignedGeometryRegression.Validate);
            RunStage(nameof(BoundaryConditionedFeedbackRollupRegression), BoundaryConditionedFeedbackRollupRegression.Validate);
            RunStage(nameof(BoundaryFeedbackIndependentReferenceRegression), BoundaryFeedbackIndependentReferenceRegression.Validate);
            RunStage(nameof(SignedTensionAnalyticalResultantReferenceRegression), SignedTensionAnalyticalResultantReferenceRegression.Validate);
            RunStage(nameof(SignedTensionCanonicalResultantEvidenceRegression), SignedTensionCanonicalResultantEvidenceRegression.Validate);
            RunStage(nameof(SignedTensionDemandDispositionRegression), SignedTensionDemandDispositionRegression.Validate);
            RunStage(nameof(WaveLoadOwnershipRegression), WaveLoadOwnershipRegression.Validate);
            RunStage(nameof(SelectedDesignEnvelopeStateRegression), SelectedDesignEnvelopeStateRegression.Validate);
            RunStage(nameof(DesignEnvelopeReferenceEvidenceRegression), DesignEnvelopeReferenceEvidenceRegression.Validate);
            RunStage(nameof(SelectedDesignTensionDemandAuthorityRegression), SelectedDesignTensionDemandAuthorityRegression.Validate);
            RunStage(nameof(AnchorEndReactionOwnershipRegression), AnchorEndReactionOwnershipRegression.Validate);
            RunStage(nameof(SelectedAnchorReactionStateRegression), SelectedAnchorReactionStateRegression.Validate);
            RunStage(nameof(AnchorHoldingCapacityDispositionRegression), AnchorHoldingCapacityDispositionRegression.Validate);
            RunStage(nameof(AcceptedFinalTensionTraceRetentionRegression), AcceptedFinalTensionTraceRetentionRegression.Validate);
            RunStage(nameof(SelectedLocalElementDemandStateRegression), SelectedLocalElementDemandStateRegression.Validate);
            RunStage(nameof(SelectedLocalStructuralCapacityStateRegression), SelectedLocalStructuralCapacityStateRegression.Validate);
            RunStage(nameof(SelectedEngineeringAssessmentStateRegression), SelectedEngineeringAssessmentStateRegression.Validate);
            RunStage(nameof(TechnicalReportIdempotencyDiagnostic), TechnicalReportIdempotencyDiagnostic.Validate);
            RunStage(nameof(SelectedUserPresentationReadModelRegression), SelectedUserPresentationReadModelRegression.Validate);
            RunStage(nameof(HistoricalGoldenImpactRegression), HistoricalGoldenImpactRegression.Validate);
            RunStage(nameof(SignedCandidateConvergenceTrajectoryRegression), SignedCandidateConvergenceTrajectoryRegression.Validate);
            RunStage(nameof(SignedCandidateDiscreteLoadSemanticsRegression), SignedCandidateDiscreteLoadSemanticsRegression.Validate);
            RunStage(nameof(SignedCandidateShadowArbitrationRegression), SignedCandidateShadowArbitrationRegression.Validate);
            RunStage(nameof(SignedCandidateCoreContractRegression), SignedCandidateCoreContractRegression.Validate);
            RunStage(nameof(SignedCandidateProductionEvaluatorRegression), SignedCandidateProductionEvaluatorRegression.Validate);
            RunStage(nameof(SignedCandidateSnapshotShadowIntegrationRegression), SignedCandidateSnapshotShadowIntegrationRegression.Validate);
            RunStage(nameof(SignedCandidateTypedArbitrationRegression), SignedCandidateTypedArbitrationRegression.Validate);
            RunStage(nameof(SignedCandidateSelectedAuthoritySwitchRegression), SignedCandidateSelectedAuthoritySwitchRegression.Validate);
            RunStage(nameof(SelectedSignedBoundaryStateAvailabilityRegression), SelectedSignedBoundaryStateAvailabilityRegression.Validate);
            RunStage(nameof(SignedScalarDivergenceEvidenceRegression), SignedScalarDivergenceEvidenceRegression.Validate);
            RunStage(nameof(SignedTensionBoundaryOwnershipRegression), SignedTensionBoundaryOwnershipRegression.Validate);
            RunStage(nameof(DownstreamAuthorityOwnershipRegression), DownstreamAuthorityOwnershipRegression.Validate);
            RunStage(nameof(SignedGeometryProductionBlockerFeasibilityRegression), SignedGeometryProductionBlockerFeasibilityRegression.Validate);
            RunStage(nameof(VerticalLimitingForceStateRegression), VerticalLimitingForceStateRegression.Validate);
            RunStage(nameof(ProjectDtoCompatibilityRegression), ProjectDtoCompatibilityRegression.Validate);
            RunStage(nameof(SignedNodeEquilibriumRegression), SignedNodeEquilibriumRegression.Validate);
            RunStage(nameof(FinalIterationDiscreteStateRegression), FinalIterationDiscreteStateRegression.Validate);
            RunStage(nameof(FinalIterationSignedNodeEquilibriumRegression), FinalIterationSignedNodeEquilibriumRegression.Validate);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Engineering validation regression failure:");
            Console.Error.WriteLine(ex);
            return 1;
        }

        return Program.Main(args);
    }

    private static void RunStage(string name, Action action)
    {
        File.WriteAllText(F4B1DiagnosticStagePath, "Validation:" + name + System.Environment.NewLine);
        action();
    }
}

internal static class ProjectDtoCompatibilityRegression
{
    public static void Validate()
    {
        const string legacyJson = "{\"ProjectName\":\"Legacy\",\"WaterDensity\":\"1025\",\"UseCurrentProfile\":\"true\"}";
        var legacy = JsonSerializer.Deserialize<BuoyProjectDto>(legacyJson)
            ?? throw new InvalidOperationException("Project DTO compatibility regression: legacy JSON did not deserialize.");

        if (legacy.PlanarXAxisAzimuthDeg != string.Empty)
            throw new InvalidOperationException("Project DTO compatibility regression: missing optional field must restore as empty string.");

        var source = new BuoyProjectDto
        {
            ProjectName = "Schema",
            WaterDensity = "1025",
            UseCurrentProfile = "true",
            PlanarXAxisAzimuthDeg = "270"
        };
        var json = JsonSerializer.Serialize(source);
        var restored = JsonSerializer.Deserialize<BuoyProjectDto>(json)
            ?? throw new InvalidOperationException("Project DTO compatibility regression: round-trip JSON did not deserialize.");

        if (restored.PlanarXAxisAzimuthDeg != "270")
            throw new InvalidOperationException("Project DTO compatibility regression: optional field did not round-trip.");
        if (restored.UseCurrentProfile != "true" || restored.WaterDensity != "1025")
            throw new InvalidOperationException("Project DTO compatibility regression: existing fields changed during round-trip.");
    }
}
