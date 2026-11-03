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

Pilot12 targeted fixtures, then full174 labelled/165 distinct-input matrix with
10 twice-replayed modes, full raw-cycle survey and exact-head Windows CI/artifact
inspection. Measure unit-tangent and force residuals independently; do not create
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
