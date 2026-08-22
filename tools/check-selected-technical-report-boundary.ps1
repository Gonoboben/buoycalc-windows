$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Read-RepoText([string]$relativePath) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required file is missing: $relativePath"
    }
    return Get-Content -LiteralPath $path -Raw
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

$entry = Read-RepoText "Services/TechnicalReportBuilder.cs"
Assert-Contains $entry "CalculationSnapshot snapshot" "TechnicalReportBuilder"
Assert-Contains $entry "var legacyReport = BuildLegacy(projectName, environment, buoy, anchor, snapshot);" "TechnicalReportBuilder"
Assert-Contains $entry "return SelectedTechnicalReportProjector.Project(legacyReport, snapshot);" "TechnicalReportBuilder"
Assert-Contains $entry "return TechnicalReportMarkdownBuilder.Build(projectName, environment, buoy, anchor, snapshot);" "TechnicalReportBuilder legacy renderer"
Assert-NotContains $entry "CalculationSnapshotBuilder.Build(" "TechnicalReportBuilder"
Assert-NotContains $entry "TechnicalReportDataBuilder.Build(" "TechnicalReportBuilder"
Assert-NotContains $entry "BuoyCalculator.Calculate(" "TechnicalReportBuilder"

$projector = Read-RepoText "Services/SelectedTechnicalReportProjector.cs"
Assert-Contains $projector "var assessment = snapshot.SelectedEngineeringAssessment;" "SelectedTechnicalReportProjector"
Assert-Contains $projector "if (assessment is null)" "SelectedTechnicalReportProjector legacy fallback"
Assert-Contains $projector "return legacyReport;" "SelectedTechnicalReportProjector legacy fallback"
Assert-Contains $projector "snapshot.SelectedDesignTensionDemand" "SelectedTechnicalReportProjector F1"
Assert-Contains $projector "snapshot.SelectedAnchorReaction" "SelectedTechnicalReportProjector F2"
Assert-Contains $projector "snapshot.SelectedLocalStructuralCapacity" "SelectedTechnicalReportProjector F3"
Assert-Contains $projector "SelectedElementCalculationDisplayProjector.Project(snapshot)" "SelectedTechnicalReportProjector selected element read model"
Assert-Contains $projector "assessment.AnchorHorizontalCapacityDisposition" "SelectedTechnicalReportProjector retained anchor capacity disposition"
Assert-Contains $projector "compatibility-only" "SelectedTechnicalReportProjector legacy labeling"
Assert-Contains $projector "legacy AnchorReserve не является selected-authority основанием для прохода" "SelectedTechnicalReportProjector legacy anchor disposition"

$forbiddenProjectorOwnership = @(
    "BuoyCalculator.Calculate(",
    "CalculationSnapshotBuilder.Build(",
    "TechnicalReportDataBuilder.Build(",
    "MooringShapeSolver.Build(",
    "MooringSignedCandidateEvaluator.Build(",
    "MooringSelectedEngineeringAssessmentStateProjector.Project(",
    "MooringSelectedDesignTensionDemandProjector.Project(",
    "MooringSelectedAnchorReactionStateProjector.Project(",
    "MooringSelectedLocalElementDemandStateProjector.Project(",
    "MooringSelectedLocalStructuralCapacityStateProjector.Project(",
    "snapshot.SignedCandidate",
    "snapshot.ShadowSelectedCore"
)

foreach ($needle in $forbiddenProjectorOwnership) {
    Assert-NotContains $projector $needle "SelectedTechnicalReportProjector presentation-only boundary"
}

$regression = Read-RepoText "validation/BuoyCalc.EngineeringRegression/SelectedTechnicalReportReadModelRegression.cs"
Assert-Contains $regression "TechnicalReportMarkdownBuilder.Build(projectName, environment, buoy, anchor, snapshot)" "F4-B2 regression legacy baseline"
Assert-Contains $regression "TechnicalReportBuilder.Build(projectName, environment, buoy, anchor, snapshot)" "F4-B2 regression selected path"
Assert-Contains $regression "non-selected technical report is not exact legacy fallback" "F4-B2 regression exact fallback"
Assert-Contains $regression "CalculationResult Verdict/MainRisk mutated" "F4-B2 regression result immutability"
Assert-Contains $regression "selected X/Z identity changed" "F4-B2 regression selected geometry immutability"
Assert-Contains $regression "F4-B1 compact user summary changed" "F4-B2 regression F4-B1 summary immutability"

$entryPoint = Read-RepoText "validation/BuoyCalc.EngineeringRegression/ValidationEntryPoint.cs"
Assert-Contains $entryPoint "SelectedTechnicalReportReadModelRegression.Validate();" "Engineering regression entry point"

Write-Host "Selected technical report boundary smoke check passed."
