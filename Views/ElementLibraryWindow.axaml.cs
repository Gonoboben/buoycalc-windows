using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BuoyCalc.Windows.Services;
using BuoyCalc.Windows.ViewModels;

namespace BuoyCalc.Windows.Views;

public partial class ElementLibraryWindow : Window
{
    public ElementLibraryWindow()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = new ElementLibraryViewModel();
    }

    private async void ExportLibraryButton_Click(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Экспортировать библиотеку элементов BuoyCalc",
            SuggestedFileName = $"BuoyCalc-library-{AppInfo.Version}.buoylib.json",
            DefaultExtension = "json",
            FileTypeChoices = LibraryFileTypes
        });

        if (file is null)
            return;

        try
        {
            var result = ElementLibraryBundleStorage.Export(file.Path.LocalPath);
            if (DataContext is ElementLibraryViewModel viewModel)
            {
                viewModel.StatusText = $"Библиотека экспортирована: {result.Total} пользовательских элементов. Встроенные пресеты не включены.";
            }
        }
        catch (Exception ex)
        {
            if (DataContext is ElementLibraryViewModel viewModel)
                viewModel.StatusText = $"Ошибка экспорта библиотеки: {ex.Message}";
        }
    }

    private async void ImportLibraryButton_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Импортировать библиотеку элементов BuoyCalc",
            AllowMultiple = false,
            FileTypeFilter = LibraryFileTypes
        });

        var file = files.FirstOrDefault();
        if (file is null)
            return;

        try
        {
            var result = ElementLibraryBundleStorage.ImportMerge(file.Path.LocalPath);
            var refreshed = new ElementLibraryViewModel
            {
                StatusText = $"Импорт завершён без замены текущих данных: добавлено {result.Imported}, пропущено {result.Skipped} совпадающих/недопустимых элементов. Встроенные пресеты не изменялись."
            };
            DataContext = refreshed;
        }
        catch (Exception ex)
        {
            if (DataContext is ElementLibraryViewModel viewModel)
                viewModel.StatusText = $"Ошибка импорта библиотеки: {ex.Message}";
        }
    }

    private static IReadOnlyList<FilePickerFileType> LibraryFileTypes { get; } = new[]
    {
        new FilePickerFileType("BuoyCalc element library")
        {
            Patterns = new[] { "*.buoylib.json", "*.json" }
        },
        FilePickerFileTypes.All
    };
}
