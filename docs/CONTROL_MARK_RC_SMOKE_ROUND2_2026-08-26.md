# Control mark: second Windows v1.0.0 RC smoke corrections — 2026-08-26

Issue: #565  
Parent release gate: #522

## Scope

This change set responds to the second manual Windows 11 RC smoke. It is limited to presentation/UI and user-library portability. Engineering physics remains frozen.

1. **One 2D presentation read model for window + PDF**
   - both consumers use `Mooring2DDiagramReadModelBuilder`;
   - selected X/Z comes from retained `SelectedShapeReadModel` only;
   - element attachment positions come from retained calculated element rows through `Mooring2DElementBoundaryProjector`;
   - ordinary 0.20 m segmentation beads are not presentation markers;
   - real line ends, connectors and payloads receive shared surface/interior/bottom label zones and lanes to reduce collisions;
   - buoy/anchor labels use retained calculated element titles rather than solver-node labels.

2. **Collapsible central sequence cards**
   - `AssemblyItemViewModel.IsExpanded` is UI-only state;
   - it is not added to project DTOs and does not enter `AssemblyItemInput`;
   - title, summary, enabled flag and ordering actions remain visible while details are collapsed.

3. **Practical library help**
   - displaced-volume help points to passport/datasheet, drawing/CAD, displacement measurement and simple geometry;
   - help distinguishes sealed/solid displacement from water-flooded open cavities;
   - `Cd` help points to manufacturer hydrodynamic data, tests/reference data and the measured-drag relation `Cd = 2F/(ρ U² A)`;
   - dimensions alone are explicitly not treated as sufficient to infer `Cd`.

4. **Portable element-library bundle**
   - format identity: `BuoyCalc.ElementLibrary`, version 1;
   - export contains only user-defined buoy, rope, connector, payload and anchor entries;
   - built-in presets are never exported or overwritten;
   - import is additive and non-destructive;
   - an imported item conflicting by user/built-in ID or case-insensitive name is skipped, never substituted;
   - imported/skipped counts are reported to the user.

## Frozen engineering invariants

No solver/physics changes. The canonical engineering baseline is not regenerated. Production segmentation remains exactly 0.20 m. Signed feedback budget remains exactly 64. Signed `WeightWaterKgM` semantics remain unchanged. Signed candidate acceptance remains the exact deterministic fixed point with no epsilon. `s=0` remains buoy/surface and `s=L` anchor/seabed. PDF and 2D remain renderers of retained calculated state. No 3D is introduced.

## Release consequence

The RC built from `c1cb21c904995457dc4595c35167ac7eeac7a8b8` is retained only as smoke evidence. After this correction merges, a fresh exact-main RC must pass the three release gates and deterministic package verifier, followed by a new manual Windows smoke before any `v1.0.0` tag or GitHub Release.
