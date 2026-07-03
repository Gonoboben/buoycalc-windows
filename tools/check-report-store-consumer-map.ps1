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

$markerPath = "docs/CONTROL_MARK_REPORT_STORE_CONSUMERS_2026-07-03.md"
Assert-FileExists $markerPath

$marker = Read-RepoText $markerPath
Assert-Contains $marker "# Control mark: report store consumers" "Report store consumer map"
Assert-Contains $marker "report-store-consumer-scan" "Report store consumer map"
Assert-Contains $marker "report-store-consumers.txt" "Report store consumer map"
Assert-Contains $marker "MooringShapeStore" "Report store consumer map"
Assert-Contains $marker "MooringIterativeSolverStore" "Report store consumer map"
Assert-Contains $marker "Total references: 8" "Report store consumer map"
Assert-Contains $marker "Write references: 2" "Report store consumer map"
Assert-Contains $marker "Explicit reads: 3" "Report store consumer map"
Assert-Contains $marker "Total references: 2" "Report store consumer map"
Assert-Contains $marker "Write references: 1" "Report store consumer map"
Assert-Contains $marker "Explicit reads: 0" "Report store consumer map"
Assert-Contains $marker "TechnicalReportData -> TechnicalReportStorePublisher -> MooringShapeStore" "Report store consumer map"
Assert-Contains $marker "TechnicalReportData -> TechnicalReportStorePublisher -> MooringIterativeSolverStore" "Report store consumer map"
Assert-Contains $marker "MooringPrimaryShapeSelectionStore.Set(selection)" "Report store consumer map"
Assert-Contains $marker "MooringShapeStore.Set(selection.Shape)" "Report store consumer map"
Assert-Contains $marker "SelectedShapeStore" "Report store consumer map"
Assert-Contains $marker "parse Markdown to recover engineering state" "Report store consumer map"
Assert-Contains $marker "No solver physics changes are allowed in this architecture-stabilization phase." "Report store consumer map"

Assert-FileExists "Services/TechnicalReportStorePublisher.cs"
Assert-FileExists "Services/MooringShapeSolver.cs"
Assert-FileExists "Services/MooringIterativeSolver.cs"
Assert-FileExists "Services/SelectedShapeStore.cs"

$storePublisher = Read-RepoText "Services/TechnicalReportStorePublisher.cs"
Assert-Contains $storePublisher "public static void Publish(TechnicalReportData data)" "TechnicalReportStorePublisher"
Assert-Contains $storePublisher "MooringShapeStore.Set(data.Shape);" "TechnicalReportStorePublisher"
Assert-Contains $storePublisher "MooringIterativeSolverStore.Set(data.IterativeSolver);" "TechnicalReportStorePublisher"

$shapeSolver = Read-RepoText "Services/MooringShapeSolver.cs"
Assert-Contains $shapeSolver "public static class MooringShapeStore" "MooringShapeSolver"
Assert-Contains $shapeSolver "public static MooringShapeResult? Current { get; private set; }" "MooringShapeSolver"
Assert-Contains $shapeSolver "public static void Set(MooringShapeResult shape)" "MooringShapeSolver"

$iterativeSolver = Read-RepoText "Services/MooringIterativeSolver.cs"
Assert-Contains $iterativeSolver "public static class MooringIterativeSolverStore" "MooringIterativeSolver"
Assert-Contains $iterativeSolver "public static MooringIterativeSolverResult? Current { get; private set; }" "MooringIterativeSolver"
Assert-Contains $iterativeSolver "var fallbackShape = MooringShapeStore.Current;" "MooringIterativeSolver"
Assert-Contains $iterativeSolver "MooringPrimaryShapeSelector.Select(fallbackShape, result)" "MooringIterativeSolver"
Assert-Contains $iterativeSolver "MooringPrimaryShapeSelectionStore.Set(selection);" "MooringIterativeSolver"
Assert-Contains $iterativeSolver "MooringShapeStore.Set(selection.Shape);" "MooringIterativeSolver"

$selectedShapeStore = Read-RepoText "Services/SelectedShapeStore.cs"
Assert-Contains $selectedShapeStore "MooringPrimaryShapeSelectionStore.Current" "SelectedShapeStore"
Assert-Contains $selectedShapeStore "var fallbackShape = MooringShapeStore.Current;" "SelectedShapeStore"
Assert-Contains $selectedShapeStore "MooringShapeStore.Current" "SelectedShapeStore"

Write-Host "Report store consumer map smoke check passed."
