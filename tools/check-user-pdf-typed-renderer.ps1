$ErrorActionPreference = "Stop"

function Require-Contains([string]$Path, [string]$Needle) {
    $text = Get-Content -LiteralPath $Path -Raw
    if (-not $text.Contains($Needle)) {
        throw "Required typed PDF contract not found in ${Path}: ${Needle}"
    }
}

function Require-NotContains([string]$Path, [string]$Needle) {
    $text = Get-Content -LiteralPath $Path -Raw
    if ($text.Contains($Needle)) {
        throw "Forbidden legacy PDF dependency found in ${Path}: ${Needle}"
    }
}

$pdf = "Services/PdfReportBuilder.cs"
$main = "Views/MainWindow.axaml.cs"
$workflow = "Views/MainWindowPdfExportWorkflowBuilder.cs"

Require-Contains $pdf "Build(string filePath, UserEngineeringReportReadModel report)"
Require-Contains $pdf "report.Assessment"
Require-Contains $pdf "report.DesignLoad"
Require-Contains $pdf "report.Structural"
Require-Contains $pdf "report.AnchorReaction"
Require-Contains $pdf "report.SelectedShape"
Require-Contains $pdf "Mooring2DDiagramReadModelBuilder.Build(report.SelectedShape, diagramRows)"
Require-Contains $pdf '"Ключевые показатели"'
Require-Contains $pdf '"Исходные условия расчёта"'
Require-Contains $pdf '"Состав постановки"'
Require-Contains $pdf '"Расчётная геометрия постановки X/Z"'
Require-NotContains $pdf "resultText"
Require-NotContains $pdf "reportText"
Require-NotContains $pdf "PdfReportStructureGuide"
Require-NotContains $pdf "TechnicalReportText"

Require-Contains $workflow "CanExport(UserEngineeringReportReadModel? report)"
Require-Contains $main "CanExport(viewModel.UserEngineeringReport)"
Require-Contains $main "PdfReportBuilder.Build(path, viewModel.UserEngineeringReport!)"
Require-NotContains $main "PdfReportStructureGuide.Apply(viewModel.ReportText)"
Require-NotContains $main "viewModel.ResultText,"

Write-Host "Typed user PDF renderer guard passed."
