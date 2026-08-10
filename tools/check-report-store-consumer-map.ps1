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
Assert-Contains $historicalMarker "TechnicalReportStorePublisher" "Historical report store consumer map"

$retirementMarker = Read-RepoText $retirementMarkerPath
Assert-Contains $retirementMarker "# Control mark: mutable shape-store retirement boundary" "Shape-store retirement marker"
Assert-Contains $retirementMarker "TechnicalReportStorePublisher.Publish(data)" "Shape-store retirement marker"
Assert-Contains $retirementMarker "SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver)" "Shape-store retirement marker"

Assert-FileExists "ApplicationModel/CalculationSnapshot.cs"
Assert-FileMissing "Services/TechnicalReportStorePublisher.cs"
Assert-FileExists "Services/MooringShapeSolver.cs"
Assert-FileExists "Services/MooringIterativeSolver.cs"
Assert-FileExists "Services/MooringPrimaryShapeGate.cs"

$calculationSnapshot = Read-RepoText "ApplicationModel/CalculationSnapshot.cs"
Assert-Contains $calculationSnapshot "var data = TechnicalReportDataBuilder.Build(environment, result);" "CalculationSnapshotBuilder"
Assert-Contains $calculationSnapshot "var selectedShape = SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver);" "CalculationSnapshotBuilder"
Assert-NotContains $calculationSnapshot "TechnicalReportStorePublisher" "CalculationSnapshotBuilder"
Assert-NotContains $calculationSnapshot "Publish(data)" "CalculationSnapshotBuilder"

# Remaining compatibility store classes are intentionally retained for the next small PR.
# This guard proves only that new calculation snapshots no longer publish into them.
$shapeSolver = Read-RepoText "Services/MooringShapeSolver.cs"
Assert-Contains $shapeSolver "public static class MooringShapeStore" "MooringShapeSolver"

$iterativeSolver = Read-RepoText "Services/MooringIterativeSolver.cs"
Assert-Contains $iterativeSolver "public static class MooringIterativeSolverStore" "MooringIterativeSolver"
Assert-Contains $iterativeSolver "MooringPrimaryShapeSelectionStore.Set(selection);" "MooringIterativeSolver"
Assert-Contains $iterativeSolver "MooringShapeStore.Set(selection.Shape);" "MooringIterativeSolver"

$primaryShapeGate = Read-RepoText "Services/MooringPrimaryShapeGate.cs"
Assert-Contains $primaryShapeGate "public static class MooringPrimaryShapeSelectionStore" "MooringPrimaryShapeGate"

Write-Host "Report store publication retirement boundary smoke check passed."
