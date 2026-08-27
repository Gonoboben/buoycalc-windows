# CONTROL MARK — v1 user PDF typed read-model boundary

Date: 2026-08-27
Parent: #575
Issue: #576

The user-facing engineering PDF now has a retained typed-data boundary available before renderer redesign.

## Authority

`UserEngineeringReportReadModelProjector` copies only already-computed state from the exact completed `CalculationSnapshot` and exact calculation inputs. It does not parse `ResultText`, `ReportText`, or technical Markdown and does not perform new engineering physics.

The retained state includes:
- environment/current-profile/wave/seabed inputs and effective values already defined by the input model;
- buoy and anchor identity/input properties;
- existing `CalculationResult` buoyancy/load/legacy anchor compatibility fields;
- exact selected X/Z read model;
- F1 selected design envelope/tension demand;
- F2 anchor reaction/contact;
- F3 local structural capacity rows;
- F4 engineering assessment/checks;
- exact calculated element rows.

`MainWindowCalculationDisplay` publishes this immutable state into `MainWindowViewModel.UserEngineeringReport`. It is cleared when a new project is created or a project is loaded, just like the other retained calculation presentation state.

## Explicitly unchanged

- visible PDF layout/content in this PR;
- solver and engineering physics;
- F1–F4 authority semantics;
- technical report text;
- project/library formats;
- v1 frozen engineering regression baseline;
- production segmentation 0.20 m;
- signed feedback budget 64;
- signed `WeightWaterKgM` semantics;
- exact deterministic fixed-point candidate acceptance;
- no 3D.
