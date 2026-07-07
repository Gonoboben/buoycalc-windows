using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace BuoyCalc.Windows.Services;

internal sealed class AvaloniaPdfExportDialogService
{
    private readonly Window _owner;

    internal AvaloniaPdfExportDialogService(Window owner)
    {
        _owner = owner;
    }

    internal async Task<string?> PickSavePathAsync(string suggestedFileName)
    {
        var file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить PDF-отчёт BuoyCalc",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "pdf",
            FileTypeChoices = PdfFileTypes
        });

        return file?.Path.LocalPath;
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
