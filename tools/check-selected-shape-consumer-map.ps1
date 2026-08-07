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

function Assert-NotContains([string]$content, [string]$needle, [string]$label) {
    if ($content.Contains($needle)) {
        throw "$label contains forbidden text: $needle"
    }
}

$markerPath = "docs/CONTROL_MARK_SELECTED_SHAPE_CONSUMERS_2026-07-03.md"
Assert-FileExists $markerPath

$marker = Read-RepoText $markerPath
Assert-Contains $marker "# Control mark: selected shape consumers" "Selected shape consumer historical marker"
Assert-Contains $marker "selected-shape-consumer-scan" "Selected shape consumer historical marker"
Assert-Contains $marker "selected-shape-consumers.txt" "Selected shape consumer historical marker"
Assert-Contains $marker "No solver physics changes are allowed in this architecture-stabilization phase." "Selected shape consumer historical marker"

Assert-FileExists "Services/PdfReportBuilder.cs"
Assert-FileExists "Services/PdfDiagramSourceSelector.cs"
Assert-FileExists "Services/Mooring2DDiagramSourceSelector.cs"
Assert-FileExists "Views/Mooring2DCanvas.cs"
Assert-FileExists "Views/MainWindow.axaml.cs"
Assert-FileExists "ViewModels/MainWindowCalculationDisplayBuilder.cs"
Assert-FileExists "ViewModels/MainWindowViewModel.cs"
Assert-FileExists "Services/SelectedShapeStore.cs"
Assert-FileExists "Services/MooringIterativeSolver.cs"
Assert-FileExists "Services/MooringPrimaryShapeGate.cs"
Assert-FileExists "ApplicationModel/SelectedMooringShapeProvider.cs"

$calculationDisplayBuilder = Read-RepoText "ViewModels/MainWindowCalculationDisplayBuilder.cs"
Assert-Contains $calculationDisplayBuilder "SelectedShapeReadModel? SelectedShape," "MainWindowCalculationDisplay"
Assert-Contains $calculationDisplayBuilder "snapshot.SelectedShape," "MainWindowCalculationDisplayBuilder"

$mainWindowViewModel = Read-RepoText "ViewModels/MainWindowViewModel.cs"
Assert-Contains $mainWindowViewModel "public SelectedShapeReadModel? SelectedShape" "MainWindowViewModel"
Assert-Contains $mainWindowViewModel "SelectedShape = display.SelectedShape;" "MainWindowViewModel"
Assert-Contains $mainWindowViewModel "SelectedShape = null;" "MainWindowViewModel"
Assert-NotContains $mainWindowViewModel "SelectedShapeStore.Current" "MainWindowViewModel"

$pdfReportBuilder = Read-RepoText "Services/PdfReportBuilder.cs"
Assert-Contains $pdfReportBuilder "SelectedShapeReadModel? selectedShape" "PdfReportBuilder"
Assert-Contains $pdfReportBuilder "var diagramSource = PdfDiagramSourceSelector.Select(selectedShape);" "PdfReportBuilder"
Assert-Contains $pdfReportBuilder "writer.SelectedShapeDiagram(diagramSource.SelectedShape!);" "PdfReportBuilder"
Assert-Contains $pdfReportBuilder "public void SelectedShapeDiagram(SelectedShapeReadModel selectedShape)" "PdfReportBuilder"
Assert-Contains $pdfReportBuilder "var shape = selectedShape.Shape;" "PdfReportBuilder"
Assert-Contains $pdfReportBuilder "shape.Nodes" "PdfReportBuilder"
Assert-Contains $pdfReportBuilder "shape.HorizontalOffsetM" "PdfReportBuilder"
Assert-Contains $pdfReportBuilder "выбранная расчётная форма X/Z" "PdfReportBuilder"
Assert-NotContains $pdfReportBuilder "SelectedShapeStore.Current" "PdfReportBuilder"
Assert-NotContains $pdfReportBuilder "AlternativeShapeDiagram" "PdfReportBuilder"
Assert-NotContains $pdfReportBuilder "MooringAlternativeShapeDisplayData" "PdfReportBuilder"

$pdfDiagramSourceSelector = Read-RepoText "Services/PdfDiagramSourceSelector.cs"
Assert-Contains $pdfDiagramSourceSelector "Select(SelectedShapeReadModel? selectedShape)" "PdfDiagramSourceSelector"
Assert-Contains $pdfDiagramSourceSelector "selectedShape is not null && selectedShape.Shape.Nodes.Count >= 2" "PdfDiagramSourceSelector"
Assert-Contains $pdfDiagramSourceSelector "selectedShape!.Shape.HorizontalOffsetM" "PdfDiagramSourceSelector"
Assert-Contains $pdfDiagramSourceSelector "new PdfDiagramSource(selectedShape, hasSelectedShape, shapeOffsetM)" "PdfDiagramSourceSelector"
Assert-NotContains $pdfDiagramSourceSelector "SelectedShapeStore.Current" "PdfDiagramSourceSelector"
Assert-NotContains $pdfDiagramSourceSelector "MooringAlternativeShapeStore" "PdfDiagramSourceSelector"
Assert-NotContains $pdfDiagramSourceSelector "TryReadReportMetric" "PdfDiagramSourceSelector"
Assert-NotContains $pdfDiagramSourceSelector "reportText" "PdfDiagramSourceSelector"
Assert-NotContains $pdfDiagramSourceSelector "visualizationOffsetM" "PdfDiagramSourceSelector"

$mainWindow = Read-RepoText "Views/MainWindow.axaml.cs"
Assert-Contains $mainWindow "viewModel.SelectedShape);" "MainWindow PDF export"

$canvas = Read-RepoText "Views/Mooring2DCanvas.cs"
Assert-Contains $canvas "var diagramSource = Mooring2DDiagramSourceSelector.Select(vm?.SelectedShape);" "Mooring2DCanvas"
Assert-Contains $canvas "DrawSelectedShape(context, selectedShape" "Mooring2DCanvas"
Assert-Contains $canvas "DrawUnavailableState(context" "Mooring2DCanvas"
Assert-Contains $canvas "var xScale = zScale;" "Mooring2DCanvas"
Assert-Contains $canvas "shape.HorizontalOffsetM" "Mooring2DCanvas"
Assert-NotContains $canvas "SelectedShapeStore.Current" "Mooring2DCanvas"
Assert-NotContains $canvas "MooringAlternativeShape" "Mooring2DCanvas"
Assert-NotContains $canvas "DrawEngineeringComparison" "Mooring2DCanvas"
Assert-NotContains $canvas "DrawFallbackLine" "Mooring2DCanvas"
Assert-NotContains $canvas "ParsedNodes" "Mooring2DCanvas"
Assert-NotContains $canvas "ReportText" "Mooring2DCanvas"
Assert-NotContains $canvas "VisualizationOffsetM" "Mooring2DCanvas"
Assert-NotContains $canvas "SequenceDiagramLines" "Mooring2DCanvas"

$diagramSourceSelector = Read-RepoText "Services/Mooring2DDiagramSourceSelector.cs"
Assert-Contains $diagramSourceSelector "Select(SelectedShapeReadModel? selectedShape)" "Mooring2DDiagramSourceSelector"
Assert-Contains $diagramSourceSelector "selectedShape is not null && selectedShape.Shape.Nodes.Count >= 2" "Mooring2DDiagramSourceSelector"
Assert-Contains $diagramSourceSelector "new Mooring2DDiagramSource(selectedShape, hasSelectedShape)" "Mooring2DDiagramSourceSelector"
Assert-NotContains $diagramSourceSelector "SelectedShapeStore.Current" "Mooring2DDiagramSourceSelector"
Assert-NotContains $diagramSourceSelector "MooringAlternativeShapeStore" "Mooring2DDiagramSourceSelector"
Assert-NotContains $diagramSourceSelector "ParseCalculatedNodes" "Mooring2DDiagramSourceSelector"
Assert-NotContains $diagramSourceSelector "reportText" "Mooring2DDiagramSourceSelector"

$selectedShapeStore = Read-RepoText "Services/SelectedShapeStore.cs"
Assert-Contains $selectedShapeStore "public static class SelectedShapeStore" "SelectedShapeStore"
Assert-Contains $selectedShapeStore "public static SelectedShapeReadModel? Current => BuildCurrent();" "SelectedShapeStore"
Assert-Contains $selectedShapeStore "var selection = MooringPrimaryShapeSelectionStore.Current;" "SelectedShapeStore"
Assert-Contains $selectedShapeStore "var fallbackShape = MooringShapeStore.Current;" "SelectedShapeStore"

$iterativeSolver = Read-RepoText "Services/MooringIterativeSolver.cs"
Assert-Contains $iterativeSolver "MooringPrimaryShapeSelectionStore.Set(selection);" "MooringIterativeSolver"
Assert-Contains $iterativeSolver "MooringShapeStore.Set(selection.Shape);" "MooringIterativeSolver"

$primaryShapeGate = Read-RepoText "Services/MooringPrimaryShapeGate.cs"
Assert-Contains $primaryShapeGate "public static class MooringPrimaryShapeSelectionStore" "MooringPrimaryShapeGate"
Assert-Contains $primaryShapeGate "public static MooringPrimaryShapeSelectionResult? Current { get; private set; }" "MooringPrimaryShapeGate"

$selectedShapeProvider = Read-RepoText "ApplicationModel/SelectedMooringShapeProvider.cs"
Assert-Contains $selectedShapeProvider "MooringPrimaryShapeSelector.Select(fallbackShape, iterativeSolver)" "SelectedMooringShapeProvider"
Assert-NotContains $selectedShapeProvider "SelectedShapeStore" "SelectedMooringShapeProvider"

Write-Host "Selected shape consumer map smoke check passed."
