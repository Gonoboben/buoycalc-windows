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

function Assert-FileMissing([string]$relativePath) {
    if (Test-Path -LiteralPath (Get-RepoPath $relativePath)) {
        throw "Retired file must remain absent: $relativePath"
    }
}

function Assert-Contains([string]$content, [string]$needle, [string]$label) {
    if (-not $content.Contains($needle)) {
        throw "$label does not contain required text: $needle"
    }
}

function Assert-NotContains([string]$content, [string]$needle, [string]$label) {
    if ($content.Contains($needle)) {
        throw "$label contains forbidden text: $needle"
    }
}

$historicalMarkerPath = "docs/CONTROL_MARK_REPORT_STORE_CONSUMERS_2026-07-03.md"
$retirementMarkerPath = "docs/CONTROL_MARK_MUTABLE_SHAPE_STORE_RETIREMENT_BOUNDARY_2026-08-10.md"
Assert-FileExists $historicalMarkerPath
Assert-FileExists $retirementMarkerPath

$historicalMarker = Read-RepoText $historicalMarkerPath
Assert-Contains $historicalMarker "# Control mark: report store consumers" "Historical report store consumer map"

$retirementMarker = Read-RepoText $retirementMarkerPath
Assert-Contains $retirementMarker "# Control mark: mutable shape-store retirement boundary" "Shape-store retirement marker"
Assert-Contains $retirementMarker "MooringIterativeSolverStore" "Shape-store retirement marker"

Assert-FileExists "ApplicationModel/CalculationSnapshot.cs"
Assert-FileMissing "Services/TechnicalReportStorePublisher.cs"
Assert-FileExists "Services/MooringShapeSolver.cs"
Assert-FileExists "Services/MooringIterativeSolver.cs"
Assert-FileExists "Services/MooringPrimaryShapeGate.cs"

$calculationSnapshot = Read-RepoText "ApplicationModel/CalculationSnapshot.cs"
Assert-Contains $calculationSnapshot "var selectedShape = SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver);" "CalculationSnapshotBuilder"
Assert-NotContains $calculationSnapshot "TechnicalReportStorePublisher" "CalculationSnapshotBuilder"

$iterativeSolver = Read-RepoText "Services/MooringIterativeSolver.cs"
Assert-Contains $iterativeSolver "public static class MooringIterativeSolver" "MooringIterativeSolver"
Assert-Contains $iterativeSolver "public static MooringIterativeSolverResult Build(" "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "public static class MooringIterativeSolverStore" "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "MooringIterativeSolverStore" "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "MooringShapeStore.Current" "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "MooringPrimaryShapeSelectionStore.Set" "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "MooringShapeStore.Set(selection.Shape)" "MooringIterativeSolver"

# These two compatibility stores are intentionally retained for one final micro-package.
$shapeSolver = Read-RepoText "Services/MooringShapeSolver.cs"
Assert-Contains $shapeSolver "public static class MooringShapeStore" "MooringShapeSolver"

$primaryShapeGate = Read-RepoText "Services/MooringPrimaryShapeGate.cs"
Assert-Contains $primaryShapeGate "public static class MooringPrimaryShapeSelectionStore" "MooringPrimaryShapeGate"

$regression = Read-RepoText "validation/BuoyCalc.EngineeringRegression/Program.cs"
Assert-NotContains $regression "MooringIterativeSolverStore" "Engineering regression harness"
Assert-Contains $regression "MooringPrimaryShapeSelectionStore.Clear();" "Engineering regression harness"
Assert-Contains $regression "MooringShapeStore.Clear();" "Engineering regression harness"

Write-Host "Iterative solver compatibility store retirement boundary smoke check passed."
