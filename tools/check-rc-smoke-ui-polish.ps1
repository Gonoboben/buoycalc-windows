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

$currentProfile = Read-RepoText "Views/CurrentProfileWindow.axaml"
Assert-Contains $currentProfile 'Text="Профиль течения по глубине"' "Current-profile title"
Assert-Contains $currentProfile 'Text="?"' "Current-profile help mark"
Assert-Contains $currentProfile 'ToolTip.Tip="Задайте U/V/W' "Current-profile model help"
Assert-Contains $currentProfile '<GridSplitter Grid.Row="2"' "Current-profile resizable blocks"

$main = Read-RepoText "Views/MainWindow.axaml"
Assert-Contains $main 'Classes="helpMark"' "Main-window field help"
Assert-Contains $main 'ItemsSource="{Binding KindDisplayOptions}"' "Russian sequence kind options"
Assert-Contains $main 'SelectedItem="{Binding KindDisplayName, Mode=TwoWay}"' "Russian sequence kind selection"
Assert-NotContains $main '<TextBlock Text="Отчёт" FontSize="20"' "Main-window inline report panel"
Assert-NotContains $main 'Text="{Binding ReportText}"' "Main-window inline report body"
Assert-Contains $main 'Click="OpenReportTextButton_Click"' "Full report command"

$assembly = Read-RepoText "ViewModels/AssemblyItemViewModel.cs"
Assert-Contains $assembly 'KindOptions { get; } = new[] { "Line", "Connector", "Payload" }' "Canonical sequence kind options"
Assert-Contains $assembly 'KindDisplayOptions { get; } = new[] { "Линия", "Соединитель", "Прибор" }' "Russian sequence kind display options"
Assert-Contains $assembly '"Соединитель" => "Connector"' "Russian-to-canonical connector mapping"
Assert-Contains $assembly '"Прибор" => "Payload"' "Russian-to-canonical payload mapping"

$library = Read-RepoText "Views/ElementLibraryWindow.axaml"
$helpCount = ([regex]::Matches($library, 'Classes="helpMark"')).Count
if ($helpCount -lt 20) {
    throw "Element library field-help coverage is unexpectedly low: $helpCount"
}
Assert-Contains $library 'Знаковый погонный вес в воде' "Signed line-water-weight help"
Assert-Contains $library 'не является подтверждённой горизонтальной несущей способностью' "Anchor compatibility help"

$canvas = Read-RepoText "Views/Mooring2DCanvas.cs"
Assert-NotContains $canvas 'nodeStep' "2D segmentation-node sampling"
Assert-Contains $canvas 'Mooring2DElementBoundaryProjector.Project' "2D element-boundary projection"
Assert-Contains $canvas 'Mooring2DElementMarkerKind.LineBoundary' "2D line boundaries"
Assert-Contains $canvas 'Mooring2DElementMarkerKind.Connector' "2D connector markers"
Assert-Contains $canvas 'Mooring2DElementMarkerKind.Payload' "2D payload markers"

$displayRow = Read-RepoText "Models/ElementCalculationDisplayRow.cs"
Assert-Contains $displayRow 'public double SourceLengthM { get; init; }' "Exact display source length"
Assert-Contains $displayRow 'SourceLengthM = row.LengthM' "Exact source-length retention"

$positioner = Read-RepoText "Services/MooringSequencePositioner.cs"
Assert-Contains $positioner 'var lengthM = element.SourceLengthM;' "2D exact s-position source"
Assert-NotContains $positioner 'ParseDisplayDouble(element.LengthM)' "Formatted length position source"

$projector = Read-RepoText "Services/Mooring2DElementBoundaryProjector.cs"
Assert-Contains $projector 'selectedShape.Shape.Nodes.OrderBy(x => x.AlongLineM)' "Selected-shape s projection"
Assert-Contains $projector 'MooringSequencePositioner.BuildDisplayPositions(elementRows)' "Canonical sequence position source"

Write-Host "RC smoke UI polish guard passed."