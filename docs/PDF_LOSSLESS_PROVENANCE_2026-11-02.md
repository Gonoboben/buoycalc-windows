# Lossless PDF provenance and signed-candidate status

Issue #575; found during the autonomous release-readiness audit.

Observed in exact-head #635 PDF evidence: an Accepted signed calculation showed
`Signed candidate status: не определён`, because the renderer read status only
from the physical-rejection disposition. Input/result SHA256 and SourceIdentity
were truncated by the generic table's 62-character ellipsis limit. The retained
typed data was complete; the user-visible PDF lost verification information.
Completed physical-rejection diagnostic text also used the truncated table path.

The typed report now retains the actual `snapshot.SignedCandidate.Status` as
metadata. The PDF renders it directly, without deriving physics or acceptance.
Calculated reports reuse the existing preflight report's explicitly numbered
32/48-character identifier chunks so every character remains available. The
provenance table uses 20 pt rows to retain the page layout. Physical diagnostic
code/text render through normal full-width text wrapping rather than ellipsis.

Regression coverage exports two PDFs from each real Accepted, RejectedPhysical,
Indeterminate and BudgetExhausted run. PdfPig extracts both exports and requires
the complete run ID, every input/result/source identifier chunk, actual retained
candidate status and (where applicable) complete physical diagnostic code/text.
The actual PDFs plus expected retained values are uploaded for independent
extraction and visual QA. Export does not mutate run provenance.

No solver, numerical algorithm, F1-F4 source, classification, persisted project
format or frozen baseline changes. An optional init-only report metadata field
preserves existing record constructor call sites. Existing PDF layout/glyph,
preflight, repeated-run and stale-export regressions remain required.

The projected field is `CandidateStatus`; the renderer never reads the raw
snapshot candidate. The existing renderer boundary guard remains unchanged.
