using System.Text.Json;
using BuoyCalc.Windows.Models;

internal static class ValidationEntryPoint
{
    public static int Main(string[] args)
    {
        try
        {
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
            HistoricalGoldenImpactRegression.Validate();
            SignedCandidateConvergenceTrajectoryRegression.Validate();
            SignedCandidateDiscreteLoadSemanticsRegression.Validate();
            SignedCandidateShadowArbitrationRegression.Validate();
            SignedCandidateCoreContractRegression.Validate();
            SignedCandidateProductionEvaluatorRegression.Validate();
            SignedCandidateSnapshotShadowIntegrationRegression.Validate();
            SignedCandidateTypedArbitrationRegression.Validate();
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
