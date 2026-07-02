# 2026-07-02 — ReportBuilder usage script

Status: done in branch `step-41-reportbuilder-usage-script`.

Added:

```text
tools/check-reportbuilder-usage.ps1
```

Purpose:

```text
Run a local repository check for ReportBuilder references before any legacy cleanup.
```

Behavior:

```text
- scans .cs files
- skips bin/ and obj/
- prints all ReportBuilder matches
- exits with failure if any external references outside Services/ReportBuilder.cs are found
```

Why:

```text
GitHub code search returned no ReportBuilder results even though Services/ReportBuilder.cs exists.
Cleanup should therefore use a repository-local usage check instead of relying on GitHub search indexing.
```

No production code was changed.

Next allowed step:

```text
run tools/check-reportbuilder-usage.ps1 and use the result before any ReportBuilder cleanup
```

CI status:

```text
pending PR check
```
