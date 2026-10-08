# BC-AUD-008 — v1 direction/axis engineering contract

Decision: **Option A**, approved 2026-10-08. Scope: magnitude-based v1 disclosure only; no solver physics switch.

- A signed East/North profile is retained as engineering **input data** and for diagnostic INFO projection.
- Existing segment base forces use `|U_h|=sqrt(East²+North²)`; the shape-force compatibility vector is `(|U_h|, W)` with W positive down.
- `PlanarXAxisAzimuthDeg` is a **passive project-level display/diagnostic field**: 0° North, 90° East, normalized modulo 360°. Changing it does not supply a solver parameter. Editing it still invalidates current exports until calculation is rerun (BC-AUD-002).
- Solver, selected X/Z, loads and F1–F4 are **not** geographic directional projections. Users must not infer Earth-frame cable offset/direction from the plotted 2D X/Z or the azimuth input.
- The centralized `CurrentDirectionModelDisclosure` constants are displayed by main/current-profile XAML and written by Full TXT and typed user PDF. UI/readers do not derive engineering forces.
- Regression `AxisAzimuth_HasDefinedEffectOrIsNotAnInputRegression` compares equal-norm E-only, N-only and negative-E profiles via production `ApplicationCalculationRunner`. It confirms current-force/selected-geometry/verdict invariance while signed profile components and input fingerprints remain distinct. A default-project UI run checks passive azimuth 0°/90° against canonical hashes and BC-AUD-002 invalidation. Full TXT and generated PDF bytes are inspected for the same limitations.

Frozen: production solver formulas and constitutive laws, 0.20 m segmentation, no cap, budget 64, exact deterministic fixed point, signed water-weight line semantics, selected authority, golden baseline, project JSON, input v1/result v3 schemas.

Excluded: directional physical solver implementation, calibration of out-of-plane loss or universal thresholds, 3D, and BC-AUD-009 (PR #628). General v1.0 release still **NOT READY FOR v1.0** pending independent findings/validation/Windows smoke.

Independent review remediation on this branch: positive Accepted signed-candidate fixture from BC-AUD-001 must supply selected X/Z with 2+ nodes and all four F1–F4 authorities. Equality is checked across the three signed current directions including F1 design tension, F2 anchor reaction, F3 local capacity rows, and F4 verdict/check statuses. No missing-authority tests are silently skipped.

The same typed-PDF regression generates `artifacts/bc-aud-008/bc-aud-008-typed-user-report.pdf` during .NET Build, validates A4 page size and glyph bounding boxes, and preserves it via a required CI artifact upload (missing artifact = workflow failure). Layout must be visually reviewed separately; glyph bound checking is not a substitute for human visual QA.

PR #628 overlaps `.github/workflows/dotnet-build.yml`, validation entrypoint, and `BuoyCalc.EngineeringRegression.csproj`. The PdfPig version **and exact package/comment lines** here match PR #628. Integrate PR #628 first and reverify PR #630 against updated `main`; GitHub automatic mergeability is not proof of conflict-free semantic integration. No duplicate PdfPig PackageReference may remain after the merge. Preserve both PDF evidence upload steps. Do not modify PR #628 here.

CI evidence and exact PR head must be added after workflow execution. Do not merge or mark Ready for Review without explicit approval.
