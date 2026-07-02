$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$files = Get-ChildItem -Path $root -Recurse -File -Include *.cs |
    Where-Object {
        $_.FullName -notmatch '\\bin\\' -and
        $_.FullName -notmatch '\\obj\\'
    }

$matches = foreach ($file in $files) {
    Select-String -Path $file.FullName -Pattern 'ReportBuilder' | ForEach-Object {
        [PSCustomObject]@{
            Path = Resolve-Path -Path $_.Path -Relative
            Line = $_.LineNumber
            Text = $_.Line.Trim()
        }
    }
}

if (-not $matches) {
    Write-Host 'No ReportBuilder references found.'
    exit 0
}

$matches | Format-Table -AutoSize

$externalMatches = $matches | Where-Object { $_.Path -ne '.\Services\ReportBuilder.cs' }

if ($externalMatches) {
    Write-Host ''
    Write-Host 'External ReportBuilder references were found. Review these before cleanup.'
    exit 1
}

Write-Host ''
Write-Host 'Only Services/ReportBuilder.cs references ReportBuilder.'
