# Series A release decision and recorded findings

BC-AUD-011 identified an evidence collection run that completed successfully
while emitting critical findings. The Series A campaign records observations.
The separate verifier in the Windows build determines whether the selected
authority and finding record meet the checked release criteria.

The original code-based verifier accepted a 33rd finding when the recognized
`IterativeChordExceedsLineLength` occurrence from A06 was copied to the
previously clean A05 record. The source was a deliberately altered copy of
an actual solver artifact for a verifier negative control.

The finding policy identifies each recorded occurrence by scenario, code,
severity and category. The initial policy comes from the actual exact-main
66e4327a3b93fe577725465f4a6368d0aedf3c01 Series A artifact 13261370670.
Its archive SHA-256 is
`9c87f03ca31dfae6079e0596e0b13f0d6438bb90d7e38ddd9b1594119abb9ba4`.
All 32 occurrences remain visible in the raw output and the decision report.

* The 23 geometry findings concern diagnostic fallback or iterative forms.
  The corresponding selected form is either a valid signed form or blocked
  by the terminal physical rejection.
* The nine historical F4 expectations refer to physical rejection. The
  terminal disposition is verified in the actual Validation 601 application
  and PDF paths. F1–F4 stay unavailable for those rows.

The verifier fails on an extra, duplicate, missing or reclassified
occurrence and on invalid selected geometry, including the indeterminate
fallback cases. Any proposed policy update needs the actual new artifact,
the numerical or reporting explanation, and review of the affected
downstream behavior. The canonical engineering regression data remains
separate from this occurrence register.

The gate establishes the disposition of Series A evidence. Release readiness
also requires the other audit contracts, exact-source CI and Windows smoke
in [RELEASE.md](../RELEASE.md).
