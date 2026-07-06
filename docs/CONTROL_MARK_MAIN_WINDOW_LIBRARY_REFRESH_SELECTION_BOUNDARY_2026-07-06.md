# Control mark: main-window library refresh selection boundary

Date: 2026-07-06
Scope: architecture stabilization / documentation only
Related issue: #104

This control mark records the current behavior of `MainWindowViewModel.RefreshLibraries()`, `RefreshBuoyLibrary(...)`, `RefreshAnchorLibrary(...)`, and `RefreshSequenceLibraryOptions()`.

This document changes no production code, storage, JSON, collection ownership, selected-preset policy, event wiring, solver physics, formulas, calculation inputs, reports, PDF, 2D, XAML, public API, or user-facing output.

## Current top-level order

`RefreshLibraries()` currently performs exactly:

```text
1. RefreshBuoyLibrary(SelectedBuoyPreset?.Id)
2. RefreshAnchorLibrary(SelectedAnchorPreset?.Id)
3. RefreshSequenceLibraryOptions()
```

The expressions are evaluated when each call is reached.

Therefore:

- the buoy selected ID is captured before buoy refresh;
- buoy collection mutation, selection assignment, preset application, diagram publication, and buoy count status can all occur before the anchor selected ID is read;
- the anchor selected ID is captured only after buoy refresh completes;
- sequence-library notifications begin only after both library refreshes complete.

The first extraction must not capture both IDs earlier in a combined snapshot and must not reorder the three calls.

## Buoy refresh order

`RefreshBuoyLibrary(selectedId)` currently performs:

```text
1. BuoyPresets.Clear()
2. BuoyLibraryStorage.LoadAllBuoys()
3. add each returned item to the existing BuoyPresets collection in returned order
4. resolve exact ID match or first-item fallback
5. assign through SelectedBuoyPreset
6. publish library-count status
```

Exact selection expression:

```text
BuoyPresets.FirstOrDefault(x => x.Id == selectedId)
    ?? BuoyPresets.FirstOrDefault()
```

Required behavior:

- the existing `ObservableCollection<BuoyLibraryItem>` instance is retained;
- `Clear()` occurs before storage loading;
- returned item order is preserved without sorting or deduplication;
- ID equality is ordinary case-sensitive string equality;
- `selectedId` is not trimmed, prefixed, normalized, or compared case-insensitively;
- fallback is the first collection item;
- an empty collection resolves to `null`;
- selection is assigned through the public `SelectedBuoyPreset` property;
- status is published only after selection assignment completes.

## Buoy storage order

`BuoyLibraryStorage.LoadAllBuoys()` currently returns:

```text
BuiltInBuoys.Concat(LoadUserBuoys()).ToList()
```

Consequences:

- built-in buoys precede user buoys;
- first-item fallback normally selects the first built-in buoy;
- user-file order is preserved after all built-ins;
- a missing user file contributes an empty user list;
- malformed or unreadable JSON can throw; no local catch exists in refresh.

The first selection extraction must not move loading into the builder or change this ordering.

## Anchor refresh order

`RefreshAnchorLibrary(selectedId)` currently performs:

```text
1. AnchorPresets.Clear()
2. AnchorLibraryStorage.LoadAllAnchors()
3. add each returned item to the existing AnchorPresets collection in returned order
4. resolve exact ID match or first-item fallback
5. assign through SelectedAnchorPreset
6. publish library-count status
```

Exact selection expression:

```text
AnchorPresets.FirstOrDefault(x => x.Id == selectedId)
    ?? AnchorPresets.FirstOrDefault()
```

Required behavior mirrors buoy refresh:

- retain the existing anchor collection instance;
- clear before storage load;
- preserve returned order;
- use case-sensitive exact ID equality;
- use first-item fallback without normalization;
- return `null` for an empty list;
- assign through the public selected-anchor property;
- publish status after selected-property side effects.

## Anchor storage order

`AnchorLibraryStorage.LoadAllAnchors()` currently returns:

```text
BuiltInAnchors.Concat(LoadUserAnchors()).ToList()
```

`BuiltInAnchors` is created from `AnchorCatalog.Presets` in catalog order.

Consequences:

- built-in anchors precede user anchors;
- fallback normally selects the first built-in anchor;
- user-file order is preserved;
- missing user JSON contributes no user rows;
- storage exceptions propagate.

## Selected-property side effects

Both resolved items are assigned through existing public properties.

For buoy:

```text
SelectedBuoyPreset setter
  -> SetProperty
  -> ApplySelectedBuoyPreset()
  -> UpdateSequenceDiagram()
```

For anchor:

```text
SelectedAnchorPreset setter
  -> SetProperty
  -> ApplySelectedAnchorPreset()
  -> UpdateSequenceDiagram()
```

`SetProperty` uses:

```text
EqualityComparer<T>.Default.Equals(oldValue, newValue)
```

Required behavior:

- no selected-property event or application occurs when equality reports the same value;
- selected user rows are normally new deserialized object instances after reload, so the same user ID can still produce a changed selected object and repeat setter side effects;
- built-in lists retain their static item objects, so reselecting the same built-in object can be equality-suppressed;
- changing from a non-null selected item to `null` still reaches the selected-property setter; preset application returns early, but the setter's explicit diagram refresh remains;
- the first extraction must return the existing item object from the repopulated collection, not a copied or newly constructed equivalent.

## Count-status timing

Both refresh methods publish exactly:

```text
Библиотека: буёв {BuoyPresets.Count}, якорей {AnchorPresets.Count}.
```

During the top-level workflow:

- buoy refresh publishes an intermediate status after the buoy collection is refreshed but before anchor refresh;
- that intermediate text therefore uses the new buoy count and the anchor count that existed before anchor refresh;
- in the constructor, that pre-anchor count can be zero;
- anchor refresh later publishes the same template using both refreshed counts;
- sequence-library refresh does not directly replace this status.

The first extraction must not combine the two statuses into one final publication or move status publication before selected-property application.

## Sequence-library option refresh

After both libraries, `RefreshSequenceLibraryOptions()` performs:

```text
for each AssemblyItems item in current collection order
    item.RefreshLibraryOptions()

UpdateSequenceSummary()
```

It does not replace or reorder `AssemblyItems`.

## Exact item notification order

Each `AssemblyItemViewModel.RefreshLibraryOptions()` publishes these eight `PropertyChanged` notifications in order:

```text
1. RopePresetOptions
2. RopePresetId
3. ConnectorPresetOptions
4. ConnectorPresetId
5. PayloadPresetOptions
6. PayloadPresetId
7. EditorHint
8. Summary
```

Every wired assembly item has:

```text
item.PropertyChanged += OnAssemblyItemChanged
```

`OnAssemblyItemChanged(...)` currently performs:

```text
if connector
    connector.Count = "1"
UpdateSequenceSummary()
```

Therefore the existing refresh intentionally or incidentally causes repeated summary, diagram, and visualization publications:

- normally one `UpdateSequenceSummary()` for each of the eight item notifications;
- one additional final `UpdateSequenceSummary()` after all items;
- when a connector count is not already `"1"`, the first notification can set `Count`, emit an additional `Summary` notification reentrantly, and cause an additional update before the outer notification continues.

The first extraction must not batch these notifications, temporarily unsubscribe handlers, suppress reentrant count correction, or replace them with one summary update.

## Empty assembly collection

When `AssemblyItems` is empty:

```text
- no item notifications occur
- the final UpdateSequenceSummary() still occurs once
```

## Exception and partial-state behavior

There is no local `try/catch` around library refresh.

Required behavior:

- buoy collection is cleared before buoy storage load; a load exception can leave it empty;
- if buoy refresh throws, anchor and sequence refresh do not run;
- anchor collection is cleared before anchor storage load; an anchor load exception can leave buoy refresh completed and anchor collection empty;
- if selected-property application or a diagram update throws, later status and later refresh stages do not run;
- if an assembly notification subscriber throws, remaining notifications/items and the final update do not run;
- no rollback, retry, error status, or restoration of previous collections is introduced in the first extraction.

## Current callers

The top-level refresh is currently used by:

```text
- MainWindowViewModel constructor, before ResetToDefaultProject()
- RefreshBuoyLibraryCommand
- SaveCurrentBuoyToLibrary(), after UpsertUserBuoy(...)
- DeleteSelectedBuoyPreset(), after every allowed DeleteUserBuoy(...) call
- FromDto(...), after restoring BuoyName and before explicit restored preset-ID selection
```

The first extraction must not change caller order or move refresh relative to storage and project-load operations.

## Preferred production boundary

A later production PR may add an internal pure selector such as:

```text
MainWindowLibraryRefreshSelectionBuilder
```

Suitable surfaces are:

```text
SelectBuoy(IReadOnlyList<BuoyLibraryItem> items, string? selectedId)
SelectAnchor(IReadOnlyList<AnchorLibraryItem> items, string? selectedId)
```

Each method should return the exact object already present in `items`:

```text
exact case-sensitive ID match
    ?? first item
    ?? null
```

The ViewModel must retain:

```text
- Clear before storage load
- collection repopulation
- selected-property assignment
- both count-status publications
- sequence option notifications
- repeated event-driven updates
```

## First implementation invariants

```text
- exact top-level order and ID-capture timing
- existing collection instances
- clear/load/add/select/status order
- storage enumeration order
- case-sensitive exact ID matching
- first-item fallback and null empty result
- exact selected object identity from the collection
- selected-property setters and all side effects
- intermediate and final count statuses
- assembly-item iteration order
- eight notification names and order
- repeated and reentrant sequence-summary updates
- current exception propagation and partial-state behavior
- existing constructor, command, save, delete, and load orchestration
```

## Non-goals

```text
- no storage or JSON changes
- no collection replacement
- no notification batching
- no selected-preset policy changes
- no setter or event changes
- no solver, physics, formulas, calculation-input, report, PDF, 2D, XAML, or public API changes
- no user-facing string changes
- no 3D
```

## Safety gate

Every follow-up PR must explicitly state whether output changes and keep `BuoyCalc Windows Build` green.

The first implementation is architecture-only and must be **Output unchanged**.
