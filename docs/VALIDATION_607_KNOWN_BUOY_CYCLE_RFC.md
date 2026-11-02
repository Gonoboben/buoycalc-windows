# Validation #607 known-buoy recovery control / RFC

## Question and allowed scope

After H2R3 itself enters a directly raw-bit-proven strict period-2 orbit, can
passing the retained initial buoy steady drag into the SAME existing boundary
solver remove cancellation from `(buoy + line + points) - line - points`?
This is H1, previously exercised from iteration one, now proposed only after
three proven cycles. Units N; positive horizontal drag; signed line weight and
vertical convention unchanged. It does not choose a member of an orbit.

## Contracts

The input is the initial solved boundary's retained BuoySteadyDragN already
used by TryApplyShapeForces to assemble every feedback total. No new buoy force
is calculated. All segment/point forces, physical root equations, trace and
geometry checks, accepted-shape construction, exact endpoint equality, selected
shape and F1-F4 authority remain the real production implementations. Same
0.20 m segmentation, sub-0.0001 m exclusion and total 64-step budget. Reset raw
history on every map transition. No epsilon, no damping, no frozen-baseline edit.

## Validation and decision

Initially validation-only, public H2R3 unchanged. Replay every experiment twice,
observe transitions, inspect final raw state and real geometry/force residuals.
Cover all 174 labelled fixtures / 165 distinct current input hashes, especially
all five distinct residual BudgetExhausted inputs and A03/A05/A06/A11/A15.
Every one of the 147 current Accepted candidate/F1-F4 trees must be identical
against retained independent H2R3 evidence. A new candidate has no selected
legacy F1-F4 reference: require real authority construction instead. Confirm
exact-head Windows CI and actual artifacts before deciding production scope.
If it requires changing physical force ownership/acceptance, stop for owner
engineering decision. Remaining nonconvergence must remain non-Accepted.

## Local pilot (FACT, computational only)

Twelve targeted fixtures × eight modes × two replays. The known-buoy stage
recovers coeff-0.5-14-1030 at iteration 26 after transitions 15:TraceTangents,
20:SquaredNormal, 23:KnownBuoyDrag; other residual failures remain failures.
A separate negative pilot also restored the square-root expression at the known
buoy transition: it adds no successful case, so that extra hook is omitted.
The public production path still uses H2R3. Full matrix and Windows proof pending.
