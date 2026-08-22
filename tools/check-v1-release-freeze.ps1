$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$expectedBaselineBlob = "97b0221ab29d8df4c9f2f435a1ba1780033d318a"
$baselinePath = "validation/BuoyCalc.EngineeringRegression/baselines/engineering-baseline.json"

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

Push-Location $repoRoot
try {
    $baselineBlob = (git rev-parse "HEAD:$baselinePath").Trim()
    if ($LASTEXITCODE -ne 0 -or $baselineBlob -ne $expectedBaselineBlob) {
        throw "Frozen canonical engineering baseline changed: expected blob $expectedBaselineBlob, got $baselineBlob"
    }
}
finally {
    Pop-Location
}

$appInfo = Read-RepoText "Services/AppInfo.cs"
Assert-Contains $appInfo 'public const string Version = "v1.0.0";' "AppInfo"
Assert-Contains $appInfo 'VersionNote = "Release Candidate' "AppInfo"
Assert-NotContains $appInfo "v0.46.4" "AppInfo"

$project = Read-RepoText "BuoyCalc.Windows.csproj"
Assert-Contains $project "<Version>1.0.0</Version>" "BuoyCalc.Windows.csproj"
Assert-Contains $project "<AssemblyVersion>1.0.0.0</AssemblyVersion>" "BuoyCalc.Windows.csproj"
Assert-Contains $project "<FileVersion>1.0.0.0</FileVersion>" "BuoyCalc.Windows.csproj"
Assert-Contains $project "<InformationalVersion>1.0.0</InformationalVersion>" "BuoyCalc.Windows.csproj"

$readme = Read-RepoText "README.md"
Assert-Contains $readme "v1.0.0" "README"
Assert-Contains $readme "Release Candidate" "README"
Assert-Contains $readme 'git tag `v1.0.0`' "README release gate"
Assert-Contains $readme "ручного smoke test" "README release gate"
Assert-NotContains $readme "Пользовательская версия приложения пока остаётся" "README stale version text"

$release = Read-RepoText "RELEASE.md"
Assert-Contains $release "BuoyCalc Windows v1.0.0 release process" "RELEASE"
Assert-Contains $release "Release Candidate" "RELEASE"
Assert-Contains $release 'Запрещено создавать git tag `v1.0.0`' "RELEASE manual approval gate"
Assert-Contains $release "явного подтверждения пользователя" "RELEASE manual approval gate"

$notes = Read-RepoText "docs/RELEASE_NOTES_V1.0.0.md"
Assert-Contains $notes "Release Candidate" "v1 release notes"
Assert-Contains $notes "F1" "v1 release notes"
Assert-Contains $notes "F2" "v1 release notes"
Assert-Contains $notes "F3" "v1 release notes"
Assert-Contains $notes "F4" "v1 release notes"
Assert-Contains $notes "only then create tag `v1.0.0`" "v1 release notes manual gate"

$control = Read-RepoText "docs/CONTROL_MARK_F5A_V1_ENGINEERING_FREEZE_2026-08-23.md"
Assert-Contains $control "5ecc0ac913ff203ea3a1015cb3e3665c74a7d6f4" "F5-A control mark base"
Assert-Contains $control $expectedBaselineBlob "F5-A control mark baseline"
Assert-Contains $control "production segment length: exactly `0.20 m`" "F5-A frozen segmentation"
Assert-Contains $control "signed feedback iteration budget: exactly `64`" "F5-A frozen signed budget"
Assert-Contains $control "exact deterministic fixed point" "F5-A fixed-point rule"
Assert-Contains $control "no git tag `v1.0.0` yet" "F5-A release gate"

Write-Host "v1 release freeze identity/baseline smoke check passed."
