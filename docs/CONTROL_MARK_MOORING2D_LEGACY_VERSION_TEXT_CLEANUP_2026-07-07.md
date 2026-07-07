# Control mark: Mooring2DWindow legacy version text cleanup

Date: 2026-07-07
Scope: UI source-of-truth cleanup / documentation only
Related issue: #145

This control mark records the exact `Mooring2DWindow` title and version-badge behavior before removing obsolete version literals from XAML.

This document changes no production code, XAML, visible output, canvas, selected-shape source, diagram source, report store, PDF, project storage, DTOs, JSON, solver, engineering physics, calculation inputs, public API, 2D rendering, or version number.

## Current XAML source values

`Views/Mooring2DWindow.axaml` currently declares:

```text
Window.Title = "2D-схема постановки v0.24.4"
version badge TextBlock.Text = "v0.24.4"
```

The same XAML contains exactly one drawing surface:

```text
<views:Mooring2DCanvas />
```

inside the existing row-1 bordered host.

## Current constructor order

`Mooring2DWindow` currently executes:

```text
1. AvaloniaXamlLoader.Load(this)
2. WindowVersionHelper.Apply(this, "2D-схема постановки")
```

Required behavior:

- XAML load remains first;
- `WindowVersionHelper.Apply(...)` remains second;
- no DataContext assignment is introduced;
- owner/modal behavior remains controlled by `MainWindow`;
- `CloseButton_Click(...)` remains unchanged even though this cleanup does not add or remove controls.

## Final title behavior

Immediately after XAML loading, `WindowVersionHelper.Apply(...)` sets:

```text
window.Title = "2D-схема постановки" + " " + AppInfo.Version
```

With the current application version, the final title is:

```text
2D-схема постановки v0.46.4
```

The production cleanup may replace the legacy XAML title with the version-free base text:

```text
2D-схема постановки
```

because the existing helper call still produces the exact final title before the window is shown.

Required behavior:

- no current-version literal is duplicated in XAML;
- no new window-specific property is added to `AppInfo`;
- no binding or ViewModel property is used for the title;
- final title remains exact after constructor completion.

## Final badge behavior

`WindowVersionHelper.Apply(...)` subscribes to the window `Opened` event. Its legacy list contains:

```text
v0.24.4
```

When the window opens, it traverses descendant `TextBlock` elements and replaces the legacy badge with:

```text
AppInfo.DisplayVersion
```

With current application metadata, the visible badge is:

```text
v0.46.4 - пользовательские статусы PDF
```

The production cleanup may source this value directly in XAML using the already validated Avalonia syntax:

```text
{x:Static services:AppInfo.DisplayVersion}
```

Required behavior:

- visible badge text remains exact;
- the services XML namespace is added only for this static presentation value;
- no DataContext binding or change notification is introduced;
- the helper's `Opened` handler remains registered, but finds no matching legacy badge in this window.

## Canvas invariants

The cleanup must not change:

```text
- `views` XML namespace
- `Mooring2DCanvas` type
- canvas instance count
- parent border
- grid row
- padding, margins, dimensions, colors, or styles
- inherited DataContext
- selected-shape/read-model selection
- drawing order, scaling, labels, geometry, or hit behavior
```

No canvas file may be modified by this issue.

## Allowed production diff

The first implementation may change only `Views/Mooring2DWindow.axaml`:

```text
- add services XML namespace
- replace legacy title with `2D-схема постановки`
- replace legacy badge with `AppInfo.DisplayVersion` via x:Static
```

`Views/Mooring2DWindow.axaml.cs` should remain byte-for-byte unchanged.

## WindowVersionHelper boundary

This issue does not modify `WindowVersionHelper` because:

- other windows still rely on its title and badge migration behavior;
- removing the helper call would broaden the change beyond XAML source cleanup;
- global legacy-list cleanup requires a separate inventory of every window;
- retaining the helper preserves final-title timing and behavior.

## Exact visible-output invariants

```text
window title:
2D-схема постановки v0.46.4

header:
2D-схема постановки

version badge:
v0.46.4 - пользовательские статусы PDF
```

## Explicit exclusions

This issue does not authorize changes to:

- `Mooring2DCanvas` or related renderer files;
- 2D shape/read-model source order;
- calculation core or engineering formulas;
- PDF diagrams or report sources;
- window layout or controls;
- `AppInfo` version/note;
- other windows;
- `WindowVersionHelper` implementation/list;
- 3D.

## First implementation invariants

```text
- visible output unchanged
- only Mooring2DWindow.axaml changes
- XAML load/helper call order unchanged
- exact final title unchanged
- exact badge unchanged
- canvas and DataContext inheritance unchanged
- no version bump
- no solver, physics, PDF, or 2D-rendering changes
```
