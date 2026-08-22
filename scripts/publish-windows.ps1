param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$Output = "artifacts/publish/BuoyCalc-Windows-win-x64"
)

$ErrorActionPreference = "Stop"

Write-Host "BuoyCalc Windows publish"
Write-Host "Runtime: $Runtime"
Write-Host "Configuration: $Configuration"
Write-Host "Output: $Output"

if (Test-Path -LiteralPath $Output) {
    Remove-Item -LiteralPath $Output -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null

dotnet restore BuoyCalc.Windows.csproj
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE"
}

dotnet publish BuoyCalc.Windows.csproj `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $Output `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$executables = @(Get-ChildItem -LiteralPath $Output -File -Filter "*.exe")
if ($executables.Count -ne 1) {
    throw "Expected exactly one published executable in '$Output', found $($executables.Count)."
}

$executable = $executables[0]
if ($executable.Name -ne "BuoyCalc.Windows.exe") {
    throw "Unexpected published executable '$($executable.Name)'; expected 'BuoyCalc.Windows.exe'."
}

if ($executable.Length -le 0) {
    throw "Published executable is empty: $($executable.FullName)"
}

Write-Host "Validated single-file executable: $($executable.FullName)"
Write-Host "Publish completed: $Output"
