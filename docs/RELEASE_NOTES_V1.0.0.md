# BuoyCalc Windows v1.0.0 — Release Candidate notes

Status: Release Candidate. Not yet tagged or published as a GitHub Release.

## Release focus

`v1.0.0` freezes the deterministic quasi-static engineering chain completed in Milestones F1-F4 and prepares it for final Windows verification.

## Engineering authority in v1

For Accepted `SignedBoundaryFeedback` selected cases:

- selected signed/quasi-static X/Z geometry is the selected geometry authority;
- F1 supplies the retained wave-aware selected design-tension demand;
- F2 supplies the retained anchor-end horizontal/vertical reaction and contact/uplift classification;
- F3 maps retained local demand to physical structural elements and supplies local MBL/WLL/reserve plus the governing valid local weak link;
- F4 supplies selected checks, Verdict and MainRisk from validated upstream selected state;
- UI, 2D, PDF and technical report remain read-model/presentation consumers only.

For non-Accepted signed candidates, the validated legacy selected read-model fallback remains in force.

## Anchor capacity boundary

The selected v1 model exposes anchor-end horizontal demand and rigid-body contact/normal reaction state. It does **not** reinterpret historical holding multipliers as a validated Coulomb friction coefficient or soil/embedment model.

Therefore horizontal anchor holding capacity remains `RequiresAdditionalPhysicalModel`; legacy `AnchorHoldingKg`, `RequiredAnchorHoldingKg`, `AnchorReserve` and historical holding multipliers are compatibility evidence only and cannot authorize a selected pass.

## Frozen numerical invariants

- production segmentation: exactly `0.20 m`;
- signed feedback budget: exactly `64`;
- signed `WeightWaterKgM` semantics: unchanged;
- Accepted signed candidate: exact deterministic fixed point, no convergence epsilon;
- line coordinate: `s=0` buoy/surface, `s=L` anchor/seabed.

## User-facing capabilities included in the RC

- ordered mooring assembly sequence;
- project create/save/load;
- deterministic engineering calculation;
- selected 2D geometry;
- PDF export from calculated/read-model state;
- compact selected user summary;
- full technical report with selected F1/F2/F3/F4 authority sections and retained compatibility diagnostics;
- canonical engineering regression suite.

## RC artifact evidence

F5-B fixes the Windows RC evidence set to:

```text
BuoyCalc-Windows-v1.0.0-win-x64.zip
BuoyCalc-Windows-v1.0.0-win-x64.sha256
BuoyCalc-Windows-v1.0.0-win-x64-manifest.json
```

The package is a self-contained single-file `win-x64` application. The manifest records the exact source commit, package SHA-256 and executable SHA-256. ZIP entry ordering/timestamp/compression are normalized so package evidence is not affected by packaging time or file enumeration order.

The automatic RC trigger ref is:

```text
release-candidate/v1.0.0
```

The release workflow refuses to build unless the checked-out commit equals `origin/main`.

## Explicit v1 non-goals

The first release intentionally excludes:

- full time-domain mooring dynamics;
- 6-DOF buoy dynamics;
- irregular wave spectra / RAO coupling;
- dynamic slack/taut transitions;
- distributed line-seabed touchdown/friction dynamics;
- fatigue/cycle counting;
- stochastic extremes;
- 3D visualization.

## Before final release

After F5-B is merged, the following still must happen:

1. exact-main green CI;
2. create/update `release-candidate/v1.0.0` at that exact main SHA;
3. build one clean self-contained Windows RC evidence artifact;
4. verify ZIP SHA-256 against both checksum file and manifest;
5. user manual Windows 11 smoke of that same ZIP;
6. explicit user approval;
7. only then create tag `v1.0.0` and GitHub Release for the tested commit/artifact.

Until step 6 is complete, this document describes a Release Candidate, not a published release.
