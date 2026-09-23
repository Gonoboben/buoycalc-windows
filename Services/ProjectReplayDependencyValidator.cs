using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Guards legacy input-only project files before any live view-model state is
/// mutated. New files carry embedded resolved snapshots; legacy references remain
/// readable only while the exact referenced engineering preset still exists.
/// </summary>
public static class ProjectReplayDependencyValidator
{
    public static void EnsureSafe(BuoyProjectDto project)
    {
        ArgumentNullException.ThrowIfNull(project);

        foreach (var item in project.AssemblyItems)
        {
            if (item.Kind == "Connector")
            {
                if (item.ResolvedConnectorPreset is null)
                {
                    _ = ConnectorLibraryStorage.ById(item.ConnectorPresetId);
                }
                else
                {
                    EnsureMatchingId(item.ConnectorPresetId, item.ResolvedConnectorPreset.Id, "connector");
                }
            }
            else if (item.Kind != "Payload")
            {
                if (item.ResolvedRopePreset is null)
                {
                    _ = RopeLibraryStorage.ById(item.RopePresetId);
                }
                else
                {
                    EnsureMatchingId(item.RopePresetId, item.ResolvedRopePreset.Id, "rope");
                }
            }
        }
    }

    private static void EnsureMatchingId(string referenceId, string snapshotId, string kind)
    {
        if (!string.Equals(referenceId, snapshotId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Project {kind} preset reference '{referenceId}' does not match embedded snapshot '{snapshotId}'.");
        }
    }
}
