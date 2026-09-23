using System.Reflection;
using System.Text.Json;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.ViewModels;

internal static class ProjectReplayPersistenceRegression
{
    public static void ProjectReplay_UsesEmbeddedResolvedPresetSnapshotOrFails()
    {
        var source = new MainWindowViewModel
        {
            BuoyName = "Audited raw buoy",
            BuoyVolume = "9.99",
            BuoyWeight = "999",
            BuoyArea = "9.99",
            BuoyCd = "9.99"
        };

        var saved = InvokeToDto(source);
        var json = JsonSerializer.Serialize(saved);
        var roundTripped = JsonSerializer.Deserialize<BuoyProjectDto>(json)
            ?? throw new InvalidOperationException("BC-AUD-003: project JSON did not deserialize.");

        var restored = new MainWindowViewModel();
        InvokeFromDto(restored, roundTripped);

        RequireEqual("9.99", restored.BuoyVolume, "buoy volume");
        RequireEqual("999", restored.BuoyWeight, "buoy weight");
        RequireEqual("9.99", restored.BuoyArea, "buoy projected area");
        RequireEqual("9.99", restored.BuoyCd, "buoy drag coefficient");
    }

    public static void MissingPresetId_MustNotFallbackSilently()
    {
        var missingRope = new AssemblyItemViewModel
        {
            Kind = "Line",
            Title = "Deleted rope",
            RopePresetStorageId = "user:deleted-rope",
            LengthM = "20.2"
        };

        try
        {
            var input = missingRope.ToInput();
            var substitutedId = input.RopePreset?.Id ?? "<null>";
            throw new SilentPresetSubstitutionException(substitutedId);
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("missing rope preset", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            // Required explicit failure for a legacy project without an embedded snapshot.
        }
    }

    private sealed class SilentPresetSubstitutionException(string substitutedId)
        : Exception($"BC-AUD-003: missing rope preset silently resolved to '{substitutedId}' instead of failing explicitly.");

    private static BuoyProjectDto InvokeToDto(MainWindowViewModel viewModel)
    {
        var method = typeof(MainWindowViewModel).GetMethod("ToDto", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BC-AUD-003: MainWindowViewModel.ToDto was not found.");
        return (BuoyProjectDto)(method.Invoke(viewModel, null)
            ?? throw new InvalidOperationException("BC-AUD-003: MainWindowViewModel.ToDto returned null."));
    }

    private static void InvokeFromDto(MainWindowViewModel viewModel, BuoyProjectDto dto)
    {
        var method = typeof(MainWindowViewModel).GetMethod("FromDto", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BC-AUD-003: MainWindowViewModel.FromDto was not found.");
        try
        {
            method.Invoke(viewModel, new object[] { dto });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static void RequireEqual(string expected, string actual, string field)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"BC-AUD-003: saved {field} '{expected}' restored as '{actual}'.");
        }
    }
}
