using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class RopeLibraryStorage
{
    private const string FileName = "user-ropes.json";

    public static string LibraryDirectory
    {
        get
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documents, "BuoyCalc", "Libraries");
        }
    }

    public static string LibraryPath => Path.Combine(LibraryDirectory, FileName);

    public static IReadOnlyList<RopeLibraryItem> BuiltInRopes { get; } = RopeCatalog.Presets
        .Select(x => new RopeLibraryItem
        {
            Id = "built-in:" + x.Id,
            Source = "Built-in",
            Name = x.Name,
            Material = x.Material,
            DiameterMm = x.DiameterMm,
            BreakingLoadKn = x.BreakingLoadKn,
            WeightWaterKgM = x.WeightWaterKgM,
            DragCoefficient = x.DragCoefficient,
            Note = x.Note
        })
        .ToList();

    public static List<RopeLibraryItem> LoadUserRopes()
    {
        if (!File.Exists(LibraryPath))
        {
            return new List<RopeLibraryItem>();
        }

        var json = File.ReadAllText(LibraryPath);
        return JsonSerializer.Deserialize<List<RopeLibraryItem>>(json) ?? new List<RopeLibraryItem>();
    }

    public static List<RopeLibraryItem> LoadAllRopes()
    {
        return BuiltInRopes.Concat(LoadUserRopes()).ToList();
    }

    public static IReadOnlyList<RopePreset> LoadAllRopePresets()
    {
        return LoadAllRopes()
            .Select(x => x.ToRopePreset())
            .ToList();
    }

    public static RopePreset ById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return RopeCatalog.Presets[0];
        }

        if (!id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            var builtIn = RopeCatalog.Presets.FirstOrDefault(x => x.Id == id);
            if (builtIn is not null)
            {
                return builtIn;
            }
        }

        var rope = LoadAllRopes().FirstOrDefault(x => x.Id == id || x.Id == "built-in:" + id);
        return rope?.ToRopePreset() ?? RopeCatalog.Presets[0];
    }

    public static void SaveUserRopes(IEnumerable<RopeLibraryItem> ropes)
    {
        Directory.CreateDirectory(LibraryDirectory);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(ropes, options);
        File.WriteAllText(LibraryPath, json);
    }

    public static void UpsertUserRope(RopeLibraryItem rope)
    {
        var userRopes = LoadUserRopes();
        rope.Source = "User";

        if (string.IsNullOrWhiteSpace(rope.Id) || rope.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            rope.Id = "user:" + Guid.NewGuid().ToString("N");
        }

        var index = userRopes.FindIndex(x => x.Id == rope.Id || x.Name.Equals(rope.Name, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            userRopes[index] = rope;
        }
        else
        {
            userRopes.Add(rope);
        }

        SaveUserRopes(userRopes);
    }

    public static bool DeleteUserRope(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var userRopes = LoadUserRopes();
        var removed = userRopes.RemoveAll(x => x.Id == id) > 0;

        if (removed)
        {
            SaveUserRopes(userRopes);
        }

        return removed;
    }
}
