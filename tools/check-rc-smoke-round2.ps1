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

Require-Contains $itemVm "UI-only card state"
Require-Contains $itemVm "ToggleExpandedCommand"
Require-Contains $itemVm "ExpandCollapseGlyph"
Require-Contains $main 'IsVisible="{Binding IsExpanded}"'
Require-Contains $main 'Command="{Binding ToggleExpandedCommand}"'

Require-Contains $library "Cd = 2F/(ρ·U²·A)"
Require-Contains $library "V=π·D²·L/4"
Require-Contains $library "V≈m/ρматериала"
Require-Contains $library "Экспорт библиотеки..."
Require-Contains $library "Импорт библиотеки..."
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

Write-Host "RC smoke round-2 UI/PDF/library guard passed."
