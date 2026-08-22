# Control mark — F5-A v1 engineering freeze

Date: 2026-08-23
Parent: #522
Package: #551
Exact F5 starting main: `5ecc0ac913ff203ea3a1015cb3e3665c74a7d6f4`
Canonical engineering baseline path: `validation/BuoyCalc.EngineeringRegression/baselines/engineering-baseline.json`
Frozen canonical baseline Git blob: `97b0221ab29d8df4c9f2f435a1ba1780033d318a`
Release identity: `v1.0.0`
Release state: Release Candidate; no tag/GitHub Release before manual Windows approval.

## Completed pre-release engineering chain

Milestones F1-F4 are complete before this freeze.

Selected Accepted authority chain:

```text
environment
  -> Accepted SignedBoundaryFeedback geometry/source
  -> retained F1 selected design-tension demand
  -> retained F2 anchor-end reaction/contact state
  -> retained F3 local structural demand/capacity/reserve
  -> retained F4 checks / Verdict / MainRisk
  -> UI / 2D / PDF / technical report projection only
```

Non-Accepted signed candidates preserve the validated legacy selected read-model fallback.

## Frozen v1 numerical/semantic boundaries

The following may not change inside F5 packaging/version work:

- production segment length: exactly `0.20 m`;
- signed feedback iteration budget: exactly `64`;
- signed `WeightWaterKgM` meaning: unchanged;
- Accepted candidate rule: exact deterministic fixed point, no convergence epsilon;
- coordinate direction: `s=0` buoy/surface -> `s=L` anchor/seabed;
- selected geometry/source arbitration semantics;
- F1 wave-aware selected design demand semantics;
- F2 anchor reaction/contact sign convention;
- F3 per-element capacity/reserve and governing valid local weak-link semantics;
- F4 hard-failure/review/verdict policy;
- renderer/read-model boundary: no report/UI/PDF/2D engineering recomputation.

## Anchor model boundary

For `v1.0.0`, selected anchor state includes horizontal demand and contact/normal/uplift reaction evidence. A validated horizontal anchor/soil capacity model is still unavailable.

Historical holding multipliers and legacy `AnchorHoldingKg`, `RequiredAnchorHoldingKg`, `AnchorReserve` remain compatibility-only evidence and cannot authorize selected `Подходит`.

## Canonical regression freeze

The canonical baseline blob identified above is already green after F1-F4. F5-A does not regenerate or modify it for versioning.

CI must continue to run the full engineering regression suite. A future change to this baseline requires a separate explicit engineering justification; release packaging/version work must not update it merely to make CI green.

## Release identity

The application and assembly metadata are synchronized to `v1.0.0` for the Release Candidate. The UI also carries a visible `Release Candidate` note.

This is intentionally separated from publication state:

- app/binary identity may be `1.0.0` during the final candidate test;
- no git tag `v1.0.0` yet;
- no GitHub Release yet;
- the tested RC commit/artifact must remain unchanged between successful manual smoke and final publication.

## Remaining F5 work

F5-B must harden deterministic `win-x64` packaging, artifact naming, manifest/checksum and release/tag flow. After F5-B, build an exact-main RC and stop for the user's Windows 11 manual smoke.

Only explicit approval of that smoke permits the final `v1.0.0` tag and GitHub Release.

## Post-v1

3D and advanced/full dynamics remain post-v1 work and are not part of this freeze.
