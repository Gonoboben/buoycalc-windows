# Calculation run provenance

BC-AUD-005 introduces one immutable provenance value for every completed application
execution authority. A completed outcome is explicitly one of two sealed typed states:

- `Calculated`: retains the existing non-null `ApplicationCalculationRun`,
  `CalculationResult` and `CalculationSnapshot` contract;
- `PreflightPhysicalRejected`: retains typed terminal physical-rejection evidence and
  creates no `CalculationResult`, calculation-result `CalculationSnapshot`, selected
  X/Z, F1/F2/F3/F4 state, or engineering loads.

The calculated value is created once after the calculation snapshot has been formed
and remains retained on `CalculationSnapshot`. The preflight-rejected value is created
from canonical input identity and typed rejection evidence without invoking the
calculation core. Both outcomes expose the same immutable provenance contract.

The public short-line preflight factory is the trusted construction boundary. It first
applies `EngineeringInputValidator.Validate` and
`CurrentProfileRequirement.EnsureUsable`, then derives depth, enabled active-line
length, minimum required line length and exact deficit from the same typed inputs used
for `InputHash`. Callers cannot supply independent authoritative rejection evidence.
Active-line length follows the calculation-core rule: sum non-negative `LengthM` for
enabled `AssemblyItemKind.Line` inputs with resolved rope presets; item `Count`,
disabled lines and non-line items do not add line length. The short-line decision uses
the existing signed-boundary `LengthToleranceM`; no new tolerance is introduced.

BC-AUD-009 now routes production short-line input through this preflight branch after
the shared physical-input and current-profile gates and before the calculation core.
The same retained `LengthToleranceM` contract controls the decision. A completed
short-line rejection therefore owns full provenance without creating a
`CalculationResult`, calculation-result snapshot, segment rows, loads, selected X/Z,
or F1/F2/F3/F4 authority. Its localized UI, Full TXT and dedicated short PDF are
projections of the typed rejection evidence and are not hash authority.

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
- result/outcome: `buoycalc-engineering-result/v3`.

Changing encoding, member set/order, numeric/null rules or list semantics requires a
new schema identifier.

`InputHash` covers the effective environment/current profile, seabed engineering
properties, buoy, ordered assembly input and preset engineering properties, anchor,
and safety factor actually supplied to the calculation boundary. Legacy scalar
current switches, preset notes and project/report metadata are not calculation
authority and are excluded.

`ResultHash` v3 begins with `Schema`, followed by the explicit `OutcomeKind`, then the
nullable `CalculatedResult` and `PreflightPhysicalRejection` union branches in that
order. Exactly one typed branch is populated:

- `Calculated` covers the retained `CalculationResult`, selected X/Z authority,
  signed candidate/disposition, selected core and retained F1/F2/F3/F4 authority
  states, preserving the v2 calculated member order inside the branch;
- `PreflightPhysicalRejected` covers classification, stable diagnostic code, typed
  terminal-verdict identity/flags and typed engineering evidence only. For the
  short-line evidence type, field order is depth, available active line length,
  minimum required active line length and deficit.

Null union branches are explicit. Outcome names and classification names are canonical
stable strings. `ResultHash` does not hash rendered TXT/PDF bytes, export time,
localized verdict/summary/action text, filenames, destinations, or renderer state.

Result schema v2 makes direct hard-precondition terminal assessment evidence explicit:
when a selected signed geometry exists but a derived hard prerequisite prevents F2/F3
composition, nullable composed-authority fields remain null and the terminal F4 state
is still fingerprinted. Schema v1 never represented that state. Input schema and
`InputHash` are unchanged.

Result schema v3 adds the discriminated completed-outcome envelope. Schema v2 always
assumed a calculation-result snapshot and therefore could not honestly fingerprint a
completed preflight physical rejection. The v3 preflight branch does not fabricate a
calculation result or loads. Input schema `buoycalc-engineering-input/v1`, input member
order, source identity, RunId and calculation-timestamp semantics are unchanged.

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
