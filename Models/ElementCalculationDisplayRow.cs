using System.Globalization;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.Models;

public sealed class ElementCalculationDisplayRow
{
    public int Number { get; init; }
    public string Kind { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string PresetName { get; init; } = string.Empty;
    public string LengthM { get; init; } = string.Empty;
    public string Count { get; init; } = string.Empty;
    public string WeightWaterKg { get; init; } = string.Empty;
    public string ProjectedAreaM2 { get; init; } = string.Empty;
    public string DragCoefficient { get; init; } = string.Empty;
    public string CurrentForceN { get; init; } = string.Empty;
    public string BreakingLoadKn { get; init; } = string.Empty;
    public string WorkingLoadKn { get; init; } = string.Empty;
    public string Reserve { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;

    public static ElementCalculationDisplayRow From(ElementCalculationRow row)
    {
        return new ElementCalculationDisplayRow
        {
            Number = row.Number,
            Kind = row.Kind,
            Title = row.Title,
            PresetName = row.PresetName,
            LengthM = Format(row.LengthM),
            Count = row.Count.ToString(CultureInfo.InvariantCulture),
            WeightWaterKg = Format(row.WeightWaterKg),
            ProjectedAreaM2 = Format(row.ProjectedAreaM2),
            DragCoefficient = Format(row.DragCoefficient),
            CurrentForceN = Format(row.CurrentForceN),
            BreakingLoadKn = Format(row.BreakingLoadKn),
            WorkingLoadKn = Format(row.WorkingLoadKn),
            Reserve = Format(row.Reserve),
            Status = UserStatusPolicy.ToUserStatus(row.Status)
        };
    }

    private static string Format(double value)
    {
        return value.ToString("0.####", CultureInfo.InvariantCulture);
    }
}
