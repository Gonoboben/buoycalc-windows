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

$retiredStores = @(
    "SelectedShapeStore",
    "MooringPrimaryShapeSelectionStore",
    "MooringIterativeSolverStore",
    "MooringShapeStore",
    "MooringAlternativeShapeStore"
)

foreach ($store in $retiredStores) {
    $escaped = [regex]::Escape($store)
    $pattern = "(\b(class|record)\s+" + $escaped + "\b)|(\b" + $escaped + "\s*\.)"
    $references = @(
        Select-String -Path $sourceFiles.FullName -Pattern $pattern
    )

    Write-Host ""
    Write-Host ("Retired selected-shape store: " + $store)
    Write-Host ("  Actual C# references: " + $references.Count)

    if ($references.Count -ne 0) {
        foreach ($item in $references) {
            Write-Host ("    " + (Get-RelativePath $item.Path) + ":" + $item.LineNumber + ": " + $item.Line.Trim())
        }
        throw "$store was retired and must have zero actual C# references."
    }

    Write-Host "  References: none"
}

$informationalTerms = @(
    "MooringShapeResult",
    "MooringShapePoint",
    "MooringShapeSegment",
    "MooringAlternativeDiscreteNodeResult"
)

foreach ($scanTerm in $informationalTerms) {
    $references = @(Select-String -Path $sourceFiles.FullName -SimpleMatch $scanTerm)
    Write-Host ""
    Write-Host ("Selected-shape calculation type: " + $scanTerm)
    Write-Host ("  Reference count: " + $references.Count)
}

Write-Host ""
Write-Host "Selected-shape consumer scan completed."
