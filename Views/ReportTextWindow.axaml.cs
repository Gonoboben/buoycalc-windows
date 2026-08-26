using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BuoyCalc.Windows.ViewModels;

namespace BuoyCalc.Windows.Views;

public partial class ReportTextWindow : Window
{
    public ReportTextWindow()
    {
        AvaloniaXamlLoader.Load(this);
        WindowVersionHelper.Apply(this, "Полный текстовый отчёт");
    }

    private async void ExportReportButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || string.IsNullOrWhiteSpace(viewModel.ReportText))
        {
            SetExportStatus("Нет рассчитанного полного отчёта для экспорта.");
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить полный технический отчёт BuoyCalc",
            SuggestedFileName = BuildSuggestedFileName(viewModel.ProjectName),
            DefaultExtension = "txt",
            FileTypeChoices = TextReportFileTypes
        });

        if (file is null)
        {
            SetExportStatus("Экспорт полного отчёта отменён.");
            return;
        }

        try
        {
            await File.WriteAllTextAsync(file.Path.LocalPath, viewModel.ReportText, new UTF8Encoding(false));
            SetExportStatus($"Полный отчёт сохранён: {file.Path.LocalPath}");
        }
        catch (Exception ex)
        {
            SetExportStatus($"Ошибка экспорта полного отчёта: {ex.Message}");
        }
    }

    private void SetExportStatus(string text)
    {
        if (this.FindControl<TextBlock>("ExportStatusText") is { } status)
        {
            status.Text = text;
        }
    }

    private static string BuildSuggestedFileName(string projectName)
    {
        var safeName = string.IsNullOrWhiteSpace(projectName) ? "BuoyCalc_Project" : projectName.Trim();
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(invalidChar, '_');
        }

        return safeName.Replace(' ', '_') + "_full_report.txt";
    }

    private static IReadOnlyList<FilePickerFileType> TextReportFileTypes { get; } = new[]
    {
        new FilePickerFileType("BuoyCalc technical report")
        {
            Patterns = new[] { "*.txt" }
        },
        FilePickerFileTypes.All
    };
}
