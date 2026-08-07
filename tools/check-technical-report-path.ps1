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
        throw "Unexpected legacy file exists: $relativePath"
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

Assert-FileMissing "Services/ReportBuilder.cs"

$technicalReportBuilder = Read-RepoText "Services/TechnicalReportBuilder.cs"
Assert-Contains $technicalReportBuilder "return TechnicalReportMarkdownBuilder.Build(projectName, environment, buoy, anchor, result);" "TechnicalReportBuilder"

Assert-FileExists "ApplicationModel/CalculationSnapshot.cs"
Assert-FileExists "ApplicationModel/SelectedMooringShapeProvider.cs"
$snapshotBoundary = Read-RepoText "ApplicationModel/CalculationSnapshot.cs"
Assert-Contains $snapshotBoundary "public sealed record CalculationSnapshot(" "CalculationSnapshot"
Assert-Contains $snapshotBoundary "CalculationResult Result," "CalculationSnapshot"
Assert-Contains $snapshotBoundary "TechnicalReportData TechnicalReportData," "CalculationSnapshot"
Assert-Contains $snapshotBoundary "SelectedShapeReadModel? SelectedShape);" "CalculationSnapshot"
Assert-Contains $snapshotBoundary "var data = TechnicalReportDataBuilder.Build(environment, result);" "CalculationSnapshotBuilder"
Assert-Contains $snapshotBoundary "TechnicalReportStorePublisher.Publish(data);" "CalculationSnapshotBuilder"
Assert-Contains $snapshotBoundary "var selectedShape = SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver);" "CalculationSnapshotBuilder"
Assert-NotContains $snapshotBoundary "SelectedShapeStore.Current" "CalculationSnapshotBuilder"

$dataBuildIndex = $snapshotBoundary.IndexOf("var data = TechnicalReportDataBuilder.Build(environment, result);")
$publishIndex = $snapshotBoundary.IndexOf("TechnicalReportStorePublisher.Publish(data);")
$selectedShapeIndex = $snapshotBoundary.IndexOf("var selectedShape = SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver);")
if ($dataBuildIndex -lt 0 -or $publishIndex -le $dataBuildIndex -or $selectedShapeIndex -le $publishIndex) {
    throw "CalculationSnapshotBuilder must preserve Build -> Publish -> stateless selected X/Z order."
}

$selectedShapeProvider = Read-RepoText "ApplicationModel/SelectedMooringShapeProvider.cs"
Assert-Contains $selectedShapeProvider "MooringPrimaryShapeSelector.Select(fallbackShape, iterativeSolver)" "SelectedMooringShapeProvider"
Assert-Contains $selectedShapeProvider "selection.Shape" "SelectedMooringShapeProvider"
Assert-Contains $selectedShapeProvider "selection.UsesDiscreteLoads" "SelectedMooringShapeProvider"
Assert-Contains $selectedShapeProvider "selection.Gate.Decision" "SelectedMooringShapeProvider"
Assert-NotContains $selectedShapeProvider "SelectedShapeStore" "SelectedMooringShapeProvider"
Assert-NotContains $selectedShapeProvider "MooringShapeStore" "SelectedMooringShapeProvider"
Assert-NotContains $selectedShapeProvider "MooringPrimaryShapeSelectionStore" "SelectedMooringShapeProvider"

$markdownBuilder = Read-RepoText "Services/TechnicalReportMarkdownBuilder.cs"
Assert-Contains $markdownBuilder "var snapshot = CalculationSnapshotBuilder.Build(environment, result);" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var data = snapshot.TechnicalReportData;" "TechnicalReportMarkdownBuilder"
Assert-NotContains $markdownBuilder "TechnicalReportDataBuilder.Build(environment, result)" "TechnicalReportMarkdownBuilder"
Assert-NotContains $markdownBuilder "TechnicalReportStorePublisher.Publish(data);" "TechnicalReportMarkdownBuilder"

$bridgeCalls = @(
    "AppendVectorBalanceRows",
    "AppendElementRows",
    "AppendSequencePositionRows",
    "AppendModelCoverageRows",
    "AppendSegmentRows",
    "AppendTensionRows",
    "AppendShapeRows",
    "AppendShapeProjectionRows",
    "AppendShapeForceRows",
    "AppendShapeTensionRows",
    "AppendDiscreteLoadTensionRows",
    "AppendDiscreteLoadShapeRows",
    "AppendAlternativeDiscreteNodeRows",
    "AppendIterativeSolverRows",
    "AppendChecks"
)

foreach ($bridgeCall in $bridgeCalls) {
    $expectedBridgeCall = 'TechnicalReportMarkdownSectionBridge.Append("' + $bridgeCall + '"'
    Assert-Contains $markdownBuilder $expectedBridgeCall "TechnicalReportMarkdownBuilder"
}

$bridge = Read-RepoText "Services/TechnicalReportMarkdownSectionBridge.cs"

$rendererClasses = @(
    "TechnicalReportMarkdownMovedSections",
    "TechnicalReportMarkdownDiscreteShapeSections",
    "TechnicalReportMarkdownDiscreteTensionSections",
    "TechnicalReportMarkdownDiscreteNodeSections",
    "TechnicalReportMarkdownIterativeSolverSections",
    "TechnicalReportMarkdownCheckSections"
)

foreach ($rendererClass in $rendererClasses) {
    $expectedRenderer = $rendererClass + ".TryAppend(methodName, args)"
    Assert-Contains $bridge $expectedRenderer "TechnicalReportMarkdownSectionBridge"
}

Assert-Contains $bridge 'throw new InvalidOperationException($"Technical report Markdown section renderer not found: {methodName}");' "TechnicalReportMarkdownSectionBridge"
Assert-NotContains $bridge "System.Reflection" "TechnicalReportMarkdownSectionBridge"
Assert-NotContains $bridge "ReportBuilder" "TechnicalReportMarkdownSectionBridge"
Assert-NotContains $bridge "GetMethod(" "TechnicalReportMarkdownSectionBridge"
Assert-NotContains $bridge ".Invoke(" "TechnicalReportMarkdownSectionBridge"

$rendererFiles = @(
    "Services/TechnicalReportMarkdownMovedSections.cs",
    "Services/TechnicalReportMarkdownDiscreteShapeSections.cs",
    "Services/TechnicalReportMarkdownDiscreteTensionSections.cs",
    "Services/TechnicalReportMarkdownDiscreteNodeSections.cs",
    "Services/TechnicalReportMarkdownIterativeSolverSections.cs",
    "Services/TechnicalReportMarkdownCheckSections.cs"
)

foreach ($rendererFile in $rendererFiles) {
    Assert-FileExists $rendererFile
}

Write-Host "Technical report path smoke check passed."
