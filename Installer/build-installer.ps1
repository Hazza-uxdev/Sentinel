$ErrorActionPreference = "Stop"

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $projectRoot "Sentinel.csproj"
$scriptPath = Join-Path $PSScriptRoot "Sentinel.iss"

dotnet publish $projectPath -c Release -r win-x64 --self-contained true /p:PublishSingleFile=false

$innoCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
)

$iscc = $innoCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    Write-Host "Published Sentinel successfully."
    Write-Host "Install Inno Setup 6 to build SentinelSetup.exe, then run this script again."
    Write-Host "Published app:"
    Write-Host (Join-Path $projectRoot "bin\Release\net8.0-windows\win-x64\publish")
    exit 0
}

& $iscc $scriptPath
Write-Host "Installer output:"
Write-Host (Join-Path $PSScriptRoot "Output\SentinelSetup.exe")
