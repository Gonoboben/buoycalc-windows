# Control mark: Avalonia project file dialog adapter boundary

Date: 2026-07-07
Scope: architecture stabilization / documentation only
Related issue: #131

This control mark records the exact behavior of the Avalonia implementation of `IProjectFileDialogService` before moving it from the private nested class in `Views/MainWindow.axaml.cs` into a dedicated service file.

This document changes no production code, commands, picker configuration, storage, DTOs, JSON, XAML, default project, collections, solver, engineering physics, reports, PDF, 2D, public API, or user-facing output.

## Current construction and ownership

`MainWindow` currently performs the following constructor sequence:

```text
AvaloniaXamlLoader.Load(this)
WindowVersionHelper.Apply(this, "BuoyCalc Windows")
ApplyMainWindowTextOverrides()
DataContext = new MainWindowViewModel(new AvaloniaProjectFileDialogService(this))
```

The adapter is created only after the window XAML is loaded and the existing version/text overrides have run.

The exact current owner argument is the active `MainWindow` instance (`this`). The adapter stores this same object reference in a readonly field:

```text
private readonly Window _owner
```

Required behavior:

- construction order in `MainWindow()` remains unchanged;
- `MainWindowViewModel` still receives a newly constructed adapter;
- the exact active window instance remains the picker owner;
- no global window lookup, service locator, application lifetime lookup, or dependency-injection container is introduced;
- the adapter lifetime remains tied to the ViewModel/window object graph.

## Existing interface

The adapter implements the existing public interface:

```text
Task<string?> PickSavePathAsync(string suggestedFileName)
Task<string?> PickOpenPathAsync()
```

Required behavior:

- the interface remains unchanged;
- return types and nullable cancellation behavior remain unchanged;
- no additional methods, cancellation tokens, status callbacks, validation callbacks, or storage methods are added;
- the extraction must not broaden the adapter into project-file orchestration.

## Save picker exact behavior

`PickSavePathAsync(suggestedFileName)` currently awaits:

```text
_owner.StorageProvider.SaveFilePickerAsync(
    new FilePickerSaveOptions
    {
        Title = "Сохранить проект BuoyCalc",
        SuggestedFileName = suggestedFileName,
        DefaultExtension = "json",
        FileTypeChoices = ProjectFileTypes
    })
```

After the await, it returns:

```text
file?.Path.LocalPath
```

Required behavior:

- the exact Russian title remains unchanged;
- the `suggestedFileName` argument is passed through exactly as received;
- no trimming, normalization, extension replacement, or fallback occurs in the adapter;
- the default extension remains exact text `json` without a leading dot;
- the existing project file-type list is used directly;
- native picker cancellation or a null file result returns null;
- a selected file returns the selected storage item's `Path.LocalPath`;
- no directory creation, file creation, JSON write, or path existence check occurs here.

## Open picker exact behavior

`PickOpenPathAsync()` currently awaits:

```text
_owner.StorageProvider.OpenFilePickerAsync(
    new FilePickerOpenOptions
    {
        Title = "Открыть проект BuoyCalc",
        AllowMultiple = false,
        FileTypeFilter = ProjectFileTypes
    })
```

After the await, it returns:

```text
files.FirstOrDefault()?.Path.LocalPath
```

Required behavior:

- the exact Russian title remains unchanged;
- multiple selection remains disabled;
- the same retained project file-type list is used as the filter;
- no selection returns null;
- if a provider nevertheless returns more than one entry, only `FirstOrDefault()` is used;
- the selected storage item's `Path.LocalPath` is returned unchanged;
- no path normalization, extension append, existence check, JSON read, or DTO operation occurs here.

## Project file-type list

The adapter currently retains one static read-only property initialized with this exact order:

```text
new FilePickerFileType("BuoyCalc project")
{
    Patterns = new[] { "*.json" }
},
FilePickerFileTypes.All
```

Required behavior:

- custom project type remains first;
- `FilePickerFileTypes.All` remains second;
- custom display name remains exact text `BuoyCalc project`;
- custom pattern remains exact text `*.json`;
- save and open pickers continue to share the same list instance/property;
- no MIME types, Apple uniform type identifiers, additional extensions, or reordered filters are added;
- moving the property to another file must not change initialization timing in any user-observable way.

## Await and exception behavior

Both methods are ordinary `async Task<string?>` methods with no local `try/catch`.

Required behavior:

- access to `_owner.StorageProvider` remains part of the adapter call;
- exceptions thrown while preparing or invoking the picker continue to propagate;
- exceptions raised by the awaited picker task continue to propagate after await resumption;
- exceptions accessing `Path` or `LocalPath` continue to propagate;
- no exception translation, logging replacement, retry, fallback path, or null-to-empty conversion is added;
- the existing `MainWindowViewModel` save/load workflow remains responsible for catching propagated exceptions and publishing its existing status text.

## Cancellation boundary

The adapter represents native cancellation only as null:

```text
save cancellation -> null
open cancellation -> null
```

The adapter does not convert null to `string.Empty` and does not publish cancellation text.

Required behavior:

- null remains the adapter-level cancellation result;
- the ViewModel workflow continues to resolve null and decide cancellation;
- no dialog-specific result model or user-facing status moves into this service.

## Separation from project workflow

The adapter must remain unaware of:

- `MainWindowProjectFileWorkflowBuilder`;
- Save versus Save As flags;
- `ProjectFilePath`;
- `ProjectName` beyond the exact suggested filename argument already supplied;
- `ProjectJsonStorage.DefaultProjectPath`;
- path normalization;
- `ToDto()` / `FromDto()`;
- JSON serialization or deserialization;
- `ProjectStatusText`;
- missing-file, success, or error messages.

The extraction changes location only, not responsibility.

## Extraction target

A later production PR may add a dedicated file such as:

```text
Services/AvaloniaProjectFileDialogService.cs
```

The class may become `internal sealed` so `MainWindow` can construct it, while the existing `IProjectFileDialogService` remains public and unchanged.

The extracted class must continue to:

```text
- accept Window owner in its constructor
- retain owner in a readonly field
- implement IProjectFileDialogService
- own the same shared ProjectFileTypes property
- issue the same save/open picker calls
- return the same LocalPath/null results
```

`MainWindow.axaml.cs` may remove only imports that become unused after the nested adapter and project filter list are removed. PDF export picker imports and PDF file-type configuration must remain intact.

## MainWindow invariants

After extraction, `MainWindow` must still effectively execute:

```text
DataContext = new MainWindowViewModel(
    new AvaloniaProjectFileDialogService(this));
```

Required behavior:

- the class name used at the construction site remains the same unless a purely internal rename is separately justified;
- the constructor call remains at the same point in the `MainWindow` constructor;
- no factory, singleton, lazy creation, or reused adapter instance is introduced;
- no changes are made to child-window opening, calculation confirmation, 2D, full-report viewing, PDF export, or library refresh.

## Imports and neighboring code

The current nested adapter uses:

```text
System.Linq
System.Threading.Tasks
Avalonia.Controls
Avalonia.Platform.Storage
BuoyCalc.Windows.Services
```

After extraction:

- `MainWindow.axaml.cs` must keep imports still required by PDF export and other code;
- removal of `System.Linq` or `System.Threading.Tasks` is allowed only if they are no longer used elsewhere in that file;
- `Avalonia.Platform.Storage` remains required by the PDF export picker and PDF file types;
- the production diff must not alter `PdfFileTypes` or `ExportPdfButton_Click`.

## First implementation invariants

```text
- output unchanged
- interface unchanged
- owner object identity unchanged
- MainWindow constructor order unchanged
- exact save picker options unchanged
- exact open picker options unchanged
- exact project filter order and strings unchanged
- exact LocalPath and FirstOrDefault behavior unchanged
- null cancellation unchanged
- exceptions still propagate to MainWindowViewModel
- no storage, DTO, status, or workflow decisions move into adapter
- no PDF picker changes
```

## Explicit exclusions

This boundary does not authorize changes to:

- save/load decision logic completed by #127;
- project JSON schema or storage;
- project DTO mapping or restore behavior;
- native picker wording, filters, extension, or owner selection;
- PDF export dialog or PDF generation;
- XAML or window layout;
- solver, engineering formulas, calculation inputs, reports, 2D, or 3D.
