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

function Assert-CSharpDirectoryNotContains([string]$relativeDirectory, [string]$needle, [string]$label) {
    $directory = Get-RepoPath $relativeDirectory
    if (-not (Test-Path -LiteralPath $directory)) {
        throw "Required directory is missing: $relativeDirectory"
    }

    foreach ($file in Get-ChildItem -LiteralPath $directory -Recurse -File -Filter "*.cs") {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        if ($content.Contains($needle)) {
            $relativePath = [System.IO.Path]::GetRelativePath($repoRoot, $file.FullName)
            throw "$label contains forbidden text in ${relativePath}: $needle"
        }
    }
}

$markerPath = "docs/CONTROL_MARK_CALCULATION_RESULT_READ_MODEL_BOUNDARY_2026-07-02.md"
Assert-FileExists $markerPath

$marker = Read-RepoText $markerPath
Assert-Contains $marker "#46" "Read-model boundary marker"
Assert-Contains $marker "CalculationResult" "Read-model boundary marker"
Assert-Contains $marker "solver-facing output" "Read-model boundary marker"
Assert-Contains $marker "renderer-facing/user-facing input" "Read-model boundary marker"
Assert-Contains $marker "explicit read-model builder" "Read-model boundary marker"
Assert-Contains $marker "Markdown / PDF / 2D / UI renderers" "Read-model boundary marker"
Assert-Contains $marker "No solver physics changes are allowed in this phase." "Read-model boundary marker"

Assert-FileExists "ViewModels/MainWindowViewModel.cs"
Assert-FileExists "ViewModels/MainWindowCalculationDisplayBuilder.cs"
Assert-FileExists "Services/TechnicalReportBuilder.cs"
Assert-FileExists "Services/TechnicalReportMarkdownBuilder.cs"
Assert-FileExists "Services/TechnicalReportDataBuilder.cs"
Assert-FileExists "Services/TechnicalReportData.cs"
Assert-FileMissing "Services/TechnicalReportStorePublisher.cs"
Assert-FileExists "ApplicationModel/ApplicationCalculationRunner.cs"
Assert-FileExists "ApplicationModel/CalculationSnapshot.cs"
Assert-FileExists "ApplicationModel/SelectedMooringShapeProvider.cs"

$viewModel = Read-RepoText "ViewModels/MainWindowViewModel.cs"
Assert-Contains $viewModel "using BuoyCalc.Windows.ApplicationModel;" "MainWindowViewModel"
Assert-Contains $viewModel "ApplicationCalculationRunner.Run(" "MainWindowViewModel"
Assert-NotContains $viewModel "BuoyCalculator.Calculate(" "MainWindowViewModel"
Assert-NotContains $viewModel "CalculationSnapshotBuilder.Build(" "MainWindowViewModel"

$displayBuilder = Read-RepoText "ViewModels/MainWindowCalculationDisplayBuilder.cs"
Assert-Contains $displayBuilder "ApplicationCalculationRun run" "MainWindowCalculationDisplayBuilder"
Assert-Contains $displayBuilder "var result = run.Result;" "MainWindowCalculationDisplayBuilder"
Assert-Contains $displayBuilder "var snapshot = run.Snapshot;" "MainWindowCalculationDisplayBuilder"
Assert-Contains $displayBuilder "ReportBuildBoundary.Build(projectName, environment, buoy, anchor, snapshot)" "MainWindowCalculationDisplayBuilder"
Assert-NotContains $displayBuilder "CalculationResult result" "MainWindowCalculationDisplayBuilder"
Assert-NotContains $displayBuilder "CalculationSnapshotBuilder.Build(" "MainWindowCalculationDisplayBuilder"

$applicationRunner = Read-RepoText "ApplicationModel/ApplicationCalculationRunner.cs"
Assert-Contains $applicationRunner "public static ApplicationCalculationRun Run(" "ApplicationCalculationRunner"
Assert-Contains $applicationRunner "var result = BuoyCalculator.Calculate(" "ApplicationCalculationRunner"
Assert-Contains $applicationRunner "var snapshot = CalculationSnapshotBuilder.Build(environment, result);" "ApplicationCalculationRunner"
Assert-Contains $applicationRunner "return new ApplicationCalculationRun(result, snapshot);" "ApplicationCalculationRunner"

Assert-CSharpDirectoryNotContains "ViewModels" "BuoyCalculator.Calculate(" "ViewModel presentation layer"
Assert-CSharpDirectoryNotContains "Views" "BuoyCalculator.Calculate(" "View presentation layer"
Assert-CSharpDirectoryNotContains "ViewModels" "CalculationSnapshotBuilder.Build(" "ViewModel presentation layer"
Assert-CSharpDirectoryNotContains "Views" "CalculationSnapshotBuilder.Build(" "View presentation layer"

$entryBuilder = Read-RepoText "Services/TechnicalReportBuilder.cs"
Assert-Contains $entryBuilder "CalculationSnapshot snapshot" "TechnicalReportBuilder"
Assert-Contains $entryBuilder "return TechnicalReportMarkdownBuilder.Build(projectName, environment, buoy, anchor, snapshot);" "TechnicalReportBuilder"
Assert-NotContains $entryBuilder "CalculationResult result" "TechnicalReportBuilder"

$markdownBuilder = Read-RepoText "Services/TechnicalReportMarkdownBuilder.cs"
Assert-Contains $markdownBuilder "CalculationSnapshot snapshot" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var result = snapshot.Result;" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var data = snapshot.TechnicalReportData;" "TechnicalReportMarkdownBuilder"
Assert-NotContains $markdownBuilder "CalculationSnapshotBuilder.Build" "TechnicalReportMarkdownBuilder"
Assert-NotContains $markdownBuilder "TechnicalReportDataBuilder.Build" "TechnicalReportMarkdownBuilder"
Assert-NotContains $markdownBuilder "TechnicalReportStorePublisher.Publish" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var tensionRows = data.TensionRows;" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var shape = data.Shape;" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var diagnostics = data.Diagnostics;" "TechnicalReportMarkdownBuilder"

$snapshotBoundary = Read-RepoText "ApplicationModel/CalculationSnapshot.cs"
Assert-Contains $snapshotBoundary "public sealed record CalculationSnapshot(" "CalculationSnapshot"
Assert-Contains $snapshotBoundary "CalculationResult Result," "CalculationSnapshot"
Assert-Contains $snapshotBoundary "TechnicalReportData TechnicalReportData," "CalculationSnapshot"
Assert-Contains $snapshotBoundary "SelectedShapeReadModel? SelectedShape);" "CalculationSnapshot"
Assert-Contains $snapshotBoundary "var data = TechnicalReportDataBuilder.Build(environment, result);" "CalculationSnapshotBuilder"
Assert-Contains $snapshotBoundary "var selectedShape = SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver);" "CalculationSnapshotBuilder"
Assert-NotContains $snapshotBoundary "TechnicalReportStorePublisher" "CalculationSnapshotBuilder"
Assert-NotContains $snapshotBoundary "SelectedShapeStore.Current" "CalculationSnapshotBuilder"

$selectedShapeProvider = Read-RepoText "ApplicationModel/SelectedMooringShapeProvider.cs"
Assert-Contains $selectedShapeProvider "MooringPrimaryShapeSelector.Select(fallbackShape, iterativeSolver)" "SelectedMooringShapeProvider"
Assert-Contains $selectedShapeProvider "selection.Shape" "SelectedMooringShapeProvider"
Assert-Contains $selectedShapeProvider "selection.Source" "SelectedMooringShapeProvider"
Assert-Contains $selectedShapeProvider "selection.UsesDiscreteLoads" "SelectedMooringShapeProvider"
Assert-Contains $selectedShapeProvider "selection.Gate.Decision" "SelectedMooringShapeProvider"
Assert-NotContains $selectedShapeProvider "SelectedShapeStore" "SelectedMooringShapeProvider"
Assert-NotContains $selectedShapeProvider "MooringShapeStore" "SelectedMooringShapeProvider"
Assert-NotContains $selectedShapeProvider "MooringPrimaryShapeSelectionStore" "SelectedMooringShapeProvider"

$reportBoundary = Read-RepoText "Services/ReportBuildBoundary.cs"
Assert-Contains $reportBoundary "CalculationSnapshot snapshot" "ReportBuildBoundary"
Assert-Contains $reportBoundary "UserReportBuilder.Build(environment, snapshot.Result)" "ReportBuildBoundary"
Assert-Contains $reportBoundary "TechnicalReportBuilder.Build(projectName, environment, buoy, anchor, snapshot)" "ReportBuildBoundary"
Assert-NotContains $reportBoundary "CalculationSnapshotBuilder.Build" "ReportBuildBoundary"

$rendererPaths = @(
    "ViewModels/MainWindowCalculationDisplayBuilder.cs",
    "Services/ReportBuildBoundary.cs",
    "Services/UserReportBuilder.cs",
    "Services/TechnicalReportBuilder.cs",
    "Services/TechnicalReportMarkdownBuilder.cs",
    "Services/PdfReportBuilder.cs"
)
foreach ($rendererPath in $rendererPaths) {
    $renderer = Read-RepoText $rendererPath
    Assert-NotContains $renderer "CalculationSnapshotBuilder.Build(" $rendererPath
}

$dataBuilder = Read-RepoText "Services/TechnicalReportDataBuilder.cs"
Assert-Contains $dataBuilder "public static TechnicalReportData Build(EnvironmentInput environment, CalculationResult result)" "TechnicalReportDataBuilder"
Assert-Contains $dataBuilder "return new TechnicalReportData(" "TechnicalReportDataBuilder"
Assert-NotContains $dataBuilder "TechnicalReportStorePublisher" "TechnicalReportDataBuilder"

Write-Host "Read-model boundary smoke check passed."
