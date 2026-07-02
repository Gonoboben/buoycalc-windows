# 2026-07-02 — ReportBuilder usage check in CI

Status: done in branch `step-42-ci-usage-check`.

Changed:

```text
.github/workflows/dotnet-build.yml
```

What changed:

```text
The existing .NET Build workflow now runs tools/check-reportbuilder-usage.ps1 after dotnet build and before publishing the success commit status.
```

Why:

```text
ReportBuilder cleanup must not rely on GitHub code search indexing.
The repository-local usage check is now part of CI before any future legacy cleanup.
```

Expected behavior:

```text
If external ReportBuilder references are found, CI fails.
If only Services/ReportBuilder.cs references ReportBuilder, CI continues successfully.
```

No application production code was changed.

Next allowed step:

```text
if CI passes, use the CI-backed usage check result before any ReportBuilder cleanup
```

CI status:

```text
pending PR check
```
