using System;
using System.Collections.Generic;
using System.Linq;

namespace BuoyCalc.Windows.Models;

/// <summary>
/// Production environmental-current input invariant.
/// A current profile is mandatory and must contain at least two finite points
/// at distinct non-negative depths. Legacy scalar current fields do not satisfy
/// this requirement and must never be promoted into a synthetic profile.
/// </summary>
public static class CurrentProfileRequirement
{
    public const string UserMessage =
        "Для расчёта обязателен профиль течения минимум из двух точек на разных глубинах. Одно значение скорости для всей толщи воды не используется.";

    public static bool IsUsable(IReadOnlyList<CurrentProfilePointInput>? points)
    {
        if (points is null || points.Count < 2)
        {
            return false;
        }

        var finiteDepths = points
            .Where(x => double.IsFinite(x.DepthM) && x.DepthM >= 0)
            .Select(x => x.DepthM)
            .Distinct()
            .Take(2)
            .Count();

        return finiteDepths >= 2 && points.All(IsFinitePoint);
    }

    public static bool IsUsable(EnvironmentInput environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return IsUsable(environment.EffectiveCurrentProfile);
    }

    public static void EnsureUsable(EnvironmentInput environment)
    {
        if (!IsUsable(environment))
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
