# BuoyCalc Windows — architecture audit closure addendum

Date: 2026-08-22  
Historical audit: `docs/ARCHITECTURE_AUDIT.md` (2026-06-29)  
Current baseline reviewed: `a3cbdb1d0fcae598419b2cd7c45b170610e228d1`

## 1. Purpose

The original architecture audit is a historical snapshot and should not be rewritten as if its June 2026 observations were still current.
This addendum records which audit risks are now closed, which recommendations were intentionally deferred, and which later project decisions superseded the original suggested sequence.

## 2. Executive conclusion

Two different questions must be kept separate:

1. **Are the four architecture-stabilization risks from section 14 closed enough to proceed with engineering work?**  
   **Yes.** The critical authority/boundary problems identified by the audit have been addressed.

2. **Was every observation and every "do later" refactor in the audit implemented literally?**  
   **No.** Several non-gating structural refactors remain intentionally deferred, and 3D was later removed from project scope.

Therefore the original audit should be considered **closed as a stabilization program**, but **not 100% literally implemented line-by-line**.

## 3. Section-14 risk closure matrix

### 3.1 Version stored in several places — CLOSED

Historical risk:

```text
AppInfo + stale XAML version text + WindowVersionHelper duplicate constants + runtime overrides
```

Current state:

- `Services/AppInfo.cs` is the runtime version authority;
- `Views/MainWindow.axaml` binds its title/badge to `AppInfo`;
- `Views/Mooring2DWindow.axaml` binds its displayed version to `AppInfo.DisplayVersion`;
- `Views/CurrentProfileWindow.axaml` binds its displayed version to `AppInfo.DisplayVersion`;
- `Views/WindowVersionHelper.cs` no longer stores its own version constants and delegates to `AppInfo.Version`.

Historical version literals in technical method notes are not runtime application-version authorities.

Disposition: **Closed**.

### 3.2 Competing fallback / alternative / candidate / selected shapes — CLOSED FOR USER-FACING AUTHORITY

Historical risk:

- PDF, 2D and technical report observed different forms;
- multiple mutable stores could become accidental user-facing authorities;
- 2D selected/compared/fell back locally.

Current state:

- `ApplicationModel/CalculationSnapshot.cs` is the immutable application boundary for one completed engineering run;
- `SelectedShapeReadModel` is the user-facing selected geometry/source projection;
- typed selected-core arbitration chooses the authority before rendering;
- Accepted `SignedBoundaryFeedback` may become selected geometry/source authority only through the validated arbitration path;
- non-Accepted cases retain the complete legacy selected read model;
- `MooringAlternativeShapeStore` has been removed;
- `MooringShapeStore` is no longer embedded in `MooringShapeSolver`;
- `Mooring2DCanvas` consumes `vm.SelectedShape` through `Mooring2DDiagramSourceSelector` and does not parse Markdown or construct fallback engineering geometry;
- `PdfReportBuilder` consumes `SelectedShapeReadModel` through `PdfDiagramSourceSelector` and does not select between engineering candidates locally.

Fallback/candidate/iterative shapes remain inside the calculation/diagnostic pipeline by design. Their continued existence is not a competing user-facing authority.

Disposition: **Closed for authority ownership**.

### 3.3 Technical OK/INFO/WARNING/FAILED status leaking directly into user UI — CLOSED AT THE DISPLAY BOUNDARY

Historical risk:

- solver and engineering diagnostics use technical status/severity strings;
- user-facing views previously displayed those strings directly.

Current state:

- `Services/UserStatusPolicy.cs` maps technical status/verdict/risk strings to user-facing language without mutating solver diagnostics;
- `Models/ElementCalculationDisplayRow.cs` maps `ElementCalculationRow.Status` through `UserStatusPolicy` before user presentation;
- technical report output may continue to contain engineering/diagnostic terms because it is explicitly a technical report;
- domain notation such as `U/V/W`, `MBL`, `Cd`, `X/Z` may remain where it is technically meaningful and is not treated as a raw severity leak.

Disposition: **Closed as an architecture boundary**. Remaining wording improvements are UX/copy work, not authority ambiguity.

### 3.4 PDF, 2D, main UI and technical report reading unrelated sources — CRITICAL RISK CLOSED; UNIVERSAL USER REPORT MODEL STILL DEFERRED

Historical risk:

- `ReportText` had side effects and was indirectly used as a data source;
- PDF mixed stores, text parsing and visualization fields;
- 2D mixed stores, Markdown parsing and approximate fallback drawing;
- main UI maintained separate summary fields.

Current state:

- legacy `ReportBuilder` has been removed;
- technical Markdown assembly is separated under the `TechnicalReportBuilder` / `TechnicalReportMarkdownBuilder` path;
- `ApplicationCalculationRunner` returns one completed `ApplicationCalculationRun` containing `CalculationResult` plus immutable `CalculationSnapshot`;
- `CalculationSnapshot` directly retains `TechnicalReportData` and `SelectedShapeReadModel`;
- 2D no longer parses the technical report and does not choose physics locally;
- PDF selected geometry comes from the already-selected read model and no longer reads a shape store or reconstructs geometry from Markdown.

Residual compatibility debt:

- `PdfReportBuilder.Build(...)` still has a legacy-shaped parameter surface including `resultText`, `sequenceLines`, `elementRows`, `reportText`, and visualization values;
- the current implementation no longer uses `reportText` as an engineering geometry authority, but the parameter remains;
- `MainWindowViewModel` still keeps summary/display fields instead of consuming one universal `UserReportModel` object;
- a single universal report/read model for every user-facing text/table is therefore not fully implemented.

Disposition: **Critical architecture risk closed; cleanup/refinement deferred**.

## 4. Historical section-13 refactors that are intentionally not all complete

The original audit explicitly marked several items as "later" work. They are not release-blocking simply because they still exist.

### Still deferred / partially deferred

- `MainWindowViewModel` remains a large ViewModel with project state, editor state and presentation fields, although calculation/file/export boundaries were extracted;
- `ElementLibraryViewModel` still owns the five buoy/rope/connector/anchor/payload editors;
- `ElementLibraryWindow.axaml` remains a dense multi-editor view;
- `Models/EngineeringModels.cs` still contains both engineering model records and `BuoyCalculator`;
- `MooringIterativeSolver` still owns some technical report/method-note assembly in addition to solver output;
- `MooringPrimaryShapeGate.cs` still contains gate and selector types in one file, although mutable selection-store ownership was removed;
- `PdfReportBuilder` still owns PDF drawing and a compatibility-oriented parameter surface rather than consuming one final `UserReportModel`.

These are maintainability/refactoring opportunities. They must not be changed merely to reach a numeric "100% audit completion" if the change adds risk without improving an active boundary.

## 5. Later scope decisions that supersede the original sequence

The historical audit ended with the conceptual sequence:

```text
unified sources -> tests -> 2D -> 3D -> stronger solver physics
```

Later project decisions changed that roadmap:

- 3D is explicitly out of scope;
- 2D and PDF were stabilized as model-data renderers before further physics migration;
- engineering validation was strengthened through deterministic regression packages and signed-candidate work;
- selected signed geometry authority was introduced only after evidence and typed arbitration;
- downstream scalar authority was intentionally frozen when evidence did not justify transfer.

Therefore the absence of 3D is **not an unfinished audit item**.

## 6. Current completion statement

Use the following project status language:

```text
Historical architecture stabilization: COMPLETE.
Four section-14 risk classes: CLOSED / bounded.
Literal implementation of every deferred refactor: NOT COMPLETE and not required for closure.
3D: intentionally excluded from scope.
Next active program: pre-v1 engineering physics roadmap, not residual architecture cleanup.
```

If a future change touches one of the deferred structural areas, it should be justified by a concrete behavior/maintenance need and introduced as a small behavior-preserving PR rather than as a broad "finish the audit" rewrite.

## 7. Next roadmap

The active pre-v1 plan is recorded separately in:

`docs/ROADMAP_PRE_V1_ENGINEERING_PHYSICS_2026-08-22.md`

That roadmap deliberately prioritizes:

1. wave-aware quasi-static local demand;
2. anchor-end vector/contact-uplift semantics;
3. local weak-link / per-element demand;
4. dependent checks/verdict integration;
5. release freeze and `v1.0.0`.

Full time-domain dynamics remains post-v1.
