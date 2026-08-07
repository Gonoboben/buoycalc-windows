# AGENTS.md — BuoyCalc Windows

This file contains mandatory instructions for GitHub/Codex/AI agents working in `Gonoboben/buoycalc-windows`.

Read this file and `docs/OPTIMIZATION_PLAN.md` before making changes.

## 1. Project mission

BuoyCalc Windows is an engineering pre-calculation tool for buoy/mooring systems. Reliability of engineering meaning is more important than feature velocity or visual polish.

## 2. Hard constraints

Agents MUST NOT:

- invent engineering physics in UI, PDF, 2D or report code;
- change solver formulas as a side effect of refactoring;
- add 3D;
- change the production line segmentation target from `0.20 m` or introduce a segment-count cap without a dedicated approved physics work package;
- normalize away signed line `WeightWaterKgM`; negative values may be physically valid for buoyant line;
- reconstruct engineering coordinates from Markdown/report text when a calculated/read-model source exists;
- merge while required CI is pending or failed;
- bypass the PR process with direct writes to `main`;
- create no-op commits only to provoke CI;
- perform unrelated cleanup inside a focused engineering PR.

## 3. Primary geometry rule

The only user-facing geometry with engineering meaning is the selected calculated X/Z shape.

The intended direction is the X/Z path that includes distributed line loads, discrete loads and solver/gate criteria.

`MooringShapeSolver` is currently a fallback/initialization/diagnostic path. Do not treat its geometry as an independent product answer.

The 2D view is a renderer only. It must not:

- calculate its own geometry;
- select between solver candidates;
- parse a report to obtain coordinates;
- present fallback versus alternative geometry as two equally valid engineering answers.

PDF and user-facing diagrams must consume the same selected X/Z read model/provider as 2D.

## 4. Architecture direction

Move incrementally toward these responsibilities:

- Domain/calculation: inputs, formulas, solver, physical invariants, calculation results;
- Application: orchestration, immutable calculation snapshot, selected-shape provider, use cases;
- Read models: selected X/Z, reports, sequence and user-facing projection data;
- Infrastructure: JSON, libraries, dialogs/platform services;
- Presentation: Avalonia ViewModels/views, 2D renderer, PDF renderer.

Do not perform a big-bang solution/project split. Stabilize dependency boundaries first.

## 5. State/store policy

Treat static stores as migration debt, not as new extension points.

Do not add new consumers of:

- `MooringShapeStore`;
- `MooringAlternativeShapeStore`;
- `MooringIterativeSolverStore`;
- `MooringPrimaryShapeSelectionStore`;
- report stores used as cross-layer state.

Prefer a completed immutable calculation snapshot and explicit read-model/provider boundaries.

Before removing a store, prove its consumer count is zero and preserve behavior through tests/scans.

## 6. Work-package policy

Default development unit:

```text
1 Issue -> 1 branch -> 1 PR
```

A coherent behavior-preserving work package may contain its documentation, code, diagnostics and tests in the same PR.

Do NOT recreate the historical pattern of separate `marker`, `result`, `live`, `diagnostic` branches for one logical change.

Use a separate preliminary RFC/control document before implementation only for high-risk changes such as:

- physical equations;
- solver convergence or primary-shape selection semantics;
- wave/current/environmental load assumptions;
- anchor/seabed/contact physics;
- safety factors or acceptance criteria;
- material persisted-format migrations.

## 7. Branch and PR conventions

Preferred branch names:

- `opt/<issue>-<topic>` — behavior-preserving optimization;
- `physics/<issue>-<topic>` — physics/solver work;
- `fix/<issue>-<topic>` — defects.

`main` is the only long-lived development branch.

Prefer squash merge for optimization work packages unless preserving multiple commits has a specific engineering reason.

PR description MUST state:

1. problem and scope;
2. files/boundaries changed;
3. whether numerical behavior changes;
4. engineering invariants preserved;
5. tests/diagnostics performed;
6. required CI state;
7. follow-up work explicitly out of scope.

## 8. Required checks

Until the repository explicitly replaces them with an approved unified gate, every PR must pass:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```

Do not merge on `queued`, `pending`, `failure`, `cancelled`, missing required run, or unknown head SHA.

Always verify checks against the exact current PR head SHA before merge.

## 9. Behavior-preserving optimization protocol

For architecture/refactor work:

1. inspect current `main`, open PRs and relevant control docs;
2. identify producers and consumers before moving a boundary;
3. keep numerical formulas and public calculation semantics unchanged;
4. make the smallest coherent change, not the smallest possible commit;
5. add/update a dependency scan or regression check when the boundary can regress;
6. compare the final branch to `main` and remove accidental changes;
7. run/inspect all required CI;
8. merge only after green CI.

If behavior changes unexpectedly, stop treating the task as a refactor and classify it as a physics/behavior change.

## 10. Physics-change protocol

A physics/solver change requires an Issue/RFC that states:

- physical question being solved;
- equations and assumptions;
- units and sign conventions;
- affected deployment modes;
- expected limiting behavior;
- validation scenarios;
- acceptance tolerances;
- effect on historical results;
- fallback/migration behavior if relevant.

A physics PR must include regression/validation evidence. Visual agreement is not validation.

## 11. Physical maturity priorities

Do not label the solver production-grade until evidence exists for:

- segment/node force equilibrium residuals;
- global force equilibrium residual;
- X/Z tangent versus force-angle consistency;
- integrated discrete-load equilibrium;
- seabed/touchdown/contact behavior;
- anchor horizontal/vertical reaction and uplift logic;
- mode-aware surface/submerged wave loading;
- comparison against a reference solver and/or experimental/field evidence.

Gate/fallback decisions currently indicate numerical reliability, not proof of physical superiority.

## 12. Reports and presentation

User PDF:

- one selected engineering X/Z shape only;
- no competing fallback/candidate diagrams;
- no report-text parsing as a physics source;
- user-facing status derived from diagnostics/read models.

Full technical report may contain:

- fallback/candidate solver history;
- convergence information;
- residuals;
- gate reasons;
- engineering diagnostics.

2D:

- pure rendering of selected X/Z nodes/read model;
- no independent engineering model.

## 13. Diagnostics policy

Diagnostics verify published engineering identities; they must not silently modify source/calculated values to make checks pass.

Prefer explicit finite/range/residual checks and report maximum residuals where useful.

Use tolerances intentionally and document their engineering meaning. Do not introduce arbitrary tolerances only to silence a failure.

## 14. Repository hygiene

Agents should help reduce repository entropy:

- do not leave superseded experimental branches intentionally;
- after merge, branches should be eligible for retirement;
- branch cleanup automation must use dry-run first;
- never delete `main` or a branch associated with an open PR;
- never infer that an unmerged branch is obsolete without evidence.

## 15. Stop conditions

Do not continue automatically if a proposed change would require choosing among genuinely different engineering assumptions not already approved by the project.

Examples:

- selecting a new wave theory;
- choosing seabed friction/soil parameters;
- changing safety factors;
- redefining anchor holding-capacity semantics;
- replacing the selected X/Z solver formulation.

In these cases, document the alternatives and request a product/engineering decision before implementing the physics.

CI delays are not permission to bypass CI.

## 16. Current roadmap

Follow `docs/OPTIMIZATION_PLAN.md`.

Immediate sequence after the governance baseline:

1. dependency inventory and immutable calculation snapshot boundary;
2. single selected X/Z provider/read model;
3. remove direct 2D/PDF/report geometry fallbacks and report parsing;
4. separate calculation execution from report generation;
5. deterministic regression harness;
6. only then begin deliberate physical solver improvements with validation.
