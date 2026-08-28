$ErrorActionPreference = "Stop"

function Require-Contains([string]$Path, [string]$Needle) {
    $text = Get-Content -LiteralPath $Path -Raw
    if (-not $text.Contains($Needle)) {
        throw "Required user-report read-model contract not found in ${Path}: ${Needle}"
    }
}

function Require-NotContains([string]$Path, [string]$Needle) {
    $text = Get-Content -LiteralPath $Path -Raw
    if ($text.Contains($Needle)) {
        throw "Forbidden user-report read-model dependency found in ${Path}: ${Needle}"
    }
}

function Require-Count([string]$Path, [string]$Needle, [int]$Expected) {
    $text = Get-Content -LiteralPath $Path -Raw
    $actual = ([regex]::Matches($text, [regex]::Escape($Needle))).Count
    if ($actual -ne $Expected) {
        throw "Unexpected user-report read-model contract count in ${Path}: ${Needle}; expected=${Expected}; actual=${actual}"
    }
}

$readModel = "Services/UserEngineeringReportReadModel.cs"
$display = "ViewModels/MainWindowCalculationDisplayBuilder.cs"
$mainVm = "ViewModels/MainWindowViewModel.cs"

Require-Contains $readModel "UserEngineeringReportReadModel"
Require-Contains $readModel "UserEngineeringReportReadModelProjector"
Require-Contains $readModel "CalculationSnapshot snapshot"
Require-Contains $readModel "snapshot.SelectedShape"
Require-Contains $readModel "snapshot.SelectedDesignTensionDemand"
Require-Contains $readModel "snapshot.SelectedDesignEnvelope"
Require-Contains $readModel "snapshot.SelectedAnchorReaction"
Require-Contains $readModel "snapshot.SelectedLocalStructuralCapacity"
Require-Contains $readModel "snapshot.SelectedEngineeringAssessment"
Require-Contains $readModel "result.ElementRows"
Require-Contains $readModel "environment.EffectiveCurrentSpeedMS"
Require-Contains $readModel "environment.EffectiveCurrentProfile.ToArray()"
Require-NotContains $readModel "double CurrentSpeedMS,"
Require-NotContains $readModel "bool UsesCurrentProfile,"
Require-NotContains $readModel "environment.CurrentSpeedMS"
Require-NotContains $readModel "environment.UseCurrentProfile"
Require-NotContains $readModel ".ResultText"
Require-NotContains $readModel ".ReportText"
Require-NotContains $readModel ".TechnicalReportText"
Require-NotContains $readModel "TechnicalReportMarkdownBuilder"
Require-NotContains $readModel "PdfReportBuilder"

Require-Contains $display "UserEngineeringReportReadModel UserEngineeringReport"
Require-Contains $display "UserEngineeringReportReadModelProjector.Project"
Require-Contains $display "userEngineeringReport"

Require-Contains $mainVm "UserEngineeringReportReadModel? _userEngineeringReport"
Require-Contains $mainVm "UserEngineeringReportReadModel? UserEngineeringReport"
Require-Contains $mainVm "UserEngineeringReport = display.UserEngineeringReport;"
Require-Count $mainVm "UserEngineeringReport = null;" 2

Write-Host "Typed user engineering report read-model boundary guard passed."
