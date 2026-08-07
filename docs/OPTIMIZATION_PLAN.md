# BuoyCalc Windows — Optimization Plan

Date: 2026-08-07  
Repository: `Gonoboben/buoycalc-windows`  
Status: governing roadmap for architecture, physics/model maturity and GitHub process optimization.

## 1. Purpose

This document replaces the previous micro-PR development rhythm with a smaller number of coherent engineering work packages.

The optimization goal is not to add features. It is to make the project easier to reason about, safer for agents to modify, and closer to a defensible engineering calculation tool without changing validated numerical behavior by accident.

## 2. Non-negotiable project invariants

1. Engineering physics lives only in the calculation/solver domain.
2. UI, PDF, 2D and report layers render calculated data; they do not invent coordinates, forces or status.
3. No 3D work is part of this optimization program.
4. The fixed production line segmentation remains `0.20 m` target step with no segment-count cap unless a dedicated physics change explicitly revises it.
5. Solver equations, force formulas and current validated numerical outputs must not change as a side effect of refactoring.
6. A physics or solver change requires its own engineering rationale, validation plan and regression evidence.
7. Merge is forbidden while any required check is pending or failed.

## 3. Engineering model decision: X/Z is the primary technical geometry

The only geometry that should carry engineering meaning for the user is the selected calculated X/Z shape.

The selected X/Z shape must ultimately represent the calculation path that includes distributed line loads, discrete loads and solver/gate criteria. Historical/fallback forms may remain temporarily for diagnostics and numerical fallback, but they are not independent user-facing engineering models.

### Consequences

- `MooringShapeSolver` is treated as a fallback/initialization/diagnostic boundary until a stronger equilibrium solver replaces it; it is not a separate product geometry.
- The discrete/iterative X/Z candidate path is the physically preferred direction because it includes discrete loads and is closer to the actual mooring chain.
- `MooringPrimaryShapeGate` is a numerical reliability gate, not a statement that the fallback shape is physically superior.
- the 2D window is only a renderer of the selected X/Z result;
- 2D must not select a shape, calculate coordinates, reconstruct geometry from report text, or compare competing geometries as if both were equivalent engineering answers;
- PDF and all user-facing diagrams must consume the same selected X/Z read model.

## 4. Current structural problems to remove

### 4.1 Multiple shape/store sources

Current code contains several historical state holders and shape paths, including:

- `MooringShapeStore`;
- `MooringAlternativeShapeStore`;
- `MooringIterativeSolverStore`;
- `MooringPrimaryShapeSelectionStore`;
- selected-shape/read-model adapters and report-specific paths.

The target is one immutable calculation snapshot with one selected X/Z shape exposed through explicit read models.

### 4.2 Solver and store colocated

Stores currently live inside solver/service files in several places. Solver code must become stateless calculation logic; mutable session state must move to an application/session layer.

### 4.3 Report orchestration is too influential

Report generation historically initiated or assembled calculation/diagnostic pipeline state. Reports must become consumers of a completed calculation snapshot.

### 4.4 ViewModels and code-behind carry too many responsibilities

Persistence, dialogs, calculation orchestration, text formatting and presentation state should be separated gradually. Do not perform a large rewrite.

### 4.5 Historical 2D responsibilities

The 2D canvas historically mixed selected/fallback/alternative shapes and report parsing. The target is a pure rendering adapter over selected X/Z nodes.

## 5. Target architecture

Use these logical boundaries. Physical folder/project splitting may happen incrementally; do not perform a big-bang rewrite.

```text
BuoyCalc.Domain
  Inputs / engineering entities
  Calculation results
  Physical formulas and invariants
  Solver contracts

BuoyCalc.Calculation
  BuoyCalculator
  Segment/load calculations
  Tension analysis
  X/Z solver pipeline
  Engineering diagnostics

BuoyCalc.Application
  Calculation orchestration
  CalculationSnapshot
  SelectedShapeProvider
  Project/session state
  Use cases

BuoyCalc.ReadModels
  Selected X/Z shape read model
  User report read model
  Technical report read model
  Sequence/element read models

BuoyCalc.Infrastructure
  JSON storage
  element libraries
  file dialogs
  platform services

BuoyCalc.Presentation
  Avalonia ViewModels
  Views / 2D renderer
  PDF renderer
```

The first optimization stages may keep a single `.csproj`; namespace/folder boundaries are sufficient until dependencies are stable.

## 6. GitHub process optimization

At the 2026-08-07 baseline the repository contains 255 branches. The previous marker/result/diagnostic rhythm produced excessive branch and PR churn.

### New default work-package policy

One coherent engineering work package should normally be:

```text
1 Issue -> 1 branch -> 1 PR -> merge -> branch retirement
```

A work package may include documentation, implementation, diagnostics and tests together when they represent one behavior-preserving boundary change.

Separate preliminary RFC/control documentation is required only when the work package changes:

- physical equations;
- solver convergence/selection semantics;
- environmental load assumptions;
- anchor/seabed physics;
- wave/submerged physics;
- safety factors or acceptance criteria;
- public persisted formats where migration risk is material.

### Branch policy

- `main` is the only long-lived development branch.
- Prefer one active implementation branch at a time; at most a small bounded number of intentional parallel branches.
- Use names such as `opt/<issue>-<topic>`, `physics/<issue>-<topic>`, `fix/<issue>-<topic>`.
- Delete merged/superseded branches automatically where permissions allow.
- Do not create marker/live/result/diagnostic branches for one logical task.
- Do not create no-op commits merely to trigger CI; fix workflow reliability instead.

### PR policy

- Default to squash merge for optimization work so one work package becomes one `main` commit.
- PR body must state: scope, invariants preserved, numerical behavior impact, validation performed, and follow-up work.
- Avoid unrelated cleanup.
- A PR is not complete while required checks are queued, pending or failed.

## 7. GitHub automation roadmap

### A. Unified quality gate

Consolidate the current independent checks behind one reusable quality-gate workflow while preserving the existing named checks during migration.

Quality gate should run:

1. restore/build;
2. selected-shape consumer boundary scan;
3. report-store/read-model boundary scan;
4. architecture dependency scans;
5. deterministic engineering regression scenarios;
6. later, physics validation scenarios.

### B. Agent task contract

Root `AGENTS.md` is mandatory. Agents must read it before changing code.

Add later:

- `.github/pull_request_template.md`;
- Issue form for optimization tasks;
- Issue form for physics/RFC changes;
- labels: `architecture`, `physics`, `behavior-preserving`, `validation`, `agent-ready`, `blocked-ci`;
- workflow that comments when a PR violates branch/PR conventions;
- workflow/manual job for stale merged branch cleanup.

### C. Branch hygiene

Create a safe cleanup tool that:

- never deletes `main`;
- never deletes a branch belonging to an open PR;
- deletes only merged or explicitly superseded branches;
- supports dry-run first;
- records a cleanup artifact/list before deletion.

## 8. Physics and model maturity roadmap

Architecture cleanup must not be mistaken for physical validation.

### Existing model class

BuoyCalc remains a pre-engineering tool. The current X/Z pipeline uses calculated segment forces/tensions and geometric closure, but it is not yet demonstrated as a full nonlinear static equilibrium solver.

### Physics priorities

#### P1 — force/shape consistency

For every segment/node, expose and validate the relationship between:

- horizontal force component;
- vertical force component;
- tension magnitude;
- tangent angle of the X/Z shape.

Add explicit residuals rather than only geometric closure metrics.

#### P2 — global equilibrium residual

Introduce a published global force-balance residual for the completed mooring system. Gate engineering trust on residuals, not only visual closure.

#### P3 — discrete-load equilibrium

Ensure connectors, instruments/payloads, buoy and anchor reactions enter the same node equilibrium model rather than being applied only as downstream corrections.

#### P4 — seabed/touchdown and anchor reaction

Model contact/touchdown separately from free-water line shape. Add anchor horizontal/vertical reaction and uplift/contact logic before treating anchor reserve as final design evidence.

#### P5 — mode-aware wave physics

Separate surface and submerged buoy wave loading. Submerged cases require depth-dependent wave kinematics rather than unconditional surface-wave load.

#### P6 — validation

Build a versioned validation suite:

- analytically simple limiting cases;
- vertical/no-current case;
- uniform-current cases;
- buoyant/heavy line cases;
- discrete payload cases;
- short/submerged cases;
- comparison with a reference solver where available;
- later, laboratory/field evidence.

No solver promotion to production-grade status without validation evidence.

## 9. Optimization phases

### Phase 0 — governance baseline

Deliverables:

- `docs/OPTIMIZATION_PLAN.md`;
- root `AGENTS.md`;
- new one-Issue/one-PR work-package policy.

Done when merged with all required CI green.

### Phase 1 — dependency inventory and calculation snapshot boundary

Goal: understand and freeze data flow before moving code.

Work packages:

1. map all producers/consumers of calculation result, shape stores, report stores and selected-shape read models;
2. introduce/standardize an immutable `CalculationSnapshot` application boundary containing the calculation result, selected X/Z shape, diagnostics and metadata;
3. make reports/UI consumers depend on snapshot/read models instead of mutable solver stores;
4. add architecture scans that reject new direct store consumers.

No numerical changes.

### Phase 2 — single selected X/Z source

Goal: remove competing user-facing geometry sources.

Work packages:

1. define `ISelectedMooringShapeProvider` or equivalent application boundary;
2. route 2D, PDF and technical/user report diagram data through it;
3. demote fallback/alternative stores to internal diagnostics;
4. remove report-text parsing as a geometry source;
5. retire obsolete stores once consumer count reaches zero.

No physics change; output differences require explicit review.

### Phase 3 — calculation pipeline decomposition

Goal: separate calculation from orchestration and rendering.

Work packages:

1. move stores out of solver files;
2. separate report building from calculation execution;
3. split oversized engineering model/service files by responsibility without changing public result semantics;
4. reduce `MainWindowViewModel` orchestration responsibility through application use cases.

No formula changes.

### Phase 4 — deterministic regression harness

Goal: make refactoring safe.

Work packages:

1. create canonical engineering scenarios;
2. snapshot important scalar results, segment rows and selected X/Z nodes;
3. define tolerances explicitly;
4. run in CI.

This phase is required before intentional solver changes.

### Phase 5 — physical solver program

Goal: evolve the selected X/Z path toward a defensible equilibrium solver.

Sequence:

1. segment/node force residuals;
2. global equilibrium residual;
3. force-shape tangent consistency;
4. integrated discrete-load equilibrium;
5. seabed/touchdown and anchor reaction;
6. surface/submerged wave model separation;
7. reference-solver validation;
8. gate criteria based on engineering residuals.

Each physical change requires its own RFC/validation work package.

### Phase 6 — presentation simplification

Goal: presentation is a pure projection of engineering results.

- 2D becomes a selected-X/Z renderer only;
- remove alternative/fallback comparison from normal user UI;
- PDF uses one selected engineering geometry;
- full technical report may retain solver history and fallback diagnostics;
- user-facing statuses are derived from engineering diagnostics/read models.

### Phase 7 — repository hygiene and release discipline

- clean historical merged/superseded branches with a dry-run cleanup workflow;
- keep `main` as the only permanent development branch;
- create checkpoint tags/releases for verified milestones;
- keep architecture and physics readiness statuses separate from marketing/app version numbers.

## 10. Work-package priority order

After Phase 0, execute in this order unless a blocking defect requires otherwise:

```text
1. CalculationSnapshot / dependency inventory
2. single selected X/Z provider boundary
3. remove 2D/PDF/report direct store fallbacks
4. report/calculation orchestration separation
5. regression harness
6. physical residuals and solver validation
7. seabed/anchor and mode-aware wave physics
8. branch-hygiene automation and long-term release cleanup
```

Repository hygiene automation can be developed in parallel only when it does not compete with calculation work.

## 11. Definition of done for optimization work

A work package is complete only when:

- the Issue has explicit acceptance criteria;
- the diff contains only the coherent scope;
- numerical impact is stated;
- required diagnostics/tests exist;
- `.NET Build`, `Selected Shape Consumer Scan` and `Report Store Consumer Scan` are green until replaced by an explicitly approved unified gate;
- no new UI/PDF physics or coordinates were invented;
- agent instructions remain satisfied;
- the PR is merged and its branch can be retired.

## 12. Immediate next work package after this baseline

Create a read-only dependency inventory of:

- `CalculationResult` producers/consumers;
- `MooringShapeStore`;
- `MooringAlternativeShapeStore`;
- `MooringIterativeSolverStore`;
- `MooringPrimaryShapeSelectionStore`;
- selected-shape/read-model consumers;
- PDF/2D/report geometry consumers.

From that inventory define the minimal `CalculationSnapshot` boundary. The first implementation must be behavior-preserving and must not change the solver or selected X/Z coordinates.
