using System;
using System.IO;
using System.Text.Json;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class ProjectJsonStorage
{
    private const string DefaultFileName = "current-project.json";

    public static string ProjectDirectory
    {
        get
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documents, "BuoyCalc", "Projects");
        }
    }

    public static string DefaultProjectPath => Path.Combine(ProjectDirectory, DefaultFileName);

    public static void Save(BuoyProjectDto project, string path)
    {
        path = NormalizeJsonPath(path);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(project, options);
        File.WriteAllText(path, json);
    }

    public static BuoyProjectDto? Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<BuoyProjectDto>(json);
    }

    public static string NormalizeJsonPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return DefaultProjectPath;
        }

        return Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase)
            ? path
            : path + ".json";
    }
}
