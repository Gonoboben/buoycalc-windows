# Control mark — signed tension demand disposition

Date: 2026-08-22  
Issue: #516  
Package: E1-C — final tension-demand authority disposition  
Base main: `6b72ea2a7c4fbb74188b066d312b19aa711bffc8`

## Decision

E1 validation is complete, but it does **not** authorize a production migration of global `CalculationResult.TensionKn`.

The first-release/current production disposition is:

```text
Selected geometry/source authority:
  SignedBoundaryFeedback where the candidate is Accepted

Global scalar tension authority:
  retain existing CalculationResult.TensionKn

SignedBoundarySurfaceResultantN:
  validated location-specific steady-current evidence only

SignedBoundaryAnchorEndResultantN:
  validated location-specific steady-current evidence only;
  candidate input for a future independently validated anchor/seabed demand model

SignedTraceMaxMidResultantN:
  validated local steady-current evidence only;
  candidate input for a future wave-aware/local structural-demand model
```

This is a deliberate non-migration decision, not a rollback of the signed geometry switch.

## Why the global tension authority stays legacy

E1-A proved that the selected signed boundary/feedback path is explicitly:

```text
steady current; wave excluded
```

E1-B1 independently validated the physical interpretation of surface and anchor-end resultants on a neutral analytical fixture.

E1-B2 proved that the two canonical Accepted candidates retain:

- direct surface H/V;
- direct anchor-end H/V;
- one final exact-fixed-point midpoint resultant per production segment;
- a measurable max-local midpoint resultant.

However the production legacy global scalar is built from a different load contract:

```text
HorizontalForceN = CurrentForceN + WaveForceN
VerticalForceN   = max(0, NetBuoyancyKg) * g
TensionKn        = sqrt(HorizontalForceN² + VerticalForceN²) / 1000
```

The canonical Accepted scenarios deliberately retain positive `WaveForceN`, while the signed boundary method explicitly excludes wave. Therefore replacing global `TensionKn` by any current signed surface/end/max-mid resultant would silently discard a load family and change the meaning of the design demand.

No numerical proximity can remove that semantic mismatch.

## Weak-link implication

Current production element reserve/status uses one global `TensionKn` against each element working load. E1-B2 now shows that the signed model contains local segment-midpoint tension information, but current local values are steady-current only.

A future migration of weak-link/WLL authority therefore requires an independently validated **wave-aware local demand contract**, including a decision for discrete connectors/payloads and boundary locations. Until then:

```text
WorkingLoad / TensionReserve / element Reserve / element Status
remain driven by the existing global legacy TensionKn contract.
```

This is conservative with respect to current behavior and prevents an incomplete local-demand model from becoming production authority.

## Anchor implication

`SignedBoundaryAnchorEndResultantN` is physically the most relevant of the three signed tension quantities for a future anchor/seabed demand path, but it is not sufficient to replace current anchor reserve automatically:

- it excludes wave;
- current anchor holding is a separate empirical holding model;
- direction/component handling and seabed mechanics require their own validation.

Therefore anchor scalar authority remains unchanged in E1.

## E1 disposition matrix

| Field / candidate quantity | E1 status | Production action |
| --- | --- | --- |
| selected X/Z/source | already validated signed authority when Accepted | unchanged |
| signed surface resultant | validated steady-current location-specific evidence | no scalar switch |
| signed anchor-end resultant | validated steady-current location-specific evidence | future anchor validation input only |
| signed max-mid resultant | validated steady-current local evidence | future wave-aware local-demand input only |
| `CalculationResult.TensionKn` | legacy wave-inclusive global demand | retain authority |
| `TensionReserve` / element reserve/status | derived from global legacy `TensionKn` | retain authority |
| weak-link/check/verdict/main risk | downstream of current scalar/check contracts | retain authority |
| anchor reserve/holding | separate legacy empirical model | retain authority |

## What would unblock a future tension migration

A later package may transfer tension/weak-link authority only after all of the following are explicit and independently validated:

1. wave contribution in the local/signed demand path, or an explicitly justified alternative load-combination rule;
2. local demand at line segments, connectors and payloads;
3. surface and anchor boundary demand semantics;
4. WLL/MBL/safety-factor application by physical location;
5. first-breaking-element selection from those local demands;
6. downstream check/verdict behavior against reference fixtures;
7. old/new evidence before the production authority switch.

## E1 completion statement

E1 has answered the question it was created to answer:

> Which currently available signed tension quantity may safely replace the production global tension demand now?

Answer:

```text
None.
```

The signed surface, anchor-end and max-mid quantities are now physically located and validated as evidence, but the wave-aware/local-demand contract required for downstream structural authority is not yet present.

Accordingly E1 closes with:

```text
GlobalProductionTensionAuthority = LegacyRetained
ProductionMigrationAuthorized    = False
GeometryAuthority                = SignedWhereAccepted
```

## Non-change statement

Unchanged:

- solver equations;
- signed candidate acceptance/exact fixed-point rule;
- selected signed geometry/source authority;
- production `CalculationResult.TensionKn`;
- tension reserve and element reserve/status formulas;
- weak-link/WLL/check/verdict/main-risk behavior;
- anchor holding/reserve formulas;
- exact 0.20 m segmentation;
- production feedback budget 64;
- signed submerged-weight semantics;
- current wave model;
- PDF/2D/UI physics;
- persistence/schema;
- golden baseline;
- 3D remains out of scope.
