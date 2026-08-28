using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;
using BuoyCalc.Windows.ViewModels;

namespace BuoyCalc.Windows.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WindowVersionHelper.Apply(this, "BuoyCalc Windows");
        DataContext = new MainWindowViewModel(new AvaloniaProjectFileDialogService(this));
        CollapseSetupSections();
    }

    private void ResetSetupSectionsButton_Click(object? sender, RoutedEventArgs e)
    {
        CollapseSetupSections();
    }

    private void CollapseSetupSections()
    {
        ConditionsExpander.IsExpanded = false;
        BuoyExpander.IsExpanded = false;
        AnchorExpander.IsExpanded = false;
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

        var profile = viewModel.CurrentProfilePoints
            .Select(x => x.ToInput())
            .ToList();
        if (!CurrentProfileRequirement.IsUsable(profile))
        {
            viewModel.ProjectStatusText = CurrentProfileRequirement.UserMessage;
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

        if (!MainWindowPdfExportWorkflowBuilder.CanExport(viewModel.UserEngineeringReport))
        {
            viewModel.ProjectStatusText = MainWindowPdfExportWorkflowBuilder.BuildPreconditionStatus();
            return;
        }

        var suggestedFileName = MainWindowPdfExportWorkflowBuilder.BuildSuggestedFileName(viewModel.ProjectName);
        var path = await new AvaloniaPdfExportDialogService(this)
            .PickSavePathAsync(suggestedFileName);
        if (MainWindowPdfExportWorkflowBuilder.IsCanceled(path))
        {
            viewModel.ProjectStatusText = MainWindowPdfExportWorkflowBuilder.BuildCanceledStatus();
            return;
        }

        try
        {
            PdfReportBuilder.Build(path, viewModel.UserEngineeringReport!);
            viewModel.ProjectStatusText = MainWindowPdfExportWorkflowBuilder.BuildSuccessStatus(path);
        }
        catch (System.Exception ex)
        {
            viewModel.ProjectStatusText = MainWindowPdfExportWorkflowBuilder.BuildErrorStatus(ex.Message);
        }
    }
}
