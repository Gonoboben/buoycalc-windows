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
Assert-FileMissing "Services/TechnicalReportStorePublisher.cs"

Assert-FileExists "ApplicationModel/ApplicationCalculationRunner.cs"
$applicationRunner = Read-RepoText "ApplicationModel/ApplicationCalculationRunner.cs"
Assert-Contains $applicationRunner "var result = BuoyCalculator.Calculate(" "ApplicationCalculationRunner"
Assert-Contains $applicationRunner "var snapshot = CalculationSnapshotBuilder.Build(environment, buoy, result);" "ApplicationCalculationRunner"

Assert-FileExists "ViewModels/MainWindowCalculationDisplayBuilder.cs"
$displayBuilder = Read-RepoText "ViewModels/MainWindowCalculationDisplayBuilder.cs"
Assert-Contains $displayBuilder "ApplicationCalculationRun run" "MainWindowCalculationDisplayBuilder"
Assert-Contains $displayBuilder "var snapshot = run.Snapshot;" "MainWindowCalculationDisplayBuilder"
Assert-Contains $displayBuilder "ReportBuildBoundary.Build(projectName, environment, buoy, anchor, snapshot)" "MainWindowCalculationDisplayBuilder"
Assert-NotContains $displayBuilder "CalculationSnapshotBuilder.Build(" "MainWindowCalculationDisplayBuilder"

$technicalReportBuilder = Read-RepoText "Services/TechnicalReportBuilder.cs"
Assert-Contains $technicalReportBuilder "CalculationSnapshot snapshot" "TechnicalReportBuilder"
Assert-Contains $technicalReportBuilder "return TechnicalReportMarkdownBuilder.Build(projectName, environment, buoy, anchor, snapshot);" "TechnicalReportBuilder"
Assert-NotContains $technicalReportBuilder "CalculationResult result" "TechnicalReportBuilder"

Assert-FileExists "ApplicationModel/CalculationSnapshot.cs"
Assert-FileExists "ApplicationModel/SelectedMooringShapeProvider.cs"
$snapshotBoundary = Read-RepoText "ApplicationModel/CalculationSnapshot.cs"
Assert-Contains $snapshotBoundary "public sealed record CalculationSnapshot(" "CalculationSnapshot"
Assert-Contains $snapshotBoundary "CalculationResult Result," "CalculationSnapshot"
Assert-Contains $snapshotBoundary "TechnicalReportData TechnicalReportData," "CalculationSnapshot"
Assert-Contains $snapshotBoundary "SelectedShapeReadModel? SelectedShape);" "CalculationSnapshot"
Assert-Contains $snapshotBoundary "return Build(environment, null, result);" "CalculationSnapshotBuilder compatibility overload"
Assert-Contains $snapshotBoundary "var data = TechnicalReportDataBuilder.Build(environment, buoy, result);" "CalculationSnapshotBuilder"
Assert-Contains $snapshotBoundary "var selectedShape = SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver);" "CalculationSnapshotBuilder"
Assert-NotContains $snapshotBoundary "TechnicalReportStorePublisher" "CalculationSnapshotBuilder"
Assert-NotContains $snapshotBoundary "SelectedShapeStore.Current" "CalculationSnapshotBuilder"

$dataBuildIndex = $snapshotBoundary.IndexOf("var data = TechnicalReportDataBuilder.Build(environment, buoy, result);")
$selectedShapeIndex = $snapshotBoundary.IndexOf("var selectedShape = SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver);")
if ($dataBuildIndex -lt 0 -or $selectedShapeIndex -le $dataBuildIndex) {
    throw "CalculationSnapshotBuilder must preserve Build -> stateless selected X/Z order."
}

$selectedShapeProvider = Read-RepoText "ApplicationModel/SelectedMooringShapeProvider.cs"
Assert-Contains $selectedShapeProvider "MooringPrimaryShapeSelector.Select(fallbackShape, iterativeSolver)" "SelectedMooringShapeProvider"
Assert-Contains $selectedShapeProvider "selection.Shape" "SelectedMooringShapeProvider"
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

$markdownBuilder = Read-RepoText "Services/TechnicalReportMarkdownBuilder.cs"
Assert-Contains $markdownBuilder "CalculationSnapshot snapshot" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var result = snapshot.Result;" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var data = snapshot.TechnicalReportData;" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var forceShapeConsistency = data.ForceShapeConsistency;" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var signedNodeEquilibrium = data.SignedNodeEquilibrium;" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "var finalIterationSignedNodeEquilibrium = data.FinalIterationSignedNodeEquilibrium;" "TechnicalReportMarkdownBuilder"
Assert-Contains $markdownBuilder "if (finalIterationSignedNodeEquilibrium is null)" "TechnicalReportMarkdownBuilder"
Assert-NotContains $markdownBuilder "CalculationSnapshotBuilder.Build" "TechnicalReportMarkdownBuilder"
Assert-NotContains $markdownBuilder "TechnicalReportDataBuilder.Build" "TechnicalReportMarkdownBuilder"
Assert-NotContains $markdownBuilder "TechnicalReportStorePublisher.Publish" "TechnicalReportMarkdownBuilder"

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
    "AppendForceShapeConsistencyRows",
    "AppendDiscreteLoadTensionRows",
    "AppendDiscreteLoadShapeRows",
    "AppendSignedNodeEquilibriumRows",
    "AppendAlternativeDiscreteNodeRows",
    "AppendIterativeSolverRows",
    "AppendFinalIterationSignedNodeEquilibriumUnavailable",
    "AppendFinalIterationSignedNodeEquilibriumRows",
    "AppendChecks"
)

foreach ($bridgeCall in $bridgeCalls) {
    $expectedBridgeCall = 'TechnicalReportMarkdownSectionBridge.Append("' + $bridgeCall + '"'
    Assert-Contains $markdownBuilder $expectedBridgeCall "TechnicalReportMarkdownBuilder"
}

$bridge = Read-RepoText "Services/TechnicalReportMarkdownSectionBridge.cs"

$rendererClasses = @(
    "TechnicalReportMarkdownMovedSections",
    "TechnicalReportMarkdownForceShapeSections",
    "TechnicalReportMarkdownDiscreteShapeSections",
    "TechnicalReportMarkdownSignedNodeSections",
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
    "Services/TechnicalReportMarkdownForceShapeSections.cs",
    "Services/TechnicalReportMarkdownDiscreteShapeSections.cs",
    "Services/TechnicalReportMarkdownSignedNodeSections.cs",
    "Services/TechnicalReportMarkdownDiscreteTensionSections.cs",
    "Services/TechnicalReportMarkdownDiscreteNodeSections.cs",
    "Services/TechnicalReportMarkdownIterativeSolverSections.cs",
    "Services/TechnicalReportMarkdownCheckSections.cs"
)

foreach ($rendererFile in $rendererFiles) {
    Assert-FileExists $rendererFile
}

$forceShapeRenderer = Read-RepoText "Services/TechnicalReportMarkdownForceShapeSections.cs"
Assert-Contains $forceShapeRenderer "MooringForceShapeConsistencyResult" "Force-shape Markdown renderer"
Assert-Contains $forceShapeRenderer "consistency.AvailableRowCount" "Force-shape Markdown renderer"
Assert-Contains $forceShapeRenderer "consistency.MaxRelativeResidual" "Force-shape Markdown renderer"
Assert-Contains $forceShapeRenderer "row.RelativeResidual" "Force-shape Markdown renderer"
Assert-Contains $forceShapeRenderer "row.Status" "Force-shape Markdown renderer"
Assert-NotContains $forceShapeRenderer "MooringForceShapeConsistencyAnalyzer.Build" "Force-shape Markdown renderer"
Assert-NotContains $forceShapeRenderer "MooringShapeSolver.Build" "Force-shape Markdown renderer"
Assert-NotContains $forceShapeRenderer "MooringShapeTensionAnalyzer.Build" "Force-shape Markdown renderer"

$signedNodeRenderer = Read-RepoText "Services/TechnicalReportMarkdownSignedNodeSections.cs"
Assert-Contains $signedNodeRenderer "MooringSignedNodeEquilibriumResult" "Signed-node Markdown renderer"
Assert-Contains $signedNodeRenderer "AppendFinalIterationSignedNodeEquilibriumUnavailable" "Signed-node Markdown renderer"
Assert-Contains $signedNodeRenderer "AppendFinalIterationSignedNodeEquilibriumRows" "Signed-node Markdown renderer"
Assert-Contains $signedNodeRenderer "(MooringSignedNodeEquilibriumResult)args[1]" "Signed-node Markdown renderer"
Assert-NotContains $signedNodeRenderer "args[1] as MooringSignedNodeEquilibriumResult" "Signed-node Markdown renderer"
Assert-Contains $signedNodeRenderer "equilibrium.AvailableNodeCount" "Signed-node Markdown renderer"
Assert-Contains $signedNodeRenderer "equilibrium.MaxRelativeResidual" "Signed-node Markdown renderer"
Assert-Contains $signedNodeRenderer "row.ResidualXN" "Signed-node Markdown renderer"
Assert-Contains $signedNodeRenderer "row.ResidualZN" "Signed-node Markdown renderer"
Assert-Contains $signedNodeRenderer "row.RelativeResidual" "Signed-node Markdown renderer"
Assert-Contains $signedNodeRenderer "row.Status" "Signed-node Markdown renderer"
Assert-NotContains $signedNodeRenderer "MooringSignedNodeEquilibriumAnalyzer.Build" "Signed-node Markdown renderer"
Assert-NotContains $signedNodeRenderer "MooringDiscreteLoadTensionAnalyzer.Build" "Signed-node Markdown renderer"
Assert-NotContains $signedNodeRenderer "MooringDiscreteLoadShapeBuilder.Build" "Signed-node Markdown renderer"
Assert-NotContains $signedNodeRenderer "MooringIterativeSolver.Build" "Signed-node Markdown renderer"

Write-Host "Technical report path smoke check passed."
