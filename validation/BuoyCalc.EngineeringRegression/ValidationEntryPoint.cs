using System.Text.Json;
using BuoyCalc.Windows.Models;

internal static class ValidationEntryPoint
{
    public static int Main(string[] args)
    {
        if (args.Contains("--preflight-outcome-only", StringComparer.Ordinal))
        {
            try
            {
                CompletedRunOutcomeSupportsPreflightPhysicalRejectionWithoutFakeCalculationResultRegression.CompletedRunOutcome_SupportsPreflightPhysicalRejectionWithoutFakeCalculationResult();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("BC-AUD-009 prerequisite targeted regression failure:");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        if (args.Contains("--bc-aud-010-only", StringComparer.Ordinal))
        {
            try
            {
                AcceptedSignedInvalidAnchorPrerequisiteIsTerminalFailureRegression.AcceptedSigned_InvalidAnchorPrerequisite_IsTerminalFailure();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("BC-AUD-010 targeted regression failure:");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        if (args.Contains("--bc-aud-007-only", StringComparer.Ordinal))
        {
            try
            {
                DuplicateCurrentProfileDepthsBlockBeforeCoreRegression.DuplicateCurrentProfileDepths_BlockBeforeCore();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("BC-AUD-007 targeted regression failure:");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        if (args.Contains("--bc-aud-004-only", StringComparer.Ordinal))
        {
            try
            {
                NegativePhysicalInputsBlockBeforeCoreRegression.NegativePhysicalInputs_BlockBeforeCore();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("BC-AUD-004 targeted regression failure:");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        if (args.Contains("--bc-aud-005-only", StringComparer.Ordinal))
        {
            try
            {
                CalculationRunIdentityRegression.CalculationRunIdentity_PropagatesExactlyAcrossArtifacts();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("BC-AUD-005 targeted regression failure:");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        if (args.Contains("--bc-aud-003-only", StringComparer.Ordinal))
        {
            try
            {
                ProjectReplayPersistenceRegression.ProjectReplay_UsesEmbeddedResolvedPresetSnapshotOrFails();
                ProjectReplayPersistenceRegression.MissingPresetId_MustNotFallbackSilently();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("BC-AUD-003 targeted regression failure:");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        try
        {
            MandatoryCurrentProfileRegression.Validate();
            ShapeLineLengthSourceRegression.Validate();
            ForceShapeConsistencyRegression.Validate();
            SignedOrientationRegression.Validate();
            BoundaryLoadOwnershipRegression.Validate();
            ConstantLoadAnalyticalReferenceRegression.Validate();
            PiecewisePointLoadAnalyticalReferenceRegression.Validate();
            BerteauxVectorOverlapRegression.Validate();
            BerteauxConstitutiveDragBoundaryRegression.Validate();
            BerteauxPlanarResistanceVectorRegression.Validate();
            UniformCurrentReadModelRegression.Validate();
            UniformCurrentReportRegression.Validate();
            RopeMetadataRegression.Validate();
            ProfilePlanarProjectionRegression.Validate();
            ProfilePlanarProjectionReadModelRegression.Validate();
            SegmentPlanarProjectionRegression.Validate();
            ProfilePlanarProjectionLossRegression.Validate();
            SurfaceBoundaryInfoAnalyzerRegression.Validate();
            SurfaceBoundaryInfoDataWiringRegression.Validate();
            SurfaceBoundaryInfoReportRegression.Validate();
            SurfaceBoundaryCanonicalMeasurementRegression.Validate();
            SurfaceBoundarySelectedShapeImpactRegression.Validate();
            SurfaceBoundaryIterationPathRegression.Validate();
            SurfaceBoundaryShapeNormalFieldRegression.Validate();
            SurfaceBoundaryTopVectorGapRegression.Validate();
            SurfaceBoundaryPerSegmentTraceRegression.Validate();
            SurfaceBoundaryGlobalReactionAccountingRegression.Validate();
            SurfaceBoundaryTensionTraceReadModelRegression.Validate();
            BoundaryConditionedSignedGeometryRegression.Validate();
            BoundaryConditionedFeedbackRollupRegression.Validate();
            BoundaryFeedbackIndependentReferenceRegression.Validate();
            SignedTensionAnalyticalResultantReferenceRegression.Validate();
            SignedTensionCanonicalResultantEvidenceRegression.Validate();
            SignedTensionDemandDispositionRegression.Validate();
            WaveLoadOwnershipRegression.Validate();
            SelectedDesignEnvelopeStateRegression.Validate();
            DesignEnvelopeReferenceEvidenceRegression.Validate();
            SelectedDesignTensionDemandAuthorityRegression.Validate();
            AnchorEndReactionOwnershipRegression.Validate();
            SelectedAnchorReactionStateRegression.Validate();
            AnchorHoldingCapacityDispositionRegression.Validate();
            AcceptedFinalTensionTraceRetentionRegression.Validate();
            SelectedLocalElementDemandStateRegression.Validate();
            SelectedLocalStructuralCapacityStateRegression.Validate();
            SelectedEngineeringAssessmentStateRegression.Validate();
            SelectedUserPresentationReadModelRegression.Validate();
            SelectedTechnicalReportReadModelRegression.Validate();
            OneRunAllArtifactsSelectedAuthorityExactlyEqualRegression.OneRun_AllArtifacts_SelectedAuthorityExactlyEqual();
            InputMutationInvalidatesLastRunAndBlocksEveryExportRegression.InputMutation_InvalidatesLastRunAndBlocksEveryExport();
            CalculationRunIdentityRegression.CalculationRunIdentity_PropagatesExactlyAcrossArtifacts();
            ProjectReplayPersistenceRegression.ProjectReplay_UsesEmbeddedResolvedPresetSnapshotOrFails();
            ProjectReplayPersistenceRegression.MissingPresetId_MustNotFallbackSilently();
            NegativePhysicalInputsBlockBeforeCoreRegression.NegativePhysicalInputs_BlockBeforeCore();
            DuplicateCurrentProfileDepthsBlockBeforeCoreRegression.DuplicateCurrentProfileDepths_BlockBeforeCore();
            AcceptedSignedInvalidAnchorPrerequisiteIsTerminalFailureRegression.AcceptedSigned_InvalidAnchorPrerequisite_IsTerminalFailure();
            CompletedRunOutcomeSupportsPreflightPhysicalRejectionWithoutFakeCalculationResultRegression.CompletedRunOutcome_SupportsPreflightPhysicalRejectionWithoutFakeCalculationResult();
            HistoricalGoldenImpactRegression.Validate();
            SignedCandidateConvergenceTrajectoryRegression.Validate();
            SignedCandidateDiscreteLoadSemanticsRegression.Validate();
            SignedCandidateShadowArbitrationRegression.Validate();
            SignedCandidateCoreContractRegression.Validate();
            SignedCandidateProductionEvaluatorRegression.Validate();
            SignedCandidateSnapshotShadowIntegrationRegression.Validate();
            SignedCandidateTypedArbitrationRegression.Validate();
            SignedCandidateSelectedAuthoritySwitchRegression.Validate();
            SelectedSignedBoundaryStateAvailabilityRegression.Validate();
            SignedScalarDivergenceEvidenceRegression.Validate();
            SignedTensionBoundaryOwnershipRegression.Validate();
            DownstreamAuthorityOwnershipRegression.Validate();
            SignedGeometryProductionBlockerFeasibilityRegression.Validate();
            VerticalLimitingForceStateRegression.Validate();
            ProjectDtoCompatibilityRegression.Validate();
            SignedNodeEquilibriumRegression.Validate();
            FinalIterationDiscreteStateRegression.Validate();
            FinalIterationSignedNodeEquilibriumRegression.Validate();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Engineering validation regression failure:");
            Console.Error.WriteLine(ex);
            return 1;
        }

        return Program.Main(args);
    }
}

internal static class ProjectDtoCompatibilityRegression
{
    public static void Validate()
    {
        const string legacyJson = "{\"ProjectName\":\"Legacy\",\"WaterDensity\":\"1025\",\"CurrentSpeed\":\"0.5\",\"UseCurrentProfile\":\"false\"}";
        var legacy = JsonSerializer.Deserialize<BuoyProjectDto>(legacyJson)
            ?? throw new InvalidOperationException("Project DTO compatibility regression: legacy JSON did not deserialize.");

        if (legacy.PlanarXAxisAzimuthDeg != string.Empty)
            throw new InvalidOperationException("Project DTO compatibility regression: missing optional field must restore as empty string.");
        if (legacy.CurrentSpeed != "0.5" || legacy.UseCurrentProfile != "false")
            throw new InvalidOperationException("Project DTO compatibility regression: legacy scalar fields must remain readable for migration.");
        if (legacy.AssemblyItems.Any(x => x.ResolvedRopePreset is not null || x.ResolvedConnectorPreset is not null))
            throw new InvalidOperationException("Project DTO compatibility regression: absent resolved snapshots must deserialize as null.");

        var source = new BuoyProjectDto
        {
            ProjectName = "Schema",
            WaterDensity = "1025",
            CurrentSpeed = string.Empty,
            UseCurrentProfile = "true",
            PlanarXAxisAzimuthDeg = "270"
        };
        var json = JsonSerializer.Serialize(source);
        var restored = JsonSerializer.Deserialize<BuoyProjectDto>(json)
            ?? throw new InvalidOperationException("Project DTO compatibility regression: round-trip JSON did not deserialize.");

        if (restored.PlanarXAxisAzimuthDeg != "270")
            throw new InvalidOperationException("Project DTO compatibility regression: optional field did not round-trip.");
        if (restored.UseCurrentProfile != "true" || restored.WaterDensity != "1025" || restored.CurrentSpeed != string.Empty)
            throw new InvalidOperationException("Project DTO compatibility regression: profile-only compatibility fields changed during round-trip.");

        var snapshotSource = new BuoyProjectDto
        {
            AssemblyItems =
            {
                new AssemblyItemDto
                {
                    Kind = "Line",
                    RopePresetId = "user:round-trip",
                    ResolvedRopePreset = new ResolvedRopePresetDto
                    {
                        Id = "user:round-trip",
                        Name = "Round-trip rope",
                        Material = "Polyester",
                        DiameterMm = 12.5,
                        BreakingLoadKn = 60,
                        WeightWaterKgM = 0.2,
                        DragCoefficient = 1.1
                    }
                }
            }
        };
        var snapshotJson = JsonSerializer.Serialize(snapshotSource);
        var snapshotRestored = JsonSerializer.Deserialize<BuoyProjectDto>(snapshotJson)
            ?? throw new InvalidOperationException("Project DTO compatibility regression: snapshot JSON did not deserialize.");
        var rope = snapshotRestored.AssemblyItems.Single().ResolvedRopePreset
            ?? throw new InvalidOperationException("Project DTO compatibility regression: optional resolved snapshot did not round-trip.");
        if (rope.Id != "user:round-trip" || rope.DiameterMm != 12.5 || rope.WeightWaterKgM != 0.2)
            throw new InvalidOperationException("Project DTO compatibility regression: resolved snapshot values changed during round-trip.");
    }
}
