# Control mark: WindowVersionHelper badge traversal retirement

Date: 2026-07-07
Related issue: #151
Scope: documentation only

This record captures the active desktop-window version presentation before removing obsolete badge migration from `Views/WindowVersionHelper.cs`.

No production code, XAML, title, badge, command, dialog, profile, library, report, PDF, 2D, storage, JSON, DTO, calculation, solver, engineering physics, or version output changes are made by this document.

## Current helper responsibilities

`WindowVersionHelper.Apply(window, titlePrefix)` currently:

```text
1. assigns window.Title = titlePrefix + " " + AppInfo.Version
2. subscribes to Window.Opened
3. scans descendant TextBlock controls
4. replaces exact legacy-version text with AppInfo.DisplayVersion
```

The legacy list contains eleven historical values from `v0.19` through `v0.38.3 - CI status bridge`.

## Active window inventory

The desktop application starts `MainWindow`. Its active child-window paths cover:

```text
MainWindow
ElementLibraryWindow
CurrentProfileWindow
SequencePreviewWindow
Mooring2DWindow
ReportTextWindow
```

Helper callers:

```text
MainWindow
CurrentProfileWindow
Mooring2DWindow
ReportTextWindow
```

Non-callers:

```text
ElementLibraryWindow
SequencePreviewWindow
```

## Badge state

Current version badges are explicit XAML sources:

```text
MainWindow -> AppInfo.DisplayVersion
CurrentProfileWindow -> AppInfo.DisplayVersion
Mooring2DWindow -> AppInfo.DisplayVersion
```

`ReportTextWindow` uses the non-version badge text:

```text
технический отчёт
```

Therefore none of the active helper callers contains a complete `TextBlock.Text` value matching the legacy list.

The `CurrentProfileWindow` explanatory paragraph containing `В v0.19 ...` is not an exact match and is not changed by the helper. It remains outside this issue.

## Allowed production change

A production PR may change only `Views/WindowVersionHelper.cs`:

```text
- remove LegacyVersionTexts
- remove the Opened event subscription
- remove RefreshBadges(...)
- remove System.Linq
- remove Avalonia.VisualTree
```

It must retain:

```csharp
public static void Apply(Window window, string titlePrefix)
{
    window.Title = titlePrefix + " " + AppInfo.Version;
}
```

## Output invariants

Final titles remain unchanged:

```text
BuoyCalc Windows v0.46.4
Профиль течения по глубине v0.46.4
2D-схема постановки v0.46.4
Полный текстовый отчёт v0.46.4
```

Visible badges and all window content remain unchanged.

## Explicit behavior note

The removed `Opened` work is a no-op for the active window set. After removal, future legacy literals will no longer be silently migrated. New version badges must explicitly use `AppInfo.DisplayVersion` in XAML.

## Exclusions

```text
- no helper rename or removal
- no caller or XAML changes
- no direct title migration
- no explanatory-copy rewrite
- no version bump
- no solver, PDF, 2D or 3D changes
```
