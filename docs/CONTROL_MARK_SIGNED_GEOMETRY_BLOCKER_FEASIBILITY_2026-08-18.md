# Control mark: signed-geometry blocker feasibility — 2026-08-18

Issue: #487 — Physics RFC: resolve signed-geometry production blockers before authority switch.

PR package: validation-only fixture feasibility classification.

This control mark records only facts proven from the committed historical fixtures and the successful exact-head engineering regression. It does **not** authorize a production solver or selected-X/Z change.

## Scope

The historical golden-impact audit found three fixtures for which the boundary-conditioned feedback candidate was unavailable:

- `vertical-zero-current`;
- `buoyant-line`;
- `depth-varying-current-profile`.

Package A reconstructs those exact committed fixtures and separates **geometric feasibility under the current inextensible/no-stretch model** from the existing numerical boundary-result classification.

No elasticity/stretch is introduced. No production tolerance is introduced.

## Governing geometric condition

For an inextensible line of total length `L` spanning target vertical depth `D` and endpoint horizontal separation `X`, any admissible geometry must satisfy at minimum:

```text
L >= sqrt(D^2 + X^2)
```

Therefore:

```text
L < D                  -> geometrically impossible
L = D and X = 0        -> straight vertical limiting geometry
L = D and X != 0       -> geometrically impossible
L > D                  -> slack geometry may be eligible for boundary search
```

The validation regression separately uses the exact historical steady horizontal force to distinguish the `L = D` limiting cases:

```text
L = D, horizontal load = 0     -> SolvableUnique geometry
L = D, horizontal load != 0    -> PhysicallyInfeasibleUnderCurrentInextensibleModel
```

This is a geometry classification. It does **not** yet claim that the associated vertical reaction/Q0 or tension state is unique.

## Exact blocked fixtures

### `vertical-zero-current`

Committed fixture facts:

```text
target depth = 50 m
line length  = 50 m
current      = 0 m/s
historical steady current force = 0 N
point loads  = 0
line signed water weight = +0.1 kg/m
current profile = false
```

Existing boundary classification:

```text
VerticalGeometryBoundaryNonUnique
```

Analytical geometry classification proven by Package A:

```text
SolvableUnique
```

Reason: `L = D` and there is no horizontal forcing. Any non-zero endpoint X would require `sqrt(D^2 + X^2) > L`; therefore the only admissible inextensible endpoint/path geometry is the straight vertical limiting geometry `X = 0`, `Z = D`.

Important limitation: this does **not** establish a unique `Q0`, vertical tension distribution or surface reaction. Those force-state semantics belong to the next #487 package. The current production boundary analyzer remains unchanged in this PR.

### `buoyant-line`

Committed fixture facts:

```text
target depth = 30 m
line length  = 30 m
uniform current = 0.3 m/s
historical steady current force = 62.72999999999993 N
point loads  = 0
line signed water weight = -0.05 kg/m
current profile = false
```

Existing boundary classification:

```text
TautNonZeroHorizontalLoadNoFiniteRoot
```

Analytical geometry classification proven by Package A:

```text
PhysicallyInfeasibleUnderCurrentInextensibleModel
```

Reason: `L = D` consumes the entire inextensible line length in the vertical span. A non-zero horizontal endpoint separation would require length greater than `L`, while a non-zero steady horizontal load cannot be represented by a straight vertical cable state with finite horizontal deflection under the current no-stretch model.

This is not treated as a bisection failure that should be forced to return a root.

### `depth-varying-current-profile`

Committed fixture facts:

```text
target depth = 50 m
line length  = 50 m
current profile:
  z=0 m  -> 0.6 m/s
  z=25 m -> 0.3 m/s
  z=50 m -> 0.1 m/s
historical steady current force = 195.9797868 N
point loads  = 0
line signed water weight = +0.1 kg/m
current profile = true
```

Existing boundary classification:

```text
TautNonZeroHorizontalLoadNoFiniteRoot
```

Analytical geometry classification proven by Package A:

```text
PhysicallyInfeasibleUnderCurrentInextensibleModel
```

Reason: again `L = D`, but the depth-varying current produces non-zero steady horizontal loading. Under the present inextensible/no-stretch model there is no available line length for a non-zero horizontal separation.

## Controlled limiting cases

The same regression also validates independently:

```text
L < D, any zero-load example      -> PhysicallyInfeasibleUnderCurrentInextensibleModel
L = D, H = 0                      -> SolvableUnique geometry
L = D, H != 0                     -> PhysicallyInfeasibleUnderCurrentInextensibleModel
L > D                             -> SlackBoundarySearchEligible
```

The `1e-12` comparison used in the regression only identifies exact controlled fixture equalities in validation. It is not a new production physical tolerance.

## CI evidence

Exact validation head before this documentation commit:

```text
a4041741daadec936c2d415c40210cf1a1ebc84e
```

Required checks all passed:

```text
.NET Build #939                    SUCCESS
Selected Shape Consumer Scan      SUCCESS
Report Store Consumer Scan        SUCCESS
```

The successful regression proves that the exact committed fixture identities, existing boundary classifications and the expected analytical classifications above all matched the implementation assertions.

Ancillary runtime values that were printed by the regression but were not available from the Actions log through the connector are intentionally not reproduced here. No value is inferred or invented.

## Package A conclusion

1. The two `TautNonZeroHorizontalLoadNoFiniteRoot` historical fixtures are analytically classified as physically infeasible under the **current inextensible/no-stretch model** rather than as mere numerical root-search failures.
2. `vertical-zero-current` is a different limiting case: its **geometry is uniquely straight vertical** because `L = D` and horizontal forcing is zero.
3. The existing `VerticalGeometryBoundaryNonUnique` result is therefore too coarse to describe the exact geometry meaning of that fixture, but production behavior is deliberately unchanged in Package A.
4. Geometry uniqueness must not be confused with uniqueness of `Q0` or the vertical tension/reaction state.
5. No elasticity/stretch, solver switch, selected-X/Z switch, golden-baseline update or presentation-layer fallback is authorized.

## Next gate under #487

Before any production special case is implemented, establish the vertical limiting **force-state semantics** for `L = D`, `H = 0`:

- signed vertical equilibrium along the straight line;
- admissible `Q0` interval/boundary condition under signed submerged line weight;
- whether a unique surface reaction exists or only a family of force states;
- treatment of zero-tension/indeterminate points or vertical-force sign reversal;
- buoy-capacity compatibility;
- explicit distinction between unique geometry and potentially non-unique force state.

Only after that validation/docs gate may a production boundary-analyzer special case be considered.