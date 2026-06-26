using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class BuoyLibraryStorage
{
    private const string FileName = "user-buoys.json";

    public static string LibraryDirectory
    {
        get
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documents, "BuoyCalc", "Libraries");
        }
    }

    public static string LibraryPath => Path.Combine(LibraryDirectory, FileName);

    public static IReadOnlyList<BuoyLibraryItem> BuiltInBuoys { get; } = new[]
    {
        new BuoyLibraryItem
        {
            Id = "built-in:cyl-0.8x1.0",
            Source = "Built-in",
            Name = "Цилиндрический буй 0.8 x 1.0 м",
            VolumeM3 = 0.50,
            WeightKg = 80,
            ProjectedAreaM2 = 0.50,
            DragCoefficient = 0.80,
            Note = "Учебный встроенный пресет для первого расчёта."
        },
        new BuoyLibraryItem
        {
            Id = "built-in:sphere-0.8",
            Source = "Built-in",
            Name = "Сферический буй 0.8 м",
            VolumeM3 = 0.27,
            WeightKg = 60,
            ProjectedAreaM2 = 0.50,
            DragCoefficient = 0.50,
            Note = "Учебный пресет сферического буя."
        },
        new BuoyLibraryItem
        {
            Id = "built-in:passport-0.5",
            Source = "Built-in",
            Name = "Паспортный буй V=0.50 м³",
            VolumeM3 = 0.50,
            WeightKg = 70,
            ProjectedAreaM2 = 0.50,
            DragCoefficient = 0.80,
            Note = "Пример буя с паспортным объёмом."
        }
    };

    public static List<BuoyLibraryItem> LoadUserBuoys()
    {
        if (!File.Exists(LibraryPath))
        {
            return new List<BuoyLibraryItem>();
        }

        var json = File.ReadAllText(LibraryPath);
        return JsonSerializer.Deserialize<List<BuoyLibraryItem>>(json) ?? new List<BuoyLibraryItem>();
    }

    public static List<BuoyLibraryItem> LoadAllBuoys()
    {
        return BuiltInBuoys.Concat(LoadUserBuoys()).ToList();
    }

    public static void SaveUserBuoys(IEnumerable<BuoyLibraryItem> buoys)
    {
        Directory.CreateDirectory(LibraryDirectory);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(buoys, options);
        File.WriteAllText(LibraryPath, json);
    }

    public static void UpsertUserBuoy(BuoyLibraryItem buoy)
    {
        var userBuoys = LoadUserBuoys();
        buoy.Source = "User";

        if (string.IsNullOrWhiteSpace(buoy.Id) || buoy.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            buoy.Id = "user:" + Guid.NewGuid().ToString("N");
        }

        var index = userBuoys.FindIndex(x => x.Id == buoy.Id || x.Name.Equals(buoy.Name, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            userBuoys[index] = buoy;
        }
        else
        {
            userBuoys.Add(buoy);
        }

        SaveUserBuoys(userBuoys);
    }

    public static bool DeleteUserBuoy(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var userBuoys = LoadUserBuoys();
        var removed = userBuoys.RemoveAll(x => x.Id == id) > 0;

        if (removed)
        {
            SaveUserBuoys(userBuoys);
        }

        return removed;
    }
}
