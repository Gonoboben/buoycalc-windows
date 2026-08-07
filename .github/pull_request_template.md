## Issue

Closes #<issue>

## Work package

Describe the single coherent problem solved by this PR. Default contract: **1 Issue -> 1 branch -> 1 PR**.

## Scope

- 

## Numerical / physics impact

Choose one and explain:

- [ ] Behavior-preserving: no intended engineering/numerical change.
- [ ] Intentional numerical/physics change: dedicated physics/RFC Issue is linked and validation evidence is included.

Impact statement:

## Engineering invariants preserved

Confirm the relevant invariants from `AGENTS.md`:

- [ ] UI, PDF, 2D and reports do not invent physics or coordinates.
- [ ] Selected calculated X/Z remains the only user-facing engineering geometry.
- [ ] Solver/formulas are unchanged unless this is an approved physics/RFC work package.
- [ ] Production segmentation remains 0.20 m target with no segment-count cap unless explicitly approved otherwise.
- [ ] Signed line `WeightWaterKgM` semantics are preserved.
- [ ] No 3D added.

## Validation / diagnostics

Describe tests, architecture guards, residual checks or regression evidence added/run:

- 

## Required CI

Merge only when the **exact current PR head** has:

- [ ] `.NET Build` — success
- [ ] `Selected Shape Consumer Scan` — success
- [ ] `Report Store Consumer Scan` — success

## Out of scope

Explicitly list follow-up work that is not part of this PR:

- 

## Agent checklist

- [ ] Read root `AGENTS.md` and `docs/OPTIMIZATION_PLAN.md` before implementation.
- [ ] Final diff contains only this work package.
- [ ] No no-op commits were added only to trigger CI.
- [ ] Branch can be retired after merge.
