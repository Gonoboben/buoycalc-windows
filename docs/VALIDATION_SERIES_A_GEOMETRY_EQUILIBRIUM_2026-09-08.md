# BuoyCalc pre-v1 validation — Series A: Geometry & equilibrium

Issue: #599

## Purpose

This package gathers engineering evidence against the current production calculation pipeline **without changing solver/physics**. It was opened after manual RC smoke exposed a contradictory presentation case: a surface mooring with depth equal to line length and non-zero horizontal loading could still expose a legacy/iterative X/Z result as `Converged`, while the stricter signed boundary candidate rejected the state and selected F1-F4 were unavailable.

Series A is intentionally focused on geometry/equilibrium truth and authority propagation. It is not a complete validation of wave dynamics or anchor-soil holding capacity.

## Independent truth used in Series A

The campaign does not use a second BuoyCalc solver as reference truth. It checks only invariants that are independently defensible:

1. **Line shorter than depth:** for a surface-to-seabed inextensible connection, `L < D` is geometrically impossible.
2. **Exactly taut vertical line:** when `L = D`, any physically admissible endpoint has zero horizontal offset. Therefore a finite inclined equilibrium under non-zero horizontal load is impossible for an inextensible line.
3. **Endpoint chord:** every reported X/Z endpoint must satisfy `sqrt(X² + Z²) <= L` within the campaign geometry tolerance.
4. **Zero-current drag:** an explicit zero horizontal-current profile must not generate current drag.
5. **Production segmentation:** maximum production segment length remains `<= 0.20 m` and segment lengths close to the production line length.
6. **Accepted authority identity:** `Accepted` signed candidate must carry exact deterministic fixed-point identity and must be selected as `SignedBoundaryFeedback`.
7. **Accepted F1-F4 chain:** once the signed candidate is accepted, selected F1/F2/F3/F4 authorities must remain internally wired; missing authority becomes an evidence finding.

For slack cases (`L > D`), Series A does **not** assert a reference X displacement or tension. Passing the geometric invariants means only that no Series A contradiction was detected. Numerical validation against an independent mooring reference model is a later package.

## Common deterministic test hardware

Unless a scenario says otherwise:

- water density: `1025 kg/m³`;
- wave height/period: `0 / 0` (steady-current geometry isolation);
- buoy: `1.0 m³`, `100 kg`, projected area `0.10 m²`, `Cd=0.8`;
- line: `14 mm`, `MBL=70 kN`, signed water weight `+0.15 kg/m`, `Cd=1.0`;
- anchor: concrete block, `3000 kg` air weight, `1.2 m³` volume;
- safety factor: `3.0`;
- current authority: explicit depth profile only.

These are deterministic campaign fixtures, not equipment recommendations and not anchor-soil validation data.

## Scenario matrix

| ID | Depth D, m | Line L, m | L/D | Current/profile | Extra | Independent geometry expectation |
|---|---:|---:|---:|---|---|---|
| A01 | 20 | 19 | 0.95 | 0 | — | impossible: `L < D` |
| A02 | 20 | 20 | 1.00 | 0 | — | vertical taut boundary |
| A03 | 20 | 20 | 1.00 | 0.2 m/s constant | — | impossible finite inclined equilibrium |
| A04 | 20 | 20 | 1.00 | 0.6 m/s constant | — | impossible finite inclined equilibrium |
| A05 | 20 | 20.2 | 1.01 | 0.2 m/s constant | — | slack geometry admissible |
| A06 | 20 | 22 | 1.10 | 0.2 m/s constant | — | slack geometry admissible |
| A07 | 50 | 49 | 0.98 | 0.3 m/s constant | — | impossible: `L < D` |
| A08 | 50 | 50 | 1.00 | 0 | — | vertical taut boundary |
| A09 | 50 | 50 | 1.00 | 0.5 m/s constant | — | impossible finite inclined equilibrium |
| A10 | 50 | 50.5 | 1.01 | 0.5 m/s constant | — | slack geometry admissible |
| A11 | 50 | 55 | 1.10 | 0.5 m/s constant | — | slack geometry admissible |
| A12 | 100 | 99 | 0.99 | 0 | — | impossible: `L < D` |
| A13 | 100 | 100 | 1.00 | 0.3 m/s constant | — | impossible finite inclined equilibrium |
| A14 | 100 | 101 | 1.01 | 0.3 m/s constant | — | slack geometry admissible |
| A15 | 100 | 110 | 1.10 | 0.3 m/s constant | one internal payload | slack geometry admissible + point load |
| A16 | 350 | 350 | 1.00 | 0.45 m/s constant | manual-smoke class | impossible finite inclined equilibrium |
| A17 | 350 | 353.5 | 1.01 | 0.45 m/s constant | — | slack geometry admissible |
| A18 | 350 | 385 | 1.10 | 0.60→0.30→0.10 m/s | sheared profile | slack geometry admissible |
| A19 | 500 | 500 | 1.00 | 0.60 m/s constant | — | impossible finite inclined equilibrium |
| A20 | 500 | 550 | 1.10 | veering E/N profile | one internal payload | slack geometry admissible + directional/profile/point-load probe |

## Evidence captured per scenario

The runner writes JSON, CSV and Markdown containing:

- production scalar current/wave force;
- fallback shape endpoint/convergence;
- iterative shape endpoint/convergence/stop reason;
- selected read-model source and endpoint;
- endpoint chord and chord-vs-line invariant;
- signed candidate status, boundary classification, diagnostic code/text, feedback iterations, exact-fixed-point flag;
- F1 design demand availability/value;
- F2 horizontal demand and signed normal reaction availability/value;
- F3 local structural-capacity coverage and governing reserve;
- F4 availability/verdict;
- maximum production segment length and segment-length closure;
- classified discrepancy findings (`Physics`, `Geometry`, `Authority`, `Presentation`, `Segmentation`, `Execution`).

## Interpretation rule

A discrepancy is evidence, not a reason to alter a baseline or weaken a test. Production solver/physics changes are out of scope for #599. Any corrective physics change must be proposed in a separate validation package based on the findings from this campaign.
