using System;
using System.IO;
using System.Text.Json;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class ProjectJsonStorage
{
    private const string FileName = "current-project.json";

    public static string ProjectDirectory
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "BuoyCalc", "Windows");
        }
    }

    public static string ProjectPath => Path.Combine(ProjectDirectory, FileName);

    public static void Save(BuoyProjectDto project)
    {
        Directory.CreateDirectory(ProjectDirectory);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(project, options);
        File.WriteAllText(ProjectPath, json);
    }

    public static BuoyProjectDto? Load()
    {
        if (!File.Exists(ProjectPath))
        {
            return null;
        }

        var json = File.ReadAllText(ProjectPath);
        return JsonSerializer.Deserialize<BuoyProjectDto>(json);
    }
}
