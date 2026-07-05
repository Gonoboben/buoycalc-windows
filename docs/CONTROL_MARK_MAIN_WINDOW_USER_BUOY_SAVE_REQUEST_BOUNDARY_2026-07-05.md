# Control mark: main-window user buoy save request boundary

Date: 2026-07-05
Scope: architecture stabilization / documentation only
Related issue: #93

This control mark records the current behavior of `MainWindowViewModel.SaveCurrentBuoyToLibrary()` and defines invariants for a later request-builder extraction.

This document changes no production code, library storage, file format, preset selection policy, solver physics, formulas, calculation inputs, reports, PDF, 2D, XAML, event wiring, collection ownership, public view-model API, validation behavior, or user-facing output.

## Current workflow

The method currently performs:

```text
1. normalize the editable buoy name
2. decide whether the selected preset ID may be reused
3. parse four editable numeric strings
4. create a BuoyLibraryItem
5. call BuoyLibraryStorage.UpsertUserBuoy(...)
6. call RefreshLibraries()
7. publish the final success status
```

The first four steps are deterministic request preparation. Storage mutation, refresh, selection, and status publication remain in `MainWindowViewModel` in the first extraction.

## Exact name normalization

The current expression is:

```text
string.IsNullOrWhiteSpace(BuoyName)
    ? "Пользовательский буй"
    : BuoyName.Trim()
```

Required behavior:

- `null`, empty, or whitespace-only input becomes exactly `Пользовательский буй`;
- nonblank input is trimmed at both ends;
- internal whitespace is preserved;
- casing is preserved;
- no length restriction, character filtering, or uniqueness suffix is added;
- the normalized name is stored in a local variable before the storage call;
- the same normalized name is later used in the final status text.

## Exact selected ID reuse rule

The current expression is:

```text
SelectedBuoyPreset is { Source: "User" }
    ? SelectedBuoyPreset.Id
    : string.Empty
```

Required behavior:

- ID is reused only when a selected preset exists and its `Source` is exactly `User`;
- source matching is case-sensitive;
- a selected built-in preset produces an empty request ID;
- a null selected preset produces an empty request ID;
- the current selected ID is not validated, trimmed, or normalized by the view model;
- built-in ID prefix checking is not performed at this stage.

The storage layer remains responsible for replacing an empty or built-in-prefixed ID with a generated `user:` ID.

## Exact numeric parsing

The following editable strings are parsed independently and in field order:

```text
BuoyVolume
BuoyWeight
BuoyArea
BuoyCd
```

They use the existing main-window parser:

```text
value = (value ?? string.Empty).Replace(',', '.')
double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
    ? result
    : 0
```

Required behavior:

- all commas are replaced with dots before parsing;
- invariant culture is used;
- `NumberStyles.Any` is used;
- invalid, empty, or null text becomes numeric zero;
- no exception or validation message is produced;
- negative, infinite, scientific-notation, or otherwise parseable values are not clamped here;
- no engineering plausibility checks are introduced;
- the parser is called once for each of the four fields.

A later builder may own equivalent parsing, but it must preserve this exact behavior rather than using solver validation or a stricter numeric parser.

## Exact request object

The method currently creates:

```text
BuoyLibraryItem
{
    Id = selectedUserId,
    Source = "User",
    Name = normalizedName,
    VolumeM3 = Parse(BuoyVolume),
    WeightKg = Parse(BuoyWeight),
    ProjectedAreaM2 = Parse(BuoyArea),
    DragCoefficient = Parse(BuoyCd),
    Note = "Сохранено пользователем из формы буя."
}
```

Required fixed values:

```text
Source = "User"
Note = "Сохранено пользователем из формы буя."
```

The first extraction must not add fields, copy the selected preset note, preserve a built-in source, or change the note punctuation.

## Storage boundary

After request creation, the method calls:

```text
BuoyLibraryStorage.UpsertUserBuoy(buoy)
```

Storage currently:

```text
1. loads user buoys
2. forces buoy.Source = "User"
3. generates user:<guid-N> when ID is blank or starts with built-in: (case-insensitive)
4. finds an existing row by exact ID OR case-insensitive name
5. replaces the existing row or appends the request
6. saves the full user list
```

These storage semantics are outside the first request-builder extraction.

The builder must not:

- generate GUIDs;
- read or write files;
- search existing library rows;
- decide whether matching by name replaces an existing row;
- create library directories;
- serialize JSON.

## Object mutation by storage

`UpsertUserBuoy(...)` may mutate the request object by setting:

```text
Source
Id
```

The current `SaveCurrentBuoyToLibrary()` workflow does not read the possibly changed ID after storage.

A later builder must return a mutable `BuoyLibraryItem` compatible with this existing storage contract, or an equivalent request that is converted before the storage call without changing storage behavior.

## Refresh order

The current order is:

```text
UpsertUserBuoy(...)
RefreshLibraries()
BuoyLibraryStatusText = final success text
```

`RefreshLibraries()` itself performs:

```text
1. refresh buoy library while preserving the pre-save selected buoy ID when possible
2. refresh anchor library while preserving its selected ID when possible
3. refresh sequence library options
```

The first extraction must not publish the success status before refresh and must not skip or move refresh.

## Selection behavior after save

### Existing selected user preset

When `SelectedBuoyPreset.Source` is exactly `User`:

- its ID is reused in the request;
- storage normally updates the matching user row;
- `RefreshLibraries()` receives the existing selected user ID;
- the refreshed user preset can be selected again by exact ID.

### Built-in selected preset

When the selected preset is built-in:

- request ID is empty;
- storage generates a new user ID;
- `RefreshLibraries()` still begins with the previously selected built-in ID;
- the built-in preset can remain selected after refresh;
- the newly saved user preset is not automatically selected solely because it was just created.

### Null selected preset

When no buoy preset is selected:

- request ID is empty;
- storage generates a user ID;
- refresh falls back according to the existing library refresh rules.

The first extraction must not introduce automatic selection of the newly created user buoy.

## Final status publication

After refresh, the method assigns exactly:

```text
Буй сохранён в библиотеку: {normalizedName}
```

Required behavior:

- the status is published unconditionally after a non-throwing storage and refresh path;
- it uses the normalized name captured before storage;
- it does not use a reloaded preset name;
- it does not include the generated ID or file path;
- storage and refresh exceptions continue to propagate according to the current command behavior; no new catch block is added in the first extraction.

## Preferred production boundary

A later production PR may add internal types such as:

```text
MainWindowUserBuoySaveSource
MainWindowUserBuoySaveRequest
MainWindowUserBuoySaveBuilder
```

A suitable deterministic surface is:

```text
Build(
    buoyName,
    selectedPreset,
    buoyVolume,
    buoyWeight,
    buoyArea,
    buoyCd)
  -> normalizedName + BuoyLibraryItem
```

Equivalent naming is acceptable.

`MainWindowViewModel` should then retain:

```text
request = builder.Build(...)
BuoyLibraryStorage.UpsertUserBuoy(request.Buoy)
RefreshLibraries()
BuoyLibraryStatusText = $"Буй сохранён в библиотеку: {request.NormalizedName}"
```

## First implementation invariants

The first production PR must preserve:

```text
- name fallback and trimming
- case-sensitive Source == "User" ID reuse
- empty ID for null or non-user selection
- exact four-field parse order and fallback-to-zero behavior
- exact request fields, source, and note
- storage call before refresh
- refresh before final status
- normalized pre-storage name in final status
- storage mutation and matching semantics
- post-save selection behavior
- existing ObservableCollection instances
- all existing library refresh and diagram side effects
- absence of validation and new error handling
```

## Non-goals

Do not change in the first production PR:

```text
- BuoyLibraryStorage
- user-buoys.json format or path
- ID generation
- name-based upsert matching
- delete workflow
- refresh orchestration
- preset selection policy
- selected-preset application
- solver physics or formulas
- calculation-input construction
- reports, PDF, or 2D
- XAML or public view-model API
- events or collection ownership
- user-facing strings
- 3D functionality
```

## Safety gate

Every follow-up PR must explicitly state whether output changes.

Every follow-up PR must keep `BuoyCalc Windows Build` green.

The first implementation is architecture-only and must be **Output unchanged**.
