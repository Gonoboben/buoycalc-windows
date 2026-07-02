# 2026-07-02 — legacy ReportBuilder removed

Status: done in branch `step-43-remove-legacy-reportbuilder`.

Changed:

```text
Removed Services/ReportBuilder.cs.
```

Reason:

```text
The technical report path had already been moved to TechnicalReportMarkdownBuilder and dedicated section renderers.
The reflection fallback to ReportBuilder had already been removed.
The CI usage check now guards against external ReportBuilder.Build calls.
```

Safety gates:

```text
- dotnet build must pass
- tools/check-reportbuilder-usage.ps1 must pass in CI
```

What was not changed:

```text
TechnicalReportMarkdownBuilder.cs was not changed.
TechnicalReportBuilder.cs was not changed.
TechnicalReportDataBuilder.cs was not changed.
TechnicalReportData.cs was not changed.
TechnicalReportStorePublisher.cs was not changed.
PDF rendering was not changed.
Solver and calculation physics were not changed.
Report output was not intentionally changed.
```

Next allowed step:

```text
wait for CI and merge only if .NET Build succeeds
```

CI status:

```text
pending PR check
```
