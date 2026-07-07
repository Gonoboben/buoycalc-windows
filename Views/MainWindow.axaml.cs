using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using BuoyCalc.Windows.Services;
using BuoyCalc.Windows.ViewModels;

namespace BuoyCalc.Windows.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        WindowVersionHelper.Apply(this, "BuoyCalc Windows");
        ApplyMainWindowTextOverrides();
        DataContext = new MainWindowViewModel(new AvaloniaProjectFileDialogService(this));
    }

    private void ApplyMainWindowTextOverrides()
    {
        foreach (var textBlock in this.GetVisualDescendants().OfType<TextBlock>())
        {
            if (textBlock.Text == "Отчёт текстом...")
            {
                textBlock.Text = "Полный отчёт...";
            }
            else if (textBlock.Text == "v0.21.3 cleanup")
            {
                textBlock.Text = AppInfo.DisplayVersion;
            }
        }
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

        if (!MainWindowPdfExportWorkflowBuilder.CanExport(viewModel.ReportText))
        {
            viewModel.ProjectStatusText = MainWindowPdfExportWorkflowBuilder.BuildPreconditionStatus();
            return;
        }

        var suggestedFileName = MainWindowPdfExportWorkflowBuilder.BuildSuggestedFileName(viewModel.ProjectName);
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить PDF-отчёт BuoyCalc",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "pdf",
            FileTypeChoices = PdfFileTypes
        });

        var path = file?.Path.LocalPath;
        if (MainWindowPdfExportWorkflowBuilder.IsCanceled(path))
        {
            viewModel.ProjectStatusText = MainWindowPdfExportWorkflowBuilder.BuildCanceledStatus();
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

            viewModel.ProjectStatusText = MainWindowPdfExportWorkflowBuilder.BuildSuccessStatus(path);
        }
        catch (System.Exception ex)
        {
            viewModel.ProjectStatusText = MainWindowPdfExportWorkflowBuilder.BuildErrorStatus(ex.Message);
        }
    }

    private static IReadOnlyList<FilePickerFileType> PdfFileTypes { get; } = new[]
    {
        new FilePickerFileType("PDF report")
        {
            Patterns = new[] { "*.pdf" }
        },
        FilePickerFileTypes.All
    };
}
