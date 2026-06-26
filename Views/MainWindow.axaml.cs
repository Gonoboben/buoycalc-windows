using System.Collections.Generic;
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
