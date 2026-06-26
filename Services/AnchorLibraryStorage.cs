using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class AnchorLibraryStorage
{
    private const string FileName = "user-anchors.json";

    public static string LibraryDirectory
    {
        get
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documents, "BuoyCalc", "Libraries");
        }
    }

    public static string LibraryPath => Path.Combine(LibraryDirectory, FileName);

    public static IReadOnlyList<AnchorLibraryItem> BuiltInAnchors { get; } = AnchorCatalog.Presets
        .Select(x => new AnchorLibraryItem
        {
            Id = "built-in:" + x.Id,
            Source = "Built-in",
            Name = x.Name,
            Type = x.Type,
            Material = x.Material,
            WeightAirKg = x.WeightAirKg,
            VolumeM3 = x.VolumeM3,
            BaseHoldingCoefficient = x.BaseHoldingCoefficient,
            Note = x.Note
        })
        .ToList();

    public static List<AnchorLibraryItem> LoadUserAnchors()
    {
        if (!File.Exists(LibraryPath))
        {
            return new List<AnchorLibraryItem>();
        }

        var json = File.ReadAllText(LibraryPath);
        return JsonSerializer.Deserialize<List<AnchorLibraryItem>>(json) ?? new List<AnchorLibraryItem>();
    }

    public static List<AnchorLibraryItem> LoadAllAnchors()
    {
        return BuiltInAnchors.Concat(LoadUserAnchors()).ToList();
    }

    public static AnchorPreset ById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return AnchorCatalog.Presets[0];
        }

        if (!id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            var builtIn = AnchorCatalog.Presets.FirstOrDefault(x => x.Id == id);
            if (builtIn is not null)
            {
                return builtIn;
            }
        }

        var anchor = LoadAllAnchors().FirstOrDefault(x => x.Id == id || x.Id == "built-in:" + id);
        return anchor?.ToAnchorPreset() ?? AnchorCatalog.Presets[0];
    }

    public static void SaveUserAnchors(IEnumerable<AnchorLibraryItem> anchors)
    {
        Directory.CreateDirectory(LibraryDirectory);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(anchors, options);
        File.WriteAllText(LibraryPath, json);
    }

    public static void UpsertUserAnchor(AnchorLibraryItem anchor)
    {
        var userAnchors = LoadUserAnchors();
        anchor.Source = "User";

        if (string.IsNullOrWhiteSpace(anchor.Id) || anchor.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            anchor.Id = "user:" + Guid.NewGuid().ToString("N");
        }

        var index = userAnchors.FindIndex(x => x.Id == anchor.Id || x.Name.Equals(anchor.Name, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            userAnchors[index] = anchor;
        }
        else
        {
            userAnchors.Add(anchor);
        }

        SaveUserAnchors(userAnchors);
    }

    public static bool DeleteUserAnchor(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var userAnchors = LoadUserAnchors();
        var removed = userAnchors.RemoveAll(x => x.Id == id) > 0;

        if (removed)
        {
            SaveUserAnchors(userAnchors);
        }

        return removed;
    }
}
