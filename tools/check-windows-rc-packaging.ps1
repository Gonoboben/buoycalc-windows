$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Read-RepoText([string]$relativePath) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required file is missing: $relativePath"
    }
    return Get-Content -LiteralPath $path -Raw
}

function Assert-Contains([string]$content, [string]$needle, [string]$label) {
    if (-not $content.Contains($needle)) {
        throw "$label does not contain required text: $needle"
    }
}

function Assert-NotContains([string]$content, [string]$needle, [string]$label) {
    if ($content.Contains($needle)) {
        throw "$label contains forbidden text: $needle"
    }
}

function Assert-PowerShellParses([string]$relativePath) {
    $path = Join-Path $repoRoot $relativePath
    $tokens = $null
    $errors = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile($path, [ref]$tokens, [ref]$errors)
    if ($errors.Count -gt 0) {
        $details = ($errors | ForEach-Object { $_.Message }) -join "; "
        throw "$relativePath has PowerShell parser errors: $details"
    }
}

$publishPath = "scripts/publish-windows.ps1"
$packagePath = "scripts/package-windows-rc.ps1"
$verifyPath = "scripts/verify-windows-rc.ps1"
$workflowPath = ".github/workflows/release-windows.yml"

foreach ($script in @($publishPath, $packagePath, $verifyPath)) {
    Assert-PowerShellParses $script
}

$publish = Read-RepoText $publishPath
Assert-Contains $publish "--runtime `$Runtime" "Windows publish script"
Assert-Contains $publish "--self-contained true" "Windows publish script"
Assert-Contains $publish "/p:PublishSingleFile=true" "Windows publish script"
Assert-Contains $publish 'Expected exactly one published executable' "Windows publish script"
Assert-Contains $publish 'BuoyCalc.Windows.exe' "Windows publish script"

$package = Read-RepoText $packagePath
Assert-Contains $package 'BuoyCalc-Windows-$Version-$Runtime' "Windows RC package script naming"
Assert-Contains $package 'Requested source commit' "Windows RC package script exact source"
Assert-Contains $package '[System.IO.Compression.CompressionLevel]::NoCompression' "Windows RC deterministic ZIP"
Assert-Contains $package '[DateTimeOffset]::new(2000, 1, 1' "Windows RC normalized timestamp"
Assert-Contains $package 'Get-FileHash -LiteralPath $zipPath -Algorithm SHA256' "Windows RC package SHA-256"
Assert-Contains $package 'sourceCommit = $SourceCommit' "Windows RC manifest source provenance"
Assert-Contains $package 'packageSha256 = $packageSha256' "Windows RC manifest package hash"
Assert-Contains $package 'selfContained = $true' "Windows RC manifest self-contained assertion"
Assert-Contains $package 'singleFile = $true' "Windows RC manifest single-file assertion"

$verify = Read-RepoText $verifyPath
Assert-Contains $verify 'Release output must contain exactly' "Windows RC verifier evidence set"
Assert-Contains $verify 'Manifest source commit mismatch' "Windows RC verifier source provenance"
Assert-Contains $verify 'Manifest package SHA-256 mismatch' "Windows RC verifier package hash"
Assert-Contains $verify 'Checksum file mismatch' "Windows RC verifier checksum"
Assert-Contains $verify 'RC ZIP must contain exactly one entry' "Windows RC verifier ZIP contents"
Assert-Contains $verify 'RC ZIP entry timestamp is not normalized' "Windows RC verifier normalized timestamp"
Assert-Contains $verify 'Manifest executable SHA-256 mismatch' "Windows RC verifier executable hash"

$workflow = Read-RepoText $workflowPath
Assert-Contains $workflow "workflow_dispatch:" "Windows release workflow manual trigger"
Assert-Contains $workflow "release-candidate/v1.0.0" "Windows release workflow RC trigger"
Assert-Contains $workflow "contents: read" "Windows release workflow repository permission"
Assert-Contains $workflow "actions: read" "Windows release workflow Actions permission"
Assert-Contains $workflow "statuses: write" "Windows release workflow status permission"
Assert-NotContains $workflow "contents: write" "Windows release workflow repository permission"
Assert-NotContains $workflow "actions: write" "Windows release workflow Actions permission"
Assert-Contains $workflow "fetch-depth: 0" "Windows release workflow full source identity"
Assert-Contains $workflow "git fetch origin main --depth=1" "Windows release workflow main verification"
Assert-Contains $workflow 'if ($head -ne $main)' "Windows release workflow exact-main guard"
Assert-Contains $workflow '/actions/runs?head_sha=$sourceSha&event=push&per_page=100' "Windows release workflow exact-main Actions query"
Assert-Contains $workflow '".NET Build"' "Windows release workflow .NET gate"
Assert-Contains $workflow '"Selected Shape Consumer Scan"' "Windows release workflow selected-shape gate"
Assert-Contains $workflow '"Report Store Consumer Scan"' "Windows release workflow report-store gate"
Assert-Contains $workflow '$_.event -eq "push"' "Windows release workflow push-run filter"
Assert-Contains $workflow '$run.status -ne "completed" -or $run.conclusion -ne "success"' "Windows release workflow success gate"
Assert-Contains $workflow 'Required exact-main CI workflow' "Windows release workflow missing/failing gate"
Assert-Contains $workflow 'context = "BuoyCalc Windows RC"' "Windows release workflow RC status context"
Assert-Contains $workflow 'state = "pending"' "Windows release workflow pending RC status"
Assert-Contains $workflow '$state = if ($jobStatus -eq "success") { "success" } else { "failure" }' "Windows release workflow final RC status"
Assert-Contains $workflow 'if: always()' "Windows release workflow final status guarantee"
Assert-Contains $workflow '/actions/runs/${{ github.run_id }}' "Windows release workflow status target URL"
Assert-Contains $workflow "./scripts/package-windows-rc.ps1" "Windows release workflow package step"
Assert-Contains $workflow "./scripts/verify-windows-rc.ps1" "Windows release workflow verify step"
Assert-Contains $workflow "BuoyCalc-Windows-v1.0.0-win-x64-RC" "Windows release workflow artifact name"
Assert-Contains $workflow "BuoyCalc-Windows-v1.0.0-win-x64.zip" "Windows release workflow ZIP upload"
Assert-Contains $workflow "BuoyCalc-Windows-v1.0.0-win-x64.sha256" "Windows release workflow checksum upload"
Assert-Contains $workflow "BuoyCalc-Windows-v1.0.0-win-x64-manifest.json" "Windows release workflow manifest upload"

$releaseFiles = $publish + "`n" + $package + "`n" + $verify + "`n" + $workflow
foreach ($forbidden in @(
    "git tag ",
    "gh release ",
    "softprops/action-gh-release",
    "actions/create-release",
    "contents: write",
    "actions: write",
    "checks: write",
    "deployments: write",
    "issues: write",
    "packages: write",
    "pull-requests: write",
    "id-token: write",
    "permissions: write-all"
)) {
    Assert-NotContains $releaseFiles $forbidden "F5-E RC-only release boundary"
}

Write-Host "Windows RC packaging smoke check passed."
