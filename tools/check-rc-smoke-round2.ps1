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

$shared = "Services/Mooring2DDiagramReadModel.cs"
$projector = "Services/Mooring2DElementBoundaryProjector.cs"
$canvas = "Views/Mooring2DCanvas.cs"
$pdf = "Services/PdfReportBuilder.cs"
$itemVm = "ViewModels/AssemblyItemViewModel.cs"
$main = "Views/MainWindow.axaml"
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

Require-Contains $itemVm "UI-only card state"
Require-Contains $itemVm "ToggleExpandedCommand"
Require-Contains $itemVm "ExpandCollapseGlyph"
Require-Contains $main 'IsVisible="{Binding IsExpanded}"'
Require-Contains $main 'Command="{Binding ToggleExpandedCommand}"'
Require-Contains $main 'TextBlock IsVisible="{Binding IsExpanded}" Text="{Binding Summary}"'
Require-Contains $main 'IsChecked="{Binding IsEnabled}" Content="В расчёте"'

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

Write-Host "RC smoke round-2/3 UI/PDF/library guard passed."
