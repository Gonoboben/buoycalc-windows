# Physics RFC: mandatory current profile

Date: 2026-08-28  
Issue: #590  
Scope: preliminary environmental-load assumption / validation contract

## Decision

Production BuoyCalc calculations require an explicit current profile versus depth.

The historical scalar-current mode is retired as an engineering assumption. BuoyCalc must not interpret one scalar speed as a uniform current through the complete water column.

An intentionally constant profile remains representable only when the user explicitly enters profile points with equal current components/speeds at multiple depths. The application must not synthesize that profile from a single scalar value.

## Physical question

The previous model allowed either:

1. a depth-dependent current profile, or
2. one scalar `CurrentSpeedMS` applied to the full water column.

The second mode is not accepted for the intended engineering workflow because the environmental current field is depth-dependent input data. Therefore the current profile becomes mandatory input rather than an optional enhancement.

## Existing equations and assumptions preserved

This RFC changes the availability of the scalar fallback, not the validated constitutive equations.

Preserve:

- existing drag constitutive equation;
- existing drag coefficients and projected-area semantics;
- profile interpolation between depth points;
- current profile component units and signs;
- existing profile density interpolation/fallback semantics;
- production line segmentation target `0.20 m` with no segment-count cap;
- signed `WeightWaterKgM` semantics;
- signed feedback budget `64`;
- exact deterministic fixed-point candidate acceptance with no convergence epsilon;
- selected X/Z authority and existing solver/gate semantics;
- F1/F2/F3/F4 semantics;
- wave model, anchor/contact model and safety factor semantics.

## Units and sign conventions

- depth: metres, positive downward from the surface;
- east current: m/s using the existing east-positive convention;
- north current: m/s using the existing north-positive convention;
- vertical current: m/s using the existing project convention;
- water density: kg/m^3;
- horizontal profile speed where currently required: `sqrt(East^2 + North^2)`.

No sign convention changes are authorized by this RFC.

## Required production behavior

### Calculation input

- The current profile is always active.
- The user-facing current-profile enable/disable switch is removed.
- The scalar current-speed field is removed as a selectable calculation mode.
- Engineering calculations must not branch to scalar `CurrentSpeedMS` fallback behavior.

### Missing profile

A calculation with no usable current-profile points must not proceed by:

- using `CurrentSpeedMS`;
- creating a hidden constant profile;
- using the historical default `0.5 m/s`;
- silently filling the water column from one profile point.

Instead the calculation/application boundary must explicitly reject or block calculation and tell the user that a current profile is required.

### New/default project

A new project starts with an editable current profile available and active. Default example points may remain as example/input defaults, but they are profile data, not a hidden scalar fallback.

### Persisted projects

Legacy JSON fields such as `CurrentSpeed` and `UseCurrentProfile` may remain temporarily in DTOs only where needed for backward deserialization compatibility.

They must not authorize scalar-current calculation after this change.

A legacy project with no usable profile must load without crashing, but calculation remains blocked until the user supplies/reviews a profile. Do not synthesize a constant profile from the legacy scalar speed.

## Affected deployment modes

All production deployment modes that consume environmental current are affected because the source of current data becomes profile-only.

This RFC does not change deployment-mode classification, solver selection, boundary conditions or wave/current coupling rules.

## Validation package

Implementation is accepted only with regression evidence covering the following scenarios.

### V1 — nonuniform profile authority

Given a multi-depth nonuniform profile, every line segment `LocalSpeedMS` must come from the existing depth-profile interpolation path. Legacy scalar `CurrentSpeedMS` must not affect the result.

### V2 — missing profile rejection

Given no current-profile points, calculation must be explicitly rejected/blocked. A populated legacy scalar value must not cause a calculation to proceed.

### V3 — constitutive continuity reference

Given an explicitly entered constant profile with equal velocity at all relevant depth points, line-segment drag must reproduce the equivalent historical scalar-current drag for the same velocity, density, geometry and Cd.

Acceptance tolerance:

- exact identity where identical arithmetic permits it;
- otherwise absolute or relative difference `<= 1e-12` for this reference comparison.

This tolerance is only for validation comparison. It is not a solver convergence epsilon.

### V4 — legacy project compatibility

A legacy DTO containing `UseCurrentProfile=false` and a populated scalar `CurrentSpeed` must deserialize/load without crashing. If it has no usable profile, calculation must remain blocked and the user must be told to provide a profile.

### V5 — profile density semantics

Existing profile density interpolation and fallback behavior must remain unchanged.

### V6 — frozen engineering invariants

Regression evidence must confirm no change to:

- `0.20 m` production segmentation;
- signed `WeightWaterKgM` handling;
- signed feedback budget `64`;
- exact deterministic fixed-point acceptance;
- selected X/Z authority;
- F1/F2/F3/F4 rules;
- anchor/contact and wave semantics.

## Historical result impact

Historical projects calculated through scalar-current mode will intentionally no longer have that shortcut available.

This is an approved behavioral change. Historical scalar results are not to be regenerated merely to keep baselines green. Any regression baseline affected by the removal must be handled through explicit validation evidence and reviewed disposition, never by weakening tests.

## Out of scope

This work does not authorize:

- changing how existing profile-mode buoy/connector/payload velocity is spatially sampled;
- changing current direction projection rules;
- turbulence, gust factors or stochastic currents;
- a new current theory;
- wave-current interaction changes;
- new anchor/seabed/contact physics;
- changes to safety factors;
- 3D.

## Implementation boundary

Implementation must be a separate physics work package after this RFC is merged.

The implementation PR must include production changes plus validation/regression evidence and must pass the exact-head required checks before merge:

- `.NET Build`;
- `Selected Shape Consumer Scan`;
- `Report Store Consumer Scan`;
- classic `BuoyCalc Windows Build` according to the repository release policy.
