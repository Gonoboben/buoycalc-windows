# Control mark: profile-current axis and vertical-sign contract

Date: 2026-08-13
Issue: #430
Scope: Physics RFC / project-coordinate decision only. No production calculation change.

## 1. Purpose

Resolve the two application-specific coordinate questions that remained after the Berteaux vector source work and the validation-only planar projection reference:

1. how a signed East/North profile is reduced to one fixed X/Z calculation plane;
2. what sign `VerticalCurrentMS` / UI `W Vert.` has relative to project `+Z = downward`.

This control mark does not switch the existing profile-force or solver path to the new signed projection.

## 2. Existing project facts

- The project planar coordinate has `+Z = downward` because `ZDepthM` increases with depth.
- The existing shape-force compatibility calculation inserts `VerticalCurrentMS` directly as its Z component.
- The existing profile UI currently labels the field only as `W Vert.` and does not explain its sign.
- East/North profile components are stored with sign, but the historical shape-force path reduces them to `sqrt(East^2 + North^2)` and therefore loses horizontal direction.
- The merged validation reference proves the signed projection algebra for an explicitly supplied horizontal axis and explicitly supplied vertical sign, but intentionally does not choose those policies.

## 3. Decision: vertical component

For BuoyCalc project data, define:

`VerticalCurrentMS > 0` = current directed **downward**.

Therefore the signed planar conversion is

`U_Z = VerticalCurrentMS`

and the project conversion factor from the stored W field to `+Z` is

`s_W = +1`.

This is an explicit **BuoyCalc project convention**, not a claim that every external oceanographic data source uses the same W convention.

Reasons:

- it matches the already established project `+Z = downward` coordinate;
- it matches the numerical sign used by the existing shape-force compatibility path;
- it avoids silently reversing the meaning of existing saved non-zero W values.

The UI must be made explicit in a later behavior-preserving step, for example `W Vertical (+ вниз), м/с`.

External data whose vertical velocity is positive upward must be converted before or during import; no automatic assumption about external conventions is introduced here.

## 4. Decision: fixed horizontal X-axis

A signed profile-current calculation in the X/Z model shall use one **explicit fixed horizontal +X axis** for the whole calculation. It shall not be derived automatically from the surface current, maximum current, average current, depth-integrated current, selected shape, or any segment-local direction.

The project-facing parameter will be an optional azimuth of the positive X axis:

`PlanarXAxisAzimuthDeg`.

Convention:

- degrees clockwise from North in the same Earth-horizontal reference frame as the saved East/North components;
- `0°` = North;
- `90°` = East;
- values are normalized modulo 360°;
- the axis describes project **+X**, not the direction of the current itself.

For azimuth `A` in radians:

`e_E = sin(A)`

`e_N = cos(A)`

so the signed profile projection is

`U_X = East * e_E + North * e_N`

`U_out = -East * e_N + North * e_E`

`U_Z = VerticalCurrentMS`.

`U_out` is the signed horizontal component discarded by the 2D reduction and must remain observable as a diagnostic.

## 5. Why the axis is explicit rather than automatic

A directional profile can rotate or reverse with depth. Any automatic choice of one plane would embed an engineering policy that is not contained in the primary source and could change the retained and discarded load components.

An explicit axis keeps the 2D assumption visible and reviewable. It also avoids introducing 3D geometry: only one user-selected vertical plane is solved, while the out-of-plane current component is reported as lost information.

## 6. Backward compatibility

A future additive project/schema field must be nullable:

`double? PlanarXAxisAzimuthDeg`.

For existing projects where the field is absent/null:

- the existing historical calculation remains unchanged;
- the legacy `( |U_h|, W )` shape-force compatibility path remains unchanged;
- signed profile-vector diagnostics remain unavailable;
- no default azimuth is silently manufactured.

Adding or editing the azimuth must not by itself change solver physics until a separate Physics RFC authorizes a consumer switch.

## 7. Required staged implementation

### Stage A — data/UI boundary

Add the optional azimuth to project/environment input plumbing and make the W-down convention explicit in the current-profile UI. Old project files must load with azimuth null. Existing calculations remain unchanged.

### Stage B — passive projection read model

Using the already validated algebra, add a read model that reports for each profile/segment:

- East/North/W input;
- `U_X`;
- `U_Z`;
- signed `U_out`;
- retained planar horizontal magnitude versus discarded horizontal magnitude;
- axis azimuth and provenance.

If azimuth is null, return unavailable rather than inventing a plane.

### Stage C — engineering validation before physics use

Measure out-of-plane loss on canonical and synthetic profiles. Define diagnostic thresholds only if justified. Do not use this read model in solver feedback, selected X/Z, anchor or weak-link verdicts without another Physics RFC and baseline impact review.

## 8. Non-goals

This control mark does not:

- add 3D;
- change `MooringShapeForceAnalyzer`;
- change any force coefficient;
- enable tangential cable resistance;
- change solver/gate/verdict behavior;
- change 2D/PDF geometry;
- alter the five-scenario golden baseline.
