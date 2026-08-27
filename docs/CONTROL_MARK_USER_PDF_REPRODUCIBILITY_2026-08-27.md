# Control mark — user PDF reproducibility — 2026-08-27

Parent issue: #575  
Implementation issue: #582

## Final presentation scope

The user engineering PDF ends with a reproducibility/provenance page. It records the project, application/model version, PDF generation UTC time, typed report source, selected X/Z status/source, F1/F2/F3/F4 authority source identities and frozen v1 implementation metadata.

The page explicitly states that the PDF is rendered from the retained `UserEngineeringReportReadModel`; full-report text is not an engineering data source for the PDF.

## Frozen v1 metadata shown

- production segmentation: 0.20 m;
- signed boundary-feedback iteration budget: 64;
- signed `WeightWaterKgM` semantics;
- accepted signed candidate requires an exact deterministic fixed point, without epsilon acceptance;
- `s=0` at buoy/surface and `s=L` at anchor/seabed.

## Authority separation

Selected F1/F2/F3/F4 state remains the engineering authority used by the user PDF. Legacy tension/anchor-holding data may appear only as explicitly labelled compatibility evidence and never replaces selected authority.

No solver, physics, F1-F4 authority, project persistence, engineering regression baseline or 3D changes are part of this step.
