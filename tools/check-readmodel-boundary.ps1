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

Assert-FileExists "Services/TechnicalReportBuilder.cs"
Assert-FileExists "Services/TechnicalReportMarkdownBuilder.cs"
Assert-FileExists "Services/TechnicalReportDataBuilder.cs"
Assert-FileExists "Services/TechnicalReportData.cs"
Assert-FileExists "Services/TechnicalReportStorePublisher.cs"
Assert-FileExists "ApplicationModel/CalculationSnapshot.cs"

$entryBuilder = Read-RepoText "Services/TechnicalReportBuilder.cs"
Assert-Contains $entryBuilder "CalculationResult result" "TechnicalReportBuilder"
Assert-Contains $entryBuilder "return TechnicalReportMarkdownBuilder.Build(projectName, environment, buoy, anchor, result);" "TechnicalReportBuilder"

$markdownBuilder = Read-RepoText "Services/TechnicalReportMarkdownBuilder.cs"
Assert-Contains $markdownBuilder "CalculationSnapshotBuilder.Build(environment, result)" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var data = snapshot.TechnicalReportData;" "TechnicalReportMarkdownBuilder"
Assert-NotContains $markdownBuilder "TechnicalReportDataBuilder.Build(environment, result)" "TechnicalReportMarkdownBuilder"
Assert-NotContains $markdownBuilder "TechnicalReportStorePublisher.Publish(data);" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var tensionRows = data.TensionRows;" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var shape = data.Shape;" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var diagnostics = data.Diagnostics;" "TechnicalReportMarkdownBuilder"

$snapshotBoundary = Read-RepoText "ApplicationModel/CalculationSnapshot.cs"
Assert-Contains $snapshotBoundary "public sealed record CalculationSnapshot(" "CalculationSnapshot"
Assert-Contains $snapshotBoundary "CalculationResult Result," "CalculationSnapshot"
Assert-Contains $snapshotBoundary "TechnicalReportData TechnicalReportData," "CalculationSnapshot"
Assert-Contains $snapshotBoundary "SelectedShapeReadModel? SelectedShape);" "CalculationSnapshot"
Assert-Contains $snapshotBoundary "var data = TechnicalReportDataBuilder.Build(environment, result);" "CalculationSnapshotBuilder"
Assert-Contains $snapshotBoundary "TechnicalReportStorePublisher.Publish(data);" "CalculationSnapshotBuilder"
Assert-Contains $snapshotBoundary "var selectedShape = SelectedShapeStore.Current;" "CalculationSnapshotBuilder"

$dataBuilder = Read-RepoText "Services/TechnicalReportDataBuilder.cs"
Assert-Contains $dataBuilder "public static TechnicalReportData Build(EnvironmentInput environment, CalculationResult result)" "TechnicalReportDataBuilder"
Assert-Contains $dataBuilder "return new TechnicalReportData(" "TechnicalReportDataBuilder"

$storePublisher = Read-RepoText "Services/TechnicalReportStorePublisher.cs"
Assert-Contains $storePublisher "public static void Publish(TechnicalReportData data)" "TechnicalReportStorePublisher"
Assert-Contains $storePublisher "MooringShapeStore.Set(data.Shape);" "TechnicalReportStorePublisher"
Assert-Contains $storePublisher "MooringIterativeSolverStore.Set(data.IterativeSolver);" "TechnicalReportStorePublisher"

Write-Host "Read-model boundary smoke check passed."
