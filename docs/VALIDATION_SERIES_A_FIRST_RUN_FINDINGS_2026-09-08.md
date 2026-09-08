# BuoyCalc validation Series A — first run findings

Issue: #599  
Workflow run: `34229653489`  
Campaign code head: `a39d0b6cc62d27ff5a1299e90d71380c8762b502`  
Evidence artifact: `buoycalc-validation-series-a-a39d0b6cc62d27ff5a1299e90d71380c8762b502` (artifact id `10057243252`)

## Executive result

The first 20-scenario Series A campaign completed successfully against the production BuoyCalc assembly. The run produced `49` raw findings (`40 CRITICAL`, `9 HIGH`) under the intentionally strict first-pass chord tolerance. The raw severity count must not be interpreted literally: several chord exceedances are millimetre-scale legacy/fallback closure residuals within the existing `0.01 m` geometry closure convention. The material findings are separated below.

## 1. Taut line + horizontal current — systematic critical presentation/legacy-geometry defect

All six exactly-taut non-zero-current scenarios behaved consistently in the strict signed authority chain:

| ID | D=L, m | U, m/s | Signed status | Boundary classification | Legacy/selected X, m | Endpoint chord, m |
|---|---:|---:|---|---|---:|---:|
| A03 | 20 | 0.2 | RejectedPhysical | TautNonZeroHorizontalLoadNoFiniteRoot | 3.8299 | 20.3634 |
| A04 | 20 | 0.6 | RejectedPhysical | TautNonZeroHorizontalLoadNoFiniteRoot | 17.3793 | 26.4961 |
| A09 | 50 | 0.5 | RejectedPhysical | TautNonZeroHorizontalLoadNoFiniteRoot | 38.6621 | 63.2041 |
| A13 | 100 | 0.3 | RejectedPhysical | TautNonZeroHorizontalLoadNoFiniteRoot | 40.1962 | 107.7763 |
| A16 | 350 | 0.45 | RejectedPhysical | TautNonZeroHorizontalLoadNoFiniteRoot | 245.9540 | 427.7773 |
| A19 | 500 | 0.6 | RejectedPhysical | TautNonZeroHorizontalLoadNoFiniteRoot | 434.4836 | 662.4017 |

Independent geometry requires `X=0` when `L=D`. The signed boundary gate therefore behaves correctly. However the selected user-facing shape falls back to `MooringIterativeSolver.FinalShape`, remains `Converged=true`, and can expose grossly impossible endpoint chords.

**Classification:** critical presentation/authority-selection defect around a correctly rejected physical state; the evidence does not justify changing the signed physical gate.

## 2. Line shorter than depth — signed rejection correct, but fallback diagnostic geometry is not physical

A01, A07 and A12 all produced:

- `SignedStatus = RejectedPhysical`;
- `Boundary = LineShorterThanDepth`;
- F1/F2/F3/F4 unavailable;
- fallback/iterative/selected diagnostic endpoint at the requested seabed depth despite `L<D`;
- those shapes are `Converged=false`.

This is not the same severity as the taut-current defect because the legacy shape is explicitly non-converged, but the presentation layer still needs to propagate the physical rejection into a direct hard-failure engineering assessment rather than simply leaving F1-F4 undefined.

## 3. Exactly taut + zero current — geometry correct, force authority remains indeterminate

A02 and A08 produced:

- `X=0`;
- endpoint chord exactly equals line length;
- `SignedStatus = Indeterminate`;
- `Boundary = VerticalGeometryUniqueForceStateFamily`;
- F1-F4 unavailable.

No Series A geometry contradiction was detected. This state exposes a separate model-scope question: the current boundary model does not select one unique vertical force state for the taut zero-horizontal-load family. It should be reviewed later with the buoy hydrostatic/draft assumptions; it must not be silently converted to an arbitrary accepted force state.

## 4. Slack cases — 6 accepted, 3 exact-fixed-point budget exhaustions

Nine slack cases (`L>D`) were run.

### Accepted — F1-F4 complete

| ID | D, m | L/D | Selected X, m | Signed iterations | F1 demand, N | F2 horizontal, N | F3 governing reserve | F4 |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| A05 | 20 | 1.01 | 2.5081 | 9 | 55.3902 | 7.3207 | 421.2538 | Требуется проверка |
| A10 | 50 | 1.01 | 6.3692 | 10 | 478.4713 | 99.0371 | 48.7664 | Требуется проверка |
| A14 | 100 | 1.01 | 12.0979 | 10 | 396.4371 | 67.6285 | 58.8576 | Требуется проверка |
| A17 | 350 | 1.01 | 42.5690 | 9 | 2498.1238 | 511.8368 | 9.3403 | Требуется проверка |
| A18 | 350 | 1.10 | 143.6019 | 12 | 964.0336 | 337.2163 | 24.2039 | Требуется проверка |
| A20 | 500 | 1.10 | 189.1955 | 11 | 1456.0212 | 353.3656 | 16.0254 | Требуется проверка |

For every `Accepted` case in this first series:

- exact fixed point was reached;
- selected source was `SignedBoundaryFeedback`;
- F1, F2, F3 and F4 were all present;
- endpoint chord was within line length;
- F4 was `Требуется проверка`, consistent with the still-unvalidated horizontal anchor-soil holding-capacity model.

### Budget exhausted — physically/numerically valid path but no selected F1-F4

| ID | D, m | L/D | Current | Payload | Selected fallback/iterative X, m | Signed status |
|---|---:|---:|---|---|---:|---|
| A06 | 20 | 1.10 | 0.2 m/s constant | no | 9.1484 | BudgetExhausted |
| A11 | 50 | 1.10 | 0.5 m/s constant | no | 22.9221 | BudgetExhausted |
| A15 | 100 | 1.10 | 0.3 m/s constant | yes | 42.5029 | BudgetExhausted |

All three ended with diagnostic `SignedCandidateExactFixedPointNotReached` after exactly 64 feedback iterations. The production evaluator explicitly reports that the candidate remained physically/numerically valid but did not reach bit-exact deterministic fixed point. Consequently F1-F4 are all unavailable.

This is a **coverage/numerical-state blocker**, not evidence that an epsilon should be added. Project policy requires exact deterministic fixed-point acceptance. The next validation step is to identify which state components still differ at iteration 64 and whether the iteration is approaching a representable fixed point, oscillating, or cycling.

## 5. Millimetre-scale legacy closure findings

A06, A10 and A11 include legacy/iterative endpoint-chord exceedances of approximately 3.8–6.9 mm. These are materially different from the gross taut-current violations above and are within the existing centimetre-scale geometry closure convention used by legacy shape/projection code.

They should be reclassified as numerical/tolerance evidence rather than critical physical failures unless they propagate into the selected signed authority. A10 is especially useful: its fallback/iterative chord is slightly over the line length, but the accepted signed-selected endpoint is valid and F1-F4 are complete.

## 6. Checks that passed across Series A

No campaign finding was produced for:

- production segment maximum exceeding `0.20 m`;
- production segment-length sum failing to close to total line length;
- explicit zero-current profile generating non-zero current drag;
- `Accepted` signed candidate lacking exact-fixed-point identity;
- `Accepted` signed candidate failing to become the selected `SignedBoundaryFeedback` source.

## Immediate next validation action

Do **not** modify solver/physics yet.

Add diagnostic instrumentation outside production physics for A06/A11/A15 to record, at each of the 64 production-equivalent feedback iterations:

- Q0;
- endpoint X/Z;
- total line current force;
- maximum per-segment current-force delta;
- maximum node X/Z delta;
- trace H/V/tangent deltas;
- whether a two-state or multi-state cycle appears.

The purpose is to explain `BudgetExhausted` while preserving the mandatory exact deterministic fixed-point rule.
