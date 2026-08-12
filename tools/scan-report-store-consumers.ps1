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

$retiredStoreSymbols = @(
    "MooringShapeStore",
    "MooringIterativeSolverStore",
    "MooringAlternativeShapeStore"
)

foreach ($storeSymbol in $retiredStoreSymbols) {
    $escaped = [regex]::Escape($storeSymbol)
    $pattern = "(\b(class|record)\s+" + $escaped + "\b)|(\b" + $escaped + "\s*\.)"
    $references = @(
        Select-String -Path $sourceFiles.FullName -Pattern $pattern
    )

    Write-Host ""
    Write-Host ("Retired report store symbol: " + $storeSymbol)
    Write-Host ("  Actual C# references: " + $references.Count)

    if ($references.Count -ne 0) {
        foreach ($item in $references) {
            Write-Host ("    " + (Get-RelativePath $item.Path) + ":" + $item.LineNumber + ": " + $item.Line.Trim())
        }
        throw "$storeSymbol was retired and must have zero actual C# references."
    }

    Write-Host "  References: none"
}

Write-Host ""
Write-Host "Report store consumer scan completed: retired mutable report/shape stores have zero actual C# references."
