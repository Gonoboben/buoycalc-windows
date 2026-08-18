# Control mark: boundary-feedback analytical reference measurement

Date: 2026-08-18
Issue: #407 — Physics RFC: preserve signed planar force orientation in tension-to-shape path
Validation implementation: PR #483
Measured workflow: `.NET Build` run #931

## Purpose

Record the measured independent/reference comparison required by acceptance item 6 of #407 before any production geometry change is considered.

This control mark is evidence only. It does not authorize a production solver switch, a golden-baseline update, a convergence-limit change, or any change to selected X/Z, gates, verdicts, 2D/PDF/report geometry, segmentation, JSON/DTO, or 3D.

## Fixture

The comparison uses the synthetic neutral-line fixture defined by the preceding analytical-reference boundary package (#482):

- line length: 100 m;
- target depth: 80 m;
- uniform horizontal current: 0.5 m/s;
- line diameter: 0.020 m;
- drag coefficient: 1.2;
- neutral submerged line weight;
- no point loads;
- candidate production discretization target: 0.20 m, giving 500 segments.

The analytical side is a continuous closed-form reference and does not call the production shape-force/integration helpers under comparison. The candidate side is the existing validation-only boundary-feedback path.

## Measured analytical reference

```text
kNPerM=3.075
Q0N=160.1062112251539
X=54.633504127415236
Z=79.99999999999999
EndHN=201.35025679395034
LineForceN=201.35025679395034
LengthResidualM=-2.842170943040401E-14
DepthResidualM=-1.4210854715202004E-14
QRootIterations=160
HRootIterations=160
```

The analytical solution closes to machine precision for both length and depth.

## Measured candidate

```text
Budget=64
Iterations=64
Stop=BudgetReached
Segments=500
Q0N=160.1182215270996
X=54.631743732148905
Z=80.00134323139692
EndHN=201.35647834808202
LineForceN=201.35647834808202
DepthResidualM=0.0013432313969161669
LastDeltaX=0
LastDeltaZ=0
LastDeltaQ0N=0
LastMaxNodeDeltaM=0
LastDeltaLineForceN=0
NegativeDz=0
PointLoads=0
```

The candidate reached a numerical fixed point by the expanded validation budget. No local negative-depth step occurred in this fixture.

## Direct comparison

| Quantity | Analytical | Candidate | Candidate - reference | Relative difference |
|---|---:|---:|---:|---:|
| Q0, N | 160.1062112251539 | 160.1182215270996 | +0.0120103019456792 | +0.007501459% |
| X, m | 54.633504127415236 | 54.631743732148905 | -0.001760395266331 | -0.003222190% |
| Z, m | 80.0000000000000 | 80.00134323139692 | +0.001343231396930 | +0.001679039% |
| End H, N | 201.35025679395034 | 201.35647834808202 | +0.006221554131685 | about +0.00309% |
| line force, N | 201.35025679395034 | 201.35647834808202 | +0.006221554131685 | about +0.00309% |

Additional candidate checks:

```text
CandidateDepthResidualM=0.0013432313969161669
CandidateNegativeDz=0
CandidatePointLoads=0
```

## Classification

The independent continuous reference and the 0.20 m discretized feedback candidate agree closely for this fixture.

The observed differences are small relative to the compared state:

- Q0 differs by about 0.0075%;
- horizontal endpoint X differs by about 0.00322% (1.760 mm);
- endpoint depth differs by 1.343 mm;
- terminal horizontal force / integrated line force differs by about 0.00309%.

No post-hoc acceptance tolerance is introduced here. The result is classified as positive independent/reference evidence because the candidate converges to a fixed point and all compared primary quantities remain close to the independently calculated continuous solution, with millimetre-scale endpoint differences for a 100 m line and sub-0.01% force/offset differences.

This comparison does not by itself prove all production scenarios correct. It verifies that the feedback formulation can reproduce an independent limiting/reference case without a material model break.

## Acceptance status for #407

This measurement supplies evidence for acceptance item 6:

> independent/reference comparison

Items 1–6 now have source-backed / deterministic / measured evidence in the validation path. Before any production geometry proposal, item 7 remains mandatory:

> explicit review of every historical golden change

The committed golden baseline must not be edited merely to accommodate new solver behaviour.

## Next allowed step

Perform a validation-only historical golden-impact audit. For each committed historical scenario, compare the current committed selected/golden result with the converged signed-feedback candidate and classify every changed field before proposing any production switch.

The audit must remain non-authoritative: no production solver, selected X/Z, gate/verdict, 2D/PDF/report geometry, segmentation, profile projection, DTO/JSON, or golden baseline change is allowed in that package.
