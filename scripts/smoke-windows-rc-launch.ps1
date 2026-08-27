param(
    [string]$Version = "v1.0.0",
    [string]$Runtime = "win-x64",
    [string]$ReleaseOutput = "artifacts/release",
    [int]$StartupSeconds = 8
)

$ErrorActionPreference = "Stop"

if ($StartupSeconds -lt 1) {
    throw "StartupSeconds must be at least 1."
}

$artifactBase = "BuoyCalc-Windows-$Version-$Runtime"
$zipPath = Join-Path $ReleaseOutput "$artifactBase.zip"
if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf)) {
    throw "RC ZIP not found: $zipPath"
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("buoycalc-rc-smoke-" + [Guid]::NewGuid().ToString("N"))
$extractRoot = Join-Path $tempRoot "extract"
New-Item -ItemType Directory -Force -Path $extractRoot | Out-Null

$process = $null
$startedAt = Get-Date
try {
    Expand-Archive -LiteralPath $zipPath -DestinationPath $extractRoot -Force

    $executables = @(Get-ChildItem -LiteralPath $extractRoot -File -Filter "BuoyCalc.Windows.exe" -Recurse)
    if ($executables.Count -ne 1) {
        throw "Expected exactly one BuoyCalc.Windows.exe in packaged ZIP, found $($executables.Count)."
    }

    $exe = $executables[0]
    Write-Host "Launching packaged RC executable: $($exe.FullName)"

    $process = Start-Process `
        -FilePath $exe.FullName `
        -WorkingDirectory $exe.DirectoryName `
        -PassThru

    Start-Sleep -Seconds $StartupSeconds
    $process.Refresh()

    if ($process.HasExited) {
        $exitCode = $process.ExitCode
        Write-Host "Packaged RC executable exited during startup smoke with code $exitCode."

        Write-Host "Recent Windows application events:"
        Get-WinEvent -FilterHashtable @{ LogName = 'Application'; StartTime = $startedAt.AddSeconds(-5) } -ErrorAction SilentlyContinue |
            Where-Object {
                $_.ProviderName -in @('.NET Runtime', 'Application Error', 'Windows Error Reporting') -or
                $_.Message -like '*BuoyCalc.Windows*'
            } |
            Select-Object -First 12 TimeCreated, ProviderName, Id, Message |
            Format-List |
            Out-String |
            Write-Host

        throw "Packaged RC executable did not survive the $StartupSeconds-second launch smoke. Exit code: $exitCode"
    }

    Write-Host "Packaged RC executable survived the $StartupSeconds-second launch smoke (PID $($process.Id))."
}
finally {
    if ($null -ne $process) {
        try {
            $process.Refresh()
            if (-not $process.HasExited) {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
                $process.WaitForExit(5000) | Out-Null
            }
        }
        catch {
            Write-Warning "Unable to terminate RC smoke process cleanly: $($_.Exception.Message)"
        }
    }

    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
