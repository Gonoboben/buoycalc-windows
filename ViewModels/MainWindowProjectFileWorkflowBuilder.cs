using System.IO;

namespace BuoyCalc.Windows.ViewModels;

internal sealed record MainWindowProjectSavePathRequest(
    bool ShouldRequestPath,
    string CurrentPath,
    string SuggestedFileName);

internal static class MainWindowProjectFileWorkflowBuilder
{
    internal static MainWindowProjectSavePathRequest BuildSavePathRequest(
        bool promptForPath,
        string currentPath,
        string projectName)
    {
        var shouldRequestPath = promptForPath || string.IsNullOrWhiteSpace(currentPath);
        return new MainWindowProjectSavePathRequest(
            shouldRequestPath,
            currentPath,
            shouldRequestPath ? MakeSafeFileName(projectName) + ".json" : string.Empty);
    }

    internal static string ResolveSaveTargetPath(
        MainWindowProjectSavePathRequest request,
        bool dialogAvailable,
        string? pickerPath,
        string defaultProjectPath)
    {
        if (!request.ShouldRequestPath)
        {
            return request.CurrentPath;
        }

        return dialogAvailable
            ? pickerPath ?? string.Empty
            : defaultProjectPath;
    }

    internal static string ResolveLoadSelectedPath(
        bool dialogAvailable,
        string? pickerPath,
        string currentPath)
    {
        return dialogAvailable
            ? pickerPath ?? string.Empty
            : currentPath;
    }

    internal static bool IsCanceled(string path)
    {
        return string.IsNullOrWhiteSpace(path);
    }

    internal static string BuildSaveSuccessStatus(string path)
    {
        return $"Проект сохранён: {path}";
    }

    internal static string BuildSaveErrorStatus(string message)
    {
        return $"Ошибка сохранения: {message}";
    }

    internal static string BuildLoadMissingStatus(string path)
    {
        return $"Файл проекта не найден: {path}";
    }

    internal static string BuildLoadSuccessStatus(string path)
    {
        return $"Проект загружен: {path}";
    }

    internal static string BuildLoadErrorStatus(string message)
    {
        return $"Ошибка загрузки: {message}";
    }

    private static string MakeSafeFileName(string value)
    {
        value = string.IsNullOrWhiteSpace(value) ? "BuoyCalc_Project" : value.Trim();
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidChar, '_');
        }

        return value.Replace(' ', '_');
    }
}
