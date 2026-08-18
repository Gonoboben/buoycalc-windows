# Control mark: independent analytical reference for boundary-conditioned feedback

Date: 2026-08-18
Issue: #407
Prerequisites: merged #478-#481
Scope: define a validation-only independent/reference experiment for the coupled signed X/Z feedback path. No production solver change is authorized here.

## 1. Why this package is needed

The raw `alpha = 1` boundary-conditioned feedback experiment now has measured convergence evidence for canonical scenarios A-E. That establishes numerical settling for those cases, but it does not establish that the settled fixed point is physically/model-consistently correct.

The converged feedback X differs from historical `SelectedX` by approximately:

- A: `+3.3191%`;
- B: `+0.5864%`;
- C: `-2.2653%`;
- D: `+8.9883%`;
- E: `+15.6060%`.

Therefore #407 acceptance item 6 requires a comparison against a reference that is not another wrapper around the same production/validation integration code.

## 2. Source basis

Primary source: H. O. Berteaux, *Buoy Engineering* / Г. О. Берто, *Океанографические буи* (1979).

Relevant source relations:

- printed p. 35, Eq. (2.3): for a cable in steady current the normal drag component varies with the square of the normal velocity, giving the `sin^2(phi)` dependence for a current-relative cable angle;
- printed pp. 37-38, Eqs. (2.6)-(2.7): static cable equilibrium preserves the signed distributed load and cable orientation;
- printed p. 46, Eqs. (2.27)-(2.28): cable coordinates follow the local cable direction, with `dx = ds cos(phi)` and vertical increment proportional to `ds sin(phi)`.

The reference case below is deliberately simpler than Berteaux's full cable-function model. It retains the model ingredients needed to independently test the current BuoyCalc feedback approximation: signed tangent geometry, normal-current drag dependence, force accumulation and surface vertical reaction closure.

## 3. Reference case

Use one synthetic uniform line with:

- line length `L = 100 m`;
- target depth `D = 80 m`;
- water density `rho = 1025 kg/m^3`;
- horizontal current `U = 0.5 m/s`;
- vertical current `W = 0`;
- line diameter `d = 0.020 m`;
- drag coefficient `Cd = 1.2`;
- signed submerged line weight `P = 0` (neutral line);
- no internal connector/payload point loads;
- zero buoy steady drag, so `H0 = 0`;
- sufficient buoyancy capacity that the required positive surface vertical reaction is inside the boundary search interval;
- no wave contribution.

The neutral-line assumption is intentional: it removes distributed vertical-load variation so the coupled drag/geometry problem has a closed-form continuous reference while still retaining genuine orientation-dependent drag feedback.

This is a validation fixture only. It is not a new production preset.

## 4. Model-consistent continuous equations

Let the top-to-bottom tangent be

```text
tx = H / T
tz = V / T
T  = sqrt(H^2 + V^2)
```

with `+Z` downward.

For horizontal current only, the current component normal to the line has squared magnitude

```text
Un^2 = U^2 * tz^2
     = U^2 * V^2 / (H^2 + V^2).
```

For a uniform cylindrical line, define the full-normal drag coefficient per unit arclength

```text
k = 0.5 * rho * Cd * d * U^2.
```

The current BuoyCalc shape-force approximation accumulates the resulting scalar drag in the project horizontal `+X` force ledger. For the neutral line with no point loads, the continuous model consistent with that approximation is therefore

```text
dH/ds = k * V^2 / (H^2 + V^2)
dV/ds = 0.
```

Thus

```text
V(s) = Q0 = constant > 0.
```

Geometry follows the signed tension direction:

```text
dx/ds = H / sqrt(H^2 + V^2)
dz/ds = V / sqrt(H^2 + V^2).
```

The analytical reference must implement these relations directly. It must not call `MooringShapeForceAnalyzer`, `MooringSurfaceBoundaryIntegrationKernel`, `MooringSurfaceBoundaryInfoAnalyzer`, or the feedback regression helper to obtain its reference result.

## 5. Closed-form arclength relation

Because `V` is constant,

```text
ds/dH = (H^2 + V^2) / (k V^2).
```

Integrating from `H0` to `H1` over the full line length gives

```text
L = (H1 - H0) / k
  + (H1^3 - H0^3) / (3 k V^2).
```

For `H0 = 0` and positive `V`, the right-hand side is strictly increasing in `H1`, so `H1` has a unique positive root for a given `V`.

The reference implementation may solve this scalar monotone relation by a small independent bisection routine. It must not reuse a production root solver.

## 6. Closed-form X coordinate

Using `H` as the integration variable,

```text
dx/dH = H sqrt(H^2 + V^2) / (k V^2).
```

Therefore

```text
X = [ (H^2 + V^2)^(3/2) ](H0 -> H1)
    / (3 k V^2).
```

Explicitly,

```text
X = ((H1^2 + V^2)^(3/2) - (H0^2 + V^2)^(3/2))
    / (3 k V^2).
```

## 7. Closed-form Z coordinate

Similarly,

```text
dz/dH = sqrt(H^2 + V^2) / (k V).
```

so

```text
Z = 1 / (2 k V) *
    [ H sqrt(H^2 + V^2) + V^2 asinh(H / V) ](H0 -> H1).
```

For the reference fixture `V > 0`, so no sign branch is ambiguous.

## 8. Independent surface-boundary solve

The analytical reference determines `V = Q0` by solving

```text
Z(Q0) = D
```

with its own monotone bisection routine.

For the fixed fixture parameters above, an independent calculation gives an expected reference neighborhood of approximately:

```text
k  = 3.075 N/m
Q0 = 160.106 N
X  = 54.634 m
Z  = 80.000 m
H1 = 201.350 N
```

These rounded values are orientation checks and review aids, not golden acceptance constants. The validation code must print the full-precision analytical result it computes.

## 9. Candidate side of the comparison

The candidate side should run the existing validation-only boundary-conditioned feedback path on an application fixture matching the same physical/model assumptions:

- one `100 m` neutral line;
- `20 mm` diameter;
- `Cd = 1.2`;
- `rho = 1025 kg/m^3`;
- `U = 0.5 m/s`;
- `W = 0`;
- depth `80 m`;
- no internal discrete loads;
- buoy projected area `0`, so steady buoy drag is zero;
- enough buoy volume/capacity for the analytical `Q0`;
- normal production segmentation remains `0.20 m` and is not changed.

The candidate is allowed to use the already-merged validation feedback implementation because that is the object being compared. The analytical reference side must remain independently coded.

## 10. Measurements

The comparison must emit at least:

```text
ReferenceQ0N
CandidateQ0N
DeltaQ0N
RelativeQ0
ReferenceX
CandidateX
DeltaX
RelativeX
ReferenceZ
CandidateZ
DeltaZ
ReferenceEndHN
CandidateEndHN
DeltaEndHN
ReferenceLineForceN
CandidateLineForceN
DeltaLineForceN
CandidateDepthResidualM
CandidateNegativeDz
CandidatePointLoads
```

It should also identify:

- production segment count used by the candidate;
- candidate feedback iteration budget;
- candidate stop classification;
- analytical root-iteration counts for `H1` and `Q0` only as diagnostic provenance.

## 11. No invented pass tolerance in the first measurement PR

The first implementation is measurement-only for reference agreement.

It may assert:

- finite analytical state;
- positive `Q0`;
- analytical `Z` closes to the analytical root tolerance;
- candidate remains solved and finite;
- fixture assumptions actually hold (`WeightWaterKgM = 0`, no internal point loads, zero vertical current, zero buoy drag);
- no production/golden authority changes.

It must **not** invent an arbitrary acceptable `DeltaX`, `DeltaQ0` or percentage merely to obtain a green build.

After the actual comparison is measured, a separate control mark will classify whether the discrepancy is consistent with the `0.20 m` midpoint discretization and the existing `0.01 m` boundary depth tolerance, or whether a model/equation mismatch remains.

## 12. Optional follow-up mesh evidence

If the first comparison leaves a discrepancy too large to attribute cleanly, the next validation-only study may evaluate the independent continuous reference against synthetic discrete meshes such as

```text
1.0 m
0.5 m
0.2 m
0.1 m
```

without changing production segmentation.

This would distinguish continuous-reference error from production-mesh error. It must remain a separate package if needed.

## 13. Production authority remains unchanged

This reference experiment does not authorize changes to:

- `BuoyCalculator`;
- `MooringShapeSolver`;
- production `MooringShapeForceAnalyzer` behavior;
- `MooringSurfaceBoundaryInfoAnalyzer`;
- `MooringSurfaceBoundaryIntegrationKernel`;
- `MooringIterativeSolver`;
- `MooringPrimaryShapeGate` or selected shape;
- selected X/Z;
- 2D/PDF/report geometry;
- drag coefficients or current projection;
- anchor/weak-link calculations;
- verdict;
- signed `WeightWaterKg` / `WeightWaterKgM` semantics;
- the `0.20 m` production segmentation target or unlimited segment count;
- JSON/DTO;
- golden baseline;
- 3D.

The next implementation package is validation-only analytical-reference measurement against this frozen fixture.
