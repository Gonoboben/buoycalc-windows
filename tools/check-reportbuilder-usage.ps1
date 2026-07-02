$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$files = Get-ChildItem -Path $root -Recurse -File -Include *.cs |
    Where-Object {
        $_.FullName -notmatch '\\bin\\' -and
        $_.FullName -notmatch '\\obj\\'
    }

$matches = foreach ($file in $files) {
    Select-String -Path $file.FullName -Pattern '\bReportBuilder\.Build\s*\(' | ForEach-Object {
        [PSCustomObject]@{
            Path = Resolve-Path -Path $_.Path -Relative
            Line = $_.LineNumber
            Text = $_.Line.Trim()
        }
    }
}

if (-not $matches) {
    Write-Host 'No external ReportBuilder.Build calls found.'
    exit 0
}

$matches | Format-Table -AutoSize

Write-Host ''
Write-Host 'ReportBuilder.Build calls were found. Review these before cleanup.'
exit 1
