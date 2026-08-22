param(
    [string]$Version = "v1.0.0",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$SourceCommit = "",
    [string]$PublishOutput = "artifacts/publish/BuoyCalc-Windows-v1.0.0-win-x64",
    [string]$ReleaseOutput = "artifacts/release"
)

$ErrorActionPreference = "Stop"

if ($Version -notmatch '^v\d+\.\d+\.\d+$') {
    throw "Version must use vMAJOR.MINOR.PATCH format; got '$Version'."
}
if ($Runtime -ne "win-x64") {
    throw "F5-B RC packaging currently supports only win-x64; got '$Runtime'."
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$artifactBase = "BuoyCalc-Windows-$Version-$Runtime"
$zipName = "$artifactBase.zip"
$checksumName = "$artifactBase.sha256"
$manifestName = "$artifactBase-manifest.json"
$fixedEntryTimestamp = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)

Push-Location $repoRoot
try {
    $headCommit = (git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or $headCommit -notmatch '^[0-9a-f]{40}$') {
        throw "Unable to resolve exact source commit from git HEAD."
    }

    if ([string]::IsNullOrWhiteSpace($SourceCommit)) {
        $SourceCommit = $headCommit
    }
    if ($SourceCommit -ne $headCommit) {
        throw "Requested source commit '$SourceCommit' does not match checked-out HEAD '$headCommit'."
    }

    & ./scripts/publish-windows.ps1 `
        -Runtime $Runtime `
        -Configuration $Configuration `
        -Output $PublishOutput
    if ($LASTEXITCODE -ne 0) {
        throw "Windows publish script failed with exit code $LASTEXITCODE."
    }

    $publishedExecutables = @(Get-ChildItem -LiteralPath $PublishOutput -File -Filter "*.exe")
    if ($publishedExecutables.Count -ne 1) {
        throw "Expected exactly one executable in '$PublishOutput', found $($publishedExecutables.Count)."
    }

    $publishedExe = $publishedExecutables[0]
    if ($publishedExe.Name -ne "BuoyCalc.Windows.exe") {
        throw "Unexpected executable '$($publishedExe.Name)'; expected 'BuoyCalc.Windows.exe'."
    }

    if (Test-Path -LiteralPath $ReleaseOutput) {
        Remove-Item -LiteralPath $ReleaseOutput -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $ReleaseOutput | Out-Null

    $stageContainer = Join-Path $ReleaseOutput ".stage"
    $stageRoot = Join-Path $stageContainer $artifactBase
    New-Item -ItemType Directory -Force -Path $stageRoot | Out-Null
    $stagedExe = Join-Path $stageRoot "BuoyCalc.Windows.exe"
    Copy-Item -LiteralPath $publishedExe.FullName -Destination $stagedExe

    $executableSha256 = (Get-FileHash -LiteralPath $stagedExe -Algorithm SHA256).Hash.ToLowerInvariant()
    $zipPath = Join-Path $ReleaseOutput $zipName

    Add-Type -AssemblyName System.IO.Compression
    $zipStream = [System.IO.File]::Open(
        $zipPath,
        [System.IO.FileMode]::CreateNew,
        [System.IO.FileAccess]::Write,
        [System.IO.FileShare]::None)
    try {
        $archive = [System.IO.Compression.ZipArchive]::new(
            $zipStream,
            [System.IO.Compression.ZipArchiveMode]::Create,
            $false)
        try {
            $files = @(Get-ChildItem -LiteralPath $stageContainer -File -Recurse | Sort-Object FullName)
            foreach ($file in $files) {
                $entryName = [System.IO.Path]::GetRelativePath($stageContainer, $file.FullName).Replace('\', '/')
                $entry = $archive.CreateEntry($entryName, [System.IO.Compression.CompressionLevel]::NoCompression)
                $entry.LastWriteTime = $fixedEntryTimestamp
                $entry.ExternalAttributes = 0

                $inputStream = [System.IO.File]::OpenRead($file.FullName)
                try {
                    $entryStream = $entry.Open()
                    try {
                        $inputStream.CopyTo($entryStream)
                    }
                    finally {
                        $entryStream.Dispose()
                    }
                }
                finally {
                    $inputStream.Dispose()
                }
            }
        }
        finally {
            $archive.Dispose()
        }
    }
    finally {
        $zipStream.Dispose()
    }

    Remove-Item -LiteralPath $stageContainer -Recurse -Force

    $packageSha256 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $checksumPath = Join-Path $ReleaseOutput $checksumName
    $checksumText = "$packageSha256  $zipName`n"
    [System.IO.File]::WriteAllText(
        $checksumPath,
        $checksumText,
        [System.Text.UTF8Encoding]::new($false))

    $manifest = [ordered]@{
        schemaVersion = 1
        version = $Version
        runtime = $Runtime
        sourceCommit = $SourceCommit
        packageFile = $zipName
        packageSha256 = $packageSha256
        executableFile = "BuoyCalc.Windows.exe"
        executableSha256 = $executableSha256
        selfContained = $true
        singleFile = $true
        archiveCompression = "store"
        archiveEntryTimestampUtc = "2000-01-01T00:00:00Z"
    }
    $manifestPath = Join-Path $ReleaseOutput $manifestName
    $manifestJson = $manifest | ConvertTo-Json -Depth 4
    [System.IO.File]::WriteAllText(
        $manifestPath,
        $manifestJson + "`n",
        [System.Text.UTF8Encoding]::new($false))

    Write-Host "Windows RC package created"
    Write-Host "Source commit: $SourceCommit"
    Write-Host "Package: $zipPath"
    Write-Host "SHA-256: $packageSha256"
    Write-Host "Manifest: $manifestPath"
    Write-Host "Checksum: $checksumPath"
}
finally {
    Pop-Location
}
