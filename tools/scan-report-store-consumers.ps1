$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Get-RelativePath([string]$path) {
    return [System.IO.Path]::GetRelativePath($repoRoot, $path).Replace("\\", "/")
}

function Assert-AnySourceFiles([object[]]$files) {
    if ($files.Count -eq 0) {
        throw "No C# source files found."
    }
}

$sourceFiles = @(
    Get-ChildItem -Path $repoRoot -Recurse -File -Filter "*.cs" |
        Where-Object {
            $_.FullName -notmatch "[\\/]bin[\\/]" -and
            $_.FullName -notmatch "[\\/]obj[\\/]"
        }
)

Assert-AnySourceFiles $sourceFiles

$storeSymbols = @(
    "MooringShapeStore",
    "MooringIterativeSolverStore"
)

foreach ($storeSymbol in $storeSymbols) {
    $escapedSymbol = [regex]::Escape($storeSymbol)
    $declarationPattern = "\b(class|record)\s+$escapedSymbol\b"
    $setPattern = "\b$escapedSymbol\.Set\s*\("
    $getPattern = "\b$escapedSymbol\.(Get|Current|Value|TryGet)\b"

    $declarations = @(
        Select-String -Path $sourceFiles.FullName -Pattern $declarationPattern
    )

    if ($declarations.Count -ne 1) {
        Write-Host ""
        Write-Host "Declaration candidates for $storeSymbol:"
        foreach ($candidate in $declarations) {
            Write-Host ("  {0}:{1}: {2}" -f (Get-RelativePath $candidate.Path), $candidate.LineNumber, $candidate.Line.Trim())
        }

        throw "Expected exactly one declaration for $storeSymbol, found $($declarations.Count)."
    }

    $allReferences = @(
        Select-String -Path $sourceFiles.FullName -Pattern "\b$escapedSymbol\b"
    )

    $writeReferences = @(
        $allReferences | Where-Object { $_.Line -match $setPattern }
    )

    $readCandidateReferences = @(
        $allReferences | Where-Object {
            $_.Line -notmatch $declarationPattern -and
            $_.Line -notmatch $setPattern
        }
    )

    $explicitReadReferences = @(
        $allReferences | Where-Object { $_.Line -match $getPattern }
    )

    Write-Host ""
    Write-Host "Report store symbol: $storeSymbol"
    Write-Host ("  Declaration: {0}:{1}" -f (Get-RelativePath $declarations[0].Path), $declarations[0].LineNumber)
    Write-Host ("  Total references: {0}" -f $allReferences.Count)
    Write-Host ("  Write references: {0}" -f $writeReferences.Count)
    Write-Host ("  Read candidate references: {0}" -f $readCandidateReferences.Count)
    Write-Host ("  Explicit read references: {0}" -f $explicitReadReferences.Count)

    Write-Host "  References:"
    foreach ($reference in $allReferences) {
        Write-Host ("    {0}:{1}: {2}" -f (Get-RelativePath $reference.Path), $reference.LineNumber, $reference.Line.Trim())
    }
}

Write-Host ""
Write-Host "Report store consumer scan completed."
