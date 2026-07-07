# Control mark: main-window PDF export workflow boundary

Date: 2026-07-07
Scope: architecture stabilization / documentation only
Related issue: #134

This control mark records the exact PDF export workflow currently implemented by `Views/MainWindow.axaml.cs`.

This document changes no production code, picker configuration, PDF content, report cleanup, diagram source, selected-shape behavior, solver, engineering physics, calculation inputs, JSON, DTOs, project save/load, XAML, 2D, public API, or user-facing output.

## Current handler boundary

The workflow is an Avalonia event handler:

```text
private async void ExportPdfButton_Click(object? sender, RoutedEventArgs e)
```

Required behavior:

- it remains an `async void` UI event handler in the first extraction step;
- sender and event arguments remain unused;
- no command conversion, `Task`-returning wrapper, cancellation token, progress state, or concurrency guard is introduced;
- UI orchestration remains in `MainWindow`.

## DataContext guard

The first operation is:

```text
if (DataContext is not MainWindowViewModel viewModel)
{
    return;
}
```

Required behavior:

- the active `DataContext` is tested exactly once at handler entry;
- failure returns silently;
- no status text, dialog, exception, logging, or fallback ViewModel is produced;
- no ViewModel property is read before this guard succeeds.

## Report availability guard

After the DataContext guard, the handler reads:

```text
viewModel.ReportText
```

and applies:

```text
string.IsNullOrWhiteSpace(viewModel.ReportText)
```

When true, it publishes the exact status:

```text
Сначала выполните расчёт, затем экспортируйте PDF.
```

and returns.

Required behavior:

- null, empty, and whitespace-only report text all block export;
- the precondition status is assigned through `viewModel.ProjectStatusText`;
- no picker is opened;
- `ProjectName`, result fields, collections, and visualization fields are not read;
- report cleanup and PDF generation are not invoked.

## Suggested filename

Only after the report availability guard succeeds, the handler reads `viewModel.ProjectName` and computes:

```text
MakeSafeFileName(viewModel.ProjectName) + "_report.pdf"
```

`MakeSafeFileName(...)` currently performs:

```text
1. blank name -> "BuoyCalc_Project"
2. otherwise trim leading/trailing whitespace
3. replace each Path.GetInvalidFileNameChars() character with '_'
4. replace each ordinary space character with '_'
5. return the result
```

The exact suffix is appended after the helper returns:

```text
_report.pdf
```

Required behavior:

- `ProjectName` is not read until the report precondition has passed;
- the fallback name remains `BuoyCalc_Project`;
- invalid filename characters use the current platform result of `Path.GetInvalidFileNameChars()`;
- spaces are replaced after invalid-character replacement;
- no underscore collapse, transliteration, reserved-name handling, case conversion, path normalization, length limit, or existing-extension detection is added;
- a project name already ending in `.pdf` still receives `_report.pdf`.

## PDF save picker

The handler awaits the active window storage provider directly:

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

- the picker remains owned by the active `MainWindow` storage provider;
- exact title remains `Сохранить PDF-отчёт BuoyCalc`;
- the prepared suggested filename passes through unchanged;
- default extension remains exact text `pdf` without a leading dot;
- the existing `PdfFileTypes` list is used;
- no default directory, overwrite prompt customization, path fallback, or adapter is introduced in the first decision-boundary step.

## PDF file types

`PdfFileTypes` currently contains this exact order:

```text
new FilePickerFileType("PDF report")
{
    Patterns = new[] { "*.pdf" }
},
FilePickerFileTypes.All
```

Required behavior:

- custom PDF type remains first;
- all-files remains second;
- custom display name remains `PDF report`;
- pattern remains `*.pdf`;
- no MIME type, Apple uniform type identifier, additional pattern, or filter reordering is added.

## Picker exception boundary

Suggested filename preparation, picker option construction, picker invocation, picker await, and selected-path extraction all occur before the existing PDF generation `try/catch`.

Required behavior:

- exceptions from `MakeSafeFileName(...)`, `StorageProvider`, picker invocation, awaited picker task, `file.Path`, or `LocalPath` are not translated by the inner `Ошибка экспорта PDF` catch;
- no new outer `try/catch` is added in the first behavior-preserving routing step;
- the current `async void` exception behavior remains unchanged;
- deterministic decision extraction must not accidentally widen the catch scope.

## Selected path and cancellation

After the picker returns, the handler executes:

```text
var path = file?.Path.LocalPath;
```

Then:

```text
if (string.IsNullOrWhiteSpace(path))
{
    viewModel.ProjectStatusText = "Экспорт PDF отменён.";
    return;
}
```

Required behavior:

- a null selected file produces null path;
- blank and whitespace paths are also treated as cancellation;
- exact cancellation status remains `Экспорт PDF отменён.`;
- cancellation occurs before report cleanup and PDF generation;
- no extension append, normalization, full-path conversion, directory creation, or previous-path fallback is performed by `MainWindow`.

## PDF generation try/catch

Only after a nonblank selected path is available does the existing `try/catch` begin.

Current order inside `try`:

```text
1. pdfReportText = PdfReportStructureGuide.Apply(viewModel.ReportText)
2. PdfReportBuilder.Build(...)
3. viewModel.ProjectStatusText = $"PDF сохранён: {path}"
```

Current catch:

```text
catch (System.Exception ex)
{
    viewModel.ProjectStatusText = $"Ошибка экспорта PDF: {ex.Message}";
}
```

Required behavior:

- report preparation remains the first operation in the `try`;
- the report text is read again at preparation time rather than relying on the earlier guard value;
- renderer invocation follows preparation;
- success status is assigned only after `PdfReportBuilder.Build(...)` returns;
- every `System.Exception` from preparation, property reads during argument evaluation, renderer work, or success-status publication is translated to the exact error status;
- the caught exception is not rethrown by this handler;
- no retry, rollback, cleanup, or partially written file deletion is added.

## Report preparation

The exact preparation call is:

```text
PdfReportStructureGuide.Apply(viewModel.ReportText)
```

`PdfReportStructureGuide.Apply(...)` currently:

```text
1. converts null report text to string.Empty
2. applies PdfReportTextCleanup.Apply(...)
3. returns cleaned text unchanged when it already contains the v0.46.1 structure heading
4. otherwise prepends the structure-guide Markdown block
```

Required behavior:

- this service remains outside the pure workflow decision builder;
- cleanup, structure heading detection, Markdown content, and report semantics are unchanged;
- the prepared string remains local to the export handler and is passed to the renderer.

## Renderer call and argument order

The exact call is:

```text
PdfReportBuilder.Build(
    path,
    viewModel.ProjectName,
    viewModel.ResultText,
    viewModel.SequenceDiagramLines,
    viewModel.ElementRows,
    pdfReportText,
    viewModel.VisualizationDepthM,
    viewModel.VisualizationLineLengthM,
    viewModel.VisualizationOffsetM)
```

Required evaluation and argument behavior:

- `path` is the exact picker `LocalPath` value;
- `ProjectName` is read again inside the renderer-call argument evaluation;
- `ResultText` is read after `ProjectName`;
- the existing `SequenceDiagramLines` collection object is passed, not a copied list;
- the existing `ElementRows` collection object is passed, not a copied list;
- the locally prepared report string is passed after the collections;
- visualization depth, line length, and offset are read in that order;
- no read model, snapshot, sorting, filtering, collection cloning, or argument reordering is introduced in the first routing step;
- `PdfReportBuilder` remains responsible for PDF rendering and its existing source reads.

## Success and failure statuses

Exact statuses:

```text
precondition:
Сначала выполните расчёт, затем экспортируйте PDF.

cancellation:
Экспорт PDF отменён.

success:
PDF сохранён: {path}

caught error:
Ошибка экспорта PDF: {exception message}
```

Required behavior:

- punctuation, capitalization, spacing, and interpolation remain exact;
- statuses continue to be assigned through `MainWindowViewModel.ProjectStatusText`;
- no localization, status enum, notification, message box, or logging replacement is introduced.

## Partial-output and exception timing

`PdfReportBuilder.Build(...)` may create or modify the target file before a later renderer exception.

Required behavior:

- the handler does not delete or restore a partially written target file;
- success status is not published when renderer work throws;
- error status may be published after partial file output exists;
- if success-status publication itself throws, the PDF may already be complete and the catch then attempts error-status publication;
- if error-status publication throws, that later exception is not handled by another local catch;
- no transaction or temporary-file replacement is added in this boundary work.

## Pure decision boundary direction

A later production PR may add an internal pure builder such as:

```text
MainWindowPdfExportWorkflowBuilder
```

It may own deterministic operations such as:

```text
CanExport(reportText)
BuildSuggestedFileName(projectName)
IsCanceled(path)
BuildPreconditionStatus()
BuildCanceledStatus()
BuildSuccessStatus(path)
BuildErrorStatus(message)
```

It must not:

- access Avalonia controls, `StorageProvider`, or picker types;
- await or invoke the picker;
- access `MainWindowViewModel` directly;
- call `PdfReportStructureGuide` or `PdfReportBuilder`;
- read selected-shape/report stores;
- mutate status text;
- normalize paths or perform file I/O;
- change the current catch scope.

## First implementation invariants

```text
- output unchanged
- silent DataContext failure unchanged
- report precondition and exact status unchanged
- ProjectName read remains after precondition
- exact safe-name algorithm and suffix unchanged
- picker options and filter list unchanged
- picker remains outside PDF generation try/catch
- LocalPath and blank cancellation unchanged
- report preparation remains first inside try
- renderer argument order and collection identity unchanged
- exact success/error statuses unchanged
- partial-output behavior unchanged
- no renderer, report, diagram, shape, or physics changes
```

## Explicit exclusions

This boundary does not authorize changes to:

- `PdfReportBuilder` layout or rendering;
- `PdfReportStructureGuide` or `PdfReportTextCleanup` content;
- PDF diagram/read-model source order;
- selected shape or report stores;
- solver, calculations, formulas, or engineering diagnostics;
- 2D rendering;
- project save/load;
- XAML or public ViewModel API;
- 3D.
