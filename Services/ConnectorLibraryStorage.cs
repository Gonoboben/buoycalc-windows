using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class ConnectorLibraryStorage
{
    private const string FileName = "user-connectors.json";

    public static string LibraryDirectory
    {
        get
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documents, "BuoyCalc", "Libraries");
        }
    }

    public static string LibraryPath => Path.Combine(LibraryDirectory, FileName);

    public static IReadOnlyList<ConnectorLibraryItem> BuiltInConnectors { get; } = ConnectorCatalog.Presets
        .Select(x => new ConnectorLibraryItem
        {
            Id = "built-in:" + x.Id,
            Source = "Built-in",
            Name = x.Name,
            Type = x.Type,
            WeightAirKg = x.WeightAirKg,
            VolumeM3 = x.VolumeM3,
            BreakingLoadKn = x.BreakingLoadKn,
            ProjectedAreaM2 = x.ProjectedAreaM2,
            DragCoefficient = x.DragCoefficient,
            Note = x.Note
        })
        .ToList();

    public static List<ConnectorLibraryItem> LoadUserConnectors()
    {
        if (!File.Exists(LibraryPath))
        {
            return new List<ConnectorLibraryItem>();
        }

        var json = File.ReadAllText(LibraryPath);
        return JsonSerializer.Deserialize<List<ConnectorLibraryItem>>(json) ?? new List<ConnectorLibraryItem>();
    }

    public static List<ConnectorLibraryItem> LoadAllConnectors()
    {
        return BuiltInConnectors.Concat(LoadUserConnectors()).ToList();
    }

    public static ConnectorPreset ById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ConnectorCatalog.Presets[0];
        }

        if (!id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            var builtIn = ConnectorCatalog.Presets.FirstOrDefault(x => x.Id == id);
            if (builtIn is not null)
            {
                return builtIn;
            }
        }

        var connector = LoadAllConnectors().FirstOrDefault(x => x.Id == id || x.Id == "built-in:" + id);
        return connector?.ToConnectorPreset() ?? ConnectorCatalog.Presets[0];
    }

    public static void SaveUserConnectors(IEnumerable<ConnectorLibraryItem> connectors)
    {
        Directory.CreateDirectory(LibraryDirectory);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(connectors, options);
        File.WriteAllText(LibraryPath, json);
    }

    public static void UpsertUserConnector(ConnectorLibraryItem connector)
    {
        var userConnectors = LoadUserConnectors();
        connector.Source = "User";

        if (string.IsNullOrWhiteSpace(connector.Id) || connector.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            connector.Id = "user:" + Guid.NewGuid().ToString("N");
        }

        var index = userConnectors.FindIndex(x => x.Id == connector.Id || x.Name.Equals(connector.Name, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            userConnectors[index] = connector;
        }
        else
        {
            userConnectors.Add(connector);
        }

        SaveUserConnectors(userConnectors);
    }

    public static bool DeleteUserConnector(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var userConnectors = LoadUserConnectors();
        var removed = userConnectors.RemoveAll(x => x.Id == id) > 0;

        if (removed)
        {
            SaveUserConnectors(userConnectors);
        }

        return removed;
    }
}
