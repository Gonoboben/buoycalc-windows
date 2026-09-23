# BuoyCalc Windows — adversarial / pre-release engineering audit record

Date of independent audit: 2026-09-22  
Repository: `Gonoboben/buoycalc-windows`  
Audited branch: `main`  
Audited commit: `0db1e7cc17e309fc1402fe58b265d2e95c2d4d39`  
Source report: `BuoyCalc_Windows_PreRelease_Adversarial_Audit_2026-09-22(1).pdf`  
Repository record issue: #609

## 1. Status and authority

**CURRENT RELEASE STATUS = NOT READY FOR v1.0.**

This status must not be changed merely because CI is green, the EXE launches, unit/regression tests pass, invariants pass, or the calculation core returns a result.

Authority split:

- repository implementation is the source of truth for current code;
- the 2026-09-22 audit report is the source of truth for the audit results actually executed against the audited SHA;
- this Markdown document is the persistent repository record of that audit and its remediation disposition;
- no finding is considered fixed because code looks different: closure requires explicit evidence/regression against the relevant finding.

The audit counted:

| Severity | Count | Release blockers |
|---|---:|---:|
| P0 | 4 | 4 |
| P1 | 8 | 6 |
| P2 | 2 | 0 |
| P3 | 0 | 0 |
| Total | 14 | 10 |

## 2. Evidence interpretation labels

Use these labels in all follow-up work:

- **FACT** — directly reproduced by the audit or directly confirmed in the audited repository state.
- **INFERENCE** — reasoned consequence from established facts, but not itself directly executed.
- **AUDIT HYPOTHESIS** — plausible root-cause explanation from the audit; must not be treated as proven without code/test evidence.
- **NOT VERIFIED** — blocked by environment or not executed.

## 3. Audit execution summary

The audit actually:

- read AGENTS.md, docs/OPTIMIZATION_PLAN.md, RELEASE.md, calculation architecture, validation, F1/F2/F3/F4 and release workflow documents;
- built Release on Linux with .NET SDK 8.0.414;
- ran BuoyCalc.EngineeringRegression, BuoyCalc.ValidationCampaign, BuoyCalc.PhysicalRejectionValidation and BuoyCalc.FeedbackDiagnostics;
- ran an external production-path black-box harness for TEST 1–13;
- generated real production PDF and Full TXT artifacts and visually inspected rendered PDF pages;
- compared five repeated identical runs by deterministic digest;
- exercised Series A, property-boundary, current-profile, anchor/soil and depth-sweep cases;
- exercised DTO persistence, ViewModel restore, type changes and stale state.

Not fully verified by the audit:

- interactive Windows RC EXE mouse/keyboard behavior;
- native file dialogs and literal Windows process close/reopen lifecycle;
- complete GUI reorder/delete/insert event routing;
- full first-user Windows walkthrough;
- Windows UI responsiveness/memory at 380 m;
- external physical validation of hydrodynamics, soil/anchor holding or dynamics.

These limitations do not invalidate the reproduced P0/P1 blockers in production calculation, persistence, ViewModel and renderer paths.

## 4. Finding register

### BC-AUD-001 — PDF and Full TXT publish different X/Z for one run

- Severity: **P0**
- Subsystem: REPORT, PDF, ARCHITECTURE, STATE
- Release blocker: **YES**
- Reproduced: **YES, 2/2**
- Evidence classification: **FACT**
- Audit evidence:
  - line 20.2 m: selected/read model X = 2.473770 m; PDF = 2.47 m; Full TXT = 2.7886 m;
  - line 20.3 m: selected/read model X = 3.043175 m; PDF = 3.04 m; Full TXT = 3.5268 m;
  - node count can coincide even when the node set belongs to different geometry authority.
- Engineering consequence: footprint/clearance/placement decisions can use the wrong geometry.
- User consequence: two official-looking artifacts of one calculation contradict each other.
- Likely root-cause area: **AUDIT HYPOTHESIS, code-localized** — selected projector replaces decision sections but legacy `TechnicalReportData.Shape` remains in the Full TXT geometry section.
- Affected areas identified by audit:
  - `Services/TechnicalReportMarkdownBuilder.cs`
  - `Services/SelectedTechnicalReportProjector.cs`
  - `Services/PdfReportBuilder.cs`
- Existing coverage: renderer fragment regressions.
- Missing regression: `OneRun_AllArtifacts_SelectedAuthorityExactlyEqual`.

### BC-AUD-002 — editable inputs do not invalidate the last run

- Severity: **P0**
- Subsystem: STATE, UI, REPORT, ARCHITECTURE
- Release blocker: **YES**
- Reproduced: **YES**
- Evidence classification: **FACT**
- Audit evidence:
  - after successful Run A, depth/project title/line length were edited without Calculate;
  - UI showed new values;
  - `UserEngineeringReport` and `SelectedShape` remained the same object references from Run A;
  - PDF export remained enabled;
  - suggested filename used the new project name while PDF content remained from the old run.
- Engineering consequence: an artifact can be attributed to inputs that did not produce it.
- User consequence: screen/filename can describe state B while report content belongs to state A.
- Likely root-cause area: **AUDIT HYPOTHESIS, strongly corroborated** — no dirty generation/run binding; editable setters do not invalidate result authority.
- Affected areas:
  - `ViewModels/MainWindowViewModel.cs`
  - `Views/MainWindow.axaml.cs`
  - PDF export precondition/workflow.
- Existing coverage: New/Load clears previous results.
- Missing regression: `InputMutation_InvalidatesLastRunAndBlocksEveryExport`.

### BC-AUD-003 — project JSON is not a self-contained input snapshot

- Severity: **P0**
- Subsystem: PERSISTENCE, DATA MODEL
- Release blocker: **YES**
- Reproduced: **YES**
- Evidence classification: **FACT**
- Audit evidence:
  - saved buoy raw values 9.99 / 999 / 9.99 / 9.99 restored as current preset values 0.5 / 80 / 0.5 / 0.8;
  - missing rope ID `user:deleted-rope` remained visible while calculation silently resolved to the first built-in polyester rope.
- Engineering consequence: the same project file can produce different resolved engineering input after library changes.
- User consequence: reopen appears successful while the engineering model has silently changed.
- Root-cause facts confirmed in audited code:
  - save DTO contains raw buoy properties;
  - buoy restore model retains only name + preset ID;
  - unresolved rope ID falls back to `RopeCatalog.Presets[0]`.
- Affected areas:
  - `ViewModels/MainWindowProjectDtoMapper.cs`
  - `ViewModels/MainWindowViewModel.cs`
  - `Services/RopeLibraryStorage.cs`
  - `Models/ProjectDtos.cs`
- Existing coverage: DTO round-trip.
- Missing regressions:
  - `ProjectReplay_UsesEmbeddedResolvedPresetSnapshotOrFails`
  - `MissingPresetId_MustNotFallbackSilently`.

### BC-AUD-004 — invalid negative physical properties can receive engineering authority

- Severity: **P0**
- Subsystem: VALIDATION, PHYSICS INPUT, DATA MODEL
- Release blocker: **YES**
- Reproduced: **YES**
- Evidence classification: **FACT**
- Audit evidence:
  - payload mass -20 kg -> Signed Accepted, F4 = Требуется проверка, diagnostics=Error;
  - negative rope diameter/Cd/payload area can produce negative current force;
  - zero/negative MBL continues calculation with diagnostics;
  - parse failure/NaN/Infinity at the UI boundary is normalized to zero.
- Engineering consequence: loads, buoyancy and reserve can be biased in a non-conservative direction.
- User consequence: the product can publish an engineering conclusion from impossible physical data.
- Root-cause fact: diagnostics are observational and are not a universal pre-core validation gate.
- Affected areas:
  - `ViewModels/MainWindowCalculationInputBuilder.cs`
  - `ViewModels/AssemblyItemViewModel.cs`
  - calculation drag/weight paths in `Models/EngineeringModels.cs`.
- Existing coverage: diagnostics identify many negative values.
- Missing regression: parameterized `NegativePhysicalInputs_BlockBeforeCore`.

### BC-AUD-005 — no verifiable calculation-run identity

- Severity: **P1**
- Subsystem: ARCHITECTURE, STATE, REPORT, DATA MODEL
- Release blocker: **YES**
- Reproduced: structural fact
- Evidence classification: **FACT**
- Audit evidence:
  - no immutable RunId;
  - no calculation timestamp;
  - no canonical input snapshot/hash;
  - no result fingerprint;
  - no source commit/version identity linked across artifacts;
  - PDF provenance contains export-time UTC rather than calculation time.
- Engineering consequence: two artifacts cannot prove they originate from the same engineering input/result.
- User consequence: discrepancies cannot be reliably investigated from saved artifacts.
- Likely root-cause area: **AUDIT HYPOTHESIS** — current `CalculationSnapshot` groups values but does not provide provenance identity.
- Affected areas:
  - `ApplicationModel/CalculationSnapshot.cs`
  - `Services/UserEngineeringReportReadModel.cs`
  - `Models/ProjectDtos.cs`
  - `Services/PdfReportBuilder.cs`.
- Existing coverage: source identity within selected F1–F4.
- Missing regression: same RunId/InputHash/ResultHash propagated through UI/TXT/PDF/project.

### BC-AUD-006 — exact fixed-point policy loses numerically stationary valid cases

- Severity: **P1**
- Subsystem: SOLVER, UX
- Release blocker: **YES**
- Reproduced: **YES**
- Evidence classification: **FACT**
- Audit evidence:
  - Series A A06/A11/A15 enter deterministic exact two-state cycles;
  - final changes are approximately 1e-15 to 1e-14;
  - status remains `BudgetExhausted` at iteration 64;
  - F1–F4 are absent.
- Engineering consequence: physically/numerically admissible inputs can finish without selected design demand/assessment.
- User consequence: calculation completes but no useful engineering authority is produced.
- Root-cause area: exact equality versus IEEE-754 two-cycle.
- Important: the audit does **not** authorize an epsilon.
- Existing repository issue: #602.
- Existing coverage: feedback diagnostics reproduce the behavior.
- Missing regression/product contract: `MachineTwoCycle_HasExplicitDeterministicDisposition`.
- Physics/solver change: **YES if acceptance/convergence semantics change**.
- Dedicated validation package required: **YES**.

### BC-AUD-007 — duplicate current-profile depths create order-dependent physics

- Severity: **P1**
- Subsystem: VALIDATION, PHYSICS INPUT
- Release blocker: **YES**
- Reproduced: **YES**
- Evidence classification: **FACT**
- Audit evidence:
  - points at depths 0,0,20 pass the production profile requirement;
  - reversing the two depth=0 rows changes force 41.0000 N -> 93.6156 N;
  - signed status changes Accepted -> BudgetExhausted.
- Engineering consequence: the same set of measurements can produce different loads from row order alone.
- User consequence: reorder can silently change design result.
- Root-cause fact: requirement demands at least two distinct depths overall but does not reject duplicates; stable sort preserves duplicate order.
- Affected areas:
  - `Models/CurrentProfileRequirement.cs`
  - `ViewModels/MainWindowCalculationInputBuilder.cs`
  - profile interpolation in `Models/EngineeringModels.cs`.
- Existing coverage: diagnostics flag duplicate-depth error, but calculation is not blocked.
- Missing regression: every permutation of a duplicate-depth set must reject.

### BC-AUD-008 — axis azimuth / East-North direction do not govern solver orientation

- Severity: **P1**
- Subsystem: PHYSICS INPUT, UI, DOCUMENTATION
- Release blocker: **YES**
- Reproduced: **YES**
- Evidence classification: **FACT**
- Audit evidence:
  - changing +X azimuth 0 deg -> 90 deg does not change result;
  - East-only 0.3 m/s and North-only 0.3 m/s produce the same result;
  - input builder has no axis parameter and horizontal components collapse to magnitude.
- Engineering consequence: geographic orientation cannot be tied to the X/Z calculation plane.
- User consequence: a prominently labelled engineering input appears meaningful but has no production effect.
- Required v1 decision:
  - either make the field explicitly passive/non-engineering and disclose magnitude semantics;
  - or connect orientation to calculation only through a separate physics/validation package.
- Existing coverage: persistence/passive projection.
- Missing regression: `AxisAzimuth_HasDefinedEffectOrIsNotAnInput`.

### BC-AUD-009 — short-line validation is late and rejected reporting is misleading

- Severity: **P1**
- Subsystem: VALIDATION, UX, REPORT, PDF
- Release blocker: **NO independently**, but must be fixed for v1 usability with authority blockers
- Reproduced: **YES**
- Evidence classification: **FACT**
- Audit case: depth=85 m, line=60 m.
- Actual:
  - full core/segmentation executes before signed physical rejection;
  - status becomes RejectedPhysical;
  - selected X/Z and F1–F4 are null;
  - rejected PDF geometry page is empty;
  - base current/wave/horizontal loads remain visible;
  - failure text exposes internal English flags;
  - no correction such as “minimum 85 m; add at least 25 m”.
- Engineering consequence: non-authoritative base loads can be mistaken for design loads after impossible geometry.
- User consequence: user does not know exactly what to change.
- Missing regression: `RejectedShortLine_ReportIsLocalizedActionableAndLoadSafe`.

### BC-AUD-010 — Accepted signed geometry with non-positive submerged anchor weight has no terminal assessment

- Severity: **P1**
- Subsystem: VALIDATION, PHYSICS PRECONDITION, ARCHITECTURE
- Release blocker: **YES**
- Reproduced: **YES**
- Evidence classification: **FACT**
- Audit evidence with anchor volume 1.2 m3:
  - 500 kg -> submerged weight about -730 kg;
  - 1000 kg -> about -230 kg;
  - both can have Signed=Accepted while F2/F4 and terminal physical disposition are null.
- Engineering consequence: geometry acceptance is observable without a complete anchor/contact decision.
- User consequence: there is no single authoritative terminal verdict.
- Architecture fact:
  - F4 policy defines non-positive anchor submerged weight as HardFailure;
  - selected assessment is unavailable when prerequisite F1/F2/F3 authority is missing, so that direct hard precondition may never become a terminal product result.
- Missing regression: `AcceptedSigned_InvalidAnchorPrerequisite_IsTerminalFailure`.

### BC-AUD-011 — validation campaign can be green with CRITICAL findings

- Severity: **P1**
- Subsystem: RELEASE PROCESS, TEST COVERAGE
- Release blocker: **YES**
- Reproduced: **YES**
- Evidence classification: **FACT**
- Audit evidence:
  - ValidationCampaign reported 34 findings: 25 CRITICAL, 9 HIGH;
  - process returned exit code 0 by design.
- Audited source explicitly documents the campaign as evidence gathering and returns 0 when evidence generation succeeds.
- Engineering consequence: CI color cannot distinguish evidence collection from release acceptance.
- User consequence: false confidence in release maturity.
- Existing coverage: rich Series A evidence exists.
- Missing release contract: maintained allowed/waived finding policy and a gate that fails on new/unresolved release-blocking P0/P1.
- Important: the evidence campaign may remain observational; a separate release-decision gate is acceptable.

### BC-AUD-012 — rejected PDF is not a sufficient engineering diagnostic artifact

- Severity: **P1**
- Subsystem: PDF, REPORT, UX
- Release blocker: **NO independently**, but MUST FIX with rejected-path UX
- Reproduced: **YES**
- Evidence classification: **FACT**
- Actual:
  - almost empty geometry page;
  - internal English/enum diagnostics;
  - base loads without sufficiently strong non-authoritative distinction;
  - no required corrective delta;
  - title clipping on A4.
- Engineering consequence: rejected artifact does not support correction/independent review.
- User consequence: long report with little actionable guidance.
- Missing regression: rendered rejected PDF baseline that verifies localized actionable fields and selected-vs-diagnostic distinction.
- Renderer rule: diagnostic visualization may represent input geometry only if it is explicitly non-selected and contains no renderer-invented physics.

### BC-AUD-013 — type change retains semantically stale name and hidden preset state

- Severity: **P2**
- Subsystem: UI, DATA MODEL, UX
- Release blocker: **NO**
- Reproduced: **YES at ViewModel level**
- Evidence classification: **FACT**
- Audit evidence:
  - Connector -> Payload -> Line changes calculation role;
  - old human-facing title remains;
  - hidden preset IDs remain and can silently supply the new type’s properties.
- Engineering consequence: domain role remains recoverable from Kind, but human review can misidentify the element.
- User consequence: misleading sequence/report naming.
- Missing regression: conversion matrix with explicit reset/confirmation and no unintended hidden state.

### BC-AUD-014 — first-user input and terminology are not release-polished

- Severity: **P2**
- Subsystem: UX, UI, DOCUMENTATION
- Release blocker: **NO**
- Evidence classification: mixed **FACT / NOT VERIFIED**
- Static FACT:
  - free-text numeric controls;
  - parse failures normalize to zero at the calculation boundary;
  - mandatory current-profile setup;
  - mixed Russian/English/internal solver terminology;
  - no dirty/stale badge.
- NOT VERIFIED:
  - complete moderated first-user Windows RC walkthrough.
- Engineering consequence: primarily usability; safety relevance increases when combined with BC-AUD-004/009.
- Missing acceptance test: executable Windows first-run checklist on exact RC.

## 5. Confirmed robust behavior from the audit

Do not lose these properties while fixing blockers:

- RejectedPhysical short/taut-line and insufficient-buoyancy cases block selected X/Z and do not fabricate F1–F4.
- Valid depth sweep 10/50/85/100/380 m with slack line produced Accepted signed selected geometry with segment-length closure.
- Five identical runs produced one deterministic digest.
- Accepted PDF uses typed `UserEngineeringReportReadModel`, selected F1–F4 and selected curve.
- F4 does not falsely claim horizontal anchor holding is validated; current accepted cases remain `Требуется проверка`.
- Project load clears previously calculated report/selected result.
- Exact audited main had green technical CI/release workflows.

These facts are regression-preservation requirements, not proof of overall release readiness.

## 6. Required state/provenance chain

Target:

```text
User input
  -> immutable canonical InputSnapshot + RunId + calculation timestamp + input fingerprint
  -> calculation core
  -> immutable ResultSnapshot + result fingerprint
  -> UI / Full TXT / PDF / persistence identity bound to the same run
```

Audited gap:

```text
mutable VM inputs
  -> transient parsed input
  -> CalculationResult + CalculationSnapshot(no run identity/input snapshot)
  -> several projections
  -> last report/shape remain stored while editable inputs continue to mutate
```

At minimum follow-up design must address:

- RunId;
- calculation timestamp;
- canonical resolved input snapshot;
- input fingerprint;
- result fingerprint;
- source/version identity;
- dirty generation / stale-state relation;
- artifact identity in TXT/PDF;
- project/preset replay provenance.

## 7. Cross-artifact release invariant

Before v1.0, one user must be able to execute:

```text
create setup
-> save project
-> calculate
-> understand result
-> view scheme
-> obtain Full TXT
-> obtain PDF
-> close/reopen project
-> recalculate
```

and all authoritative representations must refer to the **same calculation run** and not contradict one another.

For rejected input, the product must provide:

- terminal reason;
- what is physically impossible under the current model;
- what input requires correction;
- actionable correction where derivable without invented physics;
- clearly labelled diagnostic sketch where safe;
- no fallback/diagnostic value presented as selected engineering authority.

## 8. Safe remediation sequence

Do not combine these into one giant PR.

1. **BC-AUD-001** — one selected X/Z authority across accepted Full TXT/PDF/UI.
2. **BC-AUD-002** — input mutation invalidates/stales last run and blocks export.
3. **BC-AUD-005** — introduce calculation-run provenance/identity.
4. **BC-AUD-003** — reproducible project replay; remove silent missing-preset fallback.
5. **BC-AUD-004** — pre-core blocking contract for invalid physical inputs.
6. **BC-AUD-007** — reject duplicate current-profile depths deterministically.
7. **BC-AUD-010** — terminal result for invalid anchor hard prerequisite.
8. **BC-AUD-009** — early short-line rejection + actionable localized state.
9. **BC-AUD-008** — define truthful v1 axis/direction contract; do not connect new physics casually.
10. **BC-AUD-006 / #602** — separately validate deterministic exact two-cycle disposition; solver/acceptance change requires its own validation package.
11. **BC-AUD-012** — rejected PDF diagnostic usefulness / minimal diagnostic scheme.
12. **BC-AUD-011** — release gate consumes maintained unresolved/waived finding disposition.
13. **BC-AUD-013/014** — remaining type-conversion and first-user polish.
14. Re-run exact-SHA Windows RC TEST 1–7 and 14–15 plus cross-artifact consistency matrix before v1.0.

Order may be adjusted only with explicit evidence that dependencies require it. Cosmetic work must not displace unresolved P0/P1.

## 9. First proposed atomic remediation package

Finding: **BC-AUD-001**.

Proposed goal:

> For an Accepted run, Full TXT, typed user read model, UI selected state and PDF must publish the same selected X/Z authority. Legacy/fallback geometry may remain only as explicitly named diagnostic/compatibility evidence.

Expected scope:

- `Services/TechnicalReportMarkdownBuilder.cs`
- `Services/SelectedTechnicalReportProjector.cs`
- targeted cross-artifact regression tests

Do not change solver, physics, selected source arbitration, F1–F4 equations, segmentation, signed feedback semantics or persistence.

Required regression:

`OneRun_AllArtifacts_SelectedAuthorityExactlyEqual`

Acceptance must cover at least the audit golden 20.2 m line case and the changed 20.3 m case.

## 10. Frozen engineering constraints during remediation

Unless a separately approved physics/validation package explicitly changes them:

```text
production segmentation = 0.20 m
signed boundary-feedback iteration budget = 64
signed WeightWaterKgM semantics = unchanged
Accepted signed candidate = exact deterministic fixed point
convergence epsilon acceptance = forbidden
engineering physics = calculation core only
UI/PDF/TXT/visualization = consumers only
Synthetic PASS != physical validation
3D = post-v1
```

## 11. Release-blocker closure policy

A finding is not closed by:

- a green CI job alone;
- code review alone;
- a changed implementation shape;
- a synthetic smoke;
- absence of exceptions.

A blocker may be closed only when its explicit regression/validation evidence passes against the exact candidate SHA and all affected downstream artifacts are checked.

After all P0/P1 release blockers are closed, build a fresh exact-main Windows RC and perform the final manual Windows EXE verification before tag/release authorization.
