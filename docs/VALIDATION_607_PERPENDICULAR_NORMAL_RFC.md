# Validation #607 perpendicular-normal arithmetic RFC

## Facts and question

Reviewed H2RF3 evidence leaves three distinct strict period-2 inputs. Each differs
in only four full raw fields: one segment CurrentForceN, MidHN, TangentX and
SignedAngleFromDownwardVerticalDeg. Identified segments are91 (20/24/0.6),94
(20/23.8/0.65) and1025 (380/(380*1.19)/0.65). The complete geometry, endpoint
forces, retained buoy source and TangentZ are unchanged. This is NOT permission
to accept either orbit member: the existing fixed-point contract remains exact.

## Same physical equation, alternate numerical expression

For a real unit tangent t=(tx,tz) and the existing compatibility current vector
u=(|U_h|,W), the identity is:

`|u|^2 - (u dot t)^2 = (ux*tz - uz*tx)^2`.

The trace already defines tx=H/T, tz=V/T, T=sqrt(H^2+V^2), hence t is a unit
vector in the unchanged real model. Both expressions give the same normal drag
`F=0.5*rho*Cd*A*normalSpeedSquared` (N). No geographic direction, wave assumption,
signed-weight equation, force ownership or constitutive exponent changes.
IEEE-754 results of the two expressions are not claimed identical. The proposed
expression avoids cancellation and, for zero vertical current, avoids dependence
on the oscillating tx when tz is already stable. This is a testable numerical
hypothesis, not physical validation.

## Controlled experiment

Initially validation-only, public H2RF3 unchanged. Only after H2RF3 itself enters
a directly proven raw period2/3 orbit, change subsequent normal-force arithmetic.
Clear raw history at transitions; never accept an orbit member. Same total64
steps,0.20m segmentation and sub-0.0001m exclusion; no epsilon or baseline rewrite.
Use real production Accepted shape, endpoint equality, force/geometry checks,
arbitration and F1-F4 projectors. Every149 previously Accepted full candidate and
authority tree must remain exact. Newly Accepted cases have no legacy selected
F1-F4 reference; validate actual production construction instead.

Pilot12 targeted fixtures, then full198 labelled fixtures with12 twice-replayed
modes (the original174 contain165 distinct hashes; new overlaps are measured),
30 raw-cycle survey trajectories and exact-head Windows CI/artifact inspection. Measure unit-tangent and force residuals independently; do not create
a weaker replacement gate. If the identity requires an altered physical contract,
stop for owner decision. Discard the intervention if its real gates fail.

## Actual pilot and source-force ratio comparison

FACT:12 fixtures×11 twice-replayed modes. Tangent-component perpendicular
arithmetic P accepts only the deep380/(380*1.19)/0.65 residual at31.
Source-force-ratio Q uses the equivalent real expression
`normalSpeedSquared=(ux*Vmid-uz*Hmid)^2/(Hmid^2+Vmid^2)` and accepts
20/24/0.6 at31 and the deep residual at32. It avoids the square-root/division
round trip of normalizing each tangent component. Units remain m^2/s^2 before
multiplication by the unchanged drag coefficient/area/density factor.
20/(20*1.19)/0.65 remains BudgetExhausted. Existing Accepted controls remain
exact in both pilots. Public H2RF3 remains unchanged. Full174×11 matrix and
25 raw-cycle survey trajectories are pending. No physical PASS is inferred.

## Canonical restart hypothesis and negative precision control

A12-fixture fused-denominator pilot avoided one intermediate rounding of the
dominant squared-norm term using Math.FusedMultiplyAdd (documented semantics:
https://learn.microsoft.com/dotnet/api/system.math.fusedmultiplyadd). It added
no success and lost the Q gains; that hook is omitted. Higher local accuracy
is not proof of an exact floating-point fixed point.

FACT:the remaining20/(20*1.19)/0.65 input already reaches real Accepted in the
existing H1 experiment from the original signed state (15 steps). New
validation-only QK first runs Q; only after a further directly proven cycle,
restore the ORIGINAL initial signed result/boundary/trace/geometry, use the
existing coordinate-projection/known-buoy H1 map with original square-root force
arithmetic, and continue the SAME global counter. Never reset/increase64.
This is canonical initial-state reseeding, not selection of an orbit member,
fallback shape or precomputed engineering result. Require all real gates again.
An Accepted restart must have the exact same complete terminal physical raw
vector as the independently executed H1 reference. Prior Q Accepted results
must remain exact. This changes numerical initialization/arithmetic only; no
physical equation, force source, root predicate, acceptance or selection change.

FACT:the corrected H1 canonical-restart pilot reached Accepted at global48
(restart at33 plus15 H1 steps), full physical raw vector and all four F1-F4
objects exactly equal to the actual H1 reference. An earlier H1/H2-labelled
pilot failed; the reference label was corrected, not its acceptance criterion.
Actual final feedback projection/forces are captured read-only at a real fixed
point; every Accepted mode verifies their raw force/trace identities using the
real force producer. This replaces no physical/shape acceptance gate.

The next proof expands174 to198 labelled fixtures with24 one-ULP perturbations
of residual lengths/currents. Existing overlaps remain explicit. The frozen
149 prior Accepted records come from exact main66e4327 actual artifact13262020512
SHA256 fa7fa500aca0dadcf19d52c0ca3bfef934e00deec1c88f5bf736acd5366ddbd1;
canonical engineering-baseline.json is untouched. New cases have no fabricated
legacy selected F1-F4. Public remains H2RF3 pending complete proof/activation.

The canonical reset records a separate complete raw-vector witness equal to
iteration0; no precomputed H1 result enters production. Accepted reset state
and actual F1-F4 are compared with independently executed H1 in diagnostics.
Raw exact-point transcripts may also document a later RejectedNumerical shape
gate; they never imply Accepted by themselves. The completeness checker counts
actual observed fixed points separately from Accepted authority.

## Alternate perpendicular arithmetic before canonical restart

QK leaves `ulp-20-24-0.6--1--1` at BudgetExhausted64 even though the isolated
P trajectory accepts at31. The QPK experiment runs Q first. On a further
exact two- or three-cycle under Q, it switches to P from the just-computed
state and clears the cycle history. A further exact cycle under P can trigger
the existing canonical H1 restart. Every stage shares the total64 counter
and must pass the same exact fixed-point and engineering acceptance gates.

FACT, local .NET 8.0.427 pilot (13 fixtures × 13 modes): the one-ULP input
accepts at34 after the Q→P transition at30. The original canonical case
20/(20*1.19)/0.65 accepts at51 after P at33 and state0 restart at36.
Its complete terminal raw state and actual F1–F4 equal the separate 15-step
H1 result. The existing Q Accepted prefix remains exact. These are observed
numerical results from the real evaluator and authority projectors.

The added negative fixture has depth20 m, line21 m, constant horizontal
current0.3 m/s, and signed vertical current+0.3 m/s. The same reference
buoy, 14 mm line (signed weight+0.15 kg/m), density1025 kg/m³ and
zero-wave environment apply. A numerical stress sweep tested vertical
speeds −0.6, −0.3 and +0.3 m/s at these inputs. The first two accepted.
The positive fixture deterministically exhausts64, with no selected F1–F4.
Its local terminal witness has exact period5 in all seven surveyed recovery
modes. The period2/3 detector leaves this trajectory on the original map.
This fixture exercises the numerical budget using supported inputs.

The Windows proof covers199 labelled inputs × 13 twice-replayed modes, with
the retained149 frozen Windows candidates and 49 raw survey trajectories.
Each QPK switch has a separate complete consecutive-state witness, checked
against the surveyed digests, the active map interval, the stage sequence,
and the actual authority summary. The accepted projection and feedback
chords are compared at the existing geometry identity scale. Public
H2RF3 is retained pending review of the completed Windows proof.
