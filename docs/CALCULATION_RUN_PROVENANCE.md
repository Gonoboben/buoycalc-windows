# Calculation run provenance

BC-AUD-005 introduces one immutable provenance value for every completed
`ApplicationCalculationRunner.Run` authority. The value is created once after the
calculation snapshot has been formed and is retained on `CalculationSnapshot`.
Typed UI/report models and renderers only consume that retained value.

## Identity semantics

- `RunId` is a random UUID in canonical `D` form. Every explicit Calculate creates
  a new value, including repeated Calculate with unchanged input.
- `CalculationTimestampUtc` is captured when the completed snapshot authority is
  formed. It is not an export timestamp.
- PDF export time remains separate and is labelled `Время экспорта PDF, UTC`.
- `ProjectName` is report/project metadata. BC-AUD-002 invalidates presentation
  authority when it changes, but it is deliberately excluded from engineering
  `InputHash`.

## Canonical hash contract

Both fingerprints use SHA-256 over compact UTF-8 JSON. Numbers are emitted by
`System.Text.Json` as invariant JSON numbers. Null optional values are explicit.
Objects use their declared field/property order; arrays retain calculation input or
result order. Dictionaries, localized labels, filenames, UI formatting, export
destinations and export timestamps are excluded.

The first canonical field is a schema identifier:

- input: `buoycalc-engineering-input/v1`;
- result: `buoycalc-engineering-result/v1`.

Changing encoding, member set/order, numeric/null rules or list semantics requires a
new schema identifier.

`InputHash` covers the effective environment/current profile, seabed engineering
properties, buoy, ordered assembly input and preset engineering properties, anchor,
and safety factor actually supplied to the calculation boundary. Legacy scalar
current switches, preset notes and project/report metadata are not calculation
authority and are excluded.

`ResultHash` covers the retained `CalculationResult`, selected X/Z authority, signed
candidate/disposition, selected core and retained F1/F2/F3/F4 authority states. It
does not hash rendered TXT or PDF bytes.

## Source identity

`SourceIdentity` is derived from the executing `BuoyCalc.Windows` assembly
informational version. If that version contains a `+<40-hex>` source revision, it is
reported as the exact revision embedded in that binary. The project declares base
informational version `1.0.0`; the .NET SDK/source-control integration appends the
exact revision in the GitHub build (verified by the targeted regression). Builds
made without source-revision metadata honestly report `source-revision=unavailable`.
The runtime never reads a working tree or fabricates a commit SHA.

## Persistence boundary

The v1 project JSON contract stores editable project inputs only. It does not store
or claim a last calculation/result authority. Loading or creating a project clears
the current result under BC-AUD-002 and requires a new Calculate, which creates a new
provenance value. This package therefore does not change the persistence schema and
does not absorb BC-AUD-003 replay work.
