using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.ViewModels;

internal static class MainWindowCurrentProfileSummaryBuilder
{
    internal static string Build(
        bool useCurrentProfile,
        string currentSpeedText,
        IReadOnlyList<CurrentProfilePointInput> points)
    {
        // Legacy arguments remain only while older project DTO/UI plumbing is retired.
        // They are not calculation authority: the current profile is mandatory.
        if (!CurrentProfileRequirement.IsUsable(points))
        {
            var distinctDepthCount = points
                .Where(x => double.IsFinite(x.DepthM) && x.DepthM >= 0)
                .Select(x => x.DepthM)
                .Distinct()
                .Count();

            return distinctDepthCount == 0
                ? "Профиль течения обязателен. Добавьте минимум две точки на разных глубинах; до этого расчёт заблокирован."
                : "Профиль течения неполный. Нужны минимум две конечные точки на разных глубинах; до этого расчёт заблокирован.";
        }

        var orderedPoints = points.OrderBy(x => x.DepthM).ToList();
        var maxSpeed = orderedPoints.Max(x => x.HorizontalSpeedMS);
        var minDepth = orderedPoints.Min(x => x.DepthM);
        var maxDepth = orderedPoints.Max(x => x.DepthM);
        return $"Профиль обязателен: {orderedPoints.Count} точек, глубины {minDepth:0.##}–{maxDepth:0.##} м, max |Uгор|={maxSpeed:0.###} м/с. Сила течения на линии интегрируется по интерполированным сегментам ≤0.20 м; для буя, соединителей и приборов используется max |Uгор|.";
    }
}
