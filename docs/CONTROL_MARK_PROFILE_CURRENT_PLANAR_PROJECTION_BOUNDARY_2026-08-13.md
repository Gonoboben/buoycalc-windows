# Control mark: profile-current planar projection boundary

Date: 2026-08-13
Issue: #430
Scope: documentation / Physics RFC boundary only. No production physics change.

## 1. Purpose

Define what BuoyCalc Windows can and cannot mean by a signed current vector in the planar X/Z model when the project uses a depth-dependent current profile with East/North/Vertical components.

This control mark does **not** authorize profile-current vector forces in the solver. The existing profile-current calculation, selected X/Z, gates, verdict, anchor and weak-link checks remain unchanged.

## 2. Primary-source boundary already established

The Berteaux source boundary established in the preceding #428-#432 work gives the vector hydrodynamic form in terms of normal and tangential components of relative velocity. For a static cable the cable velocity is zero, so a signed fluid-velocity vector and a signed cable tangent are required before a force vector can be formed.

The source does not define BuoyCalc's application-specific mapping from the saved Earth-frame profile fields `EastCurrentMS`, `NorthCurrentMS`, `VerticalCurrentMS` into the application's single X/Z plane. That mapping is therefore a separate software/model contract and must not be invented inside a renderer or analyzer.

## 3. Facts in the current application

### 3.1 Input/storage frame

The UI and saved project schema expose profile components as:

- `U East` / `EastCurrentMS`;
- `V North` / `NorthCurrentMS`;
- `W Vert.` / `VerticalCurrentMS`.

`CurrentProfilePointViewModel`, `CurrentProfilePointDto` and `MainWindowCalculationInputBuilder` preserve the signs of all three components; the input sanitizer only replaces non-finite values with zero.

The UI currently does **not** state whether positive `W Vert.` means upward or downward physical velocity.

### 3.2 Existing horizontal reduction

For each calculation segment, the current profile keeps signed East/North/Vertical values, but `LocalSpeedMS` used by the existing base/shape force path is the horizontal magnitude

`|U_h| = sqrt(East^2 + North^2)`.

Therefore the horizontal azimuth and any reversal/rotation of the Earth-frame horizontal current with depth are lost before the existing planar shape-force magnitude calculation.

### 3.3 Existing shape-force compatibility vector

`MooringShapeForceAnalyzer` currently evaluates orientation-dependent normal-speed magnitude using the planar velocity pair

`U_legacy = ( |U_h|, W )`.

This is a **legacy co-directed planar magnitude envelope / compatibility vector**. It must not be described as a signed East/North -> X projection.

Because `ZDepthM` increases downward and `VerticalCurrentMS` is inserted directly as the second component in this existing calculation, the current software behavior numerically treats positive W as positive Z. This is a statement about existing code behavior, **not** an approved external/oceanographic sign convention for user input.

### 3.4 Uniform-current vector diagnostic

The already merged uniform-current INFO diagnostic is a narrower case. It has no East/North profile direction to project and explicitly uses the established project planar convention

`U = (-|CurrentSpeedMS|, 0)`

for the environmental current relative to `+X_shape = buoy -> anchor`.

It remains unavailable when `UseCurrentProfile == true`.

## 4. Required mathematics for a real signed profile projection

A physically signed projection into one vertical plane requires a single fixed horizontal unit axis for that plane in Earth coordinates.

Let the approved planar-axis azimuth be represented by

`e_X = (e_E, e_N)`, with `e_E^2 + e_N^2 = 1`.

Then for each profile point or interpolated segment:

`U_X = East * e_E + North * e_N`

and the signed out-of-plane horizontal component is

`U_Yout = -East * e_N + North * e_E`.

The planar vertical component must be

`U_Z = s_W * W`,

where `s_W` is an explicitly approved conversion from the UI/input W convention to the project `+Z = downward` convention.

Only after both `e_X` and `s_W` are defined may the project build a signed planar profile velocity

`U_XZ = (U_X, U_Z)`.

`U_Yout` must remain observable as a diagnostic of information discarded by the 2D reduction; it must not be silently folded into `U_X`.

## 5. Decisions

1. **Do not** promote `( |U_h|, W )` to a physical signed profile projection. It remains the historical shape-force compatibility model.
2. **Do not** choose the planar horizontal axis automatically from surface current, maximum current, average current, depth-integrated current, selected shape, or any other derived value without a separate approved RFC decision and validation.
3. **Do not** infer the physical sign convention of `W Vert.` from the variable name. Existing direct `W -> Z` behavior is recorded as historical software behavior only.
4. Profile-current `MooringUniformCurrentNormalVectorResult` must remain `Available=false` until a fixed `e_X` contract and `s_W` contract are explicitly available to the calculation core/read-model builder.
5. No 3D model is introduced. The discarded horizontal component is only a scalar/signed diagnostic associated with the planar reduction.
6. Renderer/PDF/2D code must never create this projection independently.
7. Existing solver, `MooringShapeForceAnalyzer`, profile interpolation, selected X/Z, gate/verdict, anchor/weak-link checks and golden baseline are unchanged by this control mark.

## 6. Next validation-only package

Before any production schema or solver change, add a synthetic validation-only projection reference with an **explicitly supplied** horizontal unit axis and vertical sign conversion. It should prove:

- East-aligned axis;
- North-aligned axis;
- oblique axis;
- current reversal;
- non-zero out-of-plane component;
- zero horizontal current;
- both vertical signs;
- invariance of `U_X^2 + U_Yout^2 = East^2 + North^2` within floating representation;
- no effect on existing five-scenario golden baseline.

This validation package proves the projection algebra only. It does not select the application's future planar-axis policy.

## 7. Remaining production blockers under #430

- explicit application contract for the fixed horizontal X-axis of a profile-current calculation;
- explicit user/input sign convention for `W Vert.` and conversion to project `+Z = downward`;
- separate coefficient semantics for Berteaux normal and tangential resistance (`C_n` vs `C_t` / gamma); the existing single `RopePreset.DragCoefficient` must not be silently used as both.
