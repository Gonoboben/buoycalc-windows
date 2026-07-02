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

$storeSymbols = @(
    "MooringShapeStore",
    "MooringIterativeSolverStore"
)

foreach ($storeSymbol in $storeSymbols) {
    $references = @(
        Select-String -Path $sourceFiles.FullName -SimpleMatch $storeSymbol
    )

    if ($references.Count -eq 0) {
        throw "No references found for $storeSymbol."
    }

    $escapedSymbol = [regex]::Escape($storeSymbol)
    $declarationPattern = "\b(class|record)\s+" + $escapedSymbol + "\b"
    $setPattern = $escapedSymbol + "\.Set\s*\("
    $readPattern = $escapedSymbol + "\.(Get|TryGet|Current)\b"

    $declarations = @(
        $references | Where-Object { $_.Line -match $declarationPattern }
    )

    $writeReferences = @(
        $references | Where-Object { $_.Line -match $setPattern }
    )

    $readCandidates = @(
        $references | Where-Object {
            $_.Line -notmatch $declarationPattern -and
            $_.Line -notmatch $setPattern
        }
    )

    $explicitReads = @(
        $references | Where-Object { $_.Line -match $readPattern }
    )

    Write-Host ""
    Write-Host ("Report store symbol: " + $storeSymbol)
    Write-Host ("  Total references: " + $references.Count)
    Write-Host ("  Declarations: " + $declarations.Count)
    Write-Host ("  Write references: " + $writeReferences.Count)
    Write-Host ("  Read candidates: " + $readCandidates.Count)
    Write-Host ("  Explicit reads: " + $explicitReads.Count)

    if ($declarations.Count -eq 0) {
        Write-Host "  Declaration: not found by scan pattern"
    }
    else {
        foreach ($item in $declarations) {
            Write-Host ("  Declaration: " + (Get-RelativePath $item.Path) + ":" + $item.LineNumber)
        }
    }

    Write-Host "  References:"
    foreach ($item in $references) {
        Write-Host ("    " + (Get-RelativePath $item.Path) + ":" + $item.LineNumber + ": " + $item.Line.Trim())
    }
}

Write-Host ""
Write-Host "Report store consumer scan completed."
