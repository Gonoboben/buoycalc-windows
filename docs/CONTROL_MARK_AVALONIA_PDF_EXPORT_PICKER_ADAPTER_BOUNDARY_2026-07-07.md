# Control mark: Avalonia PDF export picker adapter boundary

Date: 2026-07-07
Scope: architecture stabilization / documentation only
Related issue: #138

This control mark records the exact native Avalonia PDF save-picker behavior before moving it from `Views/MainWindow.axaml.cs` into a dedicated service.

This document changes no production code, picker behavior, PDF renderer, report content, selected-shape source, solver, engineering physics, calculation inputs, project save/load, JSON, DTOs, XAML, 2D, public API, or user-facing output.

## Current call position

Inside `ExportPdfButton_Click(...)`, picker work occurs only after:

```text
1. DataContext is confirmed as MainWindowViewModel
2. ReportText is confirmed nonblank
3. suggestedFileName is built by MainWindowPdfExportWorkflowBuilder
```

The current sequence is:

```text
suggestedFileName
  -> StorageProvider.SaveFilePickerAsync(...)
  -> file?.Path.LocalPath
  -> cancellation decision
  -> renderer try/catch
```

Required behavior:

- no picker object or owner is accessed before the report precondition succeeds;
- `ProjectName` and suggested filename preparation remain before picker construction/invocation;
- cancellation routing remains after selected-path extraction;
- PDF report preparation and rendering remain after cancellation routing.

## Owner identity and construction timing

The picker currently uses the active `MainWindow` instance directly through its inherited `StorageProvider` property.

A later adapter extraction must be equivalent to:

```text
new AvaloniaPdfExportFileDialogService(this)
```

created at export-click time immediately before the picker call.

Required behavior:

- the exact active `MainWindow` object remains the owner;
- the adapter stores the same object reference in a readonly field;
- no persistent field is added to `MainWindow` in the first extraction;
- no adapter is constructed in the window constructor;
- no global window lookup, application lifetime lookup, service locator, singleton, or dependency-injection container is introduced;
- constructor timing therefore remains after report readiness and suggested filename preparation.

This timing matters because moving adapter construction to window startup would move any constructor failure from export-click time to window creation time.

## Adapter responsibility

The extracted adapter may own only:

```text
- retained Window owner
- native SaveFilePickerAsync call
- PDF file type list
- selected file Path.LocalPath conversion
```

It must not own:

```text
- report availability decisions
- suggested filename generation
- cancellation status decisions
- PdfReportStructureGuide.Apply(...)
- PdfReportBuilder.Build(...)
- ProjectStatusText publication
- path normalization or extension append
- file or directory creation
- retry, rollback, logging, or partial-file cleanup
```

## Exact save picker options

The current call is:

```text
StorageProvider.SaveFilePickerAsync(
    new FilePickerSaveOptions
    {
        Title = "Сохранить PDF-отчёт BuoyCalc",
        SuggestedFileName = suggestedFileName,
        DefaultExtension = "pdf",
        FileTypeChoices = PdfFileTypes
    })
```

Required behavior:

- exact title remains `Сохранить PDF-отчёт BuoyCalc`;
- the `suggestedFileName` argument is passed through unchanged;
- default extension remains exact text `pdf` without a leading dot;
- the existing PDF file-type list is used;
- no suggested start location, overwrite customization, MIME type, Apple type identifier, or additional picker option is added.

## PDF file type list

The current retained list has this exact order:

```text
new FilePickerFileType("PDF report")
{
    Patterns = new[] { "*.pdf" }
},
FilePickerFileTypes.All
```

Required behavior:

- custom PDF type remains first;
- `FilePickerFileTypes.All` remains second;
- custom display name remains exact text `PDF report`;
- custom pattern remains exact text `*.pdf`;
- no additional patterns or filters are added;
- save picker continues to receive the same retained list property rather than rebuilding options in `MainWindow`.

## Await and selected-path behavior

The current handler awaits the picker and then evaluates:

```text
var path = file?.Path.LocalPath;
```

The extracted service should expose an internal method equivalent to:

```text
Task<string?> PickSavePathAsync(string suggestedFileName)
```

and return:

```text
file?.Path.LocalPath
```

Required behavior:

- native cancellation or null selected file returns null;
- selected file returns the exact `Path.LocalPath` string;
- no trimming, blank conversion, URI conversion beyond existing `LocalPath`, full-path conversion, extension append, or normalization occurs;
- blank/whitespace handling remains the responsibility of `MainWindowPdfExportWorkflowBuilder.IsCanceled(...)` after the await.

## Exception boundary

Current picker work is outside the PDF renderer `try/catch`.

The following operations currently occur before that catch begins:

```text
- owner StorageProvider access
- FilePickerSaveOptions construction
- picker invocation
- awaited picker task
- selected file Path access
- LocalPath access
```

Required behavior:

- all corresponding adapter constructor, owner, provider, picker, await, path, and LocalPath exceptions continue to propagate from the `async void` handler;
- they must not be translated to `Ошибка экспорта PDF: ...`;
- no local try/catch is added inside the adapter;
- no outer try/catch is added around adapter invocation;
- the existing renderer catch continues to start only after a nonblank path has passed cancellation routing.

## Cancellation boundary

The adapter returns null for native cancellation. It does not decide whether a returned string is blank.

Current post-adapter behavior remains:

```text
if (MainWindowPdfExportWorkflowBuilder.IsCanceled(path))
{
    viewModel.ProjectStatusText = MainWindowPdfExportWorkflowBuilder.BuildCanceledStatus();
    return;
}
```

Required behavior:

- adapter cancellation remains null;
- blank and whitespace path detection remains in the workflow builder call;
- exact cancellation status remains published by `MainWindow`;
- no status or ViewModel dependency enters the adapter.

## MainWindow changes allowed by extraction

A production extraction may:

```text
- replace direct SaveFilePickerAsync/options/LocalPath code with one awaited adapter call
- remove PdfFileTypes from MainWindow
- remove imports that become unused
- add Services/AvaloniaPdfExportFileDialogService.cs
```

It must preserve the effective handler order:

```text
var suggestedFileName = ...;
var path = await new AvaloniaPdfExportFileDialogService(this)
    .PickSavePathAsync(suggestedFileName);
if (IsCanceled(path)) ...
try { prepare report; build PDF; publish success; }
catch { publish renderer error; }
```

## Import constraints

Before extraction, `MainWindow.axaml.cs` uses:

```text
System.Collections.Generic
Avalonia.Platform.Storage
```

only for `PdfFileTypes` and direct picker options.

After moving those responsibilities, these imports may be removed only if no remaining code in the file requires them.

Other imports and neighboring handlers must remain unchanged.

## Dedicated service direction

Suggested file:

```text
Services/AvaloniaPdfExportFileDialogService.cs
```

Suggested internal shape:

```text
internal sealed class AvaloniaPdfExportFileDialogService
{
    private readonly Window _owner;

    internal AvaloniaPdfExportFileDialogService(Window owner)
    {
        _owner = owner;
    }

    internal async Task<string?> PickSavePathAsync(string suggestedFileName)
    {
        var file = await _owner.StorageProvider.SaveFilePickerAsync(...);
        return file?.Path.LocalPath;
    }
}
```

No public interface is required for this location-only extraction.

## First implementation invariants

```text
- output unchanged
- owner object identity unchanged
- adapter construction remains at click time
- construction remains after suggested filename preparation
- exact picker title unchanged
- exact suggested filename pass-through unchanged
- default extension unchanged
- exact filter strings and order unchanged
- null cancellation unchanged
- exact LocalPath return unchanged
- picker exceptions remain outside renderer catch
- cancellation/status routing remains in MainWindow
- report preparation and PdfReportBuilder call remain unchanged
- no PDF content, selected-shape, report-store, solver, or physics changes
```

## Explicit exclusions

This boundary does not authorize changes to:

- `MainWindowPdfExportWorkflowBuilder` decisions or strings;
- `PdfReportStructureGuide` or report cleanup;
- `PdfReportBuilder` rendering, pages, diagrams, fonts, or layout;
- selected-shape and report-store source order;
- project file dialog adapter;
- solver, engineering formulas, calculation outputs, 2D, or 3D;
- XAML or public ViewModel API.
