using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace BuoyCalc.Windows.Services;

internal sealed class AvaloniaProjectFileDialogService : IProjectFileDialogService
{
    private readonly Window _owner;

    internal AvaloniaProjectFileDialogService(Window owner)
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
