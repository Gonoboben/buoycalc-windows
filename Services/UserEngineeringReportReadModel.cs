using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Immutable user-report state projected once from one completed calculation run.
/// It contains no report-rendering calculations and must never be reconstructed
/// by parsing ResultText or TechnicalReportText.
/// </summary>
public sealed record UserEngineeringEnvironmentReadModel(
    double WaterDensityKgM3,
    double EffectiveWaterDensityKgM3,
    double DepthM,
    double EffectiveCurrentSpeedMS,
    IReadOnlyList<CurrentProfilePointInput> CurrentProfile,
    double WaveHeightM,
    double WavePeriodS,
    string SeabedId,
    string SeabedName,
    double SeabedHoldingMultiplier);

public sealed record UserEngineeringBuoyReadModel(
    string Name,
    double VolumeM3,
    double WeightAirKg,
    double ProjectedAreaM2,
    double DragCoefficient);

public sealed record UserEngineeringAnchorReadModel(
    string Name,
    string Type,
    string Material,
    double WeightAirKg,
    double VolumeM3,
    double BaseHoldingCoefficient,
    double WeightWaterKg,
    double LegacyTypeMultiplier,
    double LegacySeabedMultiplier,
    double LegacyHoldingKg,
    double LegacyRequiredHoldingKg,
    double LegacyReserve);

public sealed record UserEngineeringCalculationSummaryReadModel(
    double BuoyancyKg,
    double TotalWeightWaterKg,
    double NetBuoyancyKg,
    double CurrentForceN,
    double WaveForceN,
    double LegacyHorizontalForceN,
    double SafetyFactor,
    double LineLengthM,
    double LegacyEstimatedOffsetM);

public sealed record UserEngineeringElementReadModel(
    int Number,
    string Kind,
    string Title,
    string PresetName,
    double LengthM,
    int Count,
    double WeightWaterKg,
    double ProjectedAreaM2,
    double DragCoefficient,
    double CurrentForceN,
    double BreakingLoadKn,
    double WorkingLoadKn,
    double LegacyReserve,
    string LegacyStatus);

public sealed record UserEngineeringDesignLoadReadModel(
    MooringShapeSourceIdentity SourceIdentity,
    double DemandN,
    double DemandKn,
    MooringDesignTensionLocationKind LocationKind,
    int? SegmentNumber,
    string? SourceElement,
    double AlongLineM,
    double WaveHorizontalIncrementN,
    double SurfaceDesignHN,
    double SurfaceDesignVN,
    double SurfaceDesignTensionN,
    double AnchorDesignHN,
    double AnchorDesignVN,
    double AnchorDesignTensionN,
    int MaxDesignMidpointSegmentNumber,
    double MaxDesignMidpointTensionN);

public sealed record UserEngineeringAnchorReactionReadModel(
    MooringShapeSourceIdentity SourceIdentity,
    double HorizontalDemandN,
    double UpwardLinePullN,
    double DownwardLinePushN,
    double AnchorWeightWaterKg,
    double AnchorWeightWaterN,
    double SignedNormalReactionN,
    double CompressiveNormalReactionN,
    double UpliftExcessN,
    MooringAnchorContactClassification ContactClassification);

public sealed record UserEngineeringStructuralRowReadModel(
    int ElementNumber,
    string Kind,
    string Title,
    string PresetName,
    int Count,
    bool IsExpectedStructuralElement,
    bool IsCapacityCandidate,
    double StartAlongLineM,
    double EndAlongLineM,
    double PositionAlongLineM,
    double? LocalDesignDemandN,
    double? LocalDesignDemandKn,
    double? BreakingLoadKn,
    double? WorkingLoadKn,
    double? LocalReserve,
    MooringLocalStructuralCapacityStatus Status,
    string Note);

public sealed record UserEngineeringStructuralReadModel(
    MooringShapeSourceIdentity SourceIdentity,
    IReadOnlyList<UserEngineeringStructuralRowReadModel> Rows,
    int ExpectedStructuralElementCount,
    int RatedStructuralElementCount,
    int IncompleteStructuralElementCount,
    int InsufficientElementCount,
    bool CoverageComplete,
    int? GoverningElementNumber,
    string? GoverningTitle,
    string? GoverningPresetName,
    double? GoverningReserve,
    double? GoverningDemandN,
    double? GoverningWorkingLoadKn);

public sealed record UserEngineeringAssessmentCheckReadModel(
    MooringEngineeringAssessmentCheckKind Kind,
    MooringEngineeringAssessmentCheckStatus Status,
    string Code,
    string Summary,
    string Detail);

public sealed record UserEngineeringAssessmentReadModel(
    MooringShapeSourceIdentity SourceIdentity,
    string Verdict,
    string MainRiskCode,
    string MainRisk,
    bool HasHardFailure,
    bool RequiresReview,
    IReadOnlyList<UserEngineeringAssessmentCheckReadModel> Checks,
    MooringAnchorHorizontalCapacityDisposition AnchorHorizontalCapacityDisposition);

public sealed record UserEngineeringReportReadModel(
    string ProjectName,
    UserEngineeringEnvironmentReadModel Environment,
    UserEngineeringBuoyReadModel Buoy,
    UserEngineeringAnchorReadModel Anchor,
    UserEngineeringCalculationSummaryReadModel Calculation,
    IReadOnlyList<UserEngineeringElementReadModel> Elements,
    SelectedShapeReadModel? SelectedShape,
    UserEngineeringDesignLoadReadModel? DesignLoad,
    UserEngineeringAnchorReactionReadModel? AnchorReaction,
    UserEngineeringStructuralReadModel? Structural,
    UserEngineeringAssessmentReadModel? Assessment);

public static class UserEngineeringReportReadModelProjector
{
    public static UserEngineeringReportReadModel Project(
        string projectName,
        EnvironmentInput environment,
        BuoyInput buoy,
        AnchorInput anchor,
        CalculationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(buoy);
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(snapshot);

        var result = snapshot.Result;
        var environmentReadModel = new UserEngineeringEnvironmentReadModel(
            environment.WaterDensityKgM3,
            environment.EffectiveWaterDensityKgM3,
            environment.DepthM,
            environment.EffectiveCurrentSpeedMS,
            environment.EffectiveCurrentProfile.ToArray(),
            environment.WaveHeightM,
            environment.WavePeriodS,
            environment.Seabed.Id,
            environment.Seabed.Name,
            environment.Seabed.HoldingMultiplier);

        var buoyReadModel = new UserEngineeringBuoyReadModel(
            buoy.Name,
            buoy.VolumeM3,
            buoy.WeightKg,
            buoy.ProjectedAreaM2,
            buoy.DragCoefficient);

        var anchorReadModel = new UserEngineeringAnchorReadModel(
            anchor.Name,
            anchor.Type,
            anchor.Material,
            anchor.WeightAirKg,
            anchor.VolumeM3,
            anchor.BaseHoldingCoefficient,
            result.AnchorWeightWaterKg,
            result.AnchorTypeMultiplier,
            result.SeabedHoldingMultiplier,
            result.AnchorHoldingKg,
            result.RequiredAnchorHoldingKg,
            result.AnchorReserve);

        var calculationReadModel = new UserEngineeringCalculationSummaryReadModel(
            result.BuoyancyKg,
            result.TotalWeightWaterKg,
            result.NetBuoyancyKg,
            result.CurrentForceN,
            result.WaveForceN,
            result.HorizontalForceN,
            result.SafetyFactor,
            result.LineLengthM,
            result.EstimatedOffsetM);

        var elements = result.ElementRows
            .OrderBy(x => x.Number)
            .Select(x => new UserEngineeringElementReadModel(
                x.Number,
                x.Kind,
                x.Title,
                x.PresetName,
                x.LengthM,
                x.Count,
                x.WeightWaterKg,
                x.ProjectedAreaM2,
                x.DragCoefficient,
                x.CurrentForceN,
                x.BreakingLoadKn,
                x.WorkingLoadKn,
                x.Reserve,
                x.Status))
            .ToArray();

        return new UserEngineeringReportReadModel(
            string.IsNullOrWhiteSpace(projectName) ? "BuoyCalc Project" : projectName.Trim(),
            environmentReadModel,
            buoyReadModel,
            anchorReadModel,
            calculationReadModel,
            elements,
            snapshot.SelectedShape,
            ProjectDesignLoad(snapshot.SelectedDesignTensionDemand, snapshot.SelectedDesignEnvelope),
            ProjectAnchorReaction(snapshot.SelectedAnchorReaction),
            ProjectStructural(snapshot.SelectedLocalStructuralCapacity),
            ProjectAssessment(snapshot.SelectedEngineeringAssessment));
    }

    private static UserEngineeringDesignLoadReadModel? ProjectDesignLoad(
        MooringSelectedDesignTensionDemandState? demand,
        MooringSelectedDesignEnvelopeState? envelope)
    {
        if (demand is null || envelope is null)
            return null;

        return new UserEngineeringDesignLoadReadModel(
            demand.SourceIdentity,
            demand.DemandN,
            demand.DemandKn,
            demand.LocationKind,
            demand.SegmentNumber,
            demand.SourceElement,
            demand.AlongLineM,
            demand.WaveHorizontalIncrementN,
            envelope.SurfaceDesignHN,
            envelope.SurfaceDesignVN,
            envelope.SurfaceDesignTensionN,
            envelope.AnchorDesignHN,
            envelope.AnchorDesignVN,
            envelope.AnchorDesignTensionN,
            envelope.MaxDesignMidpointSegmentNumber,
            envelope.MaxDesignMidpointTensionN);
    }

    private static UserEngineeringAnchorReactionReadModel? ProjectAnchorReaction(
        MooringSelectedAnchorReactionState? state)
    {
        return state is null
            ? null
            : new UserEngineeringAnchorReactionReadModel(
                state.SourceIdentity,
                state.HorizontalDemandN,
                state.UpwardLinePullN,
                state.DownwardLinePushN,
                state.AnchorWeightWaterKg,
                state.AnchorWeightWaterN,
                state.SignedNormalReactionN,
                state.CompressiveNormalReactionN,
                state.UpliftExcessN,
                state.ContactClassification);
    }

    private static UserEngineeringStructuralReadModel? ProjectStructural(
        MooringSelectedLocalStructuralCapacityState? state)
    {
        if (state is null)
            return null;

        var rows = state.Rows
            .OrderBy(x => x.ElementNumber)
            .Select(x => new UserEngineeringStructuralRowReadModel(
                x.ElementNumber,
                x.Kind,
                x.Title,
                x.PresetName,
                x.Count,
                x.IsExpectedStructuralElement,
                x.IsCapacityCandidate,
                x.StartAlongLineM,
                x.EndAlongLineM,
                x.PositionAlongLineM,
                x.LocalDesignDemandN,
                x.LocalDesignDemandKn,
                x.BreakingLoadKn,
                x.WorkingLoadKn,
                x.LocalReserve,
                x.Status,
                x.Note))
            .ToArray();

        return new UserEngineeringStructuralReadModel(
            state.SourceIdentity,
            rows,
            state.ExpectedStructuralElementCount,
            state.RatedStructuralElementCount,
            state.IncompleteStructuralElementCount,
            state.InsufficientElementCount,
            state.StructuralCapacityCoverageComplete,
            state.GoverningElementNumber,
            state.GoverningTitle,
            state.GoverningPresetName,
            state.GoverningReserve,
            state.GoverningDemandN,
            state.GoverningWorkingLoadKn);
    }

    private static UserEngineeringAssessmentReadModel? ProjectAssessment(
        MooringSelectedEngineeringAssessmentState? state)
    {
        if (state is null)
            return null;

        return new UserEngineeringAssessmentReadModel(
            state.SourceIdentity,
            state.Verdict,
            state.MainRiskCode,
            state.MainRisk,
            state.HasHardFailure,
            state.RequiresReview,
            state.Checks
                .Select(x => new UserEngineeringAssessmentCheckReadModel(
                    x.Kind,
                    x.Status,
                    x.Code,
                    x.Summary,
                    x.Detail))
                .ToArray(),
            state.AnchorHorizontalCapacityDisposition);
    }
}
