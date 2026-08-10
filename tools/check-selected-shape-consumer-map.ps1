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

Assert-FileExists "docs/CONTROL_MARK_SELECTED_SHAPE_CONSUMERS_2026-07-03.md"
Assert-FileExists "docs/CONTROL_MARK_MUTABLE_SHAPE_STORE_RETIREMENT_BOUNDARY_2026-08-10.md"
Assert-FileExists "Services/SelectedShapeReadModel.cs"
Assert-FileMissing "Services/SelectedShapeStore.cs"
Assert-FileExists "Services/MooringShapeSolver.cs"
Assert-FileExists "Services/MooringIterativeSolver.cs"
Assert-FileExists "Services/MooringPrimaryShapeGate.cs"
Assert-FileExists "ApplicationModel/SelectedMooringShapeProvider.cs"
Assert-FileExists "ViewModels/MainWindowCalculationDisplayBuilder.cs"
Assert-FileExists "ViewModels/MainWindowViewModel.cs"
Assert-FileExists "Services/PdfReportBuilder.cs"
Assert-FileExists "Views/Mooring2DCanvas.cs"

$selectedShapeProvider = Read-RepoText "ApplicationModel/SelectedMooringShapeProvider.cs"
Assert-Contains $selectedShapeProvider "MooringPrimaryShapeSelector.Select(fallbackShape, iterativeSolver)" "SelectedMooringShapeProvider"
Assert-NotContains $selectedShapeProvider "SelectedShapeStore." "SelectedMooringShapeProvider"
Assert-NotContains $selectedShapeProvider "MooringShapeStore." "SelectedMooringShapeProvider"
Assert-NotContains $selectedShapeProvider "MooringIterativeSolverStore." "SelectedMooringShapeProvider"
Assert-NotContains $selectedShapeProvider "MooringPrimaryShapeSelectionStore." "SelectedMooringShapeProvider"

$shapeSolver = Read-RepoText "Services/MooringShapeSolver.cs"
Assert-Contains $shapeSolver "public sealed record MooringShapeResult(" "MooringShapeSolver"
Assert-Contains $shapeSolver "public static class MooringShapeSolver" "MooringShapeSolver"
Assert-NotContains $shapeSolver "public static class MooringShapeStore" "MooringShapeSolver"
Assert-NotContains $shapeSolver "MooringShapeStore." "MooringShapeSolver"

$iterativeSolver = Read-RepoText "Services/MooringIterativeSolver.cs"
Assert-Contains $iterativeSolver "public sealed record MooringIterativeSolverResult(" "MooringIterativeSolver"
Assert-Contains $iterativeSolver "public static class MooringIterativeSolver" "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "public static class MooringIterativeSolverStore" "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "MooringIterativeSolverStore." "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "MooringShapeStore." "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "MooringPrimaryShapeSelectionStore." "MooringIterativeSolver"

$gate = Read-RepoText "Services/MooringPrimaryShapeGate.cs"
Assert-Contains $gate "public static class MooringPrimaryShapeGate" "MooringPrimaryShapeGate"
Assert-Contains $gate "public static class MooringPrimaryShapeSelector" "MooringPrimaryShapeGate"
Assert-NotContains $gate "public static class MooringPrimaryShapeSelectionStore" "MooringPrimaryShapeGate"
Assert-NotContains $gate "MooringPrimaryShapeSelectionStore." "MooringPrimaryShapeGate"

$displayBuilder = Read-RepoText "ViewModels/MainWindowCalculationDisplayBuilder.cs"
Assert-Contains $displayBuilder "snapshot.SelectedShape," "MainWindowCalculationDisplayBuilder"

$viewModel = Read-RepoText "ViewModels/MainWindowViewModel.cs"
Assert-Contains $viewModel "public SelectedShapeReadModel? SelectedShape" "MainWindowViewModel"
Assert-Contains $viewModel "SelectedShape = display.SelectedShape;" "MainWindowViewModel"
Assert-NotContains $viewModel "SelectedShapeStore." "MainWindowViewModel"

$pdf = Read-RepoText "Services/PdfReportBuilder.cs"
Assert-Contains $pdf "SelectedShapeReadModel? selectedShape" "PdfReportBuilder"
Assert-NotContains $pdf "SelectedShapeStore." "PdfReportBuilder"

$canvas = Read-RepoText "Views/Mooring2DCanvas.cs"
Assert-Contains $canvas "Mooring2DDiagramSourceSelector.Select(vm?.SelectedShape)" "Mooring2DCanvas"
Assert-Contains $canvas "var xScale = zScale;" "Mooring2DCanvas"
Assert-NotContains $canvas "SelectedShapeStore." "Mooring2DCanvas"

Write-Host "Selected shape consumer map smoke check passed."
