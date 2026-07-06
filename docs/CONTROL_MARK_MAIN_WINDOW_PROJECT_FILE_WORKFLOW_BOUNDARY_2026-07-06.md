# Control mark: main-window project file workflow boundary

Date: 2026-07-06
Scope: architecture stabilization / documentation only
Related issue: #127

This control mark records the current save/load workflow around `MainWindowViewModel`, `IProjectFileDialogService`, the Avalonia picker adapter, `MainWindowProjectDtoMapper`, and `ProjectJsonStorage`.

This document changes no production code, commands, dialogs, storage, JSON schema, DTO mapping, default project, collections, solver physics, formulas, calculation inputs, reports, PDF, 2D, XAML, public API, or user-facing output.

## Existing responsibility split

The current path is already divided into three responsibilities:

```text
MainWindowViewModel
  -> command orchestration, path decisions, statuses, ViewModel publication

IProjectFileDialogService / AvaloniaProjectFileDialogService
  -> native save/open picker calls and LocalPath conversion

MainWindowProjectDtoMapper + ProjectJsonStorage
  -> deterministic DTO mapping and JSON file I/O
```

The next architecture boundary must not merge these responsibilities into one service and must not move actual UI or I/O into a pure builder.

## Command construction

`MainWindowViewModel` creates these commands:

```text
SaveProjectCommand   = new RelayCommand(async () => await SaveProjectAsync(promptForPath: false))
SaveProjectAsCommand = new RelayCommand(async () => await SaveProjectAsync(promptForPath: true))
LoadProjectCommand   = new RelayCommand(async () => await LoadProjectAsync())
```

Required behavior:

- normal Save always passes `false`;
- Save As always passes `true`;
- Load has no prompt flag;
- all three commands use the existing `RelayCommand` construction;
- no `CanExecute` predicate, cancellation token, progress state, command replacement, or concurrent-execution guard is added;
- the awaited workflow remains inside the current command lambda.

## File-dialog dependency

The ViewModel stores an optional dependency:

```text
private readonly IProjectFileDialogService? _fileDialogService
```

The public constructor accepts `null` and preserves that value.

Required behavior:

- a ViewModel created without a dialog service remains supported;
- no mandatory UI dependency is introduced;
- fallback behavior differs between save and load and must remain exact;
- dialog calls remain outside any pure workflow decision builder.

## Save workflow exact order

`SaveProjectAsync(bool promptForPath)` is entirely inside one `try/catch`.

Current order:

```text
1. targetPath = ProjectFilePath
2. evaluate: promptForPath || string.IsNullOrWhiteSpace(targetPath)
3. when picker branch is required:
     a. suggestedFileName = MakeSafeFileName(ProjectName) + ".json"
     b. if dialog service exists:
          await PickSavePathAsync(suggestedFileName)
          replace null result with empty string
        otherwise:
          targetPath = ProjectJsonStorage.DefaultProjectPath
4. if targetPath is null/empty/whitespace:
     ProjectStatusText = "Сохранение отменено."
     return
5. targetPath = ProjectJsonStorage.NormalizeJsonPath(targetPath)
6. project = ToDto()
7. ProjectJsonStorage.Save(project, targetPath)
8. ProjectFilePath = targetPath
9. ProjectStatusText = $"Проект сохранён: {targetPath}"
10. on any exception:
      ProjectStatusText = $"Ошибка сохранения: {ex.Message}"
```

Required behavior:

- the current `ProjectFilePath` is read before the picker condition;
- normal Save with a nonblank path does not call the picker;
- Save As calls the picker regardless of the current path when a dialog service exists;
- normal Save with a blank path enters the same picker/fallback branch as Save As;
- the suggested filename is prepared only when that branch is entered;
- cancellation is checked before normalization and before DTO mapping;
- normalization occurs before `ToDto()`;
- DTO mapping completes before storage is called;
- `ProjectFilePath` changes only after storage returns successfully;
- success status is assigned after `ProjectFilePath`;
- no intermediate saving status is published.

## Save path selection matrix

The current decision matrix is:

```text
promptForPath = false, current path nonblank, dialog present/absent
  -> use current path, no picker

promptForPath = false, current path blank, dialog present
  -> call save picker

promptForPath = true, dialog present
  -> call save picker even when current path is nonblank

picker branch, dialog absent
  -> use ProjectJsonStorage.DefaultProjectPath
```

Consequences:

- a no-dialog Save As does not cancel; it saves to the default path;
- a no-dialog normal Save with a nonblank current path uses that current path;
- a null picker result becomes `string.Empty` and reaches the cancellation guard;
- no fallback to the previous path occurs after picker cancellation.

## Suggested save filename

The picker branch computes:

```text
MakeSafeFileName(ProjectName) + ".json"
```

`MakeSafeFileName(...)` currently performs:

```text
1. if null/empty/whitespace, use "BuoyCalc_Project"
2. otherwise trim leading and trailing whitespace
3. replace every Path.GetInvalidFileNameChars() character with '_'
4. replace every space character with '_'
5. return the result
```

Required behavior:

- fallback text remains exact;
- trimming occurs before character replacement;
- invalid characters are processed using the current platform result from `Path.GetInvalidFileNameChars()`;
- spaces are replaced after invalid characters;
- no repeated-underscore collapse, transliteration, case conversion, length limit, reserved-name handling, or extension detection is added;
- `".json"` is appended after this helper returns.

## Save picker adapter

`MainWindow` supplies a private nested `AvaloniaProjectFileDialogService`.

`PickSavePathAsync(suggestedFileName)` currently calls:

```text
_owner.StorageProvider.SaveFilePickerAsync(
    Title = "Сохранить проект BuoyCalc"
    SuggestedFileName = suggestedFileName
    DefaultExtension = "json"
    FileTypeChoices = ProjectFileTypes)
```

It returns:

```text
file?.Path.LocalPath
```

Required behavior:

- the ViewModel-provided suggested filename passes through unchanged;
- exact title and default extension remain unchanged;
- native cancellation returns `null` from the adapter;
- URI/path conversion remains the selected file's `Path.LocalPath`;
- no normalization or file creation occurs in the adapter.

## Project file types

The dialog adapter uses one retained read-only list:

```text
new FilePickerFileType("BuoyCalc project")
{
    Patterns = new[] { "*.json" }
},
FilePickerFileTypes.All
```

Required behavior:

- custom JSON type remains first;
- the all-files option remains present and second;
- picker filters are not moved into the ViewModel or storage layer;
- no file-type restriction is added to the pure workflow boundary.

## Save normalization and storage

Before storage, the ViewModel calls:

```text
ProjectJsonStorage.NormalizeJsonPath(targetPath)
```

`ProjectJsonStorage.Save(...)` calls the same normalization again.

`NormalizeJsonPath(...)` currently behaves as follows:

```text
blank path
  -> DefaultProjectPath

extension equals ".json" using OrdinalIgnoreCase
  -> unchanged path

otherwise
  -> path + ".json"
```

Required behavior:

- the ViewModel's cancellation guard prevents its normal blank path from reaching normalization;
- extension matching remains case-insensitive ordinal comparison;
- paths with another extension receive an additional `.json` suffix rather than replacement;
- double normalization remains harmless and observable only through the same final path;
- a later builder must not remove the storage layer's own normalization in this phase.

`ProjectJsonStorage.Save(...)` then:

```text
1. normalize path again
2. resolve directory
3. create the directory when nonblank
4. serialize with WriteIndented = true
5. File.WriteAllText(path, json)
```

Storage behavior remains outside the main-window workflow decision boundary.

## Save evaluation and exception timing

The `try/catch` includes:

- reading the current properties used by the method;
- suggested-name preparation;
- the awaited picker call;
- path normalization;
- `ToDto()` and all row conversion performed by the mapper;
- directory creation, serialization, and file writing;
- `ProjectFilePath` publication;
- success-status publication.

Required behavior:

- any exception from these stages is translated to the exact save-error status;
- the exception is not rethrown by this method;
- no exception type filtering or retry is introduced;
- mutation completed before an exception is not rolled back;
- if storage fails, the old `ProjectFilePath` remains;
- if `ProjectFilePath` publication throws, storage may already have succeeded and the success status is skipped;
- if final status publication throws, the file and path publication may already be complete.

## Load workflow exact order

`LoadProjectAsync()` is entirely inside one `try/catch`.

Current order:

```text
1. if dialog service exists:
     await PickOpenPathAsync()
     replace null result with empty string
   otherwise:
     selectedPath = ProjectFilePath
2. if selectedPath is null/empty/whitespace:
     ProjectStatusText = "Загрузка отменена."
     return
3. dto = ProjectJsonStorage.Load(selectedPath)
4. if dto is null:
     ProjectStatusText = $"Файл проекта не найден: {selectedPath}"
     return
5. FromDto(dto)
6. ProjectFilePath = selectedPath
7. ProjectStatusText = $"Проект загружен: {selectedPath}"
8. on any exception:
     ProjectStatusText = $"Ошибка загрузки: {ex.Message}"
```

Required behavior:

- when a dialog service exists, Load always calls the open picker and does not first reuse `ProjectFilePath`;
- when no dialog service exists, Load uses `ProjectFilePath` unchanged;
- cancellation/blank guard runs before storage;
- storage completes before any `FromDto()` application;
- a null DTO prevents all restore mutation;
- `FromDto()` completes before `ProjectFilePath` is changed;
- the selected path is assigned without normalization;
- final success status is assigned after the path;
- no intermediate loading status is published.

## Open picker adapter

`PickOpenPathAsync()` currently calls:

```text
_owner.StorageProvider.OpenFilePickerAsync(
    Title = "Открыть проект BuoyCalc"
    AllowMultiple = false
    FileTypeFilter = ProjectFileTypes)
```

It returns:

```text
files.FirstOrDefault()?.Path.LocalPath
```

Required behavior:

- exact title remains unchanged;
- multiple selection remains disabled;
- the same project file-type list is used;
- even if a provider returns more than one entry, only `FirstOrDefault()` is read;
- no selection returns `null`;
- LocalPath conversion remains in the adapter.

## Load storage behavior

`ProjectJsonStorage.Load(path)` currently performs:

```text
if path is blank or File.Exists(path) is false
    return null

json = File.ReadAllText(path)
return JsonSerializer.Deserialize<BuoyProjectDto>(json)
```

Required behavior:

- load does not normalize or append an extension;
- a path selected without `.json` is attempted exactly as supplied;
- blank/missing paths return null rather than throwing from storage;
- a JSON `null` result also reaches the ViewModel null guard;
- invalid JSON, read errors, and deserialization exceptions propagate to the ViewModel catch;
- serializer options and DTO format remain outside this boundary.

## Missing/null load behavior

When storage returns null, the exact status is:

```text
Файл проекта не найден: {selectedPath}
```

This wording is used for all storage-null cases, including:

- a missing file;
- a storage blank-path return if the ViewModel guard were bypassed;
- deserialization returning null without throwing.

Required behavior:

- the wording is not changed to distinguish these causes;
- `FromDto()` is not called;
- `ProjectFilePath` is not changed;
- no fallback to the default project occurs.

## Restore and publication timing

For a non-null DTO:

```text
FromDto(dto)
ProjectFilePath = selectedPath
ProjectStatusText = success text
```

Required behavior:

- all existing scalar assignments, library refreshes, collection clears/additions, handler wiring, fallback resets, and repeated display publications inside `FromDto()` remain before path publication;
- `ProjectFilePath` continues to describe the newly selected file only after restore succeeds;
- selected path casing, extension, and spelling remain exactly as returned by the dialog or current property;
- no canonicalization, full-path conversion, or `.json` normalization is introduced.

## Load exception and partial-state behavior

The `try/catch` includes:

- the awaited open picker call;
- storage existence/read/deserialization;
- all `FromDto()` work;
- `ProjectFilePath` publication;
- final status publication.

Required behavior:

- every exception is translated to `Ошибка загрузки: {ex.Message}`;
- no exception is rethrown by this method;
- `FromDto()` may already have changed part of the ViewModel before a later assignment, collection event, library operation, or subscriber throws;
- such partial restore state is retained;
- the old `ProjectFilePath` remains when failure occurs before its assignment;
- no snapshot, rollback, transaction, retry, or default-project recovery is added.

## Status strings

The exact current workflow statuses are:

```text
Сохранение отменено.
Проект сохранён: {path}
Ошибка сохранения: {exception message}

Загрузка отменена.
Файл проекта не найден: {path}
Проект загружен: {path}
Ошибка загрузки: {exception message}
```

Required behavior:

- punctuation, capitalization, and interpolation remain exact;
- statuses continue to be assigned through `ProjectStatusText`;
- no status enum, localization change, notification popup, or logging replacement is introduced in the first boundary extraction.

## Pure decision boundary direction

A later production PR may add a small internal builder such as:

```text
MainWindowProjectFileWorkflowBuilder
```

It may return immutable decisions for:

```text
ShouldRequestSavePath(promptForPath, currentPath)
BuildSuggestedProjectFileName(projectName)
ResolveSavePickerResult(dialogAvailable, pickerResult, defaultPath)
ResolveSaveCancellation(targetPath)
NormalizeSaveTarget(targetPath)
ResolveLoadSelection(dialogAvailable, pickerResult, currentPath)
ResolveLoadCancellation(selectedPath)
BuildMissingLoadStatus(selectedPath)
BuildSaveSuccessStatus(path)
BuildLoadSuccessStatus(path)
BuildSaveErrorStatus(message)
BuildLoadErrorStatus(message)
```

The exact method split may be smaller, but the builder must remain pure and deterministic.

It must not:

- await or invoke file pickers;
- access Avalonia `StorageProvider`;
- read or write files;
- call `ToDto()`, `FromDto()`, or `ProjectJsonStorage`;
- mutate `ProjectFilePath` or `ProjectStatusText`;
- own exceptions, retries, rollback, or command execution.

`MainWindowViewModel` must retain orchestration and publication. The Avalonia adapter must retain native picker configuration. `ProjectJsonStorage` must retain JSON I/O.

## First implementation invariants

```text
- Save flag false; Save As flag true
- optional dialog dependency remains optional
- normal Save reuses nonblank current path
- picker branch uses exact suggested filename behavior
- no-dialog picker branch falls back to DefaultProjectPath
- exact cancellation guards and strings
- save normalize -> ToDto -> storage -> path -> status
- storage performs its own second normalization
- Load with dialog always opens picker
- Load without dialog reuses current path
- load storage -> null guard -> FromDto -> unnormalized selected path -> status
- picker awaits remain within try/catch
- exact success/error statuses
- partial state and exception timing preserved
- output unchanged
```

## Explicit exclusions

This boundary does not authorize changes to:

- JSON format, DTO fields, serialization options, or migrations;
- project restore precedence or compatibility behavior;
- default project data or library content;
- dialog titles, filters, or native picker behavior;
- autosave, recent projects, dirty-state tracking, confirmations, or recovery;
- solver, engineering physics, reports, PDF, 2D, XAML, public API, or 3D.
