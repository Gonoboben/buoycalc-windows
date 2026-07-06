using System;
using System.Collections.Generic;

namespace BuoyCalc.Windows.ViewModels;

internal enum MainWindowAssemblyItemLifecycleRoute
{
    RemoveRequested,
    MoveUpRequested,
    MoveDownRequested,
    DuplicateRequested,
    PropertyChanged
}

internal sealed record MainWindowAssemblyItemLifecyclePlan(
    IReadOnlyList<MainWindowAssemblyItemLifecycleRoute> WireRoutes,
    IReadOnlyList<MainWindowAssemblyItemLifecycleRoute> UnwireRoutes);

internal static class MainWindowAssemblyItemLifecyclePlanBuilder
{
    private static readonly MainWindowAssemblyItemLifecyclePlan Plan = new(
        Array.AsReadOnly(new[]
        {
            MainWindowAssemblyItemLifecycleRoute.RemoveRequested,
            MainWindowAssemblyItemLifecycleRoute.MoveUpRequested,
            MainWindowAssemblyItemLifecycleRoute.MoveDownRequested,
            MainWindowAssemblyItemLifecycleRoute.DuplicateRequested,
            MainWindowAssemblyItemLifecycleRoute.PropertyChanged
        }),
        Array.AsReadOnly(new[]
        {
            MainWindowAssemblyItemLifecycleRoute.PropertyChanged,
            MainWindowAssemblyItemLifecycleRoute.RemoveRequested,
            MainWindowAssemblyItemLifecycleRoute.MoveUpRequested,
            MainWindowAssemblyItemLifecycleRoute.MoveDownRequested,
            MainWindowAssemblyItemLifecycleRoute.DuplicateRequested
        }));

    internal static MainWindowAssemblyItemLifecyclePlan Build()
    {
        return Plan;
    }

    internal static int? ResolveMoveUpTarget(int currentIndex)
    {
        return currentIndex <= 0 ? null : currentIndex - 1;
    }

    internal static int? ResolveMoveDownTarget(int currentIndex, int count)
    {
        return currentIndex < 0 || currentIndex >= count - 1
            ? null
            : currentIndex + 1;
    }

    internal static int? ResolveDuplicateInsertionIndex(int currentIndex, int count)
    {
        return currentIndex < 0 || currentIndex >= count - 1
            ? null
            : currentIndex + 1;
    }
}
