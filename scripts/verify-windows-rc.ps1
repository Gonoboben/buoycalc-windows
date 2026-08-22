param(
    [string]$Version = "v1.0.0",
    [string]$Runtime = "win-x64",
    [string]$SourceCommit = "",
    [string]$ReleaseOutput = "artifacts/release"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$artifactBase = "BuoyCalc-Windows-$Version-$Runtime"
$zipName = "$artifactBase.zip"
$checksumName = "$artifactBase.sha256"
$manifestName = "$artifactBase-manifest.json"
$expectedEntryName = "$artifactBase/BuoyCalc.Windows.exe"
$expectedTimestamp = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)

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
        throw "Verification source commit '$SourceCommit' does not match checked-out HEAD '$headCommit'."
    }

    $zipPath = Join-Path $ReleaseOutput $zipName
    $checksumPath = Join-Path $ReleaseOutput $checksumName
    $manifestPath = Join-Path $ReleaseOutput $manifestName

    $expectedFiles = @($zipName, $checksumName, $manifestName) | Sort-Object
    $actualFiles = @(Get-ChildItem -LiteralPath $ReleaseOutput -File | Select-Object -ExpandProperty Name | Sort-Object)
    if ($actualFiles.Count -ne $expectedFiles.Count -or (Compare-Object $expectedFiles $actualFiles)) {
        throw "Release output must contain exactly: $($expectedFiles -join ', '); found: $($actualFiles -join ', ')."
    }

    foreach ($path in @($zipPath, $checksumPath, $manifestPath)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Required RC evidence file is missing: $path"
        }
    }

    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.schemaVersion -ne 1) { throw "Unexpected RC manifest schema version: $($manifest.schemaVersion)" }
    if ($manifest.version -ne $Version) { throw "Manifest version mismatch: $($manifest.version)" }
    if ($manifest.runtime -ne $Runtime) { throw "Manifest runtime mismatch: $($manifest.runtime)" }
    if ($manifest.sourceCommit -ne $SourceCommit) { throw "Manifest source commit mismatch: $($manifest.sourceCommit)" }
    if ($manifest.packageFile -ne $zipName) { throw "Manifest package filename mismatch: $($manifest.packageFile)" }
    if ($manifest.executableFile -ne "BuoyCalc.Windows.exe") { throw "Manifest executable filename mismatch: $($manifest.executableFile)" }
    if ($manifest.selfContained -ne $true -or $manifest.singleFile -ne $true) {
        throw "Manifest must assert selfContained=true and singleFile=true."
    }
    if ($manifest.archiveCompression -ne "store") { throw "Manifest archive compression must be 'store'." }
    if ($manifest.archiveEntryTimestampUtc -ne "2000-01-01T00:00:00Z") {
        throw "Manifest archive timestamp normalization changed."
    }

    $packageSha256 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($manifest.packageSha256 -ne $packageSha256) {
        throw "Manifest package SHA-256 mismatch: expected $packageSha256, got $($manifest.packageSha256)."
    }

    $expectedChecksumText = "$packageSha256  $zipName"
    $checksumText = (Get-Content -LiteralPath $checksumPath -Raw).TrimEnd("`r", "`n")
    if ($checksumText -ne $expectedChecksumText) {
        throw "Checksum file mismatch: expected '$expectedChecksumText', got '$checksumText'."
    }

    Add-Type -AssemblyName System.IO.Compression
    $archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $zipPath).Path)
    try {
        if ($archive.Entries.Count -ne 1) {
            throw "RC ZIP must contain exactly one entry; found $($archive.Entries.Count)."
        }

        $entry = $archive.Entries[0]
        if ($entry.FullName -ne $expectedEntryName) {
            throw "RC ZIP entry mismatch: expected '$expectedEntryName', got '$($entry.FullName)'."
        }
        if ($entry.Length -le 0) {
            throw "RC executable entry is empty."
        }
        if ($entry.CompressedLength -ne $entry.Length) {
            throw "RC ZIP must use store/no-compression mode for deterministic packaging."
        }
        if ($entry.LastWriteTime.UtcDateTime -ne $expectedTimestamp.UtcDateTime) {
            throw "RC ZIP entry timestamp is not normalized to $($expectedTimestamp.ToString('O'))."
        }

        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            $entryStream = $entry.Open()
            try {
                $entryHashBytes = $sha.ComputeHash($entryStream)
            }
            finally {
                $entryStream.Dispose()
            }
        }
        finally {
            $sha.Dispose()
        }
        $entrySha256 = [Convert]::ToHexString($entryHashBytes).ToLowerInvariant()
        if ($manifest.executableSha256 -ne $entrySha256) {
            throw "Manifest executable SHA-256 mismatch: expected $entrySha256, got $($manifest.executableSha256)."
        }
    }
    finally {
        $archive.Dispose()
    }

    Write-Host "Windows RC package verification passed."
    Write-Host "Source commit: $SourceCommit"
    Write-Host "Package: $zipName"
    Write-Host "SHA-256: $packageSha256"
}
finally {
    Pop-Location
}
