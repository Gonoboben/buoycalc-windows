# BC-AUD-009 — production short-line preflight control

Date: 2026-09-24

## Production order

The application execution boundary uses this order:

```text
typed engineering input
-> EngineeringInputValidator
-> CurrentProfileRequirement
-> LineShorterThanDepth preflight classification
-> BuoyCalculator.Calculate only when preflight allows it
```

The preflight decision sums non-negative `LengthM` only for enabled
`AssemblyItemKind.Line` items with resolved rope presets. Disabled line items and
non-line `LengthM` fields do not contribute. It compares that active length with
`EnvironmentInput.DepthM` under the already approved
`MooringSurfaceBoundaryIntegrationKernel.LengthToleranceM`; this package introduces
no epsilon or new tolerance.

## Completed rejected authority

For depth 85 m and active line 60 m the application completes a typed
`PreflightPhysicalRejected` outcome with minimum required active length 85 m and
exact deficit 25 m. The calculation core is not invoked. No `CalculationResult`,
calculation-result `CalculationSnapshot`, segment rows, engineering loads, selected
or fallback X/Z, or F1/F2/F3/F4 state exists.

The completed outcome retains RunId, calculation timestamp, InputHash, ResultHash and
SourceIdentity under the unchanged input v1/result v3 schemas. UI, Full TXT and the
dedicated short preflight PDF project the same typed evidence and provenance. Their
localized text and PDF bytes are not canonical hash authority.

## Rendered PDF regression evidence

The BC-AUD-009 engineering regression parses the generated PDF with a
test-only PDF reader, rather than treating a non-empty file as sufficient.
It checks the two A4 pages, extracted text/glyph bounds, exact short-line
values and correction, absence of calculated load sections, and the
completed preflight RunId, timestamps, segmented SHA-256 fingerprints and
source identity. The existing Full TXT, input-mutation, repeat-run and
save/reload controls remain part of the same regression.

A passing PDF content extraction test is **not** an independent visual
typography review and is **not** closure of general rejected-PDF
finding BC-AUD-012. A4 layout/readability and non-short-line rejected
states remain separate release acceptance work.

## Scope boundary

This control does not change the calculated branch, solver formulas, signed physical
classification, fixed-point semantics, production segmentation 0.20 m, feedback
budget 64, signed `WeightWaterKgM`, project persistence, or the general rejected-PDF
work tracked by BC-AUD-012.
