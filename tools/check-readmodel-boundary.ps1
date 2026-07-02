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

$markerPath = "docs/CONTROL_MARK_CALCULATION_RESULT_READ_MODEL_BOUNDARY_2026-07-02.md"
Assert-FileExists $markerPath

$marker = Read-RepoText $markerPath
Assert-Contains $marker "#46" "Read-model boundary marker"
Assert-Contains $marker "Treat `CalculationResult` as solver-facing output." "Read-model boundary marker"
Assert-Contains $marker "Treat explicit read models such as technical report data as renderer-facing/user-facing input." "Read-model boundary marker"
Assert-Contains $marker "CalculationResult" "Read-model boundary marker"
Assert-Contains $marker "explicit read-model builder" "Read-model boundary marker"
Assert-Contains $marker "Markdown / PDF / 2D / UI renderers" "Read-model boundary marker"
Assert-Contains $marker "No solver physics changes are allowed in this phase." "Read-model boundary marker"

Assert-FileExists "Services/TechnicalReportBuilder.cs"
Assert-FileExists "Services/TechnicalReportMarkdownBuilder.cs"
Assert-FileExists "Services/TechnicalReportDataBuilder.cs"
Assert-FileExists "Services/TechnicalReportData.cs"
Assert-FileExists "Services/TechnicalReportStorePublisher.cs"

$entryBuilder = Read-RepoText "Services/TechnicalReportBuilder.cs"
Assert-Contains $entryBuilder "CalculationResult result" "TechnicalReportBuilder"
Assert-Contains $entryBuilder "return TechnicalReportMarkdownBuilder.Build(projectName, environment, buoy, anchor, result);" "TechnicalReportBuilder"

$markdownBuilder = Read-RepoText "Services/TechnicalReportMarkdownBuilder.cs"
Assert-Contains $markdownBuilder "TechnicalReportDataBuilder.Build(environment, result)" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "TechnicalReportStorePublisher.Publish(data);" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var tensionRows = data.TensionRows;" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var shape = data.Shape;" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var diagnostics = data.Diagnostics;" "TechnicalReportMarkdownBuilder"

$dataBuilder = Read-RepoText "Services/TechnicalReportDataBuilder.cs"
Assert-Contains $dataBuilder "public static TechnicalReportData Build(EnvironmentInput environment, CalculationResult result)" "TechnicalReportDataBuilder"
Assert-Contains $dataBuilder "return new TechnicalReportData(" "TechnicalReportDataBuilder"

$storePublisher = Read-RepoText "Services/TechnicalReportStorePublisher.cs"
Assert-Contains $storePublisher "public static void Publish(TechnicalReportData data)" "TechnicalReportStorePublisher"
Assert-Contains $storePublisher "MooringShapeStore.Set(data.Shape);" "TechnicalReportStorePublisher"
Assert-Contains $storePublisher "MooringIterativeSolverStore.Set(data.IterativeSolver);" "TechnicalReportStorePublisher"

Write-Host "Read-model boundary smoke check passed."
