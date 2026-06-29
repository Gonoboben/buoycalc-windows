using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BuoyCalc.Windows.Services;
using BuoyCalc.Windows.ViewModels;

namespace BuoyCalc.Windows.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        WindowVersionHelper.Apply(this, "BuoyCalc Windows");
        DataContext = new MainWindowViewModel(new AvaloniaProjectFileDialogService(this));
    }

    private async void OpenLibraryButton_Click(object? sender, RoutedEventArgs e)
    {
        var libraryWindow = new ElementLibraryWindow();
        await libraryWindow.ShowDialog(this);

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.RefreshBuoyLibraryCommand.Execute(null);
        }
    }

    private async void OpenCurrentProfileButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var window = new CurrentProfileWindow
        {
            DataContext = viewModel
        };

        await window.ShowDialog(this);
    }

    private async void CalculateButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var previewWindow = new SequencePreviewWindow
        {
            DataContext = viewModel
        };

        var confirmed = await previewWindow.ShowDialog<bool>(this);
        if (confirmed)
        {
            viewModel.CalculateCommand.Execute(null);
        }
    }

    private async void Open2DButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var window = new Mooring2DWindow
        {
            DataContext = viewModel
        };

        await window.ShowDialog(this);
    }

    private async void OpenReportTextButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(viewModel.ReportText))
        {
            viewModel.ProjectStatusText = "Сначала выполните расчёт, затем откройте полный отчёт.";
            return;
        }

        var window = new ReportTextWindow
        {
            DataContext = viewModel
        };

        await window.ShowDialog(this);
    }

    private async void ExportPdfButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(viewModel.ReportText))
        {
            viewModel.ProjectStatusText = "Сначала выполните расчёт, затем экспортируйте PDF.";
            return;
        }

        var suggestedFileName = MakeSafeFileName(viewModel.ProjectName) + "_report.pdf";
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить PDF-отчёт BuoyCalc",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "pdf",
            FileTypeChoices = PdfFileTypes
        });

        var path = file?.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            viewModel.ProjectStatusText = "Экспорт PDF отменён.";
            return;
        }

        try
        {
            var pdfReportText = PdfReportStructureGuide.Apply(viewModel.ReportText);
            PdfReportBuilder.Build(
                path,
                viewModel.ProjectName,
                viewModel.ResultText,
                viewModel.SequenceDiagramLines,
                viewModel.ElementRows,
                pdfReportText,
                viewModel.VisualizationDepthM,
                viewModel.VisualizationLineLengthM,
                viewModel.VisualizationOffsetM);

            viewModel.ProjectStatusText = $"PDF сохранён: {path}";
        }
        catch (System.Exception ex)
        {
            viewModel.ProjectStatusText = $"Ошибка экспорта PDF: {ex.Message}";
        }
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

    private static IReadOnlyList<FilePickerFileType> PdfFileTypes { get; } = new[]
    {
        new FilePickerFileType("PDF report")
        {
            Patterns = new[] { "*.pdf" }
        },
        FilePickerFileTypes.All
    };

    private sealed class AvaloniaProjectFileDialogService : IProjectFileDialogService
    {
        private readonly Window _owner;

        public AvaloniaProjectFileDialogService(Window owner)
        {
            _owner = owner;
        }

        public async Task<string?> PickSavePathAsync(string suggestedFileName)
        {
            var file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить проект BuoyCalc",
                SuggestedFileName = suggestedFileName,
                DefaultExtension = "json",
                FileTypeChoices = ProjectFileTypes
            });

            return file?.Path.LocalPath;
        }

        public async Task<string?> PickOpenPathAsync()
        {
            var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Открыть проект BuoyCalc",
                AllowMultiple = false,
                FileTypeFilter = ProjectFileTypes
            });

            return files.FirstOrDefault()?.Path.LocalPath;
        }

        private static IReadOnlyList<FilePickerFileType> ProjectFileTypes { get; } = new[]
        {
            new FilePickerFileType("BuoyCalc project")
            {
                Patterns = new[] { "*.json" }
            },
            FilePickerFileTypes.All
        };
    }
}
