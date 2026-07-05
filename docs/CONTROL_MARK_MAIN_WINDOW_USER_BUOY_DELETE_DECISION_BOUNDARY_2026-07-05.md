# Control mark: main-window user buoy delete decision boundary

Date: 2026-07-05
Scope: architecture stabilization / documentation only
Related issue: #97

This control mark records the current behavior of `MainWindowViewModel.DeleteSelectedBuoyPreset()` and defines invariants for a later deterministic delete-decision builder.

This document changes no production code, library storage, JSON, preset policy, solver physics, formulas, calculation inputs, reports, PDF, 2D, XAML, events, collections, public API, or user-facing output.

## Current workflow

```text
SelectedBuoyPreset
  -> reject null selection
  -> reject non-User or built-in-prefixed selection
  -> capture selected name and ID
  -> BuoyLibraryStorage.DeleteUserBuoy(ID)
  -> RefreshLibraries()
  -> success or not-found status
```

Only the eligibility decision and immutable request preparation are candidates for extraction. Storage mutation, refresh, selection changes, preset application, diagram refresh, and status publication remain in `MainWindowViewModel`.

## State 1 — no selection

Condition:

```text
SelectedBuoyPreset is null
```

Exact status:

```text
Выберите пользовательский буй для удаления.
```

Current behavior:

```text
1. assign status
2. return
3. do not call DeleteUserBuoy
4. do not call RefreshLibraries
```

A later builder must return a blocked decision and must not invent an ID or name.

## State 2 — protected or non-user selection

Current guard:

```text
SelectedBuoyPreset.Source != "User"
|| SelectedBuoyPreset.Id.StartsWith(
    "built-in:",
    StringComparison.OrdinalIgnoreCase)
```

Exact status:

```text
Встроенный буй удалить нельзя. Удалять можно только пользовательские буи.
```

Required semantics:

- source comparison is case-sensitive;
- only exact `User` passes;
- `user`, `USER`, `Built-in`, empty, and other values fail;
- `||` short-circuit order remains: the ID check is skipped when source already fails;
- built-in prefix comparison remains `OrdinalIgnoreCase`;
- IDs are not trimmed or normalized;
- blocked decisions call neither storage nor refresh;
- the existing wording remains unchanged even when rejection is caused only by source.

## State 3 — allowed delete attempt

Allowed only when:

```text
selected preset is not null
Source == "User"
ID does not start with built-in: using OrdinalIgnoreCase
```

Before storage, current code captures:

```text
deletedName = SelectedBuoyPreset.Name
selectedId = SelectedBuoyPreset.Id
```

Required semantics:

- name is captured from the selected library item, not editable `BuoyName`;
- name is captured before storage and refresh;
- ID is passed unchanged;
- neither value is trimmed or reformatted;
- selected object is not cleared first;
- no confirmation or undo is introduced.

## Storage behavior

Allowed decisions call:

```text
BuoyLibraryStorage.DeleteUserBuoy(selectedId)
```

Storage currently performs:

```text
if ID is null/blank or starts with built-in: using OrdinalIgnoreCase
    return false

load user buoys
remove all rows where x.Id == id
removed = removed count > 0
if removed
    save remaining rows
return removed
```

Required storage invariants:

- removal ID equality is case-sensitive;
- deletion is not performed by name;
- all exact duplicate IDs are removed by `RemoveAll`;
- missing ID returns `false`;
- protected or blank ID returns `false` before file loading;
- missing user file is an empty list;
- JSON is rewritten only when at least one row was removed;
- storage rules stay outside the decision builder.

## Refresh timing

After every allowed storage call, current code executes:

```text
RefreshLibraries()
```

This occurs for both results:

```text
DeleteUserBuoy == true  -> refresh
DeleteUserBuoy == false -> refresh
```

Blocked states do not refresh.

The first extraction must not optimize away the refresh after a not-found result.

## Refresh side effects

`RefreshLibraries()` remains ordered as:

```text
1. RefreshBuoyLibrary(SelectedBuoyPreset?.Id)
2. RefreshAnchorLibrary(SelectedAnchorPreset?.Id)
3. RefreshSequenceLibraryOptions()
```

After successful deletion:

- the old selected user ID is still supplied to buoy refresh;
- the collection is cleared and reloaded;
- exact-ID reselection fails because the row was removed;
- fallback becomes `BuoyPresets.FirstOrDefault()`;
- built-in presets precede user presets;
- the normal fallback is therefore the first built-in buoy;
- assigning the fallback through `SelectedBuoyPreset` applies its values through existing setters;
- existing diagram refreshes remain;
- anchor and assembly-library refreshes still run;
- intermediate library-count statuses are later overwritten by the final delete status.

These effects remain outside the decision builder.

## Success status

When storage returns `true`, exact final text is:

```text
Удалён пользовательский буй: {deletedName}
```

Required behavior:

- uses the name captured before storage and refresh;
- does not use the fallback selected name after refresh;
- does not use editable `BuoyName` after preset application;
- publishes after complete refresh;
- contains no ID or file path.

## Not-found status

When storage returns `false`, exact final text is:

```text
Пользовательский буй не найден в файле библиотеки.
```

Required behavior:

- refresh has already completed;
- no captured name appears in this text;
- missing file and missing exact ID are not distinguished;
- no alternate warning is introduced.

## Exception behavior

The method has no local `try/catch`.

Required behavior:

- storage and refresh exceptions continue to propagate through the existing command path;
- final status is not published if an earlier operation throws;
- no retry, rollback, swallowing, logging, or new error text is introduced.

## Preferred production boundary

Possible internal types:

```text
MainWindowUserBuoyDeleteDecisionKind
MainWindowUserBuoyDeleteDecision
MainWindowUserBuoyDeleteDecisionBuilder
```

Suggested pure surface:

```text
Build(BuoyLibraryItem? selectedPreset)
  -> blocked decision with exact status
     OR
     allowed request with selected ID and captured selected name
```

The builder must not call storage, mutate collections, change selection, apply presets, refresh diagrams, or publish properties.

## Preferred later live orchestration

```text
decision = builder.Build(SelectedBuoyPreset)

if blocked
    publish decision status
    return

deleted = DeleteUserBuoy(decision.SelectedId)
RefreshLibraries()
publish success with decision.CapturedName
    or exact not-found status
```

## First implementation invariants

```text
- null guard before selected-object access
- exact no-selection status
- case-sensitive Source == "User" requirement
- OrdinalIgnoreCase built-in prefix protection
- current short-circuit order
- no storage or refresh for blocked states
- selected name captured before storage
- selected ID passed unchanged
- exact-ID case-sensitive storage removal
- storage save only when removal occurred
- refresh after every allowed attempt, including false
- success text uses captured pre-refresh name
- exact not-found status
- existing refresh, fallback selection, preset application, and diagram effects
- existing ObservableCollection instances and public bindings
- current exception propagation
```

## Non-goals

```text
- no BuoyLibraryStorage changes
- no user-buoys.json changes
- no confirmation or undo
- no refresh or selection-policy changes
- no selected-preset application changes
- no save-workflow changes
- no solver, calculation, report, PDF, 2D, XAML, API, event, or collection changes
- no user-facing string changes
- no 3D
```

## Safety gate

Every follow-up PR must explicitly state whether output changes and keep `BuoyCalc Windows Build` green.

The first implementation is architecture-only and must be **Output unchanged**.
