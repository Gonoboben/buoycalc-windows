# Signed-Geometry Production Authority Contract

Date: 2026-08-20  
RFC: #487  
Status: control contract only; no production authority switch in this package

## Purpose

This document defines the authority boundary that a future signed-geometry primary-shape candidate must satisfy before it can become a production-selected mooring shape.

The contract is intentionally behavior-preserving. It does not change the solver, selected X/Z, source identity, tensions, anchor load, weak-link evaluation, verdicts, reports, PDF, 2D, UI, golden baselines, elasticity assumptions, or any other runtime output.

## 1. Current production authority path

Current primary-shape authority belongs to the calculation core.

- `MooringPrimaryShapeSelector` arbitrates between the existing iterative candidate and fallback candidate.
- `CalculationSnapshot` carries the selected geometry and the metadata describing that selection.
- `SelectedShapeReadModel` and `SelectedMooringShapeProvider` project the already-selected snapshot; they do not own selection authority.
- Report, PDF, 2D, and UI consumers must continue to render the selected core result and must not create or replace engineering coordinates.

A future signed-geometry candidate must therefore enter the core selection path before downstream read models. It must never be injected from a renderer, report builder, UI layer, or read-model projection.

## 2. Signed-candidate ingress

Before a signed-geometry result may compete for primary-shape authority, the calculation core must expose an explicit candidate/eligibility result with enough information to determine whether that candidate is admissible.

A later selector change may arbitrate among signed geometry, iterative geometry, and fallback geometry only after the gates in this document are closed.

Until then, the current iterative/fallback selection behavior remains authoritative.

## 3. Truthful source identity

Selected source identity must always describe the source that actually produced the selected coordinates.

A signed-geometry candidate must not masquerade as `Iterative` or `Fallback` merely to reuse the existing enum or downstream schema.

Adding a `SignedGeometry` source value, or an equivalent explicit source representation, is a future implementation gate. This document does not change `MooringPrimaryShapeSource` or any DTO/schema.

## 4. Candidate acceptance and deterministic rejection

A signed candidate may become eligible for selection only when all conditions required by its production contract are satisfied. At minimum, the selected geometry must be finite, node-aligned with the sequence expected by downstream consumers, and physically admissible under the current inextensible/no-stretch model.

Eligibility must be explicit and deterministic. A rejected signed candidate must leave the existing iterative/fallback selection path in force; rejection must not silently mutate fallback geometry, convergence tolerances, or golden data.

`MooringSurfaceBoundaryInfo.Available` or a surface-boundary classification is not by itself equivalent to signed-candidate acceptance.

## 5. Convergence and selected-status semantics

Selection metadata must remain factual.

- `SelectedConverged` must describe the acceptance/convergence state of the candidate that actually owns the selected geometry. It must not be copied from the iterative solver when a different source is selected.
- Iterative iteration count, stop reason, tolerances, invalid-node information, and other iterative diagnostics remain facts about the iterative solver even when another candidate is selected.
- A signed candidate needs its own explicit acceptance/solution-state semantics before it can become authoritative.

No implementation may report a signed-selected geometry as an iterative convergence success simply because the iterative metadata already exists.

## 6. Discrete-load semantics are a gate

`SelectedUsesDiscreteLoads` has established meaning for the current selected path. Its value for a future signed candidate is not defined by this contract.

Before a signed candidate can own selected X/Z, the project must explicitly define whether and how that candidate incorporates connector, payload, and other discrete-load effects, and which core result owns that information.

The value must not be guessed or hard-coded merely to satisfy an existing downstream consumer.

## 7. Diagnostics and gating remain core-owned

Engineering diagnostics and eligibility gates remain calculation-core responsibilities.

A future signed candidate must expose deterministic rejection/acceptance diagnostics at the core boundary. UI, PDF, 2D, and report code may display those diagnostics but may not reinterpret a rejected candidate as selected or suppress an unmet engineering gate.

Diagnostic severity must remain truthful to the selected result and to any rejected candidate that materially affects engineering interpretation.

## 8. Geometry authority does not automatically transfer downstream physics

Changing selected X/Z authority does not, by itself, authorize signed-geometry values for:

- line/segment tension;
- anchor demand or reserve;
- weak-link evaluation;
- final engineering verdict;
- other force-derived downstream quantities.

Each downstream quantity must have a defined and validated data owner before it can claim signed-geometry authority.

If a future package selects signed geometry while some downstream force quantities still come from another validated source, that mixed-authority state must be explicit and non-misleading. It must not be presented as a fully signed solved state.

## 9. Required gates before any production authority switch

A later implementation package may change selected-shape authority only after all applicable gates below are explicitly resolved and validated:

1. explicit signed source identity/schema is approved;
2. signed candidate/result DTO and eligibility contract are approved;
3. signed geometry has regression coverage beyond the exact vertical limiting fixture, including admissible non-taut cases required by the intended production domain;
4. signed rejection and deterministic iterative/fallback behavior are regression-tested;
5. node count/order and finite-coordinate requirements are validated against selected-shape consumers;
6. `SelectedConverged` and signed solution/acceptance status semantics are defined;
7. `SelectedUsesDiscreteLoads` semantics are explicitly resolved;
8. diagnostics and severity/gate behavior are explicitly resolved;
9. tension, anchor, weak-link, verdict, and other downstream authority are either validated for signed results or explicitly isolated from the geometry-only switch;
10. no committed golden baseline is changed merely to make the candidate appear compatible;
11. no elasticity/stretch model is introduced implicitly;
12. exact-final-head `.NET Build`, `Selected Shape Consumer Scan`, and `Report Store Consumer Scan` are green.

## 10. What the known vertical fixture proves — and does not prove

For the established `L = depth`, zero-horizontal-load fixture, the current validation evidence supports a unique straight vertical geometry (`X = 0`, `Z = depth`) under the current inextensible model.

The same fixture does not establish a unique `Q0`. The validated force state is a family bounded by the required lower limit and available capacity. Therefore this fixture is useful evidence for signed-geometry geometry semantics, but it is not sufficient by itself to authorize a global signed primary-shape source or a unique solved force state.

## 11. Exit condition for Package D

This package is complete when this contract is reviewed by CI and merged without runtime changes.

A later implementation package may introduce signed-candidate data structures or validation-only selection tests while remaining behavior-preserving. The actual production selected X/Z authority switch must remain a separate, explicit package after the gates above are closed.
