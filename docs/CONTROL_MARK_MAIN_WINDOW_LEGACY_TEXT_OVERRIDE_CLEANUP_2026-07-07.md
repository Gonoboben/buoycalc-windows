# Control mark: MainWindow legacy text override cleanup

Date: 2026-07-07
Scope: UI source-of-truth cleanup / documentation only
Related issue: #142

This control mark records the exact current `MainWindow` XAML and runtime text behavior before removing legacy source strings and the MainWindow-specific visual-tree override.

This document changes no production code, XAML, visible text, window behavior, commands, dialogs, PDF rendering, project storage, DTOs, JSON, reports, 2D, solver, engineering physics, calculation inputs, public API, or version number.

## Current XAML source values

`Views/MainWindow.axaml` currently declares:

```text
Window.Title = "BuoyCalc Windows v0.21.3"
report button TextBlock.Text = "Отчёт текстом..."
version badge TextBlock.Text = "v0.21.3 cleanup"
```

These are legacy source values. They are not the final user-visible values after constructor setup.

## Current constructor order

The current `MainWindow` constructor executes:

```text
1. AvaloniaXamlLoader.Load(this)
2. WindowVersionHelper.Apply(this, "BuoyCalc Windows")
3. ApplyMainWindowTextOverrides()
4. DataContext = new MainWindowViewModel(
       new AvaloniaProjectFileDialogService(this))
```

Required behavior during cleanup:

- XAML loading remains first;
- `WindowVersionHelper.Apply(...)` remains immediately after XAML loading in the first production step;
- ViewModel construction remains last;
- project dialog adapter construction and owner identity remain unchanged;
- no child-window handler or export workflow moves.

## Current final window title

`WindowVersionHelper.Apply(this, "BuoyCalc Windows")` performs:

```text
window.Title = "BuoyCalc Windows" + " " + AppInfo.Version
```

With the current `AppInfo` values, the final title is:

```text
BuoyCalc Windows v0.46.4
```

Required behavior:

- the final title remains exactly `AppInfo.WindowTitle`;
- the current version remains `v0.46.4`;
- no version bump is part of this cleanup;
- no title formatting or punctuation changes are introduced;
- `WindowVersionHelper.Apply(...)` remains in place so this PR does not broaden into global window-version cleanup.

## Current report button text

After XAML load, `ApplyMainWindowTextOverrides()` traverses all visual descendants and changes every `TextBlock` whose text is exactly:

```text
Отчёт текстом...
```

to:

```text
Полный отчёт...
```

Required behavior:

- the visible report button text remains exactly `Полный отчёт...`;
- ellipsis remains three ordinary period characters;
- only the legacy source value is replaced in XAML;
- click handler remains `OpenReportTextButton_Click`;
- button structure, style, dimensions, and visual tree remain unchanged.

## Current version badge text

The same MainWindow-specific traversal changes a `TextBlock` whose text is exactly:

```text
v0.21.3 cleanup
```

to:

```text
AppInfo.DisplayVersion
```

With current `AppInfo` values, the visible badge is:

```text
v0.46.4 - пользовательские статусы PDF
```

`WindowVersionHelper.Apply(...)` also registers an `Opened` event that scans legacy version badges. Because `v0.21.3 cleanup` is in its legacy list, the helper would also replace this badge when the window opens if the MainWindow-specific traversal had not already done so.

Required behavior:

- the visible badge remains exactly `AppInfo.DisplayVersion`;
- no ViewModel property is added solely for application-version presentation;
- no `DataContext` binding is used for the badge;
- no duplicate literal current-version string is placed in XAML;
- the cleanup should use the existing static presentation source where supported by Avalonia XAML compilation;
- build validation is mandatory.

## MainWindow-specific override method

Current method responsibility:

```text
ApplyMainWindowTextOverrides()
  -> enumerate TextBlock descendants
  -> replace report-button legacy text
  -> replace MainWindow legacy version badge
```

After the XAML source values produce the same visible output directly, this method has no remaining responsibility.

Allowed removal:

```text
- remove ApplyMainWindowTextOverrides() constructor call
- remove ApplyMainWindowTextOverrides() method
- remove System.Linq when no longer used
- remove Avalonia.VisualTree when no longer used
```

Required behavior:

- no other constructor statement changes order;
- no other handler changes;
- no generic text replacement service is introduced;
- no visual-tree traversal remains solely for these two values.

## Intended XAML direction

A production PR may add the services XML namespace:

```text
xmlns:services="using:BuoyCalc.Windows.Services"
```

and may source static presentation values from:

```text
AppInfo.WindowTitle
AppInfo.DisplayVersion
```

The exact Avalonia markup syntax must be accepted by the repository's current Avalonia 12.0.5 XAML compiler and verified by `BuoyCalc Windows Build`.

Required behavior:

- static values are evaluated during XAML load;
- no mutable binding or change notification is required;
- application version remains a presentation constant for the running process;
- failure to compile must be corrected without falling back to a new ViewModel property or hard-coded current version.

## WindowVersionHelper boundary

This cleanup does not remove or modify `WindowVersionHelper`.

Reasons:

- other windows still contain legacy title/badge values;
- helper removal would broaden the scope beyond `MainWindow`;
- the helper's `Opened` event and legacy-version list need a separate audit;
- preserving the helper call provides a behavior-equivalence safety net for the title.

The later global cleanup may remove helper entries only after each affected window is migrated and verified separately.

## Exception and timing behavior

Current possible failures before DataContext assignment include:

```text
- XAML loading failure
- WindowVersionHelper.Apply(...) failure
- MainWindow visual-tree traversal failure
```

After cleanup, the removed traversal can no longer fail, while static XAML value resolution becomes part of XAML loading.

This is accepted as source cleanup only when:

- successful startup visible output is unchanged;
- ViewModel construction remains after XAML and version setup;
- no new catch or fallback changes exception presentation;
- CI confirms XAML compilation.

## Exact visible-output invariants

```text
window title:
BuoyCalc Windows v0.46.4

report button:
Полный отчёт...

version badge:
v0.46.4 - пользовательские статусы PDF
```

These strings are derived from the current `AppInfo` values and must not be changed by this issue.

## Explicit exclusions

This cleanup does not authorize changes to:

- `AppInfo.Version` or `AppInfo.VersionNote`;
- other windows or their old versions;
- `WindowVersionHelper` implementation or legacy list;
- current-profile summary text;
- technical report method/version notes;
- layout, colors, fonts, spacing, controls, or styles;
- window handlers or commands;
- project save/load;
- PDF export or renderer;
- 2D visualization;
- solver, calculation core, or engineering physics;
- 3D.

## First implementation invariants

```text
- visible output unchanged
- XAML load remains first
- WindowVersionHelper.Apply order unchanged
- DataContext construction order unchanged
- exact title unchanged
- exact report button text unchanged
- exact version badge unchanged
- no current-version literal duplicated in XAML
- no new ViewModel property
- no generic text override service
- no production behavior outside MainWindow presentation source
```
