$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$builderPath = Join-Path $root 'ViewModels/MainWindowCalculationInputBuilder.cs'
$viewModelPath = Join-Path $root 'ViewModels/MainWindowViewModel.cs'

if (-not (Test-Path $builderPath)) {
    throw 'MainWindowCalculationInputBuilder.cs is missing.'
}

$builder = Get-Content $builderPath -Raw
$viewModel = Get-Content $viewModelPath -Raw

$builderMarkers = @(
    'internal sealed record MainWindowCalculationInput(',
    'internal static class MainWindowCalculationInputBuilder',
    '.Select(x => x.ToInput())',
    '.OrderBy(x => x.DepthM)',
    'SelectedSeabedPreset ?? SeabedCatalog.ById("unknown")',
    'NumberStyles.Any',
    'CultureInfo.InvariantCulture'
)

foreach ($marker in $builderMarkers) {
    if (-not $builder.Contains($marker)) {
        throw "Calculation input boundary marker is missing: $marker"
    }
}

$viewModelMarkers = @(
    'MainWindowCalculationInputBuilder.Build(',
    'BuoyCalculator.Calculate(',
    'input.Environment,',
    'input.Buoy,',
    'input.AssemblyItems,',
    'input.Anchor,',
    'input.SafetyFactor)'
)

foreach ($marker in $viewModelMarkers) {
    if (-not $viewModel.Contains($marker)) {
        throw "MainWindowViewModel calculation input publication marker is missing: $marker"
    }
}

Write-Host 'Main-window calculation input boundary check passed.'
