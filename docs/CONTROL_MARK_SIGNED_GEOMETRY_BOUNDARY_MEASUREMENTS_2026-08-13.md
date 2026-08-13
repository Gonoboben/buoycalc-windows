# Control mark: signed geometry boundary measurements

Date: 2026-08-13  
Issue: #407  
Measurement PR: #410  
Stable base before measurement: `1f39ef6fb07835f7c4ee63b3c74080c928d39560`

## Purpose

This record captures a validation-only experiment performed after Phase-A signed planar orientation diagnostics were merged.

The experiment intentionally did **not** change production geometry. It independently integrated the Phase-A signed tangent diagnostic as:

```text
dx_i = L_i * TangentX_i
dz_i = L_i * TangentZ_i
```

and compared the resulting endpoint with the existing selected X/Z shape.

The temporary measurement logger was used only to collect evidence and is removed from the final PR.

## Source context

Primary project source:

> H. O. Berteaux / Г. О. Берто, *Buoy Engineering / Океанографические буи*, 1979.

Chapter 2 §2.1 establishes:

```text
P = W - B                                      (2.1)
```

with gravitational/buoyancy load allowed to act upward or downward.

For a flexible cable element, Eqs. (2.6)–(2.7) preserve the sign of `P`.

For variable currents, the step approximation on printed p. 57 determines the resultant tension vector at nodes and then approximates geometry along those vectors. Importantly, Eqs. (2.34)–(2.35) include buoy boundary terms together with accumulated cable loads. Surface-buoy equilibrium is then stated in Eqs. (2.36)–(2.37).

This boundary-inclusive construction is important to interpreting the experiment below.

## Phase-A state used by the measurement

`MooringSignedOrientationAnalyzer` is intentionally a read-only diagnostic over existing `SegmentTensionRow` values.

For each row:

```text
H = CumulativeHorizontalForceN
V = CumulativeVerticalForceN
T = hypot(H, V)
TangentX = H / T
TangentZ = V / T
```

This preserves directional quadrant information that historical `Abs(H)` / `Abs(V)` angles erase.

However, `SegmentTensionAnalyzer` starts with zero cumulative H/V at the lower end and accumulates only segment distributed loads. It does not solve the buoy or anchor boundary reaction/tension state.

Therefore Phase-A orientation is a **signed load-ledger direction**, not yet a proven boundary-conditioned cable tangent.

## Measurement environment

The same five deterministic scenario families used by the engineering golden regression were evaluated:

1. vertical zero current;
2. uniform-current slack heavy line;
3. buoyant line;
4. discrete payload between two heavy line items;
5. depth-varying current profile.

The existing committed golden baseline was not changed.

The measurement branch built with 0 warnings / 0 errors and the unchanged five-scenario golden verifier passed.

## Results

### 1. Vertical zero current

```text
DepthM                 = 50
LineLengthM            = 50
SignedEndpointX        = 0
SignedEndpointZ        = 50.00000000000016
SignedDepthResidualM   = 1.6342482922482304E-13
NegativeDzSegmentCount = 0
PositiveDzSegmentCount = 250
```

Representative state:

```text
Top:    H=0, V=49.03325000000019 N, Tangent=(0,+1)
Middle: H=0, V=24.51662500000026 N, Tangent=(0,+1)
Bottom: H=0, V=0.1961329999999958 N, Tangent=(0,+1)
```

Interpretation:

The simplest limiting case closes exactly. Signed vector normalization is internally consistent when the only relevant distributed direction is downward and no horizontal boundary component is required.

### 2. Uniform-current slack heavy line

```text
DepthM                 = 50
LineLengthM            = 55
SignedEndpointX        = 52.399799203072185
SignedEndpointZ        = 16.7111053936522
SignedDepthResidualM   = -33.288894606347796
NegativeDzSegmentCount = 0
PositiveDzSegmentCount = 275
HistoricalSelectedX    = 22.904164818523228
HistoricalSelectedZ    = 50
```

Representative rows all have approximately:

```text
TangentX = +0.952723621874
TangentZ = +0.303838279885
angle from +Z-down ~= 72.311714305 deg
```

Interpretation:

The signed distributed-load direction does not satisfy the anchored endpoint condition. The historical shape closes depth only because it applies a separate geometric angle-scale closure.

This disproves the idea that normalized existing cumulative distributed H/V can directly replace the production shape angle.

### 3. Buoyant line

```text
DepthM                 = 30
LineLengthM            = 30
SignedEndpointX        = 27.429659239050817
SignedEndpointZ        = -12.149641724328697
SignedDepthResidualM   = -42.149641724328696
NegativeDzSegmentCount = 150
PositiveDzSegmentCount = 0
UpwardArcLengthM       ~= 30
HistoricalSelectedX    = 27.429659239050817
HistoricalSelectedZ    = 30
```

All 150 rows have negative signed vertical cumulative force.

Representative top row:

```text
H = +33.209999999999894 N
V = -14.70997499999997 N
TangentX = +0.9143219746350296
TangentZ = -0.40498805747762523
SignedAngleFromVerticalDeg = 113.89037876318349
HistoricalUnsignedAngle   = 66.10962123681651
```

A key numerical identity explains the unchanged horizontal endpoint:

```text
sin(113.890378763 deg) = sin(66.109621237 deg)
```

while the sign of cosine is reversed.

Thus the historical `Abs`-based angle reflects the raw signed vertical direction into the positive-Z half-plane while preserving horizontal projection.

Interpretation:

This directly proves that historical geometry loses the vertical quadrant carried by the signed distributed ledger.

It does **not** prove that the raw upward signed-ledger tangent is the correct physical anchored-cable geometry. The line has prescribed endpoints and boundary reactions that are absent from `SegmentTensionAnalyzer`.

### 4. Discrete payload

The Phase-A base signed orientation produced the same distributed-line endpoint as the uniform-current heavy-line case:

```text
SignedEndpointX = 52.39979920307219
SignedEndpointZ = 16.7111053936522
```

Interpretation:

This is expected and important: Phase-A `SignedOrientation` reads `SegmentTensionRows`, which do not include connector/payload point loads.

Therefore it cannot be used as a complete discrete-load tension state.

Any future boundary-conditioned orientation must define the ownership of:

```text
distributed line loads
+ point loads
+ buoy boundary loads
+ anchor/bottom reaction
```

without double counting.

### 5. Depth-varying current profile

```text
DepthM                 = 50
LineLengthM            = 50
SignedEndpointX        = 24.284559370352596
SignedEndpointZ        = 41.65811815114197
SignedDepthResidualM   = -8.34188184885803
NegativeDzSegmentCount = 0
PositiveDzSegmentCount = 250
HistoricalSelectedX    = 24.284559370352596
HistoricalSelectedZ    = 50
```

Representative signed angle changes substantially along the cable:

```text
Top    ~= 57.8120 deg
Middle ~= 28.5243 deg
Bottom ~= 7.2626 deg
```

Interpretation:

The signed distributed ledger correctly reflects depth-varying drag direction/magnitude trends, but direct integration still does not close the prescribed endpoint.

## Main finding

Two statements are now separately established:

### A. Historical angle representation loses quadrant

Confirmed.

The buoyant case proves that the current `Abs(H)` / `Abs(V)` path reflects negative vertical cumulative force into the positive-Z geometry half-plane.

### B. Raw signed distributed H/V is not sufficient to define anchored geometry

Also confirmed.

Direct normalization fails endpoint closure in current-bearing heavy cases and produces an impossible endpoint for the constrained buoyant case.

Therefore the next production design must **not** be:

```text
remove Abs -> directly use existing SegmentTensionAnalyzer H/V as cable tangent
```

## Boundary-condition gap

The existing base cumulative state is not a solved full free-body state.

`SegmentTensionAnalyzer` accumulates segment distributed loads from a zero lower-end baseline. It does not include a solved boundary reaction/tension vector.

This is consistent with the existing Candidate-B policy that buoy and anchor boundary nodes are `INDETERMINATE` because their reactions are not solved.

For an anchored flexible cable, direction must be established from a boundary-conditioned tension field, not from distributed loads alone.

## Berteaux mapping now required before further feedback experiments

The next physics work must map Berteaux Eqs. (2.34)–(2.37) into the current project conventions.

The source construction includes the buoy-side boundary terms before determining node tension direction:

```text
buoyancy / vertical buoy balance
buoy drag / horizontal buoy balance
+ accumulated cable distributed loads
```

The exact BuoyCalc mapping must define:

1. whether the tension field is integrated from buoy to anchor or anchor to buoy;
2. signs in `+X`, `+Z-down` coordinates;
3. which buoy drag term corresponds to the existing calculated buoy/current force family;
4. whether wave force is excluded from this static-state validation or treated separately;
5. how line drag already contained in segment rows enters exactly once;
6. how connector/payload point loads enter at their `s` positions exactly once;
7. what boundary condition/reaction is used at the anchor;
8. how an inextensible line with `L == depth` and nonzero horizontal load is classified physically.

## Revised order for Issue #407

The previously planned expanded iteration-budget study is postponed.

The safe order is now:

```text
1. boundary-inclusive tension-state source mapping
2. deterministic analytical free-body validation
3. validation-only boundary-conditioned signed geometry
4. endpoint/equilibrium checks
5. discrete point-load extension
6. only then genuine iterative feedback coupling
7. only then validation-only iteration-budget study
8. only after reference evidence, consider production solver change
```

## Non-goals / unchanged production behavior

This measurement does not authorize changes to:

```text
MooringShapeSolver
MooringDiscreteLoadShapeBuilder
MooringIterativeSolver
MooringPrimaryShapeGate
CalculationResult.Verdict
selected X/Z
anchor or weak-link calculations
0.20 m target segmentation
unlimited segment count
signed WeightWaterKg
PDF/2D physics
JSON/DTO
golden baseline
3D
```

## Decision

Phase-A signed orientation remains useful as an INFO-only diagnostic and as proof of historical quadrant loss.

It is **not** promoted to production geometry authority.

The next allowed work is documentation/source mapping of a boundary-inclusive planar tension state, using Berteaux (2.34)–(2.37) as the primary source and preserving the project `+X`, `+Z-down` convention.
