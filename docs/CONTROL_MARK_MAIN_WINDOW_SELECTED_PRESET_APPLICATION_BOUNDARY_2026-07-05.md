# Control mark: main-window selected preset application boundary

Date: 2026-07-05
Scope: architecture stabilization / documentation only
Related issue: #89

This control mark records the current behavior of selected buoy and anchor preset application in `MainWindowViewModel`.

This document changes no production code, library storage, preset selection policy, solver physics, formulas, calculation inputs, reports, PDF, 2D, XAML, project JSON, event wiring, collection ownership, public view-model API, or user-facing output.

## Current responsibility

The main window currently owns both:

```text
selected library object -> formatted editable field values
```

and:

```text
property-setter publication order -> sequence diagram refresh side effects
```

The deterministic mapping may be extracted later. Selection triggers and application through existing setters remain in `MainWindowViewModel`.

## Selected buoy setter

The public setter currently performs:

```text
if (SetProperty(ref _selectedBuoyPreset, value))
{
    ApplySelectedBuoyPreset();
    UpdateSequenceDiagram();
}
```

Important behavior:

- application occurs only when `SetProperty(...)` reports a changed selected value;
- assigning the same selected value does not apply fields and does not call the explicit diagram refresh;
- the setter calls `ApplySelectedBuoyPreset()` before its explicit `UpdateSequenceDiagram()`;
- selection itself remains a public bindable property;
- the first extraction must not move this trigger into the builder.

## Null buoy selection

`ApplySelectedBuoyPreset()` currently begins with:

```text
if (SelectedBuoyPreset is null) return;
```

When the selected value changes to `null`:

```text
1. SetProperty succeeds
2. ApplySelectedBuoyPreset() returns without field changes
3. the setter still calls UpdateSequenceDiagram()
```

Therefore null selection does not clear editable buoy fields.

The first extraction must not replace current fields with empty strings, zeros, or fallback preset values when the selected object is null.

## Exact buoy field application order

For a non-null selected buoy, the current order is:

```text
1. BuoyName = SelectedBuoyPreset.Name
2. BuoyVolume = FormatDouble(SelectedBuoyPreset.VolumeM3)
3. BuoyWeight = FormatDouble(SelectedBuoyPreset.WeightKg)
4. BuoyArea = FormatDouble(SelectedBuoyPreset.ProjectedAreaM2)
5. BuoyCd = FormatDouble(SelectedBuoyPreset.DragCoefficient)
```

The order is behavior.

### Buoy diagram side effects

`BuoyName` uses a setter that calls `UpdateSequenceDiagram()` only when the name value changes.

The four numeric buoy fields use normal `SetProperty(...)` setters and do not directly refresh the sequence diagram.

After `ApplySelectedBuoyPreset()` returns, the selected-preset setter calls one explicit `UpdateSequenceDiagram()` regardless of whether any individual mapped field changed.

For a typical changed non-null buoy selection, possible diagram calls are therefore:

```text
- one call caused by changed BuoyName
- one final explicit call from SelectedBuoyPreset setter
```

If the new preset has the same buoy name as the previous editable value, the name setter may not call the diagram refresh, but the final explicit call remains.

The first extraction must not batch, suppress, or reorder these calls.

## Selected anchor setter

The public setter currently performs:

```text
if (SetProperty(ref _selectedAnchorPreset, value))
{
    ApplySelectedAnchorPreset();
    UpdateSequenceDiagram();
}
```

Important behavior:

- application occurs only when `SetProperty(...)` reports a changed selected value;
- assigning the same selected value does not apply fields and does not call the explicit diagram refresh;
- application occurs before the final explicit diagram refresh;
- the selected property remains public and bindable.

## Null anchor selection

`ApplySelectedAnchorPreset()` currently begins with:

```text
if (SelectedAnchorPreset is null) return;
```

When the selected value changes to `null`:

```text
1. SetProperty succeeds
2. ApplySelectedAnchorPreset() returns without field changes
3. the setter still calls UpdateSequenceDiagram()
```

Null selection therefore preserves the current editable anchor fields.

The first extraction must not clear or replace those fields.

## Exact anchor field application order

For a non-null selected anchor, the current order is:

```text
1. AnchorName = SelectedAnchorPreset.Name
2. AnchorType = SelectedAnchorPreset.Type
3. AnchorMaterial = SelectedAnchorPreset.Material
4. AnchorWeight = FormatDouble(SelectedAnchorPreset.WeightAirKg)
5. AnchorVolume = FormatDouble(SelectedAnchorPreset.VolumeM3)
6. AnchorCoefficient = FormatDouble(SelectedAnchorPreset.BaseHoldingCoefficient)
```

The order is behavior.

### Anchor diagram side effects

`AnchorName` calls `UpdateSequenceDiagram()` when its string value changes.

`AnchorType` also calls `UpdateSequenceDiagram()` when its string value changes.

`AnchorMaterial`, `AnchorWeight`, `AnchorVolume`, and `AnchorCoefficient` do not directly refresh the sequence diagram.

After application, `SelectedAnchorPreset` calls one final explicit `UpdateSequenceDiagram()`.

For a typical changed non-null anchor selection, possible diagram calls are therefore:

```text
- one call caused by changed AnchorName
- one call caused by changed AnchorType
- one final explicit call from SelectedAnchorPreset setter
```

The exact number depends on which individual string setters observe a changed value. The final explicit call remains whenever the selected object itself changes.

The first extraction must not reduce these calls to a single batched refresh.

## Numeric formatting

All numeric selected-preset values currently use:

```text
value.ToString("0.###", CultureInfo.InvariantCulture)
```

This behavior produces:

```text
- decimal point rather than locale comma
- up to three fractional digits
- no unnecessary trailing zeros
- zero represented as "0"
```

The mapping boundary must preserve this exact format and invariant culture.

It must not use current UI culture, `Parse(...)`, general formatting, or a different number of fractional digits.

## Editable fields remain strings

The selected-preset application output is written into editable string properties.

A later builder should therefore return immutable string values, not replace the editable fields with numeric properties or library-object bindings.

Suggested internal read models:

```text
MainWindowBuoyPresetDisplay
MainWindowAnchorPresetDisplay
MainWindowSelectedPresetDisplayBuilder
```

Equivalent naming is acceptable.

A suitable builder surface is:

```text
BuildBuoy(BuoyLibraryItem preset)
    -> name, volume, weight, area, drag coefficient strings

BuildAnchor(AnchorLibraryItem preset)
    -> name, type, material, weight, volume, coefficient strings
```

Null handling may remain entirely in `MainWindowViewModel`, preserving the current early return.

## Library refresh interaction

`RefreshLibraries()` currently performs:

```text
1. RefreshBuoyLibrary(SelectedBuoyPreset?.Id)
2. RefreshAnchorLibrary(SelectedAnchorPreset?.Id)
3. RefreshSequenceLibraryOptions()
```

The first selected-preset extraction must not change this order.

### Buoy refresh

`RefreshBuoyLibrary(selectedId)` currently:

```text
1. clears the existing BuoyPresets collection
2. loads all buoy objects from BuoyLibraryStorage.LoadAllBuoys()
3. adds every loaded object to the existing collection
4. assigns SelectedBuoyPreset to the case-sensitive ID match
   or BuoyPresets.FirstOrDefault()
5. publishes the library count status
```

The selected assignment can apply preset fields and refresh the diagram before the status text is published.

The existing `ObservableCollection` instance must remain.

### Anchor refresh

`RefreshAnchorLibrary(selectedId)` currently:

```text
1. clears the existing AnchorPresets collection
2. loads all anchors from AnchorLibraryStorage.LoadAllAnchors()
3. adds every loaded object to the existing collection
4. assigns SelectedAnchorPreset to the case-sensitive ID match
   or AnchorPresets.FirstOrDefault()
5. publishes the library count status
```

The selected assignment can apply fields and refresh the diagram before the status text is published.

The first extraction must not change ID comparison, fallback order, collection mutation, or status timing.

## Built-in and user library order

Storage currently returns:

```text
built-in presets followed by user presets
```

for both buoy and anchor libraries.

This affects `FirstOrDefault()` fallback selection.

Preset application extraction must not reorder library objects or redefine which object is selected.

## Constructor interaction

The constructor currently performs:

```text
1. create observable collections and commands
2. RefreshLibraries()
3. ResetToDefaultProject()
```

During `RefreshLibraries()`, first available buoy and anchor objects may be selected and applied.

`ResetToDefaultProject()` then selects its default buoy and preferred anchor through the same public selected properties.

A later mapping builder must not bypass either application pass.

## Default-project interaction

The default-project workflow currently selects:

```text
SelectedBuoyPreset = BuoyPresets.FirstOrDefault()
```

when no preferred buoy ID is present, and:

```text
SelectedAnchorPreset = matching built-in:concrete_500
    ?? AnchorPresets.FirstOrDefault()
```

The selected-property setters continue to own application and diagram refresh behavior.

The preset display builder must not choose default objects.

## Project-load interaction

The current load order is significant.

For the buoy path:

```text
1. BuoyName = restore.Buoy.Name
2. RefreshLibraries()
3. SelectedBuoyPreset = matching restored preset ID
   ?? current SelectedBuoyPreset
```

`RefreshLibraries()` and the later selected-preset assignment may overwrite `BuoyName` and all buoy numeric editable fields through preset application.

This existing precedence is preserved. The first extraction must not make the restored buoy name or stored numeric text authoritative in a new way.

For the anchor path:

```text
1. RefreshLibraries()
2. select restored anchor preset ID when found
3. apply nonblank restored anchor Name
4. apply nonblank restored anchor Type
5. apply nonblank restored anchor Material
6. apply nonblank restored anchor Weight
7. apply nonblank restored anchor Volume
8. apply nonblank restored anchor BaseHoldingCoefficient
```

Therefore preset application occurs before nonblank DTO anchor overrides.

The first extraction must not reverse this precedence.

## Save-current-buoy interaction

`SaveCurrentBuoyToLibrary()` currently:

```text
1. resolves a trimmed name or "Пользовательский буй"
2. reuses selected ID only when SelectedBuoyPreset.Source == "User"
3. parses editable numeric strings
4. creates a BuoyLibraryItem
5. calls BuoyLibraryStorage.UpsertUserBuoy(...)
6. calls RefreshLibraries()
7. publishes "Буй сохранён в библиотеку: {name}"
```

`RefreshLibraries()` may reselect and reapply a preset before the final save-status text.

The selected-preset display boundary must not absorb save parsing, ID generation, storage, refresh, or status publication.

## Delete-selected-buoy interaction

The delete workflow currently has these early returns:

```text
SelectedBuoyPreset is null
    -> "Выберите пользовательский буй для удаления."

Source != "User" or ID starts with "built-in:"
    -> "Встроенный буй удалить нельзя. Удалять можно только пользовательские буи."
```

For an allowed user preset:

```text
1. capture selected name
2. call BuoyLibraryStorage.DeleteUserBuoy(selected ID)
3. RefreshLibraries()
4. publish success or not-found status
```

The refresh can select a fallback preset and apply it before the final delete status.

This orchestration remains outside the first selected-preset mapping extraction.

## First implementation invariants

The first production PR must preserve:

```text
- selected property public surface
- SetProperty change gating
- null-selection early return without clearing fields
- final explicit diagram refresh after changed selection
- exact buoy field assignment order
- exact anchor field assignment order
- all individual property setter side effects
- repeated diagram refreshes
- invariant 0.### formatting
- editable string properties
- library refresh order
- existing collection instances
- storage calls and library ordering
- case-sensitive selected-ID matching
- first-item fallback behavior
- constructor application passes
- default-project selection policy
- project-load precedence
- save/delete refresh and status timing
```

## Non-goals

Do not change in the first production PR:

```text
- BuoyLibraryStorage or AnchorLibraryStorage
- library files or JSON formats
- save/delete/refresh commands
- selected preset policy
- default project template
- project persistence mapping
- solver physics or formulas
- calculation-input construction
- reports, PDF, or 2D
- XAML or public view-model API
- event wiring or collection ownership
- user-facing strings
- 3D functionality
```

## Safety gate

Every follow-up PR must explicitly state whether output changes.

Every follow-up PR must keep `BuoyCalc Windows Build` green.

The first implementation is architecture-only and must be **Output unchanged**.
