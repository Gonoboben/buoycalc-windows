using System;
using System.Collections.Generic;

namespace BuoyCalc.Windows.ViewModels;

internal static class AssemblyItemLibraryOptionNotificationPlanBuilder
{
    private static readonly IReadOnlyList<string> PropertyNames = Array.AsReadOnly(new[]
    {
        nameof(AssemblyItemViewModel.RopePresetOptions),
        nameof(AssemblyItemViewModel.RopePresetId),
        nameof(AssemblyItemViewModel.ConnectorPresetOptions),
        nameof(AssemblyItemViewModel.ConnectorPresetId),
        nameof(AssemblyItemViewModel.PayloadPresetOptions),
        nameof(AssemblyItemViewModel.PayloadPresetId),
        nameof(AssemblyItemViewModel.EditorHint),
        nameof(AssemblyItemViewModel.Summary)
    });

    internal static IReadOnlyList<string> Build()
    {
        return PropertyNames;
    }
}
