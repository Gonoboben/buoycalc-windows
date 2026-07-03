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

$markerPath = "docs/CONTROL_MARK_SELECTED_SHAPE_CONSUMERS_2026-07-03.md"
Assert-FileExists $markerPath

$marker = Read-RepoText $markerPath
Assert-Contains $marker "# Control mark: selected shape consumers" "Selected shape consumer map"
Assert-Contains $marker "selected-shape-consumer-scan" "Selected shape consumer map"
Assert-Contains $marker "selected-shape-consumers.txt" "Selected shape consumer map"
Assert-Contains $marker "SelectedShapeStore" "Selected shape consumer map"
Assert-Contains $marker "Reference count: 4" "Selected shape consumer map"
Assert-Contains $marker "MooringPrimaryShapeSelectionStore" "Selected shape consumer map"
Assert-Contains $marker "Reference count: 5" "Selected shape consumer map"
Assert-Contains $marker "MooringShapeStore.Current" "Selected shape consumer map"
Assert-Contains $marker "Reference count: 3" "Selected shape consumer map"
Assert-Contains $marker "PdfReportBuilder.cs:92: var selectedShape = SelectedShapeStore.Current;" "Selected shape consumer map"
Assert-Contains $marker "Mooring2DCanvas.cs:67: var selectedShape = SelectedShapeStore.Current;" "Selected shape consumer map"
Assert-Contains $marker "PDF renderer / PDF diagram source selection" "Selected shape consumer map"
Assert-Contains $marker "2D renderer / engineering shape drawing" "Selected shape consumer map"
Assert-Contains $marker "MooringPrimaryShapeSelectionStore.Current" "Selected shape consumer map"
Assert-Contains $marker "fallback MooringShapeStore.Current" "Selected shape consumer map"
Assert-Contains $marker "PDF and 2D are already the first consumers using" "Selected shape consumer map"
Assert-Contains $marker "No solver physics changes are allowed in this architecture-stabilization phase." "Selected shape consumer map"

Assert-FileExists "Services/PdfReportBuilder.cs"
Assert-FileExists "Services/PdfDiagramSourceSelector.cs"
Assert-FileExists "Views/Mooring2DCanvas.cs"
Assert-FileExists "Services/SelectedShapeStore.cs"
Assert-FileExists "Services/MooringIterativeSolver.cs"
Assert-FileExists "Services/MooringPrimaryShapeGate.cs"

$pdfReportBuilder = Read-RepoText "Services/PdfReportBuilder.cs"
Assert-Contains $pdfReportBuilder "var diagramSource = PdfDiagramSourceSelector.Select(reportText, visualizationOffsetM);" "PdfReportBuilder"

$pdfDiagramSourceSelector = Read-RepoText "Services/PdfDiagramSourceSelector.cs"
Assert-Contains $pdfDiagramSourceSelector "var selectedShape = SelectedShapeStore.Current;" "PdfDiagramSourceSelector"
Assert-Contains $pdfDiagramSourceSelector "new PdfDiagramSource(alternativeShape, selectedShape" "PdfDiagramSourceSelector"
Assert-Contains $pdfDiagramSourceSelector "TryReadReportMetric(reportText" "PdfDiagramSourceSelector"

$canvas = Read-RepoText "Views/Mooring2DCanvas.cs"
Assert-Contains $canvas "var selectedShape = SelectedShapeStore.Current;" "Mooring2DCanvas"
Assert-Contains $canvas "if (selectedShape is not null && shape is { Nodes.Count: >= 2 })" "Mooring2DCanvas"
Assert-Contains $canvas "DrawEngineeringComparison(context, selectedShape" "Mooring2DCanvas"

$selectedShapeStore = Read-RepoText "Services/SelectedShapeStore.cs"
Assert-Contains $selectedShapeStore "public static class SelectedShapeStore" "SelectedShapeStore"
Assert-Contains $selectedShapeStore "public static SelectedShapeReadModel? Current => BuildCurrent();" "SelectedShapeStore"
Assert-Contains $selectedShapeStore "var selection = MooringPrimaryShapeSelectionStore.Current;" "SelectedShapeStore"
Assert-Contains $selectedShapeStore "var fallbackShape = MooringShapeStore.Current;" "SelectedShapeStore"
Assert-Contains $selectedShapeStore "Форма выбрана без gate selection" "SelectedShapeStore"

$iterativeSolver = Read-RepoText "Services/MooringIterativeSolver.cs"
Assert-Contains $iterativeSolver "MooringPrimaryShapeSelectionStore.Set(selection);" "MooringIterativeSolver"
Assert-Contains $iterativeSolver "MooringShapeStore.Set(selection.Shape);" "MooringIterativeSolver"

$primaryShapeGate = Read-RepoText "Services/MooringPrimaryShapeGate.cs"
Assert-Contains $primaryShapeGate "public static class MooringPrimaryShapeSelectionStore" "MooringPrimaryShapeGate"
Assert-Contains $primaryShapeGate "public static MooringPrimaryShapeSelectionResult? Current { get; private set; }" "MooringPrimaryShapeGate"

Write-Host "Selected shape consumer map smoke check passed."
