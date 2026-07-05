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
        if (!useCurrentProfile)
        {
            return $"Профиль течения отключён. Используется одно значение скорости: {currentSpeedText} м/с.";
        }

        if (points.Count == 0)
        {
            return "Профиль включён, но точки не заданы. Будет использовано одно значение скорости.";
        }

        var orderedPoints = points.OrderBy(x => x.DepthM).ToList();
        var maxSpeed = orderedPoints.Max(x => x.HorizontalSpeedMS);
        var minDepth = orderedPoints.Min(x => x.DepthM);
        var maxDepth = orderedPoints.Max(x => x.DepthM);
        return $"Профиль включён: {orderedPoints.Count} точек, глубины {minDepth:0.##}–{maxDepth:0.##} м, max |Uгор|={maxSpeed:0.###} м/с. В v0.19 расчёт использует эту max-скорость как переходную оценку.";
    }
}
