using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BuoyCalc.Windows.Models;

/// <summary>
/// Production environmental-current input invariant.
/// A current profile is mandatory and must contain at least two finite points
/// at unique non-negative depths. Legacy scalar current fields do not satisfy
/// this requirement and must never be promoted into a synthetic profile.
/// </summary>
public static class CurrentProfileRequirement
{
    public const string UserMessage =
        "Для расчёта обязателен профиль течения минимум из двух точек на разных глубинах. Одно значение скорости для всей толщи воды не используется.";

    public const string DuplicateDepthCode = "CURRENT_PROFILE_DUPLICATE_DEPTH";

    public static bool IsUsable(IReadOnlyList<CurrentProfilePointInput>? points)
    {
        if (points is null || points.Count < 2)
        {
            return false;
        }

        return points.All(IsFinitePoint) &&
               points.Select(x => x.DepthM).Distinct().Count() == points.Count;
    }

    public static bool IsUsable(EnvironmentInput environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return IsUsable(environment.EffectiveCurrentProfile);
    }

    public static void EnsureUsable(EnvironmentInput environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        EnsureUsable(environment.EffectiveCurrentProfile);
    }

    public static void EnsureUsable(IReadOnlyList<CurrentProfilePointInput>? points)
    {
        if (points is not null && points.Count >= 2 && points.All(IsFinitePoint))
        {
            var duplicate = points
                .GroupBy(x => x.DepthM)
                .FirstOrDefault(x => x.Count() > 1);
            if (duplicate is not null)
            {
                throw new CurrentProfileValidationException(duplicate.Key);
            }
        }

        if (!IsUsable(points))
        {
            throw new InvalidOperationException(UserMessage);
        }
    }

    private static bool IsFinitePoint(CurrentProfilePointInput point)
    {
        return double.IsFinite(point.DepthM) &&
               point.DepthM >= 0 &&
               double.IsFinite(point.EastCurrentMS) &&
               double.IsFinite(point.NorthCurrentMS) &&
               double.IsFinite(point.VerticalCurrentMS) &&
               double.IsFinite(point.WaterDensityKgM3);
    }
}

public sealed class CurrentProfileValidationException : InvalidOperationException
{
    public CurrentProfileValidationException(double duplicateDepthM)
        : base(
            $"BC-AUD-007 [{CurrentProfileRequirement.DuplicateDepthCode}] " +
            $"Профиль течения содержит несколько точек на глубине {duplicateDepthM.ToString("R", CultureInfo.InvariantCulture)} м. " +
            "Для каждой глубины допускается одна точка.")
    {
        Code = CurrentProfileRequirement.DuplicateDepthCode;
        DuplicateDepthM = duplicateDepthM;
    }

    public string Code { get; }
    public double DuplicateDepthM { get; }
}
