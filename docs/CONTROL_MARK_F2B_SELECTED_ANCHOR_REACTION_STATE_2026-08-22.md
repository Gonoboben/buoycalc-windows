# Control mark — F2-B selected anchor reaction/contact state — 2026-08-22

Parent: #522  
Package: #534

## Decision

F2-A validated the lower-boundary sign/action-reaction/contact semantics. F2-B promotes only those validated quantities into a separately named immutable selected state:

`MooringSelectedAnchorReactionState`.

It is not a horizontal holding-capacity model and does not replace the legacy anchor reserve.

## Availability

The state is available only when:

1. an F1-B selected design envelope exists;
2. its source identity is `SignedBoundaryFeedback`;
3. anchor-end H/V/resultant are finite and internally consistent;
4. the existing core `AnchorWeightWaterKg` is finite and positive.

If no signed-selected design envelope exists, the state is unavailable. A zero or negative submerged anchor weight also yields no selected physical contact state instead of fabricating compressive contact.

## State contents

The state preserves both force ownership views:

- internal selected anchor-end `H`, `V`, resultant;
- line-on-anchor `H`, `V` in the same `+Z` downward coordinates, exactly opposite the internal vector.

It also exposes:

- horizontal demand magnitude `abs(H)`;
- upward line pull `max(0, V)`;
- downward line push `max(0, -V)`;
- anchor submerged weight in kg and N;
- signed normal balance `N = Wsubmerged - V`;
- non-negative compressive normal `max(0, N)`;
- uplift excess `max(0, -N)`;
- contact classification: `CompressiveContact`, `ZeroNormalLimit`, or `UpliftSeparation`.

## Weight authority

F2-B does not duplicate the anchor buoyancy calculation. It consumes `CalculationResult.AnchorWeightWaterKg`, whose production definition remains:

`WeightAirKg - rho * VolumeM3`.

F2-A independently reconstructed and checked that value from the input contract.

## What the state does not claim

The state is a quasi-static lower-boundary reaction/contact projection only. It does not define:

- soil shear strength;
- friction coefficient;
- embedment or drag-anchor mechanics;
- suction-anchor capacity;
- penetration/pullout;
- horizontal/vertical interaction diagrams;
- line touchdown or seabed friction;
- dynamic uplift/contact;
- a rule that horizontal holding capacity scales with normal reaction.

Those would require an independently justified anchor/soil model.

## Explicitly unchanged

- `RequiredAnchorHoldingKg`;
- `AnchorHoldingKg`;
- `AnchorReserve`;
- anchor type/base/seabed coefficients;
- `CalculationResult.TensionKn`;
- weak-link logic;
- checks/verdict/main risk;
- selected X/Z/source;
- solver equations, wave equation, 0.20 m segmentation, feedback budget 64 and signed weight semantics;
- persistence, PDF, 2D and UI;
- 3D remains post-v1.

## Next boundary

F2-C must compare the existing horizontal holding-capacity semantics with the selected local anchor demand/contact state and decide whether a v1 transfer is physically justified. It must not assume capacity scaling from normal reaction without evidence.
