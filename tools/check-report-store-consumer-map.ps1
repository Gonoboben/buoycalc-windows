$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Get-RepoPath([string]$relativePath) { Join-Path $repoRoot $relativePath }
function Read-RepoText([string]$relativePath) {
    $path = Get-RepoPath $relativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Required file is missing: $relativePath" }
    Get-Content -LiteralPath $path -Raw
}
function Assert-FileExists([string]$relativePath) {
    if (-not (Test-Path -LiteralPath (Get-RepoPath $relativePath))) { throw "Required file is missing: $relativePath" }
}
function Assert-FileMissing([string]$relativePath) {
    if (Test-Path -LiteralPath (Get-RepoPath $relativePath)) { throw "Retired file must remain absent: $relativePath" }
}
function Assert-Contains([string]$content, [string]$needle, [string]$label) {
    if (-not $content.Contains($needle)) { throw "$label does not contain required text: $needle" }
}
function Assert-NotContains([string]$content, [string]$needle, [string]$label) {
    if ($content.Contains($needle)) { throw "$label contains retired text: $needle" }
}

Assert-FileExists "docs/CONTROL_MARK_MUTABLE_SHAPE_STORE_RETIREMENT_BOUNDARY_2026-08-10.md"
Assert-FileExists "ApplicationModel/CalculationSnapshot.cs"
Assert-FileMissing "Services/TechnicalReportStorePublisher.cs"
Assert-FileMissing "Services/MooringAlternativeShapeStore.cs"
Assert-FileExists "Services/MooringShapeSolver.cs"
Assert-FileExists "Services/MooringIterativeSolver.cs"
Assert-FileExists "Services/MooringPrimaryShapeGate.cs"
Assert-FileExists "validation/BuoyCalc.EngineeringRegression/Program.cs"

$snapshot = Read-RepoText "ApplicationModel/CalculationSnapshot.cs"
Assert-Contains $snapshot "return Build(environment, null, result);" "CalculationSnapshot compatibility overload"
Assert-Contains $snapshot "var data = TechnicalReportDataBuilder.Build(environment, buoy, result);" "CalculationSnapshotBuilder"
Assert-Contains $snapshot "SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver)" "CalculationSnapshotBuilder"
Assert-NotContains $snapshot "TechnicalReportStorePublisher" "CalculationSnapshotBuilder"

$shapeSolver = Read-RepoText "Services/MooringShapeSolver.cs"
Assert-Contains $shapeSolver "public static class MooringShapeSolver" "MooringShapeSolver"
Assert-Contains $shapeSolver "public sealed record MooringShapeResult(" "MooringShapeSolver"
Assert-NotContains $shapeSolver "public static class MooringShapeStore" "MooringShapeSolver"
Assert-NotContains $shapeSolver "MooringShapeStore." "MooringShapeSolver"

$iterativeSolver = Read-RepoText "Services/MooringIterativeSolver.cs"
Assert-Contains $iterativeSolver "public static class MooringIterativeSolver" "MooringIterativeSolver"
Assert-Contains $iterativeSolver "public sealed record MooringIterativeSolverResult(" "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "public static class MooringIterativeSolverStore" "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "MooringIterativeSolverStore." "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "MooringShapeStore." "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "MooringPrimaryShapeSelectionStore." "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "MooringAlternativeShapeStore." "MooringIterativeSolver"

$primaryShapeGate = Read-RepoText "Services/MooringPrimaryShapeGate.cs"
Assert-Contains $primaryShapeGate "public static class MooringPrimaryShapeGate" "MooringPrimaryShapeGate"
Assert-Contains $primaryShapeGate "public static class MooringPrimaryShapeSelector" "MooringPrimaryShapeGate"
Assert-NotContains $primaryShapeGate "public static class MooringPrimaryShapeSelectionStore" "MooringPrimaryShapeGate"
Assert-NotContains $primaryShapeGate "MooringPrimaryShapeSelectionStore." "MooringPrimaryShapeGate"

$regression = Read-RepoText "validation/BuoyCalc.EngineeringRegression/Program.cs"
Assert-NotContains $regression "MooringAlternativeShapeStore." "Engineering regression harness"
Assert-NotContains $regression "MooringShapeStore." "Engineering regression harness"
Assert-NotContains $regression "MooringIterativeSolverStore." "Engineering regression harness"
Assert-NotContains $regression "MooringPrimaryShapeSelectionStore." "Engineering regression harness"

Write-Host "Mutable shape-store retirement boundary smoke check passed."
