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
Require-Contains $pdf '"Плавучесть и расчётные нагрузки"'
Require-Contains $pdf '"Selected F1 design-нагрузка"'
Require-Contains $pdf '"Локальная прочность элементов — F3"'
Require-Contains $pdf '"Якорь и контакт с грунтом — F2"'
Require-Contains $pdf '"СПРАВОЧНО: legacy holding estimate — compatibility only"'
Require-Contains $pdf '"Инженерные проверки и заключение — F4"'
Require-Contains $pdf '"Воспроизводимость и provenance"'
Require-Contains $pdf '"Typed source", "UserEngineeringReportReadModel"'
Require-Contains $pdf '"Production segmentation: 0.20 м."'
Require-Contains $pdf '"Signed boundary-feedback iteration budget: 64."'
Require-Contains $pdf '"WeightWaterKgM сохраняет signed-семантику."'
Require-Contains $pdf '"Принятый signed candidate должен быть точной детерминированной fixed point без epsilon-acceptance."'
Require-Contains $pdf '"Координатная конвенция: s=0 у буя/поверхности, s=L у якоря/дна."'
Require-Contains $pdf "StructuralTable(structural.Rows)"
Require-Contains $pdf "foreach (var check in assessment.Checks)"
Require-Contains $pdf "AnchorHorizontalCapacityDisposition"
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
