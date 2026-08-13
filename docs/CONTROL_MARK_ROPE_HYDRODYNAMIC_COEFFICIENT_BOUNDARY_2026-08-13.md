# Control mark: rope hydrodynamic coefficient boundary

Date: 2026-08-13
Issue: #430
Scope: documentation / Physics RFC boundary only. No solver or schema change.

## 1. Purpose

Define how the existing single rope `DragCoefficient` may and may not be related to the Berteaux normal and tangential cable-resistance coefficients before any data-model or solver migration.

## 2. Source-backed distinction

The preceding Berteaux source validation established separate normal and tangential resistance terms. In the vector form already recorded for #430, the two terms use distinct coefficients:

- normal resistance coefficient `C_n`;
- tangential resistance coefficient `C_t` (equivalently the tangential-resistance parameterization used in the static treatment).

The project therefore must not silently represent both physical terms with one generic coefficient.

## 3. Current BuoyCalc data contract

### 3.1 Rope calculation model

`RopePreset` currently contains one field:

`DragCoefficient`.

`SegmentCalculationRow` copies that same value into its own `DragCoefficient` field.

The existing base line-current force and `MooringShapeForceAnalyzer` both use this single value. The latter uses it with projected area `d * ds` and the normal-speed magnitude, so it is already closest to a **normal-resistance coefficient candidate** in the current shape-force path.

### 3.2 Rope library storage

`RopeLibraryItem` also has only one `DragCoefficient` property. User ropes are serialized directly to `user-ropes.json` with `System.Text.Json`.

`RopeLibraryStorage` copies the same value in both directions between built-in/user library items and `RopePreset`.

Existing user JSON therefore has no independent `C_n` or `C_t` fields.

### 3.3 UI

The line editor exposes one field labelled only `Cd` (`RopeCd`). It does not tell the user that the value is normal or tangential resistance.

The built-in rope presets are explicitly described as educational presets. Their legacy Cd values are not, by themselves, evidence for a Berteaux `C_t` value.

## 4. Decisions

1. The existing `DragCoefficient` remains a **legacy compatibility coefficient** until migration is complete.
2. For the already approved normal-only INFO diagnostic, legacy `DragCoefficient` may continue to be used as the historical **`C_n` candidate** because this preserves exact overlap with the current shape normal-magnitude path.
3. Legacy `DragCoefficient` must **not** be interpreted as `C_t`.
4. Missing `C_t` must be represented as **unknown / unavailable**, not silently as zero.
5. `C_t = 0` is a physical/model assertion corresponding to a special normal-only case; it may only be used when that case is explicitly selected or source-backed. Missing data and zero are not the same state.
6. Full Berteaux normal+tangential vector resistance must remain unavailable for a rope until an explicit tangential coefficient/parameter is available.
7. No renderer, report, PDF or 2D consumer may invent `C_n` or `C_t`.
8. Existing base forces, shape forces, solver feedback, selected X/Z, gate/verdict, anchor/weak-link calculations and golden baseline remain unchanged by this control mark.

## 5. Required migration shape

A future additive migration should preserve old files and old calculations before any physics switch.

The target data contract needs to distinguish at least:

- legacy generic coefficient, for backward compatibility;
- explicit normal coefficient `C_n` (nullable / not supplied);
- explicit tangential coefficient `C_t` or equivalent source-backed tangential parameter (nullable / not supplied);
- provenance/note sufficient to tell whether a coefficient is legacy, user-supplied or source-backed.

The exact implementation type may be a dedicated hydrodynamic-coefficient value object or additive nullable fields. That architecture decision should prefer minimal constructor/DTO churn and must be made before production code is written.

## 6. Backward-compatibility rules

For an existing `user-ropes.json` entry that contains only legacy `DragCoefficient`:

- the file must still load;
- the legacy value must remain unchanged;
- existing calculations must continue to receive exactly the same historical coefficient and produce the same golden results;
- an effective normal-only diagnostic may resolve `C_n_candidate = legacy DragCoefficient` and must label that provenance as legacy;
- `C_t` remains unknown;
- loading and saving must not silently manufacture a numeric tangential coefficient.

For a future entry with explicit `C_n` and `C_t`, those fields must be distinguishable from the legacy coefficient and must not change the legacy calculation path until a separate Physics RFC authorizes a solver/model switch.

## 7. Proposed staged implementation

### Stage A — data boundary only

Add a backward-compatible representation of explicit normal/tangential rope coefficients and a resolver/read model that reports:

- legacy Cd;
- explicit `C_n` if present;
- effective normal candidate and provenance;
- explicit `C_t` if present;
- whether full normal+tangential hydrodynamics are available.

No existing force formula consumes the new fields in this stage.

### Stage B — validation

Validate:

- old JSON without new properties;
- new JSON with only explicit `C_n`;
- new JSON with explicit `C_n` and `C_t`;
- missing `C_t` remains unknown after round-trip;
- explicit `C_t = 0` remains distinguishable from missing;
- built-in presets preserve the existing baseline;
- five-scenario golden regression is unchanged.

### Stage C — physics use

Only after separate source validation and impact measurements may a future analyzer/solver consume explicit `C_n` / `C_t`. This stage is not authorized by this control mark.

## 8. Remaining #430 blockers after this decision

- profile-current fixed planar-axis policy and user/input W sign contract remain unresolved for signed profile vectors;
- coefficient storage/resolver migration is permitted only as behavior-preserving data plumbing;
- full normal+tangential production hydrodynamics remain blocked until explicit coefficient data and validation exist.
