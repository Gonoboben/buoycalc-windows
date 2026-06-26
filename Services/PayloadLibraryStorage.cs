using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class PayloadLibraryStorage
{
    private const string FileName = "user-payloads.json";

    public static string LibraryDirectory
    {
        get
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documents, "BuoyCalc", "Libraries");
        }
    }

    public static string LibraryPath => Path.Combine(LibraryDirectory, FileName);

    public static IReadOnlyList<PayloadLibraryItem> BuiltInPayloads { get; } = new List<PayloadLibraryItem>
    {
        new() { Id = "built-in:adcp_40", Source = "Built-in", Name = "ADCP 40 kg", Type = "ADCP", WeightAirKg = 40, VolumeM3 = 0.015, ProjectedAreaM2 = 0.05, DragCoefficient = 1.0, Note = "Учебный пресет ADCP." },
        new() { Id = "built-in:sensor_10", Source = "Built-in", Name = "Sensor 10 kg", Type = "Sensor", WeightAirKg = 10, VolumeM3 = 0.004, ProjectedAreaM2 = 0.02, DragCoefficient = 1.0, Note = "Учебный пресет компактного прибора." },
        new() { Id = "built-in:frame_25", Source = "Built-in", Name = "Instrument frame 25 kg", Type = "Frame", WeightAirKg = 25, VolumeM3 = 0.010, ProjectedAreaM2 = 0.08, DragCoefficient = 1.2, Note = "Учебный пресет рамы прибора." }
    };

    public static List<PayloadLibraryItem> LoadUserPayloads()
    {
        if (!File.Exists(LibraryPath))
        {
            return new List<PayloadLibraryItem>();
        }

        var json = File.ReadAllText(LibraryPath);
        return JsonSerializer.Deserialize<List<PayloadLibraryItem>>(json) ?? new List<PayloadLibraryItem>();
    }

    public static List<PayloadLibraryItem> LoadAllPayloads()
    {
        return BuiltInPayloads.Concat(LoadUserPayloads()).ToList();
    }

    public static PayloadLibraryItem ById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BuiltInPayloads[0];
        }

        var payload = LoadAllPayloads().FirstOrDefault(x => x.Id == id || x.Id == "built-in:" + id);
        return payload ?? BuiltInPayloads[0];
    }

    public static void SaveUserPayloads(IEnumerable<PayloadLibraryItem> payloads)
    {
        Directory.CreateDirectory(LibraryDirectory);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(payloads, options);
        File.WriteAllText(LibraryPath, json);
    }

    public static void UpsertUserPayload(PayloadLibraryItem payload)
    {
        var userPayloads = LoadUserPayloads();
        payload.Source = "User";

        if (string.IsNullOrWhiteSpace(payload.Id) || payload.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            payload.Id = "user:" + Guid.NewGuid().ToString("N");
        }

        var index = userPayloads.FindIndex(x => x.Id == payload.Id || x.Name.Equals(payload.Name, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            userPayloads[index] = payload;
        }
        else
        {
            userPayloads.Add(payload);
        }

        SaveUserPayloads(userPayloads);
    }

    public static bool DeleteUserPayload(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var userPayloads = LoadUserPayloads();
        var removed = userPayloads.RemoveAll(x => x.Id == id) > 0;

        if (removed)
        {
            SaveUserPayloads(userPayloads);
        }

        return removed;
    }
}
