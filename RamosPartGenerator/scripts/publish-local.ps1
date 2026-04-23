param(
    [string]$OutputRoot = "C:\JSH_Folder\Part_auto",
    [int]$Port = 5371
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")
$frontendDir = Join-Path $repoRoot "frontend"
$apiProject = Join-Path $repoRoot "src\RamosPartGenerator.Api\RamosPartGenerator.Api.csproj"
$apiWwwroot = Join-Path $repoRoot "src\RamosPartGenerator.Api\wwwroot"
$publishRoot = Join-Path $OutputRoot "RamosPartGenerator_Rev30_Localhost5371"
$zipPath = "$publishRoot.zip"

Write-Host "Building frontend..."
Push-Location $frontendDir
npm run build
Pop-Location

if (Test-Path $apiWwwroot) {
    Remove-Item -LiteralPath $apiWwwroot -Recurse -Force
}

New-Item -ItemType Directory -Path $apiWwwroot | Out-Null
Copy-Item -Path (Join-Path $frontendDir "dist\*") -Destination $apiWwwroot -Recurse -Force

if (Test-Path $publishRoot) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force
}

Write-Host "Publishing API..."
dotnet publish $apiProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $publishRoot `
    /p:PublishSingleFile=false `
    /p:DebugType=None `
    /p:DebugSymbols=false

$runBat = @"
@echo off
setlocal
cd /d "%~dp0"
set ASPNETCORE_URLS=http://localhost:$Port
start "" "http://localhost:$Port"
RamosPartGenerator.Api.exe
pause
"@

Set-Content -Path (Join-Path $publishRoot "run.bat") -Value $runBat -Encoding ASCII

$readme = @"
Ramos Part Generator Rev30 Local

How to run:
1. Unzip this folder anywhere on the PC.
2. Double-click run.bat.
3. Browser opens http://localhost:$Port
4. Keep the console window open while using the app.
5. Close the console window to stop the app.

Notes:
- localhost means this PC only.
- No separate frontend command is required.
- If port $Port is already in use, stop the other program first.
"@

Set-Content -Path (Join-Path $publishRoot "README.txt") -Value $readme -Encoding UTF8

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishRoot "*") -DestinationPath $zipPath -Force

Remove-Item -LiteralPath $apiWwwroot -Recurse -Force

Write-Host "Publish folder: $publishRoot"
Write-Host "Zip file: $zipPath"
