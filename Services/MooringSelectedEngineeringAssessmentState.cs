using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public enum MooringEngineeringAssessmentCheckStatus
{
    Ok,
    RequiresReview,
    HardFailure
}

public enum MooringEngineeringAssessmentCheckKind
{
    PositiveBuoyancy,
    LineLength,
    AnchorSubmergedWeight,
    AnchorContact,
    LocalStructuralCapacity,
    AnchorHorizontalCapacity
}

public enum MooringAnchorHorizontalCapacityDisposition
{
    RequiresAdditionalPhysicalModel
}

public sealed record MooringSelectedEngineeringAssessmentCheck(
    MooringEngineeringAssessmentCheckKind Kind,
    MooringEngineeringAssessmentCheckStatus Status,
    string Code,
    string Summary,
    string Detail);

/// <summary>
/// Selected engineering assessment built only from validated F1/F2/F3 selected authorities
/// plus direct calculation-core hard preconditions. It is deliberately separate from legacy
/// CalculationResult.Checks/Verdict/MainRisk until F4-B migrates presentation consumers.
/// </summary>
public sealed record MooringSelectedEngineeringAssessmentState(
    MooringShapeSourceIdentity SourceIdentity,
    IReadOnlyList<MooringSelectedEngineeringAssessmentCheck> Checks,
    string Verdict,
    string MainRiskCode,
    string MainRisk,
    bool HasHardFailure,
    bool RequiresReview,
    double DesignTensionDemandN,
    double DesignTensionDemandKn,
    int? GoverningWeakLinkElementNumber,
    string? GoverningWeakLinkTitle,
    string? GoverningWeakLinkPresetName,
    double? GoverningWeakLinkReserve,
    MooringAnchorContactClassification AnchorContactClassification,
    double AnchorHorizontalDemandN,
    double AnchorSignedNormalReactionN,
    MooringAnchorHorizontalCapacityDisposition AnchorHorizontalCapacityDisposition,
    string MethodNote);

public static class MooringSelectedEngineeringAssessmentStateProjector
{
    public static MooringSelectedEngineeringAssessmentState? Project(
        EnvironmentInput environment,
        CalculationResult result,
        MooringSelectedDesignTensionDemandState? designTension,
        MooringSelectedAnchorReactionState? anchorReaction,
        MooringSelectedLocalStructuralCapacityState? localCapacity)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(result);

        if (designTension is null || anchorReaction is null || localCapacity is null)
            return null;

        RequireSelectedSource(designTension.SourceIdentity, nameof(designTension));
        RequireSelectedSource(anchorReaction.SourceIdentity, nameof(anchorReaction));
        RequireSelectedSource(localCapacity.SourceIdentity, nameof(localCapacity));

        if (designTension.SourceIdentity != anchorReaction.SourceIdentity ||
            designTension.SourceIdentity != localCapacity.SourceIdentity)
        {
            throw new InvalidOperationException(
                "Selected engineering assessment requires one common selected source identity across F1/F2/F3 authorities.");
        }

        if (!FinitePositive(designTension.DemandN) ||
            !FinitePositive(designTension.DemandKn) ||
            designTension.DemandKn != designTension.DemandN / 1000.0)
        {
            throw new InvalidOperationException(
                "Selected engineering assessment requires internally consistent finite positive F1 design tension demand.");
        }

        if (!double.IsFinite(result.NetBuoyancyKg) ||
            !double.IsFinite(result.LineLengthM) ||
            !double.IsFinite(environment.DepthM) ||
            !double.IsFinite(result.AnchorWeightWaterKg))
        {
            throw new InvalidOperationException(
                "Selected engineering assessment requires finite direct hard-precondition inputs.");
        }

        if (anchorReaction.AnchorWeightWaterKg != result.AnchorWeightWaterKg)
        {
            throw new InvalidOperationException(
                "Selected engineering assessment requires exact F2 anchor submerged-weight provenance from CalculationResult.");
        }

        if (localCapacity.WaveHorizontalIncrementN != designTension.WaveHorizontalIncrementN)
        {
            throw new InvalidOperationException(
                "Selected engineering assessment requires exact F1/F3 wave-increment identity.");
        }

        var checks = new List<MooringSelectedEngineeringAssessmentCheck>(6)
        {
            BuildPositiveBuoyancyCheck(result),
            BuildLineLengthCheck(environment, result),
            BuildAnchorSubmergedWeightCheck(result),
            BuildAnchorContactCheck(anchorReaction),
            BuildLocalStructuralCapacityCheck(localCapacity),
            BuildAnchorHorizontalCapacityCheck(anchorReaction)
        };

        var hasHardFailure = checks.Any(x => x.Status == MooringEngineeringAssessmentCheckStatus.HardFailure);
        var requiresReview = checks.Any(x => x.Status == MooringEngineeringAssessmentCheckStatus.RequiresReview);
        var verdict = hasHardFailure
            ? "Не подходит"
            : requiresReview
                ? "Требуется проверка"
                : "Подходит";

        var mainRisk = SelectMainRisk(checks);

        return new MooringSelectedEngineeringAssessmentState(
            designTension.SourceIdentity,
            checks,
            verdict,
            mainRisk.Code,
            mainRisk.Summary,
            hasHardFailure,
            requiresReview,
            designTension.DemandN,
            designTension.DemandKn,
            localCapacity.GoverningElementNumber,
            localCapacity.GoverningTitle,
            localCapacity.GoverningPresetName,
            localCapacity.GoverningReserve,
            anchorReaction.ContactClassification,
            anchorReaction.HorizontalDemandN,
            anchorReaction.SignedNormalReactionN,
            MooringAnchorHorizontalCapacityDisposition.RequiresAdditionalPhysicalModel,
            "Selected pre-v1 engineering assessment: direct buoyancy/line-length/anchor-weight hard preconditions plus validated F2 anchor contact and F3 local structural capacity. F2-C did not validate a horizontal soil/anchor capacity model, so legacy AnchorReserve cannot authorize a selected pass; horizontal anchor capacity remains RequiresAdditionalPhysicalModel. Legacy CalculationResult checks/verdict remain unchanged until F4-B presentation migration.");
    }

    private static MooringSelectedEngineeringAssessmentCheck BuildPositiveBuoyancyCheck(CalculationResult result)
    {
        return result.NetBuoyancyKg > 0.0
            ? Check(
                MooringEngineeringAssessmentCheckKind.PositiveBuoyancy,
                MooringEngineeringAssessmentCheckStatus.Ok,
                "PositiveBuoyancyOk",
                "Положительная чистая плавучесть подтверждена.",
                $"NetBuoyancyKg={result.NetBuoyancyKg:R}.")
            : Check(
                MooringEngineeringAssessmentCheckKind.PositiveBuoyancy,
                MooringEngineeringAssessmentCheckStatus.HardFailure,
                "NonPositiveNetBuoyancy",
                "Чистая плавучесть нулевая или отрицательная.",
                $"NetBuoyancyKg={result.NetBuoyancyKg:R}; поверхностная постановка не проходит базовое условие плавучести.");
    }

    private static MooringSelectedEngineeringAssessmentCheck BuildLineLengthCheck(
        EnvironmentInput environment,
        CalculationResult result)
    {
        var tooShort = environment.DepthM > 0.0 && result.LineLengthM < environment.DepthM;
        return !tooShort
            ? Check(
                MooringEngineeringAssessmentCheckKind.LineLength,
                MooringEngineeringAssessmentCheckStatus.Ok,
                "LineLengthOk",
                "Длина линии не меньше расчётной глубины.",
                $"LineLengthM={result.LineLengthM:R}; DepthM={environment.DepthM:R}.")
            : Check(
                MooringEngineeringAssessmentCheckKind.LineLength,
                MooringEngineeringAssessmentCheckStatus.HardFailure,
                "LineShorterThanDepth",
                "Линия короче расчётной глубины.",
                $"LineLengthM={result.LineLengthM:R}; DepthM={environment.DepthM:R}; поверхностная постановка невозможна в этой геометрии.");
    }

    private static MooringSelectedEngineeringAssessmentCheck BuildAnchorSubmergedWeightCheck(CalculationResult result)
    {
        return result.AnchorWeightWaterKg > 0.0
            ? Check(
                MooringEngineeringAssessmentCheckKind.AnchorSubmergedWeight,
                MooringEngineeringAssessmentCheckStatus.Ok,
                "PositiveAnchorSubmergedWeight",
                "Якорь имеет положительный вес в воде.",
                $"AnchorWeightWaterKg={result.AnchorWeightWaterKg:R}.")
            : Check(
                MooringEngineeringAssessmentCheckKind.AnchorSubmergedWeight,
                MooringEngineeringAssessmentCheckStatus.HardFailure,
                "NonPositiveAnchorSubmergedWeight",
                "Якорь имеет нулевой или отрицательный вес в воде.",
                $"AnchorWeightWaterKg={result.AnchorWeightWaterKg:R}; сжимающий контакт с грунтом не может считаться обеспеченным.");
    }

    private static MooringSelectedEngineeringAssessmentCheck BuildAnchorContactCheck(
        MooringSelectedAnchorReactionState anchorReaction)
    {
        return anchorReaction.ContactClassification switch
        {
            MooringAnchorContactClassification.CompressiveContact => Check(
                MooringEngineeringAssessmentCheckKind.AnchorContact,
                MooringEngineeringAssessmentCheckStatus.Ok,
                "AnchorCompressiveContact",
                "На нижней границе сохраняется сжимающий контакт якоря с грунтом.",
                $"SignedNormalReactionN={anchorReaction.SignedNormalReactionN:R}; UpliftExcessN={anchorReaction.UpliftExcessN:R}. Это rigid-body contact state, а не soil/embedment capacity proof."),

            MooringAnchorContactClassification.ZeroNormalLimit => Check(
                MooringEngineeringAssessmentCheckKind.AnchorContact,
                MooringEngineeringAssessmentCheckStatus.RequiresReview,
                "AnchorZeroNormalLimit",
                "Якорь находится на пределе нулевой нормальной реакции.",
                $"SignedNormalReactionN={anchorReaction.SignedNormalReactionN:R}; требуется инженерная проверка контакта. Это не утверждение о geotechnical uplift capacity."),

            MooringAnchorContactClassification.UpliftSeparation => Check(
                MooringEngineeringAssessmentCheckKind.AnchorContact,
                MooringEngineeringAssessmentCheckStatus.RequiresReview,
                "AnchorUpliftSeparation",
                "Расчётная вертикальная составляющая выводит rigid-body contact state в отрыв.",
                $"SignedNormalReactionN={anchorReaction.SignedNormalReactionN:R}; UpliftExcessN={anchorReaction.UpliftExcessN:R}. Требуется отдельная проверка анкеровки/грунта; F2 не определяет soil/embedment uplift capacity."),

            _ => throw new ArgumentOutOfRangeException(
                nameof(anchorReaction.ContactClassification),
                anchorReaction.ContactClassification,
                null)
        };
    }

    private static MooringSelectedEngineeringAssessmentCheck BuildLocalStructuralCapacityCheck(
        MooringSelectedLocalStructuralCapacityState localCapacity)
    {
        if (!localCapacity.StructuralCapacityCoverageComplete)
        {
            return Check(
                MooringEngineeringAssessmentCheckKind.LocalStructuralCapacity,
                MooringEngineeringAssessmentCheckStatus.RequiresReview,
                "LocalStructuralCapacityIncomplete",
                "Локальная проверка прочности несущих элементов имеет неполное покрытие.",
                $"Expected={localCapacity.ExpectedStructuralElementCount}; Rated={localCapacity.RatedStructuralElementCount}; Incomplete={localCapacity.IncompleteStructuralElementCount}; governing reserve={FormatNullable(localCapacity.GoverningReserve)}.");
        }

        if (localCapacity.InsufficientElementCount > 0)
        {
            return Check(
                MooringEngineeringAssessmentCheckKind.LocalStructuralCapacity,
                MooringEngineeringAssessmentCheckStatus.RequiresReview,
                "LocalStructuralCapacityInsufficient",
                "Минимальный локальный запас несущего элемента меньше единицы.",
                $"Insufficient={localCapacity.InsufficientElementCount}; governing element={localCapacity.GoverningElementNumber?.ToString() ?? "None"}; governing reserve={FormatNullable(localCapacity.GoverningReserve)}.");
        }

        return Check(
            MooringEngineeringAssessmentCheckKind.LocalStructuralCapacity,
            MooringEngineeringAssessmentCheckStatus.Ok,
            "LocalStructuralCapacityOk",
            "Локальная проверка прочности доступных несущих элементов пройдена.",
            $"Expected={localCapacity.ExpectedStructuralElementCount}; Rated={localCapacity.RatedStructuralElementCount}; governing element={localCapacity.GoverningElementNumber?.ToString() ?? "None"}; governing reserve={FormatNullable(localCapacity.GoverningReserve)}.");
    }

    private static MooringSelectedEngineeringAssessmentCheck BuildAnchorHorizontalCapacityCheck(
        MooringSelectedAnchorReactionState anchorReaction)
    {
        return Check(
            MooringEngineeringAssessmentCheckKind.AnchorHorizontalCapacity,
            MooringEngineeringAssessmentCheckStatus.RequiresReview,
            "AnchorHorizontalCapacityRequiresAdditionalPhysicalModel",
            "Горизонтальная удерживающая способность якоря требует отдельной валидированной модели якорь/грунт.",
            $"Validated selected horizontal demand={anchorReaction.HorizontalDemandN:R} N. Legacy AnchorHoldingKg/AnchorReserve remain compatibility-only and are not interpreted as Coulomb μ or embedment capacity.");
    }

    private static MooringSelectedEngineeringAssessmentCheck SelectMainRisk(
        IReadOnlyList<MooringSelectedEngineeringAssessmentCheck> checks)
    {
        var hardPriority = new[]
        {
            MooringEngineeringAssessmentCheckKind.PositiveBuoyancy,
            MooringEngineeringAssessmentCheckKind.LineLength,
            MooringEngineeringAssessmentCheckKind.AnchorSubmergedWeight
        };

        foreach (var kind in hardPriority)
        {
            var hard = checks.First(x => x.Kind == kind);
            if (hard.Status == MooringEngineeringAssessmentCheckStatus.HardFailure)
                return hard;
        }

        var reviewPriority = new[]
        {
            MooringEngineeringAssessmentCheckKind.AnchorContact,
            MooringEngineeringAssessmentCheckKind.LocalStructuralCapacity,
            MooringEngineeringAssessmentCheckKind.AnchorHorizontalCapacity
        };

        foreach (var kind in reviewPriority)
        {
            var review = checks.First(x => x.Kind == kind);
            if (review.Status == MooringEngineeringAssessmentCheckStatus.RequiresReview)
                return review;
        }

        return Check(
            MooringEngineeringAssessmentCheckKind.LocalStructuralCapacity,
            MooringEngineeringAssessmentCheckStatus.Ok,
            "NoCriticalSelectedRisk",
            "Критичных рисков по валидированным selected-authority проверкам не найдено.",
            string.Empty);
    }

    private static MooringSelectedEngineeringAssessmentCheck Check(
        MooringEngineeringAssessmentCheckKind kind,
        MooringEngineeringAssessmentCheckStatus status,
        string code,
        string summary,
        string detail) =>
        new(kind, status, code, summary, detail);

    private static void RequireSelectedSource(MooringShapeSourceIdentity source, string label)
    {
        if (source != MooringShapeSourceIdentity.SignedBoundaryFeedback)
        {
            throw new InvalidOperationException(
                $"Selected engineering assessment requires SignedBoundaryFeedback {label} source identity.");
        }
    }

    private static bool FinitePositive(double value) =>
        double.IsFinite(value) && value > 0.0;

    private static string FormatNullable(double? value) =>
        value.HasValue ? value.Value.ToString("R") : "None";
}
