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

if (Test-Path $Output) {
    Remove-Item $Output -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null

dotnet restore BuoyCalc.Windows.csproj

dotnet publish BuoyCalc.Windows.csproj `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $Output `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true

Write-Host "Publish completed: $Output"
