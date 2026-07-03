$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Get-RepoPath([string]$relativePath) {
    return Join-Path $repoRoot $relativePath
}

function Read-RepoText([string]$relativePath) {
    $path = Get-RepoPath $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required file is missing: $relativePath"
    }

    return Get-Content -LiteralPath $path -Raw
}

function Assert-FileExists([string]$relativePath) {
    if (-not (Test-Path -LiteralPath (Get-RepoPath $relativePath))) {
        throw "Required file is missing: $relativePath"
    }
}

function Assert-Contains([string]$content, [string]$needle, [string]$label) {
    if (-not $content.Contains($needle)) {
        throw "$label does not contain required text: $needle"
    }
}

$markerPath = "docs/CONTROL_MARK_PDF_DIAGRAM_SOURCE_ORDER_2026-07-03.md"
Assert-FileExists $markerPath

$marker = Read-RepoText $markerPath
Assert-Contains $marker "# Control mark: PDF diagram source order" "PDF diagram source order marker"
Assert-Contains $marker "PdfReportBuilder.Build(...)" "PDF diagram source order marker"
Assert-Contains $marker "SelectDiagramSource(reportText, visualizationOffsetM)" "PDF diagram source order marker"
Assert-Contains $marker "NormalizeResultText(resultText, diagramSource.ShapeOffsetM)" "PDF diagram source order marker"
Assert-Contains $marker "1. MooringAlternativeShapeStore.Current" "PDF diagram source order marker"
Assert-Contains $marker "2. SelectedShapeStore.Current" "PDF diagram source order marker"
Assert-Contains $marker "3. TryReadReportMetric(reportText, \"- Снос формы X/Z:\")" "PDF diagram source order marker"
Assert-Contains $marker "4. TryReadReportMetric(reportText, \"- Горизонтальный снос по узлам X/Z:\")" "PDF diagram source order marker"
Assert-Contains $marker "5. visualizationOffsetM" "PDF diagram source order marker"
Assert-Contains $marker "PDF should move toward explicit renderer-facing/read-model input." "PDF diagram source order marker"
Assert-Contains $marker "PDF reportText parsing should not be expanded." "PDF diagram source order marker"
Assert-Contains $marker "No solver physics changes are allowed in this architecture-stabilization phase." "PDF diagram source order marker"

Assert-FileExists "Services/PdfReportBuilder.cs"
Assert-FileExists "Services/MooringAlternativeShapeStore.cs"
Assert-FileExists "Services/SelectedShapeStore.cs"

$pdfReportBuilder = Read-RepoText "Services/PdfReportBuilder.cs"
Assert-Contains $pdfReportBuilder "private static PdfDiagramSource SelectDiagramSource(string reportText, double visualizationOffsetM)" "PdfReportBuilder"
Assert-Contains $pdfReportBuilder "var alternativeShape = MooringAlternativeShapeStore.Current;" "PdfReportBuilder"
Assert-Contains $pdfReportBuilder "var selectedShape = SelectedShapeStore.Current;" "PdfReportBuilder"
Assert-Contains $pdfReportBuilder "var hasAlternativeShape = alternativeShape is not null && alternativeShape.Shape.Rows.Count >= 2;" "PdfReportBuilder"
Assert-Contains $pdfReportBuilder "? alternativeShape!.Shape.DiscreteHorizontalOffsetM" "PdfReportBuilder"
Assert-Contains $pdfReportBuilder ": selectedShape?.Shape.HorizontalOffsetM" "PdfReportBuilder"
Assert-Contains $pdfReportBuilder "?? TryReadReportMetric(reportText, \"- Снос формы X/Z:\")" "PdfReportBuilder"
Assert-Contains $pdfReportBuilder "?? TryReadReportMetric(reportText, \"- Горизонтальный снос по узлам X/Z:\")" "PdfReportBuilder"
Assert-Contains $pdfReportBuilder "?? visualizationOffsetM;" "PdfReportBuilder"
Assert-Contains $pdfReportBuilder "return new PdfDiagramSource(alternativeShape, selectedShape, hasAlternativeShape, selectedShape is not null, shapeOffsetM);" "PdfReportBuilder"

$alternativeShapeStore = Read-RepoText "Services/MooringAlternativeShapeStore.cs"
Assert-Contains $alternativeShapeStore "public static class MooringAlternativeShapeStore" "MooringAlternativeShapeStore"
Assert-Contains $alternativeShapeStore "public static MooringAlternativeShapeReadModel? Current" "MooringAlternativeShapeStore"

$selectedShapeStore = Read-RepoText "Services/SelectedShapeStore.cs"
Assert-Contains $selectedShapeStore "public static class SelectedShapeStore" "SelectedShapeStore"
Assert-Contains $selectedShapeStore "public static SelectedShapeReadModel? Current" "SelectedShapeStore"

Write-Host "PDF diagram source order smoke check passed."
