# BuoyCalc Windows v0.45

Windows branch of BuoyCalc on C# + Avalonia.

## Status

v0.45 completes the planned PDF report structure step. The calculation model, solver, 2D coordinates and PDF diagrams were not changed. A dedicated PDF report structure block is now injected into the PDF export text stream before the full engineering report.

The old `MooringShapeSolver` remains available as fallback.

## Version plan

```text
v0.39   - iterative solver skeleton
v0.39.1 - solver convergence criteria
v0.39.2 - iteration report
v0.39.3 - gate before candidate shape can become primary
v0.40   - discrete loads in the primary solver path
v0.40.1 - primary shape selection report
v0.40.2 - primary shape selection table in report
v0.41   - deployment modes: surface/submerged/short/excess line
v0.42   - test scenarios and autochecks
v0.43   - element database improvements
v0.44   - sequence editor UX
v0.45   - final PDF report structure
v0.46   - release build preparation
```

## What was added in v0.45

1. New service:

```text
Services/PdfReportStructureGuide.cs
```

2. The service adds the section:

```text
## PDF report structure v0.45
```

3. The section defines the PDF order:

```text
1. Result summary
2. Solver 2D scheme
3. Shape comparison
4. Element table
5. Full engineering report
6. Diagnostics and limitations
```

4. The structure guide states the source of truth for each part:

```text
CalculationResult and EngineeringDiagnostics
MooringShapeStore / MooringShapeSolver output
main shape plus alternative discrete-load shape
CalculationResult.ElementRows
ReportBuilder markdown
MethodNote fields from calculation services
```

5. `Views/MainWindow.axaml.cs` now applies the structure only during PDF export:

```text
var pdfReportText = PdfReportStructureGuide.Apply(viewModel.ReportText);
PdfReportBuilder.Build(..., pdfReportText, ...);
```

6. The regular text report in the UI remains unchanged. The structure block is PDF-export specific.

7. `AppInfo` display version is now:

```text
v0.45 - PDF report structure
```

## What did not change

1. Physics did not change.
2. Solver did not change.
3. X/Z coordinates did not change.
4. PDF diagrams still use model outputs from `MooringShapeStore` and related stores.
5. No coordinates are invented in UI or PDF.
6. Release build preparation remains v0.46.

## How to check

1. Pull the project locally:

```text
Git -> Pull
Build -> Clean Solution
Build -> Rebuild Solution
```

2. Run a calculation.
3. Export PDF.
4. In the full text report part of the PDF, find:

```text
PDF report structure v0.45
```

5. Confirm the PDF still contains:

```text
Result summary
Solver 2D scheme
Element table
Full engineering report
```

6. Check CI status:

```text
BuoyCalc Windows Build: success
```

## Next safe step

If v0.45 passes CI, the next planned step is v0.46: release build preparation.
