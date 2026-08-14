# Control mark: boundary-conditioned signed geometry measurements

Date: 2026-08-14
Issue: #407
Prerequisites: completed #413, merged #474 and #475
Scope: CI-derived validation evidence only. No production selected-X/Z change.

## 1. Measurement provenance

The measurements below come from the successful exact-head CI run for validation PR #475:

- exact head: `38cb5a5c3b02e6593c3a4f23bb321578fb97f03c`;
- `.NET Build`: run #915, success;
- `Selected Shape Consumer Scan`: run #375, success;
- `Report Store Consumer Scan`: run #378, success;
- engineering golden verification: 5 scenarios passed without baseline rewrite.

The validation reconstruction read the already stored `SurfaceBoundaryTensionTrace` from the calculation snapshot and used only:

```text
dx = ds * TangentX
dz = ds * TangentZ
```

It did not call the surface-boundary integration kernel, did not use `SelectedShape` as an input and did not introduce `Abs(H)`, `Abs(V)` or an unsigned angle clamp.

## 2. Canonical A–E identity measurements

| Case | Trace X, m | Trace Z, m | Parent boundary X, m | Parent boundary Z, m | dz < 0 segments | Point loads | Current selected X, m | Boundary - selected X, m |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| A | 21.64602261169438 | 50.00032484663539 | 21.64602261169438 | 50.00032484663539 | 0 | 3 | 21.151843441661676 | +0.4941791700327052 |
| B | 57.84849971237418 | 120.002163416273 | 57.84849971237418 | 120.00216341627299 | 0 | 4 | 58.26919754625708 | -0.4206978338829046 |
| C | 139.13182540637308 | 379.9917406829415 | 139.13182540637308 | 379.9917406829415 | 0 | 4 | 143.90606326947662 | -4.7742378631035365 |
| D | 150.74304272227087 | 379.9943097703955 | 150.74304272227087 | 379.9943097703955 | 0 | 4 | 138.63369646261512 | +12.109346259655752 |
| E | 149.51702268181404 | 379.99707269103715 | 149.517022681814 | 379.99707269103715 | 0 | 4 | 130.0129289611826 | +19.50409372063143 |

The reconstructed trace endpoint is numerically identical to the parent frozen-load boundary integration within the regression tolerance (`1e-9 m`). The tiny displayed last-digit differences in B/E are floating-point summation order only.

This proves an implementation identity:

```text
stored boundary-conditioned H/V
-> stored signed tangent
-> ds * tangent
-> same frozen-load endpoint X/Z
```

It is intentionally not a second independent physical validation claim; both representations have the same physics owner.

## 3. Signed/quadrant gate

The same validation package also passed controlled mechanical checks:

- `V > 0` preserves `TangentZ > 0` and `dz > 0`;
- a mechanically defined buoyant distributed-load state with `V < 0` preserves `TangentZ < 0` and `dz < 0`;
- signed negative water weight increases the downward cable component under the approved top-to-bottom `V_after = V_before - W_water*g` convention;
- a buoyant point-load jump preserves its signed vertical effect and is crossed exactly once;
- a zero resultant remains indeterminate and no artificial tangent or X/Z increment is manufactured.

No normal solved free-surface canonical A–E case produced a local `dz < 0`. This is a measured result, not a reason to force an upward segment into those cases.

## 4. Relationship to current selected X/Z

The existing production `SelectedShape` remains authoritative. Its X values were logged only for impact comparison.

The boundary-conditioned frozen-load result is not numerically interchangeable with the current iterative candidate:

- A/B are close but not identical;
- C differs by about `-4.77 m`;
- D differs by about `+12.11 m`;
- E differs by about `+19.50 m`.

Therefore this gate does **not** justify replacing selected X/Z, changing `MooringPrimaryShapeGate`, or rewriting the engineering golden baseline.

## 5. Phase-B conclusion

The blocker identified by the old #410 experiment is now resolved at the validation level:

```text
raw SegmentTensionRows H/V
    != valid anchored-cable direction field

solved surface boundary + shared load ownership
    -> boundary-conditioned H/V trace
    -> signed tangent
    -> deterministic frozen-load geometry
```

The signed vector representation is now sufficient to reconstruct the already solved frozen-load geometry without quadrant loss.

## 6. Next allowed experiment

The next package under #407 may be a **validation-only boundary-conditioned feedback-coupling study**.

Before code, the experiment must keep these contracts explicit:

1. Start from a solved boundary-conditioned shape/tension state, not historical raw cumulative H/V.
2. Recompute the chosen orientation-dependent distributed hydrodynamic force field from the current candidate shape.
3. Re-solve the surface boundary reaction `Q0` for that updated distributed field before producing the next tangent field.
4. Keep connector/payload point loads separate and cross each exactly once.
5. Build the next validation geometry directly from signed tangent components; do not route through the historical unsigned `0..89°` geometry clamp.
6. Record convergence history at validation-only budgets `4 / 8 / 16 / 32 / 64`.
7. Do not change production `MooringIterativeSolver.MaxIterations`, selected X/Z, gate/verdict, PDF/2D, anchor/weak-link calculations or golden baseline.

The exact choice of updated hydrodynamic force field and any under-relaxation rule must be documented before implementing that feedback loop. A high iteration count is not itself evidence of a correct model.

## 7. Non-goals

This measurement record does not authorize changes to:

- `BuoyCalculator`;
- `MooringShapeSolver`;
- production `MooringShapeForceAnalyzer` behavior;
- `MooringShapeTensionAnalyzer`;
- `MooringDiscreteLoadTensionAnalyzer`;
- `MooringDiscreteLoadShapeBuilder`;
- `MooringIterativeSolver`;
- `MooringPrimaryShapeGate` / selector;
- selected X/Z;
- production MaxIterations;
- 2D/PDF geometry;
- anchor/weak-link calculations;
- verdict;
- signed `WeightWaterKg`;
- production 0.20 m segmentation target or unlimited segment count;
- JSON/DTO;
- engineering golden baseline;
- 3D.
