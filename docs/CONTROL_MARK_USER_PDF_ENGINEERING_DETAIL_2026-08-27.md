# Control mark — user PDF engineering detail — 2026-08-27

Parent issue: #575  
Implementation issue: #580

## Scope

The typed user PDF now includes the retained engineering-detail state required for review of a real mooring calculation:

- buoyancy balance and retained horizontal load values;
- F1 selected design tension authority and surface/anchor/max-midpoint resultants;
- F3 local structural-capacity coverage, governing row and per-element table;
- F2 selected anchor-boundary reaction/contact state;
- legacy anchor holding estimate in a visually separate compatibility-only section;
- F4 selected engineering checks, verdict, main risk and conclusion.

## Authority separation

The renderer reads only `UserEngineeringReportReadModel`. It does not parse `ResultText`, `ReportText` or the technical report and does not calculate engineering physics.

The legacy holding estimate is retained only as compatibility evidence. It cannot authorize a selected pass. Horizontal anchor/soil capacity remains `RequiresAdditionalPhysicalModel` pending a validated physical model for the actual anchor and seabed.

## Frozen invariants

No solver, F1-F4 authority or engineering regression baseline changes. Production segmentation remains 0.20 m. Signed feedback budget remains 64. Signed `WeightWaterKgM` semantics and exact deterministic fixed-point acceptance remain unchanged. No 3D.
