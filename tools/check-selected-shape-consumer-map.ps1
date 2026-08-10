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

$markerPath = "docs/CONTROL_MARK_SELECTED_SHAPE_CONSUMERS_2026-07-03.md"
$retirementMarkerPath = "docs/CONTROL_MARK_MUTABLE_SHAPE_STORE_RETIREMENT_BOUNDARY_2026-08-10.md"
Assert-FileExists $markerPath
Assert-FileExists $retirementMarkerPath

$marker = Read-RepoText $markerPath
Assert-Contains $marker "# Control mark: selected shape consumers" "Selected shape consumer historical marker"
Assert-Contains $marker "No solver physics changes are allowed in this architecture-stabilization phase." "Selected shape consumer historical marker"

Assert-FileExists "Services/PdfReportBuilder.cs"
Assert-FileExists "Services/PdfDiagramSourceSelector.cs"
Assert-FileExists "Services/Mooring2DDiagramSourceSelector.cs"
Assert-FileExists "Views/Mooring2DCanvas.cs"
Assert-FileExists "Views/MainWindow.axaml.cs"
Assert-FileExists "ViewModels/MainWindowCalculationDisplayBuilder.cs"
Assert-FileExists "ViewModels/MainWindowViewModel.cs"
Assert-FileExists "Services/SelectedShapeReadModel.cs"
Assert-FileMissing "Services/SelectedShapeStore.cs"
Assert-FileExists "Services/MooringIterativeSolver.cs"
Assert-FileExists "Services/MooringPrimaryShapeGate.cs"
Assert-FileExists "ApplicationModel/SelectedMooringShapeProvider.cs"

$selectedShapeReadModel = Read-RepoText "Services/SelectedShapeReadModel.cs"
Assert-Contains $selectedShapeReadModel "public sealed record SelectedShapeReadModel(" "SelectedShapeReadModel"
Assert-Contains $selectedShapeReadModel "MooringShapeResult Shape," "SelectedShapeReadModel"
Assert-Contains $selectedShapeReadModel "bool UsesDiscreteLoads," "SelectedShapeReadModel"
Assert-Contains $selectedShapeReadModel "MooringPrimaryShapeGateDecision? GateDecision," "SelectedShapeReadModel"

$calculationDisplayBuilder = Read-RepoText "ViewModels/MainWindowCalculationDisplayBuilder.cs"
Assert-Contains $calculationDisplayBuilder "snapshot.SelectedShape," "MainWindowCalculationDisplayBuilder"

$mainWindowViewModel = Read-RepoText "ViewModels/MainWindowViewModel.cs"
Assert-Contains $mainWindowViewModel "public SelectedShapeReadModel? SelectedShape" "MainWindowViewModel"
Assert-Contains $mainWindowViewModel "SelectedShape = display.SelectedShape;" "MainWindowViewModel"
Assert-NotContains $mainWindowViewModel "SelectedShapeStore" "MainWindowViewModel"

$pdfReportBuilder = Read-RepoText "Services/PdfReportBuilder.cs"
Assert-Contains $pdfReportBuilder "SelectedShapeReadModel? selectedShape" "PdfReportBuilder"
Assert-NotContains $pdfReportBuilder "SelectedShapeStore" "PdfReportBuilder"

$pdfDiagramSourceSelector = Read-RepoText "Services/PdfDiagramSourceSelector.cs"
Assert-Contains $pdfDiagramSourceSelector "Select(SelectedShapeReadModel? selectedShape)" "PdfDiagramSourceSelector"
Assert-NotContains $pdfDiagramSourceSelector "SelectedShapeStore" "PdfDiagramSourceSelector"

$canvas = Read-RepoText "Views/Mooring2DCanvas.cs"
Assert-Contains $canvas "var diagramSource = Mooring2DDiagramSourceSelector.Select(vm?.SelectedShape);" "Mooring2DCanvas"
Assert-Contains $canvas "var xScale = zScale;" "Mooring2DCanvas"
Assert-NotContains $canvas "SelectedShapeStore" "Mooring2DCanvas"

$diagramSourceSelector = Read-RepoText "Services/Mooring2DDiagramSourceSelector.cs"
Assert-Contains $diagramSourceSelector "Select(SelectedShapeReadModel? selectedShape)" "Mooring2DDiagramSourceSelector"
Assert-NotContains $diagramSourceSelector "SelectedShapeStore" "Mooring2DDiagramSourceSelector"

$iterativeSolver = Read-RepoText "Services/MooringIterativeSolver.cs"
Assert-Contains $iterativeSolver "public static class MooringIterativeSolver" "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "MooringIterativeSolverStore" "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "MooringShapeStore.Current" "MooringIterativeSolver"
Assert-NotContains $iterativeSolver "MooringPrimaryShapeSelectionStore.Set" "MooringIterativeSolver"

# Final two compatibility stores remain for the next micro-package.
$primaryShapeGate = Read-RepoText "Services/MooringPrimaryShapeGate.cs"
Assert-Contains $primaryShapeGate "public static class MooringPrimaryShapeSelectionStore" "MooringPrimaryShapeGate"

$shapeSolver = Read-RepoText "Services/MooringShapeSolver.cs"
Assert-Contains $shapeSolver "public static class MooringShapeStore" "MooringShapeSolver"

$selectedShapeProvider = Read-RepoText "ApplicationModel/SelectedMooringShapeProvider.cs"
Assert-Contains $selectedShapeProvider "MooringPrimaryShapeSelector.Select(fallbackShape, iterativeSolver)" "SelectedMooringShapeProvider"
Assert-NotContains $selectedShapeProvider "SelectedShapeStore" "SelectedMooringShapeProvider"
Assert-NotContains $selectedShapeProvider "MooringIterativeSolverStore" "SelectedMooringShapeProvider"
Assert-NotContains $selectedShapeProvider "MooringShapeStore" "SelectedMooringShapeProvider"
Assert-NotContains $selectedShapeProvider "MooringPrimaryShapeSelectionStore" "SelectedMooringShapeProvider"

Write-Host "Selected shape consumer map smoke check passed."
