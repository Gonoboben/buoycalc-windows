# BuoyCalc Windows — pre-v1 engineering physics roadmap

Date: 2026-08-22  
Baseline main: `a3cbdb1d0fcae598419b2cd7c45b170610e228d1`  
Status: planned, pre-v1 scope

## 1. Release objective

The first official `v1.0.0` release is not limited to packaging the current `v0.46.4` behavior.
Before release, BuoyCalc should complete one final engineering-physics milestone so that the user-facing design chain is internally consistent:

```text
environment
  -> selected signed/quasi-static geometry
  -> wave-aware local force state
  -> local tension demand
  -> anchor-end reaction / contact state
  -> local element / weak-link reserve
  -> governing checks / verdict
  -> PDF / 2D / UI projection only
```

The goal is a stronger quasi-static engineering release, not a full dynamic offshore simulation package.

## 2. Validated starting point

The following boundaries are already validated and must remain intact unless a later package explicitly proves a replacement:

- selected X/Z/source authority may use `SignedBoundaryFeedback` only for Accepted signed candidates;
- non-Accepted candidates preserve the legacy selected-shape read model;
- signed boundary integration direction is `s=0` buoy/surface -> `s=L` anchor/seabed;
- direct signed state exposes surface and anchor-end H/V state and Accepted shape stores local midpoint tension magnitudes;
- current signed boundary/feedback path is steady-current and explicitly excludes wave;
- current production `CalculationResult.TensionKn` remains legacy-authoritative and wave-inclusive;
- `EstimatedOffsetM` is not an alias for selected endpoint X;
- no renderer, PDF, 2D or UI layer may invent engineering physics;
- production segmentation remains exactly `0.20 m` until separately validated;
- signed feedback budget remains exactly `64` until separately validated;
- signed `WeightWaterKgM` semantics remain unchanged;
- no 3D.

This roadmap follows completed validation work in Issues #511 and #516 and PRs #512-#520.

## 3. Milestone F1 — wave-aware quasi-static local demand

Expected size: approximately 4-5 small PRs.

### F1.1 Wave-load ownership RFC

Define, with source/model basis, what the existing design wave model physically loads and where that load belongs in the mooring chain.

Required questions:

- buoy wave load versus submerged line / connector / payload wave load;
- whether the existing `WaveForceN` is a buoy-level design force only or can be distributed;
- phase/envelope convention for quasi-static design use;
- whether wave contribution is applied as an incremental H/V boundary/load term or requires a separate local contribution;
- which quantities remain unavailable under the current model.

No production authority change in this package.

### F1.2 Wave-aware local force-state contract

Introduce the smallest immutable calculation-core state needed to describe local design demand along `s` without changing existing public scalar meanings prematurely.

Conceptually named quantities may include:

```text
LocalHN(s)
LocalVN(s)
LocalResultantN(s)
SurfaceResultantN
AnchorEndResultantN
MaxLocalResultantN
WaveContributionN / load-source identity
```

Exact type names are implementation details; semantics are not.

### F1.3 Independent/reference validation

Add analytical/reference fixtures before production migration.
At minimum cover:

- zero current / wave case;
- current + wave surface-buoy case;
- slack line;
- discrete payload;
- buoyant line / signed submerged weight;
- depth-varying current profile;
- cases where max-local demand is not at the same location as surface or anchor-end demand.

Do not tune tolerances merely to reproduce the legacy global scalar.

### F1.4 Shadow production path

Compute wave-aware local H/V/resultant demand in the calculation core while retaining legacy `TensionKn`, weak-link reserve, anchor reserve and verdict authority.

Record deterministic old/new evidence.

### F1.5 Tension-demand authority disposition

Only after F1.1-F1.4 decide whether the production design-demand contract should become maximum local demand, a location-specific demand family, or remain partly legacy.

Any migration must be explicit and isolated. Do not silently redefine `TensionKn`.

## 4. Milestone F2 — anchor-end vector and contact/uplift model

Expected size: approximately 3-4 small PRs.

The anchor calculation must consume an explicitly validated anchor-end load state rather than assume that one global horizontal force is sufficient.

Required model outputs/concepts:

```text
AnchorEndHN
AnchorEndVN
AnchorEndResultantN
contact / uplift state
horizontal holding demand
vertical / uplift demand
capacity / utilization / reserve
indeterminate or unsupported state when the model basis is insufficient
```

The package must explicitly define sign convention and the meaning of positive/negative vertical anchor reaction.

### v1 boundary

For `v1.0.0`, "contact model" means the anchor boundary and its horizontal/vertical reaction, including explicit uplift/contact validity.

The following are intentionally postponed unless separately approved and independently validated:

- full line-seabed touchdown mechanics;
- distributed seabed friction along the mooring line;
- cyclic soil degradation;
- embedded-anchor geotechnical models beyond supported current anchor types.

## 5. Milestone F3 — local weak-link / element demand

Expected size: approximately 2-3 small PRs.

Map every physical assembly item to its position or interval along `s`:

```text
buoy -> connector -> line -> connector -> payload -> ... -> anchor
```

For each strength-bearing item, derive demand only from the validated local-demand model at that item's actual location/range.

Conceptually:

```text
Demand_i = governing local resultant for item i
WorkingLoad_i = MBL_i / safety factor
Reserve_i = WorkingLoad_i / Demand_i
```

The governing weak link becomes the minimum valid local reserve, not simply the minimum capacity compared with one global tension scalar.

The implementation must preserve item identity, discrete-load ordering and source mapping.

## 6. Milestone F4 — integrated engineering verdict and reporting

Expected size: approximately 1-2 small PRs.

After F1-F3 are validated:

- migrate dependent checks one family at a time;
- update `Verdict` / `MainRisk` only after their upstream demand is validated;
- project the new calculated fields into user-facing read models;
- PDF, 2D and UI remain consumers only;
- the technical report may expose detailed diagnostics and old/new evidence;
- no renderer may recompute tension, anchor reaction, reserve or verdict.

## 7. Milestone F5 — release freeze and `v1.0.0`

Expected size: approximately 2 release PRs plus the final Release Candidate smoke pass.

Required release work:

1. freeze the engineering model and canonical regression set;
2. synchronize `AppInfo`, README and release notes with the chosen `v1.0.0` identity;
3. harden Windows publish artifact naming and SHA-256 checksum generation;
4. create/verify tag-to-GitHub-Release flow;
5. build clean self-contained `win-x64` artifact from exact `main`;
6. run full CI and Release Candidate smoke test:
   - app starts on Windows;
   - project create/save/load works;
   - calculation completes;
   - selected 2D opens;
   - PDF exports and uses selected geometry;
   - technical report opens;
   - canonical engineering regression suite passes.

## 8. Explicitly post-v1: full dynamics

Do not put a full time-domain mooring solver into the first release.

The following belong to a future Advanced Dynamics / v2-class milestone:

- time-domain integration of buoy and line motion;
- 6-DOF buoy response;
- added mass and dynamic damping models;
- irregular wave spectra and stochastic sea-state realization;
- RAO-based response coupling;
- dynamic slack/taut transitions;
- dynamic line-seabed touchdown/contact;
- fatigue / cycle counting;
- stochastic extreme-value analysis.

A full dynamic solver would be a separate calculation engine and must not destabilize the deterministic quasi-static v1 core.

## 9. Estimated implementation volume

Current estimate from the validated `a3cbdb1...` baseline:

| Block | Expected PR count |
|---|---:|
| F1 wave-aware local demand | 4-5 |
| F2 anchor-end vector/contact | 3-4 |
| F3 local weak-link / element demand | 2-3 |
| F4 verdict/report integration | 1-2 |
| F5 release hardening | 2 + RC |

Total expected before `v1.0.0`: approximately 12-16 small PRs plus the final RC smoke/release pass.

This estimate assumes no full dynamic solver and no line-seabed touchdown/friction model in v1.

## 10. Merge discipline

For every engineering package:

```text
architecture / physics definition
  -> independent/reference evidence
  -> shadow calculation when appropriate
  -> exact-head CI
  -> old/new evidence
  -> explicit authority decision
  -> production migration only when justified
```

Keep changes small. Never combine wave-aware tension, anchor authority, weak-link authority and verdict migration in one broad PR.

Every merged package must keep the repository's exact-final-head build/consumer checks green.
