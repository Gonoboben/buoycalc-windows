# MainWindow UI orchestration closure

Date: 2026-07-07
Scope: architecture stabilization / documentation only
Related issue: #141

This document closes the architecture-stabilization review of `Views/MainWindow.axaml.cs` after the project-file and PDF-export boundaries were separated.

This record changes no production code, window behavior, commands, dialogs, reports, PDF rendering, 2D, project storage, DTOs, JSON, solver, engineering physics, calculation inputs, public API, XAML, or user-facing output.

## Historical context

The original `docs/ARCHITECTURE_AUDIT.md` was written on 2026-06-29. At that time `MainWindow.axaml.cs` combined:

```text
- window construction
- runtime text overrides
- child-window orchestration
- project file dialog implementation
- PDF export decisions
- PDF save-picker configuration
- PDF renderer invocation
```

The following later boundaries supersede part of that historical observation:

```text
#127  main-window project file workflow boundary
#131  Avalonia project file dialog adapter boundary
#134  main-window PDF export workflow boundary
#138  Avalonia PDF export picker adapter boundary
```

Current `MainWindow.axaml.cs` no longer contains:

```text
- project save/load decision logic
- project picker implementation
- PDF suggested-name/status decision implementation
- native PDF picker options or file-type list
- PDF selected-file LocalPath conversion
```

It still intentionally contains Avalonia UI orchestration and the imperative PDF renderer call.

## Current constructor behavior

The constructor remains:

```text
AvaloniaXamlLoader.Load(this)
WindowVersionHelper.Apply(this, "BuoyCalc Windows")
ApplyMainWindowTextOverrides()
DataContext = new MainWindowViewModel(
    new AvaloniaProjectFileDialogService(this))
```

Required interpretation:

- XAML loading is a window responsibility;
- window version application is presentation setup;
- runtime text override remains a presentation compatibility step;
- the active window remains the owner captured by the project dialog adapter;
- ViewModel construction remains composition-root work for this small desktop application.

A dependency-injection container or separate composition-root framework is not justified solely to remove these four constructor statements.

## Runtime text overrides

`ApplyMainWindowTextOverrides()` currently traverses visual descendants and changes two exact legacy strings:

```text
Отчёт текстом... -> Полный отчёт...
v0.21.3 cleanup -> AppInfo.DisplayVersion
```

This is still a compatibility workaround for legacy XAML text.

Closure decision:

- it may remain in code-behind during the current stabilization block;
- replacing the legacy strings directly in XAML is a separate user-interface cleanup, not an orchestration-boundary requirement;
- no generic text-replacement service should be created;
- future XAML cleanup should remove the corresponding override branch only after confirming output equivalence.

## Library window handler

Current sequence:

```text
1. construct ElementLibraryWindow
2. await libraryWindow.ShowDialog(this)
3. after dialog close, read the current DataContext
4. when it is MainWindowViewModel, execute RefreshBuoyLibraryCommand
```

Important timing behavior:

- there is no DataContext guard before opening the library;
- the library can open even if the main window DataContext is absent or replaced;
- the DataContext is evaluated after the modal await, not captured before it;
- refresh uses the then-current MainWindow ViewModel;
- no refresh occurs when the post-dialog DataContext is not `MainWindowViewModel`;
- command exceptions propagate from the `async void` handler.

Closure decision:

This is UI orchestration with meaningful post-dialog timing. It should remain explicit. A shared dialog helper that captures the ViewModel before the await would change behavior.

## Current-profile window handler

Current sequence:

```text
1. read DataContext at handler entry
2. silently return unless it is MainWindowViewModel
3. construct CurrentProfileWindow
4. assign the captured ViewModel as child DataContext
5. await window.ShowDialog(this)
```

Important behavior:

- the child receives the exact captured ViewModel object;
- later replacement of the main-window DataContext does not change the child DataContext;
- the active MainWindow remains modal owner;
- no status is published when the entry guard fails;
- no work occurs after the modal await.

Closure decision:

This is a conventional and appropriate Avalonia code-behind handler. Extracting a generic `OpenWindow<T>()` helper would hide ownership and DataContext identity without removing domain responsibility.

## Calculation-preview handler

Current sequence:

```text
1. read DataContext at handler entry
2. silently return unless it is MainWindowViewModel
3. construct SequencePreviewWindow
4. assign the captured ViewModel as child DataContext
5. await ShowDialog<bool>(this)
6. only when the returned value is true, execute CalculateCommand with null
```

Important behavior:

- calculation is not started before preview confirmation;
- false, default false, or window cancellation causes no command execution;
- the command is read from the captured ViewModel after the await;
- command execution remains UI-trigger orchestration; engineering work stays in the ViewModel/core path;
- no status is published for a canceled preview;
- exceptions from preview or command execution are not translated locally.

Closure decision:

The `if (confirmed)` branch is already the smallest clear representation of the behavior. Moving it to a decision builder would add indirection without creating a reusable domain boundary.

## 2D window handler

Current sequence:

```text
1. read DataContext at handler entry
2. silently return unless it is MainWindowViewModel
3. construct Mooring2DWindow
4. assign the captured ViewModel as child DataContext
5. await window.ShowDialog(this)
```

Important behavior:

- the handler does not calculate or choose shape data;
- it passes the same ViewModel object to the display window;
- source-selection and rendering boundaries remain elsewhere;
- the active MainWindow remains modal owner;
- no post-dialog action occurs.

Closure decision:

This is pure presentation orchestration and should remain in code-behind. It is not evidence that 2D source selection belongs in `MainWindow`.

## Full-report window handler

Current sequence:

```text
1. read DataContext at handler entry
2. silently return unless it is MainWindowViewModel
3. read viewModel.ReportText
4. when null/empty/whitespace:
     publish exact status
     return
5. construct ReportTextWindow
6. assign the captured ViewModel as child DataContext
7. await window.ShowDialog(this)
```

Exact blocked status:

```text
Сначала выполните расчёт, затем откройте полный отчёт.
```

Important behavior:

- `ReportText` is read only after the DataContext guard succeeds;
- no report window is constructed for blank text;
- status publication remains on the captured ViewModel;
- the report window receives the exact captured ViewModel object;
- no post-dialog action occurs.

Closure decision:

The blank-report check is specific to opening the technical report. It should not reuse `MainWindowPdfExportWorkflowBuilder.CanExport(...)`, because that builder represents a different workflow and different status contract. Identical blank-string mechanics do not justify coupling the two UI actions.

## PDF export handler after stabilization

The current handler intentionally retains:

```text
- DataContext read
- ReportText readiness read
- suggested filename builder call
- click-time Avalonia PDF dialog service construction
- cancellation/status routing
- PdfReportStructureGuide.Apply(...)
- PdfReportBuilder.Build(...)
- success/error status publication
```

Separated responsibilities:

```text
MainWindowPdfExportWorkflowBuilder
  deterministic readiness, filename, cancellation, status strings

AvaloniaPdfExportDialogService
  owner, native picker options, await, LocalPath conversion

PdfReportStructureGuide / PdfReportBuilder
  report preparation and PDF rendering
```

The renderer call remains in the UI handler because moving it again without a stable PDF export application service would only relocate imperative argument passing. A future change should be motivated by a concrete export feature such as testable export jobs, batch export, progress, or alternative output targets.

## Why no generic dialog service is added

The five remaining window handlers are superficially similar but semantically different:

| Handler | DataContext timing | Result | Post-dialog action |
|---|---|---|---|
| Library | checked after await | none | refresh current VM library |
| Current profile | captured before construction | none | none |
| Calculation preview | captured before construction | bool | execute command only on true |
| 2D | captured before construction | none | none |
| Full report | captured before construction plus report guard | none | none |

A generic helper would need callbacks, result generics, guard callbacks, post-await callbacks, and different capture policies. That would make the code harder to verify and could accidentally change evaluation timing.

The correct boundary is therefore:

```text
code-behind owns Avalonia window orchestration
ViewModel owns user/project state and commands
services/builders own deterministic decisions, storage adapters, report preparation, and rendering
calculation core owns engineering physics
```

## Superseded observations from the historical audit

The following historical statements are no longer current:

```text
MainWindow contains a nested project file dialog service
MainWindow directly owns project picker implementation
MainWindow directly owns PDF picker configuration and file types
MainWindow owns all PDF export decisions and status strings
```

The following observations remain current but are not blockers for closing this block:

```text
MainWindow uses runtime text overrides for legacy XAML strings
MainWindow constructs and opens child windows
MainWindow invokes the PDF renderer after stabilized preparation/picker boundaries
```

The original `docs/ARCHITECTURE_AUDIT.md` should remain unchanged as a dated historical baseline. This closure document records the later state rather than rewriting history.

## Architecture closure decision

The `MainWindow` architecture-stabilization block is complete at merge of this record.

No additional behavior-preserving extraction is required for the remaining handlers.

Future work should not continue splitting code-behind merely to reduce line count. A new architecture issue is justified only when a concrete behavior requires one of the following:

```text
- reusable non-UI application workflow
- testable cross-window navigation behavior
- new PDF export targets or progress/cancellation
- removal of legacy runtime text overrides through XAML cleanup
- a stable result read model shared by UI/PDF/2D
```

## Final invariants

```text
- output unchanged
- child-window ownership unchanged
- DataContext capture timing unchanged
- library post-dialog refresh timing unchanged
- calculation confirmation behavior unchanged
- full-report blocked status unchanged
- PDF export boundaries unchanged
- no generic dialog factory
- no DI framework
- no solver or engineering physics changes
- no 3D
```
