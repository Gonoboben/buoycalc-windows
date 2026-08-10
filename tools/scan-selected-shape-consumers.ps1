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
    "MooringShapeStore"
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

# Issue #358 audit-only classification. This section intentionally does not
# make a retirement decision; it records the exact topology for the CI artifact.
$alternativeStore = "MooringAlternativeShapeStore"
$alternativeReferences = @(
    Select-String -Path $sourceFiles.FullName -SimpleMatch $alternativeStore
)
$escapedAlternativeStore = [regex]::Escape($alternativeStore)
$alternativeDeclarationPattern = "\b(class|record)\s+" + $escapedAlternativeStore + "\b"
$alternativeSetPattern = "\b" + $escapedAlternativeStore + "\s*\.\s*Set\s*\("
$alternativeClearPattern = "\b" + $escapedAlternativeStore + "\s*\.\s*Clear\s*\("
$alternativeCurrentPattern = "\b" + $escapedAlternativeStore + "\s*\.\s*Current\b"

$alternativeDeclarations = @($alternativeReferences | Where-Object { $_.Line -match $alternativeDeclarationPattern })
$alternativeSetWrites = @($alternativeReferences | Where-Object { $_.Line -match $alternativeSetPattern })
$alternativeClearWrites = @($alternativeReferences | Where-Object { $_.Line -match $alternativeClearPattern })
$alternativeCurrentReads = @($alternativeReferences | Where-Object { $_.Line -match $alternativeCurrentPattern })

Write-Host ""
Write-Host "Alternative shape store audit: MooringAlternativeShapeStore"
Write-Host ("  Total textual C# references: " + $alternativeReferences.Count)
Write-Host ("  Declarations: " + $alternativeDeclarations.Count)
Write-Host ("  Set writes: " + $alternativeSetWrites.Count)
Write-Host ("  Clear writes: " + $alternativeClearWrites.Count)
Write-Host ("  Current reads: " + $alternativeCurrentReads.Count)
Write-Host "  References:"
foreach ($item in $alternativeReferences) {
    Write-Host ("    " + (Get-RelativePath $item.Path) + ":" + $item.LineNumber + ": " + $item.Line.Trim())
}

if ($alternativeDeclarations.Count -ne 1) {
    throw "Issue #358 audit expected exactly one MooringAlternativeShapeStore declaration."
}

$informationalTerms = @(
    "MooringShapeResult",
    "MooringShapePoint",
    "MooringShapeSegment"
)

foreach ($scanTerm in $informationalTerms) {
    $references = @(Select-String -Path $sourceFiles.FullName -SimpleMatch $scanTerm)
    Write-Host ""
    Write-Host ("Selected-shape calculation type: " + $scanTerm)
    Write-Host ("  Reference count: " + $references.Count)
}

Write-Host ""
Write-Host "Selected-shape consumer scan completed."
