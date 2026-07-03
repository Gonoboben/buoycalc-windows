$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Get-RelativePath([string]$path) {
    return [System.IO.Path]::GetRelativePath($repoRoot, $path).Replace("\\", "/")
}

$sourceFiles = @(
    Get-ChildItem -Path $repoRoot -Recurse -File -Filter "*.cs" |
        Where-Object {
            $_.FullName -notmatch "[\\/]bin[\\/]" -and
            $_.FullName -notmatch "[\\/]obj[\\/]"
        }
)

if ($sourceFiles.Count -eq 0) {
    throw "No C# source files found."
}

$scanTerms = @(
    "SelectedShapeStore",
    "MooringPrimaryShapeSelectionStore",
    "MooringShapeStore.Current",
    "MooringShapeStore.Set(",
    "MooringShapeResult",
    "MooringShapePoint",
    "MooringShapeSegment"
)

foreach ($scanTerm in $scanTerms) {
    $references = @(
        Select-String -Path $sourceFiles.FullName -SimpleMatch $scanTerm
    )

    Write-Host ""
    Write-Host ("Selected-shape scan term: " + $scanTerm)
    Write-Host ("  Reference count: " + $references.Count)

    if ($references.Count -eq 0) {
        Write-Host "  References: none"
        continue
    }

    Write-Host "  References:"
    foreach ($item in $references) {
        Write-Host ("    " + (Get-RelativePath $item.Path) + ":" + $item.LineNumber + ": " + $item.Line.Trim())
    }
}

Write-Host ""
Write-Host "Selected-shape consumer scan completed."
