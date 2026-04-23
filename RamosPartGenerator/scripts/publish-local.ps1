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

$startHiddenVbs = @"
Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
appDir = fso.GetParentFolderName(WScript.ScriptFullName)
shell.CurrentDirectory = appDir
shell.Environment("Process")("ASPNETCORE_URLS") = "http://localhost:$Port"
shell.Run """" & appDir & "\RamosPartGenerator.Api.exe" & """", 0, False
"@

Set-Content -Path (Join-Path $publishRoot "start-hidden.vbs") -Value $startHiddenVbs -Encoding ASCII

$openPageBat = @"
@echo off
start "" "http://localhost:$Port"
"@

Set-Content -Path (Join-Path $publishRoot "open-page.bat") -Value $openPageBat -Encoding ASCII

$stopBat = @"
@echo off
taskkill /IM RamosPartGenerator.Api.exe /F >nul 2>nul
if errorlevel 1 (
  echo Ramos Part Generator is not running.
) else (
  echo Ramos Part Generator stopped.
)
pause
"@

Set-Content -Path (Join-Path $publishRoot "stop.bat") -Value $stopBat -Encoding ASCII

$installStartupBat = @"
@echo off
setlocal
set STARTUP=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
copy /Y "%~dp0start-hidden.vbs" "%STARTUP%\RamosPartGenerator_Rev30_Localhost5371.vbs" >nul
echo Startup registration completed.
echo The app will run in the background after Windows login.
echo To start it now, double-click start-hidden.vbs or run run.bat.
pause
"@

Set-Content -Path (Join-Path $publishRoot "install-startup.bat") -Value $installStartupBat -Encoding ASCII

$uninstallStartupBat = @"
@echo off
setlocal
set STARTUP=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
del "%STARTUP%\RamosPartGenerator_Rev30_Localhost5371.vbs" >nul 2>nul
echo Startup registration removed.
pause
"@

Set-Content -Path (Join-Path $publishRoot "uninstall-startup.bat") -Value $uninstallStartupBat -Encoding ASCII

$readme = @"
Ramos Part Generator Rev30 Local

Manual run:
1. Unzip this folder anywhere on the PC.
2. Double-click run.bat.
3. Browser opens http://localhost:$Port
4. Keep the console window open while using the app.
5. Close the console window to stop the app.

Background run:
1. Double-click start-hidden.vbs.
2. Open http://localhost:$Port in a browser.
3. Double-click stop.bat to stop the background server.

Auto start after Windows login:
1. Double-click install-startup.bat once.
2. Restart Windows or log out/in.
3. Open http://localhost:$Port in a browser.
4. Double-click uninstall-startup.bat to remove auto start.

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
