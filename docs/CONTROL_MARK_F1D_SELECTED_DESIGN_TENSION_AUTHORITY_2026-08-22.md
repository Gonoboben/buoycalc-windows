# Control mark — F1-D selected design-tension authority — 2026-08-22

Parent: #522  
Package: #530

## Decision

`CalculationResult.TensionKn` is **not** redefined.

The validated F1-B/F1-C wave-aware envelope has location-specific quasi-static semantics that differ from the legacy global aggregate scalar. F1-D therefore introduces a separately named typed selected authority:

`MooringSelectedDesignTensionDemandState`.

It exists only when the selected design envelope belongs to an Accepted `SignedBoundaryFeedback` shape.

## Governing definition

The selected design demand is the maximum of the already validated design-envelope resultants at:

1. surface boundary;
2. anchor-end boundary;
3. maximum local production-segment midpoint.

No new force equation is introduced in F1-D. The projector only selects among F1-B resultants validated independently in F1-C.

For exact equal-resultant ties, provenance selection is deterministic and matches F1-C evidence ordering: `AnchorEnd`, `Midpoint`, `Surface`. The force demand itself is identical in a tie.

## Typed provenance

The state exposes:

- source identity (`SignedBoundaryFeedback`);
- governing demand in N and kN;
- physical location kind (`Surface`, `AnchorEnd`, `Midpoint`);
- midpoint segment/source-element identity when applicable;
- along-line coordinate;
- the existing horizontal wave increment used by the envelope;
- an explicit quasi-static/no-dynamic-claim method note.

## Availability

Accepted signed-selected canonical cases expose this authority.

Non-Accepted / legacy-selected cases return no signed selected design-demand authority. They continue to have the legacy `CalculationResult.TensionKn` compatibility scalar; F1-D does not fabricate local signed physics for them.

## Explicitly unchanged

- `CalculationResult.TensionKn` and its current calculation path;
- selected X/Z and source selection;
- signed solver equations;
- wave-force equation;
- 0.20 m production segmentation;
- signed feedback budget 64;
- signed submerged-weight semantics;
- anchor model/reserve;
- weak-link policy/reserve;
- checks, verdict and main risk;
- persistence;
- PDF, 2D and UI;
- 3D (post-v1 only).

## Next authority boundary

F2 may consume validated anchor-end H/V from the design envelope for an independently validated anchor reaction/contact model. F3 may consume local demand at actual element/weak-link locations. Neither migration is performed by F1-D.
