using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace BuoyCalc.Windows.Views;

internal static class MainWindowPdfExportWorkflowBuilder
{
    internal static bool CanExport(string reportText)
    {
        return !string.IsNullOrWhiteSpace(reportText);
    }

    internal static string BuildSuggestedFileName(string projectName)
    {
        return MakeSafeFileName(projectName) + "_report.pdf";
    }

    internal static bool IsCanceled([NotNullWhen(false)] string? path)
    {
        return string.IsNullOrWhiteSpace(path);
    }

    internal static string BuildPreconditionStatus()
    {
        return "Сначала выполните расчёт, затем экспортируйте PDF.";
    }

    internal static string BuildCanceledStatus()
    {
        return "Экспорт PDF отменён.";
    }

    internal static string BuildSuccessStatus(string path)
    {
        return $"PDF сохранён: {path}";
    }

    internal static string BuildErrorStatus(string message)
    {
        return $"Ошибка экспорта PDF: {message}";
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
