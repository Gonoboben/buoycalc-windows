using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BuoyCalc.Windows.ViewModels;

internal enum MainWindowCurrentProfilePointLifecycleRoute
{
    RemoveRequested,
    PropertyChanged
}

internal sealed record MainWindowCurrentProfilePointLifecyclePlan(
    IReadOnlyList<MainWindowCurrentProfilePointLifecycleRoute> WireRoutes,
    IReadOnlyList<MainWindowCurrentProfilePointLifecycleRoute> UnwireRoutes);

internal sealed record MainWindowCurrentProfilePointDefaults(
    string DepthM,
    string EastCurrentMS,
    string NorthCurrentMS,
    string VerticalCurrentMS,
    string WaterDensityKgM3);

internal static class MainWindowCurrentProfilePointLifecyclePlanBuilder
{
    private static readonly MainWindowCurrentProfilePointLifecyclePlan Plan = new(
        Array.AsReadOnly(new[]
        {
            MainWindowCurrentProfilePointLifecycleRoute.RemoveRequested,
            MainWindowCurrentProfilePointLifecycleRoute.PropertyChanged
        }),
        Array.AsReadOnly(new[]
        {
            MainWindowCurrentProfilePointLifecycleRoute.RemoveRequested,
            MainWindowCurrentProfilePointLifecycleRoute.PropertyChanged
        }));

    internal static MainWindowCurrentProfilePointLifecyclePlan Build()
    {
        return Plan;
    }

    internal static MainWindowCurrentProfilePointDefaults BuildNewPointDefaults(
        IReadOnlyList<double> existingDepths,
        bool useCurrentSpeed,
        string currentSpeed,
        string waterDensity)
    {
        var depth = existingDepths.Count == 0
            ? 0
            : existingDepths.Max() + 10;

        // The legacy scalar-current arguments are intentionally ignored.
        // A new profile point must never inherit a hidden whole-column speed.
        return new MainWindowCurrentProfilePointDefaults(
            depth.ToString("0.###", CultureInfo.InvariantCulture),
            "0",
            "0",
            "0",
            waterDensity);
    }
}
