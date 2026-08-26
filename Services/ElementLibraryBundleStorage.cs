using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record ElementLibraryBundleExportResult(
    int Buoys,
    int Ropes,
    int Connectors,
    int Payloads,
    int Anchors)
{
    public int Total => Buoys + Ropes + Connectors + Payloads + Anchors;
}

public sealed record ElementLibraryBundleImportResult(
    int ImportedBuoys,
    int ImportedRopes,
    int ImportedConnectors,
    int ImportedPayloads,
    int ImportedAnchors,
    int Skipped)
{
    public int Imported => ImportedBuoys + ImportedRopes + ImportedConnectors + ImportedPayloads + ImportedAnchors;
}

/// <summary>
/// Portable user-library bundle. Built-in presets are never exported or overwritten.
/// Import is additive: a conflicting id or name is skipped rather than replacing local data.
/// </summary>
public static class ElementLibraryBundleStorage
{
    public const string BundleFormat = "BuoyCalc.ElementLibrary";
    public const int CurrentFormatVersion = 1;

    public static ElementLibraryBundleExportResult Export(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Не задан путь для экспорта библиотеки.", nameof(filePath));

        var bundle = new ElementLibraryBundle
        {
            Format = BundleFormat,
            FormatVersion = CurrentFormatVersion,
            AppVersion = AppInfo.Version,
            Buoys = BuoyLibraryStorage.LoadUserBuoys(),
            Ropes = RopeLibraryStorage.LoadUserRopes(),
            Connectors = ConnectorLibraryStorage.LoadUserConnectors(),
            Payloads = PayloadLibraryStorage.LoadUserPayloads(),
            Anchors = AnchorLibraryStorage.LoadUserAnchors()
        };

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(filePath, JsonSerializer.Serialize(bundle, options));

        return new ElementLibraryBundleExportResult(
            bundle.Buoys.Count,
            bundle.Ropes.Count,
            bundle.Connectors.Count,
            bundle.Payloads.Count,
            bundle.Anchors.Count);
    }

    public static ElementLibraryBundleImportResult ImportMerge(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            throw new FileNotFoundException("Файл библиотеки не найден.", filePath);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var bundle = JsonSerializer.Deserialize<ElementLibraryBundle>(File.ReadAllText(filePath), options)
            ?? throw new InvalidDataException("Файл библиотеки пуст или имеет неверный формат.");

        if (!string.Equals(bundle.Format, BundleFormat, StringComparison.Ordinal))
            throw new InvalidDataException($"Неизвестный формат библиотеки: {bundle.Format}");
        if (bundle.FormatVersion != CurrentFormatVersion)
            throw new InvalidDataException($"Неподдерживаемая версия формата библиотеки: {bundle.FormatVersion}");

        var skipped = 0;
        var importedBuoys = MergeCategory(
            bundle.Buoys,
            BuoyLibraryStorage.LoadUserBuoys(),
            BuoyLibraryStorage.BuiltInBuoys,
            x => x.Id,
            x => x.Name,
            x => { x.Source = "User"; x.Id = x.Id.Trim(); x.Name = x.Name.Trim(); },
            BuoyLibraryStorage.SaveUserBuoys,
            ref skipped);

        var importedRopes = MergeCategory(
            bundle.Ropes,
            RopeLibraryStorage.LoadUserRopes(),
            RopeLibraryStorage.BuiltInRopes,
            x => x.Id,
            x => x.Name,
            x => { x.Source = "User"; x.Id = x.Id.Trim(); x.Name = x.Name.Trim(); },
            RopeLibraryStorage.SaveUserRopes,
            ref skipped);

        var importedConnectors = MergeCategory(
            bundle.Connectors,
            ConnectorLibraryStorage.LoadUserConnectors(),
            ConnectorLibraryStorage.BuiltInConnectors,
            x => x.Id,
            x => x.Name,
            x => { x.Source = "User"; x.Id = x.Id.Trim(); x.Name = x.Name.Trim(); },
            ConnectorLibraryStorage.SaveUserConnectors,
            ref skipped);

        var importedPayloads = MergeCategory(
            bundle.Payloads,
            PayloadLibraryStorage.LoadUserPayloads(),
            PayloadLibraryStorage.BuiltInPayloads,
            x => x.Id,
            x => x.Name,
            x => { x.Source = "User"; x.Id = x.Id.Trim(); x.Name = x.Name.Trim(); },
            PayloadLibraryStorage.SaveUserPayloads,
            ref skipped);

        var importedAnchors = MergeCategory(
            bundle.Anchors,
            AnchorLibraryStorage.LoadUserAnchors(),
            AnchorLibraryStorage.BuiltInAnchors,
            x => x.Id,
            x => x.Name,
            x => { x.Source = "User"; x.Id = x.Id.Trim(); x.Name = x.Name.Trim(); },
            AnchorLibraryStorage.SaveUserAnchors,
            ref skipped);

        return new ElementLibraryBundleImportResult(
            importedBuoys,
            importedRopes,
            importedConnectors,
            importedPayloads,
            importedAnchors,
            skipped);
    }

    private static int MergeCategory<T>(
        IReadOnlyList<T>? incoming,
        List<T> existingUser,
        IReadOnlyList<T> builtIn,
        Func<T, string> getId,
        Func<T, string> getName,
        Action<T> normalize,
        Action<IEnumerable<T>> save,
        ref int skipped)
        where T : class
    {
        incoming ??= Array.Empty<T>();

        var usedIds = new HashSet<string>(
            existingUser.Concat(builtIn).Select(getId).Where(x => !string.IsNullOrWhiteSpace(x)),
            StringComparer.OrdinalIgnoreCase);
        var usedNames = new HashSet<string>(
            existingUser.Concat(builtIn).Select(getName).Where(x => !string.IsNullOrWhiteSpace(x)),
            StringComparer.OrdinalIgnoreCase);

        var imported = 0;
        foreach (var item in incoming)
        {
            if (item is null)
            {
                skipped++;
                continue;
            }

            var id = (getId(item) ?? string.Empty).Trim();
            var name = (getName(item) ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id) ||
                !id.StartsWith("user:", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(name) ||
                usedIds.Contains(id) ||
                usedNames.Contains(name))
            {
                skipped++;
                continue;
            }

            normalize(item);
            existingUser.Add(item);
            usedIds.Add(id);
            usedNames.Add(name);
            imported++;
        }

        if (imported > 0)
            save(existingUser);

        return imported;
    }

    private sealed class ElementLibraryBundle
    {
        public string Format { get; set; } = BundleFormat;
        public int FormatVersion { get; set; } = CurrentFormatVersion;
        public string AppVersion { get; set; } = AppInfo.Version;
        public List<BuoyLibraryItem> Buoys { get; set; } = new();
        public List<RopeLibraryItem> Ropes { get; set; } = new();
        public List<ConnectorLibraryItem> Connectors { get; set; } = new();
        public List<PayloadLibraryItem> Payloads { get; set; } = new();
        public List<AnchorLibraryItem> Anchors { get; set; } = new();
    }
}
