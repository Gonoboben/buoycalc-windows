using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Presentation-only projection of an immutable calculation snapshot into the existing
/// UI/PDF element table contract. No engineering quantity is recomputed here.
/// </summary>
public static class SelectedElementCalculationDisplayProjector
{
    public static IReadOnlyList<ElementCalculationDisplayRow> Project(CalculationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var result = snapshot.Result;
        var assessment = snapshot.SelectedEngineeringAssessment;
        var capacity = snapshot.SelectedLocalStructuralCapacity;

        if (assessment is null || capacity is null)
            return result.ElementRows.Select(ElementCalculationDisplayRow.From).ToList();

        var capacityByNumber = capacity.Rows.ToDictionary(x => x.ElementNumber);
        var rows = new List<ElementCalculationDisplayRow>(result.ElementRows.Count);

        foreach (var row in result.ElementRows.OrderBy(x => x.Number))
        {
            if (string.Equals(row.Kind, "Буй", StringComparison.OrdinalIgnoreCase))
            {
                rows.Add(ProjectBuoy(row, assessment));
                continue;
            }

            if (string.Equals(row.Kind, "Якорь", StringComparison.OrdinalIgnoreCase))
            {
                rows.Add(ProjectAnchor(row, assessment));
                continue;
            }

            if (!capacityByNumber.TryGetValue(row.Number, out var selected))
            {
                throw new InvalidOperationException(
                    $"Selected element display cannot join internal element {row.Number} to F3-C local capacity state.");
            }

            if (!string.Equals(row.Kind, selected.Kind, StringComparison.Ordinal) ||
                !string.Equals(row.Title, selected.Title, StringComparison.Ordinal) ||
                !string.Equals(row.PresetName, selected.PresetName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Selected element display identity mismatch at internal element {row.Number}.");
            }

            rows.Add(ProjectInternal(row, selected));
        }

        return rows;
    }

    private static ElementCalculationDisplayRow ProjectBuoy(
        ElementCalculationRow row,
        MooringSelectedEngineeringAssessmentState assessment)
    {
        var check = assessment.Checks.Single(x => x.Kind == MooringEngineeringAssessmentCheckKind.PositiveBuoyancy);
        var status = check.Status == MooringEngineeringAssessmentCheckStatus.HardFailure
            ? "не подходит: чистая плавучесть ≤ 0"
            : "подходит: положительная чистая плавучесть";

        return Base(row) withValues(
            breakingLoadKn: string.Empty,
            workingLoadKn: string.Empty,
            reserve: string.Empty,
            status: status);
    }

    private static ElementCalculationDisplayRow ProjectAnchor(
        ElementCalculationRow row,
        MooringSelectedEngineeringAssessmentState assessment)
    {
        var status = assessment.AnchorContactClassification switch
        {
            MooringAnchorContactClassification.CompressiveContact =>
                "требует проверки: контакт сжат; horizontal capacity требует модели якорь/грунт",
            MooringAnchorContactClassification.ZeroNormalLimit =>
                "требует проверки: предел нулевой нормальной реакции; нужна модель якорь/грунт",
            MooringAnchorContactClassification.UpliftSeparation =>
                "требует проверки: расчётный отрыв; нужна модель якорь/грунт",
            _ => "требует проверки: нужна валидированная модель якорь/грунт"
        };

        var baseRow = Base(row);
        return new ElementCalculationDisplayRow
        {
            Number = baseRow.Number,
            Kind = baseRow.Kind,
            Title = baseRow.Title,
            PresetName = "compatibility holding factors only; " + baseRow.PresetName,
            LengthM = baseRow.LengthM,
            Count = baseRow.Count,
            WeightWaterKg = baseRow.WeightWaterKg,
            ProjectedAreaM2 = baseRow.ProjectedAreaM2,
            DragCoefficient = baseRow.DragCoefficient,
            CurrentForceN = baseRow.CurrentForceN,
            BreakingLoadKn = string.Empty,
            WorkingLoadKn = string.Empty,
            Reserve = string.Empty,
            Status = status
        };
    }

    private static ElementCalculationDisplayRow ProjectInternal(
        ElementCalculationRow row,
        MooringSelectedLocalStructuralCapacityRow selected)
    {
        var baseRow = Base(row);
        return new ElementCalculationDisplayRow
        {
            Number = baseRow.Number,
            Kind = baseRow.Kind,
            Title = baseRow.Title,
            PresetName = baseRow.PresetName,
            LengthM = baseRow.LengthM,
            Count = baseRow.Count,
            WeightWaterKg = baseRow.WeightWaterKg,
            ProjectedAreaM2 = baseRow.ProjectedAreaM2,
            DragCoefficient = baseRow.DragCoefficient,
            CurrentForceN = baseRow.CurrentForceN,
            BreakingLoadKn = Format(selected.BreakingLoadKn),
            WorkingLoadKn = Format(selected.WorkingLoadKn),
            Reserve = Format(selected.LocalReserve),
            Status = CapacityStatusText(selected.Status)
        };
    }

    private static ElementCalculationDisplayRow Base(ElementCalculationRow row) =>
        ElementCalculationDisplayRow.From(row);

    private static ElementCalculationDisplayRow withValues(
        this ElementCalculationDisplayRow source,
        string breakingLoadKn,
        string workingLoadKn,
        string reserve,
        string status)
    {
        return new ElementCalculationDisplayRow
        {
            Number = source.Number,
            Kind = source.Kind,
            Title = source.Title,
            PresetName = source.PresetName,
            LengthM = source.LengthM,
            Count = source.Count,
            WeightWaterKg = source.WeightWaterKg,
            ProjectedAreaM2 = source.ProjectedAreaM2,
            DragCoefficient = source.DragCoefficient,
            CurrentForceN = source.CurrentForceN,
            BreakingLoadKn = breakingLoadKn,
            WorkingLoadKn = workingLoadKn,
            Reserve = reserve,
            Status = status
        };
    }

    private static string CapacityStatusText(MooringLocalStructuralCapacityStatus status)
    {
        return status switch
        {
            MooringLocalStructuralCapacityStatus.Ok => "подходит: локальный запас ≥ 1",
            MooringLocalStructuralCapacityStatus.Insufficient => "требует проверки: локальный запас < 1",
            MooringLocalStructuralCapacityStatus.DemandUnavailable => "требует проверки: локальная нагрузка недоступна",
            MooringLocalStructuralCapacityStatus.CapacityUnavailable => "требует проверки: MBL не задан",
            MooringLocalStructuralCapacityStatus.SafetyFactorUnavailable => "требует проверки: коэффициент запаса недоступен",
            MooringLocalStructuralCapacityStatus.UnsupportedConnectorCount => "требует проверки: Count соединителя не поддержан capacity-моделью",
            MooringLocalStructuralCapacityStatus.NotRatedByCurrentModel => "не оценивается: текущая модель не задаёт MBL",
            MooringLocalStructuralCapacityStatus.NoPositiveDemand => "подходит: локальная расчётная нагрузка равна 0",
            _ => status.ToString()
        };
    }

    private static string Format(double? value) =>
        value.HasValue
            ? value.Value.ToString("0.####", CultureInfo.InvariantCulture)
            : string.Empty;
}
