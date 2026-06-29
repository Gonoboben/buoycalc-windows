param(
    [string]$Root = "Views",
    [switch]$FailOnFinding
)

Write-Host "BuoyCalc Windows XAML version audit"
Write-Host "Root: $Root"

if (-not (Test-Path $Root)) {
    Write-Host "Audit root not found: $Root"
    exit 2
}

$files = Get-ChildItem -Path $Root -Recurse -File -Filter *.axaml
$pattern = "v0\."
$found = $false

foreach ($file in $files) {
    $matches = Select-String -Path $file.FullName -Pattern $pattern

    foreach ($match in $matches) {
        $found = $true
        $relativePath = Resolve-Path -Path $file.FullName -Relative
        Write-Host ("{0}:{1}: {2}" -f $relativePath, $match.LineNumber, $match.Line.Trim())
    }
}

if (-not $found) {
    Write-Host "No hardcoded v0.* versions found in XAML."
    exit 0
}

Write-Host ""
Write-Host "Hardcoded v0.* versions were found in XAML."
Write-Host "This audit is manual at this stage. Use -FailOnFinding after the baseline is cleaned or agreed."

if ($FailOnFinding) {
    exit 1
}

exit 0
