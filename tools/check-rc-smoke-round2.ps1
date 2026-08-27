$ErrorActionPreference = "Stop"

function Require-Contains([string]$Path, [string]$Needle) {
    $text = Get-Content -LiteralPath $Path -Raw
    if (-not $text.Contains($Needle)) {
        throw "Required contract not found in ${Path}: ${Needle}"
    }
}

function Require-NotContains([string]$Path, [string]$Needle) {
    $text = Get-Content -LiteralPath $Path -Raw
    if ($text.Contains($Needle)) {
        throw "Forbidden legacy contract found in ${Path}: ${Needle}"
    }
}

function Require-Count([string]$Path, [string]$Needle, [int]$Expected) {
    $text = Get-Content -LiteralPath $Path -Raw
    $actual = ([regex]::Matches($text, [regex]::Escape($Needle))).Count
    if ($actual -ne $Expected) {
        throw "Unexpected contract count in ${Path}: ${Needle}; expected=${Expected}; actual=${actual}"
    }
}

$shared = "Services/Mooring2DDiagramReadModel.cs"
$projector = "Services/Mooring2DElementBoundaryProjector.cs"
$canvas = "Views/Mooring2DCanvas.cs"
$pdf = "Services/PdfReportBuilder.cs"
$itemVm = "ViewModels/AssemblyItemViewModel.cs"
$main = "Views/MainWindow.axaml"
$mainCode = "Views/MainWindow.axaml.cs"
$reportWindow = "Views/ReportTextWindow.axaml"
$reportCode = "Views/ReportTextWindow.axaml.cs"
$library = "Views/ElementLibraryWindow.axaml"
$libraryCode = "Views/ElementLibraryWindow.axaml.cs"
$bundle = "Services/ElementLibraryBundleStorage.cs"

Require-Contains $shared "Mooring2DDiagramReadModelBuilder"
Require-Contains $shared "Mooring2DElementBoundaryProjector.Project"
Require-Contains $projector "Mooring2DLabelZone.NearSurface"
Require-Contains $projector "Mooring2DLabelZone.NearBottom"
Require-Contains $projector "ratio >= 0.90"
Require-Contains $canvas "Mooring2DDiagramReadModelBuilder.Build"
Require-Contains $canvas "foreach (var marker in diagram.ElementMarkers)"
Require-Contains $pdf "Mooring2DDiagramReadModelBuilder.Build"
Require-Contains $pdf "foreach (var marker in diagram.ElementMarkers)"
Require-NotContains $pdf "var step = Math.Max(1, points.Count / 22);"
Require-NotContains $pdf "_canvas.DrawCircle(points[i], 3.8f"

# RC round 3: diagrams retain calculated marker positions, but no element-name callouts/leaders.
Require-NotContains $canvas '"конец линии:'
Require-NotContains $canvas '"прибор:'
Require-NotContains $canvas '"соединитель:'
Require-NotContains $canvas "ResolveLabelOrigin"
Require-NotContains $canvas "diagram.BuoyTitle"
Require-NotContains $canvas "diagram.AnchorTitle"
Require-NotContains $pdf '"конец линии:'
Require-NotContains $pdf '"прибор:'
Require-NotContains $pdf '"соединитель:'
Require-NotContains $pdf "ResolveLabelPoint"
Require-NotContains $pdf "diagram.BuoyTitle"
Require-NotContains $pdf "diagram.AnchorTitle"

# RC round 4: marker geometry is presentation-only and intentionally lighter.
Require-Contains $canvas "LinePen = new Pen(LineBrush, 2.2)"
Require-Contains $canvas "NodePen = new Pen(LineBrush, 1.0)"
Require-Contains $canvas "point, 3.8, 3.8"
Require-Contains $canvas "new Rect(point.X - 3.2, point.Y - 3.2, 6.4, 6.4)"
Require-Contains $canvas "point, 11, 11"
Require-Contains $canvas "new Rect(point.X - 14, point.Y - 7, 28, 14)"
Require-Contains $pdf 'linePaint = Stroke("#315B9A", 2.0f)'
Require-Contains $pdf 'markerStroke = Stroke("#315B9A", 0.9f)'
Require-Contains $pdf "DrawCircle(buoyPoint, 9"
Require-Contains $pdf "point, 3.4f, payloadFill"
Require-Contains $pdf "point.X - 3.0f"

Require-Contains $itemVm "UI-only card state"
Require-Contains $itemVm "ToggleExpandedCommand"
Require-Contains $itemVm "ExpandCollapseGlyph"
Require-Contains $main 'IsVisible="{Binding IsExpanded}"'
Require-Contains $main 'Command="{Binding ToggleExpandedCommand}"'
Require-Contains $main 'TextBlock IsVisible="{Binding IsExpanded}" Text="{Binding Summary}"'
Require-Contains $main 'IsChecked="{Binding IsEnabled}" Content="В расчёте"'

# RC round 5: only the three requested left-column setup cards are collapsible and all start collapsed.
Require-Count $main 'IsExpanded="False"' 3
Require-Count $main 'IsExpanded="True"' 0
Require-Contains $main 'x:Name="ConditionsExpander" IsExpanded="False"'
Require-Contains $main 'x:Name="BuoyExpander" IsExpanded="False"'
Require-Contains $main 'x:Name="AnchorExpander" IsExpanded="False"'
Require-Contains $main 'Text="Условия постановки" FontSize="18" FontWeight="Bold"'
Require-Contains $main 'Text="Буй" FontSize="18" FontWeight="Bold"'
Require-Contains $main 'Text="Якорь и запас" FontSize="18" FontWeight="Bold"'
Require-Contains $main 'Text="Проект" FontSize="18" FontWeight="Bold"'
Require-Contains $main 'Content="Новый" Command="{Binding NewProjectCommand}" Click="ResetSetupSectionsButton_Click"'
Require-Contains $main 'Content="Загрузить..." Command="{Binding LoadProjectCommand}" Click="ResetSetupSectionsButton_Click"'
Require-Contains $mainCode "CollapseSetupSections();"
Require-Contains $mainCode "ConditionsExpander.IsExpanded = false;"
Require-Contains $mainCode "BuoyExpander.IsExpanded = false;"
Require-Contains $mainCode "AnchorExpander.IsExpanded = false;"
Require-Count $main 'Content="Проверить схему и рассчитать"' 1

$mainText = Get-Content -LiteralPath $main -Raw
$anchorHeaderIndex = $mainText.IndexOf('Text="Якорь и запас"', [StringComparison]::Ordinal)
$anchorExpanderEndIndex = $mainText.IndexOf('</Expander>', $anchorHeaderIndex, [StringComparison]::Ordinal)
$calculateButtonIndex = $mainText.IndexOf('Content="Проверить схему и рассчитать"', [StringComparison]::Ordinal)
if ($anchorHeaderIndex -lt 0 -or $anchorExpanderEndIndex -lt 0 -or $calculateButtonIndex -lt 0 -or $calculateButtonIndex -lt $anchorExpanderEndIndex) {
    throw "Calculate action must remain outside the AnchorExpander."
}

# RC round 4: full-report export writes the exact retained ReportText to UTF-8 text.
Require-Contains $reportWindow 'Click="ExportReportButton_Click"'
Require-Contains $reportWindow 'Text="Экспорт .txt"'
Require-Contains $reportWindow 'x:Name="ExportStatusText"'
Require-Contains $reportCode "StorageProvider.SaveFilePickerAsync"
Require-Contains $reportCode 'viewModel.ReportText, new UTF8Encoding(false)'
Require-Contains $reportCode '"_full_report.txt"'
Require-Contains $reportCode 'Patterns = new[] { "*.txt" }'
Require-NotContains $reportCode "PdfReportStructureGuide"

Require-Contains $library "Cd = 2F/(ρ·U²·A)"
Require-Contains $library "V=π·D²·L/4"
Require-Contains $library "V≈m/ρматериала"
Require-Contains $library "Экспорт библиотеки..."
Require-Contains $library "Импорт библиотеки..."
Require-Contains $library 'Style Selector="Button.headerCommand"'
Require-Contains $library 'Classes="headerCommand" Content="Экспорт библиотеки..."'
Require-Contains $library 'Classes="headerCommand" Content="Импорт библиотеки..."'
Require-Contains $library "Коэффициент сопротивления формы буя."
Require-Contains $library "Коэффициент сопротивления линии."
Require-Contains $library "Коэффициент сопротивления формы соединителя."
Require-Contains $library "Коэффициент сопротивления формы прибора/рамы."
Require-Contains $libraryCode "ElementLibraryBundleStorage.Export"
Require-Contains $libraryCode "ElementLibraryBundleStorage.ImportMerge"

Require-Contains $bundle 'BundleFormat = "BuoyCalc.ElementLibrary"'
Require-Contains $bundle "LoadUserBuoys()"
Require-Contains $bundle "LoadUserRopes()"
Require-Contains $bundle "LoadUserConnectors()"
Require-Contains $bundle "LoadUserPayloads()"
Require-Contains $bundle "LoadUserAnchors()"
Require-Contains $bundle "SaveUserBuoys"
Require-Contains $bundle "SaveUserRopes"
Require-Contains $bundle "SaveUserConnectors"
Require-Contains $bundle "SaveUserPayloads"
Require-Contains $bundle "SaveUserAnchors"
Require-Contains $bundle "usedIds.Contains(id)"
Require-Contains $bundle "usedNames.Contains(name)"
Require-Contains $bundle '!id.StartsWith("user:", StringComparison.OrdinalIgnoreCase)'
Require-NotContains $bundle "DeleteUserBuoy"
Require-NotContains $bundle "DeleteUserRope"
Require-NotContains $bundle "DeleteUserConnector"
Require-NotContains $bundle "DeleteUserPayload"
Require-NotContains $bundle "DeleteUserAnchor"

Write-Host "RC smoke round-2/3/4/5 UI/PDF/library guard passed."
